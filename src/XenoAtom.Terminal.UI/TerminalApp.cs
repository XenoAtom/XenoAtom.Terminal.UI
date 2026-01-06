// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

public sealed class TerminalApp : IAsyncDisposable
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _pendingActions = new();
    private readonly TerminalInstance _terminal;
    private readonly TerminalAppOptions _options;
    private readonly InlineInteractiveHost? _inlineHost;
    private readonly FullscreenHost? _fullscreenHost;
    private readonly AsyncAutoResetEvent _wakeUp = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _renderRequested = true;
    private Visual? _pointerCapture;
    private Visual? _hoveredElement;
    private int? _inlineLiveRegionTopRow;
    private bool _lastCursorVisible;
    private TerminalPosition _lastCursorPosition;
    private bool _debugOverlayVisible;
    private int _renderFrameIndex;
    private Task? _runTask;

    public TerminalApp(Visual root, TerminalInstance? terminal = null, TerminalAppOptions? options = null)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _options = options ?? new TerminalAppOptions();
        Dispatcher = new Dispatcher(this);

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            _fullscreenHost = new FullscreenHost(_terminal);
        }
        else
        {
            _inlineHost = new InlineInteractiveHost(_terminal);
        }
    }

    public TerminalInstance Terminal => _terminal;

    public Visual Root { get; }

    public Dispatcher Dispatcher { get; }

    public Visual? FocusedElement { get; private set; }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _pendingActions.Enqueue(action);
        _wakeUp.Set();
    }

    public void Stop() => _cts.Cancel();

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_runTask is not null)
        {
            try
            {
                await _runTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore.
            }
        }
        _inlineHost?.Dispose();
        _fullscreenHost?.Dispose();
        _cts.Dispose();
        await ValueTask.CompletedTask;
    }

    public void WriteMarkupLine(string markup)
    {
        Dispatcher.VerifyAccess();
        if (_inlineHost is null)
        {
            throw new InvalidOperationException("Flow output is only supported in inline host mode.");
        }

        _inlineHost.WriteMarkupLine(markup);
        RequestRender();
    }

    public void Append(Visual block)
    {
        ArgumentNullException.ThrowIfNull(block);
        Dispatcher.VerifyAccess();

        if (_inlineHost is null)
        {
            throw new InvalidOperationException("Flow output is only supported in inline host mode.");
        }

        if (block.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be appended as flow output.");
        }

        var width = Math.Max(1, _terminal.Size.Columns);

        block.AttachToApp(this);
        try
        {
            block.Measure(new Size(width, int.MaxValue / 4));
            block.Arrange(new Rectangle(0, 0, width, block.DesiredSize.Height));

            var buffer = new CellBuffer(width, Math.Max(1, block.DesiredSize.Height));
            buffer.Clear(block.GetTheme().BaseTextStyle());
            block.RenderTree(buffer);

            _inlineHost.WriteMarkupLines(buffer.ToMarkupLines());
        }
        finally
        {
            block.DetachFromApp();
        }

        RequestRender();
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The app is already running.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runTask = tcs.Task;

        var thread = new Thread(() =>
        {
            try
            {
                RunCore(cancellationToken);
                tcs.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "XenoAtom.Terminal.UI",
        };

        thread.Start();
        return _runTask;
    }

    private void RunCore(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        Dispatcher.BindToCurrentThread();

        Root.AttachToApp(this);
        BindingManager.Current.ValueChanged += OnValueChanged;

        TerminalScope alternateScope = default;
        TerminalScope rawScope = default;
        TerminalScope echoScope = default;
        TerminalScope mouseScope = default;
        TerminalScope pasteScope = default;
        TerminalScope cursorScope = default;

        try
        {
            if (!_terminal.IsInputRunning)
            {
                _terminal.StartInput(new TerminalInputOptions { TreatControlCAsInput = true });
            }

            if (_options.HostKind == TerminalHostKind.Fullscreen)
            {
                alternateScope = _terminal.UseAlternateScreen();
            }

            rawScope = _terminal.UseRawMode(_options.RawMode);
            if (_options.DisableInputEcho)
            {
                echoScope = _terminal.SetInputEcho(false);
            }

            cursorScope = _terminal.HideCursor();

            if (_options.EnableMouse)
            {
                mouseScope = _terminal.EnableMouseInput(_options.MouseMode);
            }

            if (_options.EnableBracketedPaste)
            {
                pasteScope = _terminal.EnableBracketedPasteInput();
            }

            EnsureInitialFocus();
            RequestRender();

            while (!token.IsCancellationRequested)
            {
                while (_pendingActions.TryDequeue(out var action))
                {
                    action();
                }

                if (_renderRequested)
                {
                    _renderRequested = false;
                    Render();
                }

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var readEventTask = _terminal.ReadEventAsync(waitCts.Token).AsTask();
                var wakeTask = _wakeUp.WaitAsync(token);

                var completed = Task.WhenAny(readEventTask, wakeTask).GetAwaiter().GetResult();
                if (completed == wakeTask)
                {
                    waitCts.Cancel();

                    try
                    {
                        var maybeEvent = readEventTask.GetAwaiter().GetResult();
                        HandleTerminalEvent(maybeEvent);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore.
                    }
                    continue;
                }

                var ev = readEventTask.GetAwaiter().GetResult();
                HandleTerminalEvent(ev);
            }
        }
        finally
        {
            pasteScope.Dispose();
            mouseScope.Dispose();
            cursorScope.Dispose();
            echoScope.Dispose();
            rawScope.Dispose();
            alternateScope.Dispose();
            BindingManager.Current.ValueChanged -= OnValueChanged;
            Root.DetachFromApp();
        }
    }

    internal void RequestRender()
    {
        _renderRequested = true;
        _wakeUp.Set();
    }

    private void OnValueChanged(object owner, string name)
    {
        _ = name;
        if (ReferenceEquals(owner, Root) || owner is Visual)
        {
            RequestRender();
        }
    }

    private void Render()
    {
        _inlineLiveRegionTopRow = null;
        _renderFrameIndex++;

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var height = Math.Max(1, _terminal.Size.Rows);

            Root.Measure(new Size(width, height));
            Root.Arrange(new Rectangle(0, 0, width, height));

            var buffer = new CellBuffer(width, height);
            buffer.Clear(Root.GetTheme().BaseTextStyle());
            Root.RenderTree(buffer);
            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            _fullscreenHost!.Render(buffer);
            UpdateCursor();
            return;
        }

        {
            var width = Math.Max(1, _terminal.Size.Columns);

            Root.Measure(new Size(width, int.MaxValue / 4));
            Root.Arrange(new Rectangle(0, 0, width, Root.DesiredSize.Height));

            var buffer = new CellBuffer(width, Math.Max(1, Root.DesiredSize.Height));
            buffer.Clear(Root.GetTheme().BaseTextStyle());
            Root.RenderTree(buffer);
            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            _inlineHost!.Render(buffer.ToMarkupLines());

            if (_terminal.Capabilities.SupportsCursorPositionGet && _terminal.TryGetCursorPosition(out var position))
            {
                var reserved = _inlineHost.ReservedHeight;
                if (reserved > 0)
                {
                    _inlineLiveRegionTopRow = position.Row - reserved;
                }
            }

            UpdateCursor();
        }
    }

    private void RenderDebugOverlay(CellBuffer buffer)
    {
        var maxWidth = buffer.Width;
        var maxHeight = buffer.Height;
        if (maxWidth <= 0 || maxHeight <= 0)
        {
            return;
        }

        var theme = Root.GetTheme();

        var focus = FocusedElement;
        var hover = _hoveredElement;

        var lines = new[]
        {
            $"Frame: {_renderFrameIndex}",
            $"Focus: {(focus is null ? "<none>" : focus.GetType().Name)}",
            $"Hover: {(hover is null ? "<none>" : hover.GetType().Name)}",
        };

        var contentWidth = 0;
        foreach (var line in lines)
        {
            contentWidth = Math.Max(contentWidth, TerminalTextUtility.GetWidth(line.AsSpan()));
        }

        var width = Math.Min(maxWidth, Math.Max(3, contentWidth + 2));
        var height = Math.Min(maxHeight, Math.Max(3, lines.Length + 2));
        if (width < 3 || height < 3)
        {
            return;
        }

        var borderStyle = theme.BorderStyle(focused: true) | TextStyle.Bold;
        var backgroundStyle = CellStyle.None | TextStyle.Dim;
        if (theme.Background is { } bg)
        {
            backgroundStyle = backgroundStyle.WithBackground(bg);
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
            }
        }

        var right = width - 1;
        var bottom = height - 1;

        buffer.SetCell(0, 0, new Rune('+'), borderStyle);
        buffer.SetCell(right, 0, new Rune('+'), borderStyle);
        buffer.SetCell(0, bottom, new Rune('+'), borderStyle);
        buffer.SetCell(right, bottom, new Rune('+'), borderStyle);

        for (var x = 1; x < right; x++)
        {
            buffer.SetCell(x, 0, new Rune('-'), borderStyle);
            buffer.SetCell(x, bottom, new Rune('-'), borderStyle);
        }

        for (var y = 1; y < bottom; y++)
        {
            buffer.SetCell(0, y, new Rune('|'), borderStyle);
            buffer.SetCell(right, y, new Rune('|'), borderStyle);
        }

        for (var i = 0; i < lines.Length && i + 1 < bottom; i++)
        {
            buffer.WriteText(1, 1 + i, lines[i].AsSpan(), CellStyle.None);
        }
    }

    private void UpdateCursor()
    {
        var focused = FocusedElement;
        var x = 0;
        var y = 0;
        var wantsCursor = focused is ICursorProvider provider && provider.TryGetCursorCell(out x, out y);

        TerminalPosition position = default;
        if (wantsCursor)
        {
            if (_options.HostKind == TerminalHostKind.Inline)
            {
                var topRow = _inlineLiveRegionTopRow;
                if (topRow is null)
                {
                    wantsCursor = false;
                }
                else
                {
                    position = new TerminalPosition(x, topRow.Value + y);
                }
            }
            else
            {
                position = new TerminalPosition(x, y);
            }
        }

        try
        {
            if (wantsCursor)
            {
                if (!_lastCursorVisible)
                {
                    _terminal.SetCursorVisible(true);
                    _lastCursorVisible = true;
                }

                if (!_lastCursorPosition.Equals(position))
                {
                    _terminal.SetCursorPosition(position);
                    _lastCursorPosition = position;
                }
            }
            else
            {
                if (_lastCursorVisible)
                {
                    _terminal.SetCursorVisible(false);
                    _lastCursorVisible = false;
                }
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private void DispatchKeyEvent(TerminalKeyEvent keyEvent)
    {
        EnsureFocusInScope();
        if (FocusedElement is null || !FocusedElement.IsEnabled || !FocusedElement.IsVisible)
        {
            return;
        }

        var args = new KeyEventArgs { RawEvent = keyEvent };

        for (var v = FocusedElement; v is not null; v = v.Parent)
        {
            if (v.TryHandleKeyBinding(args))
            {
                return;
            }
        }

        FocusedElement.RaiseEvent(Visual.KeyDownEvent, args);
    }

    private void EnsureInitialFocus()
    {
        EnsureFocusInScope();
    }

    private void FocusNext()
    {
        var scope = GetFocusScopeRoot();
        var focusables = scope.EnumerateVisualsDepthFirst().Where(v => v.Focusable && v.IsVisible && v.IsEnabled).ToList();
        if (focusables.Count == 0)
        {
            return;
        }

        if (FocusedElement is null || !focusables.Contains(FocusedElement))
        {
            FocusedElement = focusables[0];
            RequestRender();
            return;
        }

        var index = focusables.IndexOf(FocusedElement);
        FocusedElement = focusables[(index + 1) % focusables.Count];
        RequestRender();
    }

    private void FocusPrevious()
    {
        var scope = GetFocusScopeRoot();
        var focusables = scope.EnumerateVisualsDepthFirst().Where(v => v.Focusable && v.IsVisible && v.IsEnabled).ToList();
        if (focusables.Count == 0)
        {
            return;
        }

        if (FocusedElement is null || !focusables.Contains(FocusedElement))
        {
            FocusedElement = focusables[^1];
            RequestRender();
            return;
        }

        var index = focusables.IndexOf(FocusedElement);
        FocusedElement = focusables[(index - 1 + focusables.Count) % focusables.Count];
        RequestRender();
    }

    private void HandleTerminalEvent(TerminalEvent ev)
    {
        if (ev is TerminalResizeEvent)
        {
            _fullscreenHost?.Reset();
            RequestRender();
            return;
        }

        if (ev is TerminalMouseEvent mouseEvent)
        {
            DispatchMouseEvent(mouseEvent);
            return;
        }

        if (ev is TerminalTextEvent textEvent)
        {
            DispatchTextInput(textEvent.Text);
            return;
        }

        if (ev is TerminalPasteEvent pasteEvent)
        {
            DispatchPaste(pasteEvent.Text);
            return;
        }

        if (ev is not TerminalKeyEvent keyEvent)
        {
            return;
        }

        if (_options.ToggleDebugOverlayGesture.Matches(keyEvent))
        {
            _debugOverlayVisible = !_debugOverlayVisible;
            RequestRender();
            return;
        }

        if (keyEvent.Key == TerminalKey.Escape)
        {
            _cts.Cancel();
            return;
        }

        if (keyEvent.Key == TerminalKey.Tab)
        {
            if ((keyEvent.Modifiers & TerminalModifiers.Shift) != 0)
            {
                FocusPrevious();
            }
            else
            {
                FocusNext();
            }
            return;
        }

        DispatchKeyEvent(keyEvent);
    }

    private void DispatchTextInput(string text)
    {
        EnsureFocusInScope();
        if (FocusedElement is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var args = new TextInputEventArgs { Text = text };
        FocusedElement.RaiseEvent(Visual.TextInputEvent, args);
    }

    private void DispatchPaste(string text)
    {
        EnsureFocusInScope();
        if (FocusedElement is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var args = new PasteEventArgs { Text = text };
        FocusedElement.RaiseEvent(Visual.PasteEvent, args);
    }

    private void DispatchMouseEvent(TerminalMouseEvent mouseEvent)
    {
        var inputRoot = GetInputRoot();

        if (_pointerCapture is not null && !IsInScope(_pointerCapture, inputRoot))
        {
            _pointerCapture = null;
        }

        if (_hoveredElement is not null && !IsInScope(_hoveredElement, inputRoot))
        {
            _hoveredElement.IsHovered = false;
            _hoveredElement = null;
        }

        Visual? hitTarget;
        Visual? target;
        var uiY = mouseEvent.Y;
        var localY = mouseEvent.Y;

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            hitTarget = inputRoot.HitTest(mouseEvent.X, mouseEvent.Y);
            target = _pointerCapture ?? hitTarget;
        }
        else
        {
            var topRow = _inlineLiveRegionTopRow;
            var height = _inlineHost?.ReservedHeight ?? 0;
            if (topRow is null || height <= 0)
            {
                UpdateHover(null);
                return;
            }

            var translatedY = mouseEvent.Y - topRow.Value;
            if ((uint)translatedY >= (uint)height)
            {
                UpdateHover(null);
                return;
            }

            uiY = translatedY;
            localY = translatedY;
            hitTarget = inputRoot.HitTest(mouseEvent.X, translatedY);
            target = _pointerCapture ?? hitTarget;
        }

        if (target is null)
        {
            UpdateHover(null);
            return;
        }

        UpdateHover(hitTarget);

        while (target is not null && (!target.IsVisible || !target.IsEnabled))
        {
            target = target.Parent;
        }

        if (target is null)
        {
            return;
        }

        if (mouseEvent.Kind is TerminalMouseKind.Down or TerminalMouseKind.DoubleClick)
        {
            var focusTarget = target;
            while (focusTarget is not null && !focusTarget.Focusable)
            {
                focusTarget = focusTarget.Parent;
            }

            if (focusTarget is not null && !ReferenceEquals(FocusedElement, focusTarget))
            {
                FocusedElement = focusTarget;
                RequestRender();
            }
        }

        var args = new PointerEventArgs
        {
            RawEvent = mouseEvent,
            UiX = mouseEvent.X,
            UiY = uiY,
            ClickCount = mouseEvent.Kind == TerminalMouseKind.DoubleClick ? 2 : 1,
            LocalX = mouseEvent.X - target.Bounds.X,
            LocalY = localY - target.Bounds.Y,
        };

        switch (mouseEvent.Kind)
        {
            case TerminalMouseKind.Move:
            case TerminalMouseKind.Drag:
                target.RaiseEvent(Visual.PointerMovedEvent, args);
                break;
            case TerminalMouseKind.Down:
            case TerminalMouseKind.DoubleClick:
                _pointerCapture = target;
                target.RaiseEvent(Visual.PointerPressedEvent, args);
                break;
            case TerminalMouseKind.Up:
                target.RaiseEvent(Visual.PointerReleasedEvent, args);
                _pointerCapture = null;
                break;
            case TerminalMouseKind.Wheel:
                target.RaiseEvent(Visual.PointerWheelEvent, args);
                break;
        }
    }

    private void UpdateHover(Visual? hitTarget)
    {
        var hovered = hitTarget;
        while (hovered is not null && (!hovered.IsVisible || !hovered.IsEnabled))
        {
            hovered = hovered.Parent;
        }

        if (ReferenceEquals(_hoveredElement, hovered))
        {
            return;
        }

        if (_hoveredElement is not null)
        {
            _hoveredElement.IsHovered = false;
        }

        _hoveredElement = hovered;
        if (_hoveredElement is not null)
        {
            _hoveredElement.IsHovered = true;
        }
    }

    private Visual GetInputRoot() => FindActiveModalRoot(Root) ?? Root;

    private Visual GetFocusScopeRoot() => FindActiveModalRoot(Root) ?? Root;

    private void EnsureFocusInScope()
    {
        var scopeRoot = GetFocusScopeRoot();

        if (FocusedElement is not null)
        {
            if (!FocusedElement.IsEnabled || !FocusedElement.IsVisible || !IsInScope(FocusedElement, scopeRoot))
            {
                FocusedElement = null;
            }
        }

        if (FocusedElement is null)
        {
            FocusedElement = scopeRoot.EnumerateVisualsDepthFirst().FirstOrDefault(v => v.Focusable && v.IsVisible && v.IsEnabled);
            if (FocusedElement is not null)
            {
                RequestRender();
            }
        }
    }

    private static bool IsInScope(Visual visual, Visual scopeRoot)
    {
        for (var v = visual; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, scopeRoot))
            {
                return true;
            }
        }

        return false;
    }

    private static Visual? FindActiveModalRoot(Visual root)
    {
        if (!root.IsVisible || !root.IsEnabled)
        {
            return null;
        }

        for (var i = root.GetChildrenCount() - 1; i >= 0; i--)
        {
            var found = FindActiveModalRoot(root.GetChildUnsafe(i));
            if (found is not null)
            {
                return found;
            }
        }

        return root is IModalVisual { IsModal: true } ? root : null;
    }
}

internal sealed class AsyncAutoResetEvent
{
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private bool _signaled;

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        lock (_waiters)
        {
            if (_signaled)
            {
                _signaled = false;
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(tcs);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(static state =>
                {
                    var source = (TaskCompletionSource<bool>)state!;
                    source.TrySetCanceled();
                }, tcs);
            }

            return tcs.Task;
        }
    }

    public void Set()
    {
        TaskCompletionSource<bool>? toRelease = null;

        lock (_waiters)
        {
            if (_waiters.Count > 0)
            {
                toRelease = _waiters.Dequeue();
            }
            else if (!_signaled)
            {
                _signaled = true;
            }
        }

        toRelease?.TrySetResult(true);
    }
}
