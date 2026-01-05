// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

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

    public TerminalApp(Visual root, TerminalInstance? terminal = null, TerminalAppOptions? options = null)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _options = options ?? new TerminalAppOptions();

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
        _inlineHost?.Dispose();
        _fullscreenHost?.Dispose();
        _cts.Dispose();
        await ValueTask.CompletedTask;
    }

    public void WriteMarkupLine(string markup)
    {
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
            block.Measure(new CellSize(width, int.MaxValue / 4));
            block.Arrange(new CellRect(0, 0, width, block.DesiredSize.Height));

            var buffer = new CellBuffer(width, Math.Max(1, block.DesiredSize.Height));
            block.RenderTree(buffer);

            _inlineHost.WriteMarkupLines(buffer.ToMarkupLines());
        }
        finally
        {
            block.DetachFromApp();
        }

        RequestRender();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

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

                var completed = await Task.WhenAny(readEventTask, wakeTask).ConfigureAwait(false);
                if (completed == wakeTask)
                {
                    waitCts.Cancel();

                    try
                    {
                        var maybeEvent = await readEventTask.ConfigureAwait(false);
                        HandleTerminalEvent(maybeEvent);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore.
                    }
                    continue;
                }

                var ev = await readEventTask.ConfigureAwait(false);
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

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var height = Math.Max(1, _terminal.Size.Rows);

            Root.Measure(new CellSize(width, height));
            Root.Arrange(new CellRect(0, 0, width, height));

            var buffer = new CellBuffer(width, height);
            Root.RenderTree(buffer);

            _fullscreenHost!.Render(buffer);
            return;
        }

        {
            var width = Math.Max(1, _terminal.Size.Columns);

            Root.Measure(new CellSize(width, int.MaxValue / 4));
            Root.Arrange(new CellRect(0, 0, width, Root.DesiredSize.Height));

            var buffer = new CellBuffer(width, Math.Max(1, Root.DesiredSize.Height));
            Root.RenderTree(buffer);

            _inlineHost!.Render(buffer.ToMarkupLines());

            if (_terminal.Capabilities.SupportsCursorPositionGet && _terminal.TryGetCursorPosition(out var position))
            {
                var reserved = _inlineHost.ReservedHeight;
                if (reserved > 0)
                {
                    _inlineLiveRegionTopRow = position.Row - reserved;
                }
            }
        }
    }

    private void DispatchKeyEvent(TerminalKeyEvent keyEvent)
    {
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
        if (FocusedElement is not null)
        {
            return;
        }

        FocusedElement = Root.EnumerateVisualsDepthFirst().FirstOrDefault(v => v.Focusable && v.IsVisible && v.IsEnabled);
    }

    private void FocusNext()
    {
        var focusables = Root.EnumerateVisualsDepthFirst().Where(v => v.Focusable && v.IsVisible && v.IsEnabled).ToList();
        if (focusables.Count == 0)
        {
            return;
        }

        if (FocusedElement is null)
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
        var focusables = Root.EnumerateVisualsDepthFirst().Where(v => v.Focusable && v.IsVisible && v.IsEnabled).ToList();
        if (focusables.Count == 0)
        {
            return;
        }

        if (FocusedElement is null)
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
        if (FocusedElement is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var args = new TextInputEventArgs { Text = text };
        FocusedElement.RaiseEvent(Visual.TextInputEvent, args);
    }

    private void DispatchPaste(string text)
    {
        if (FocusedElement is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var args = new PasteEventArgs { Text = text };
        FocusedElement.RaiseEvent(Visual.PasteEvent, args);
    }

    private void DispatchMouseEvent(TerminalMouseEvent mouseEvent)
    {
        Visual? hitTarget;
        Visual? target;
        var localY = mouseEvent.Y;

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            hitTarget = Root.HitTest(mouseEvent.X, mouseEvent.Y);
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

            localY = translatedY;
            hitTarget = Root.HitTest(mouseEvent.X, translatedY);
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
            if (target.Focusable && !ReferenceEquals(FocusedElement, target))
            {
                FocusedElement = target;
                RequestRender();
            }
        }

        var args = new PointerEventArgs
        {
            RawEvent = mouseEvent,
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
