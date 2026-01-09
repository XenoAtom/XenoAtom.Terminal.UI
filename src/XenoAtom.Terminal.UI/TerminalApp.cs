// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using System.Diagnostics;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI;

public sealed class TerminalApp : DispatcherObject, IAsyncDisposable
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _pendingActions = new();
    private readonly TerminalInstance _terminal;
    private readonly TerminalAppOptions _options;
    private readonly WindowLayer? _windowLayer;
    private readonly InlineInteractiveHost? _inlineHost;
    private readonly FullscreenHost? _fullscreenHost;
    private readonly AsyncAutoResetEvent _wakeUp = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _renderRequested = true;
    private Visual? _pointerCapture;
    private Visual? _hoveredElement;
    private List<Visual>? _hoveredPath;
    private List<Visual>? _hoveredPathScratch;
    private int? _inlineLiveRegionTopRow;
    private bool _debugOverlayVisible;
    private int _renderFrameIndex;
    private Task? _runTask;
    private CellBuffer? _renderBuffer;
    private Func<bool>? _onUpdate;
    private readonly AnsiBuilder _updateOutputBuilder = new(initialCapacity: 4096);

    private readonly List<IAnimatedVisual> _animatedVisuals = new();
    private long _nextAnimationTick = long.MaxValue;

    private readonly HashSet<Binding> _pendingBindings = new(BindingReferenceComparer.Instance);

    internal enum DependencyKind
    {
        DynamicUpdate = 0,
        Measure = 1,
        Arrange = 2,
        Render = 3,
    }

    private readonly DependencyIndex _dynamicUpdateIndex = new();
    private readonly DependencyIndex _measureIndex = new();
    private readonly DependencyIndex _arrangeIndex = new();
    private readonly DependencyIndex _renderIndex = new();

    public TerminalApp(Visual root, TerminalInstance? terminal = null, TerminalAppOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _options = options ?? new TerminalAppOptions();

        ContentRoot = root;
        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            if (root is WindowLayer layer)
            {
                _windowLayer = layer;
                Root = layer;
            }
            else
            {
                _windowLayer = new WindowLayer { Content = root };
                Root = _windowLayer;
            }
        }
        else
        {
            Root = root;
        }

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

    public Visual ContentRoot { get; }

    internal void SetUpdateCallback(Func<bool>? onUpdate)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        _onUpdate = onUpdate;
    }

    private Visual? _focusedElement;

    public Visual? FocusedElement
    {
        get
        {
            VerifyAccess();
            return _focusedElement;
        }
        private set => _focusedElement = value;
    }

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
        _updateOutputBuilder.Dispose();
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

            // Flow output can allocate (it's not per-frame). Keep it simple for now.
            var buffer = new CellBuffer(width, Math.Max(1, block.DesiredSize.Height));
            buffer.Clear(block.GetTheme().ForegroundTextStyle());
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
        Run(cancellationToken);
        return Task.CompletedTask;
    }

    public Task RunInBackgroundAsync(CancellationToken cancellationToken = default)
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The app is already running.");
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new ManualResetEventSlim(false);
        _runTask = tcs.Task;

        var thread = new Thread(() =>
        {
            try
            {
                RunCore(cancellationToken, started);
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
            finally
            {
                started.Set();
                started.Dispose();
            }
        })
        {
            IsBackground = true,
            Name = "XenoAtom.Terminal.UI",
        };

        thread.Start();

        if (!started.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Timed out waiting for the UI thread to initialize.");
        }

        return _runTask;
    }

    public void Run(CancellationToken cancellationToken = default)
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The app is already running.");
        }

        _runTask = Task.CompletedTask;

        try
        {
            RunCore(cancellationToken);
        }
        finally
        {
            _runTask = null;
        }
    }

    private void RunCore(CancellationToken cancellationToken, ManualResetEventSlim? started = null)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        Dispatcher.BindToCurrentThread(this);
        started?.Set();

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

                while (_terminal.TryReadEvent(out var ev))
                {
                    HandleTerminalEvent(ev);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                AdvanceAnimations();

                if (_onUpdate is not null && !token.IsCancellationRequested)
                {
                    var keepGoing = false;
                    using (_terminal.CaptureOutput(_updateOutputBuilder))
                    {
                        keepGoing = _onUpdate();
                    }

                    if (_updateOutputBuilder.Length > 0 && _options.HostKind == TerminalHostKind.Inline)
                    {
                        _inlineHost?.PrepareForUserUpdate();
                        _terminal.WriteAtomic((TextWriter w) => w.Write(_updateOutputBuilder.UnsafeAsSpan()));
                        _updateOutputBuilder.Clear();
                        _renderRequested = true;
                    }

                    if (!keepGoing)
                    {
                        ProcessPendingBindings();
                        _renderRequested = true;
                        Render();
                        _cts.Cancel();
                        break;
                    }
                }

                ProcessPendingBindings();

                if (_renderRequested)
                {
                    _renderRequested = false;
                    Render();
                }

                Thread.Sleep(1);
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
            Dispatcher.DetachFromThread(this);
        }
    }

    internal void RequestRender()
    {
        _renderRequested = true;
        _wakeUp.Set();
    }

    internal void ClearInlineLiveRegion()
    {
        _inlineHost?.PrepareForUserUpdate();
    }

    internal void FinalizeInlineLiveRegion()
    {
        _inlineHost?.FinalizeAfterLive();
    }

    internal void ShowWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        VerifyAccess();

        if (_windowLayer is null)
        {
            throw new InvalidOperationException("Showing dialogs/windows is only supported in fullscreen apps.");
        }

        if (window.Parent is not null)
        {
            throw new InvalidOperationException("The visual is already part of the UI tree.");
        }

        _windowLayer.AddWindow(window);

        var focusCandidate = window.Focusable
            ? window
            : window.EnumerateVisualsDepthFirst().FirstOrDefault(v => v.Focusable && v.IsVisible && v.IsEnabled);

        if (focusCandidate is not null)
        {
            Focus(focusCandidate);
        }
    }

    internal bool CloseWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        VerifyAccess();

        if (_windowLayer is null)
        {
            throw new InvalidOperationException("Closing dialogs/windows is only supported in fullscreen apps.");
        }

        return _windowLayer.RemoveWindow(window);
    }

    internal void Focus(Visual? visual)
    {
        VerifyAccess();
        _focusedElement = visual;
        RequestRender();
    }

    internal void RegisterAnimatedVisual(IAnimatedVisual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _animatedVisuals.Add(visual);
        _nextAnimationTick = 0;
    }

    internal void UnregisterAnimatedVisual(IAnimatedVisual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _animatedVisuals.Remove(visual);
        _nextAnimationTick = 0;
    }

    private void AdvanceAnimations()
    {
        if (_animatedVisuals.Count == 0)
        {
            _nextAnimationTick = long.MaxValue;
            return;
        }

        var now = Stopwatch.GetTimestamp();
        if (_nextAnimationTick != 0 && now < _nextAnimationTick)
        {
            return;
        }

        var next = long.MaxValue;
        var changed = false;

        for (var i = 0; i < _animatedVisuals.Count; i++)
        {
            var visual = _animatedVisuals[i];
            if (now >= visual.NextAnimationTick)
            {
                changed |= visual.AdvanceAnimation(now);
            }

            next = Math.Min(next, visual.NextAnimationTick);
        }

        if (changed)
        {
            _renderRequested = true;
        }

        _nextAnimationTick = next;
    }

    private void OnValueChanged(Binding binding)
    {
        _pendingBindings.Add(binding);
    }

    internal void UpdateDependencies(Visual visual, DependencyKind kind, IReadOnlyCollection<Binding> dependencies)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(dependencies);

        switch (kind)
        {
            case DependencyKind.DynamicUpdate:
                _dynamicUpdateIndex.Update(visual, dependencies);
                break;
            case DependencyKind.Measure:
                _measureIndex.Update(visual, dependencies);
                break;
            case DependencyKind.Arrange:
                _arrangeIndex.Update(visual, dependencies);
                break;
            case DependencyKind.Render:
                _renderIndex.Update(visual, dependencies);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    internal void UnregisterDependencies(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _dynamicUpdateIndex.Remove(visual);
        _measureIndex.Remove(visual);
        _arrangeIndex.Remove(visual);
        _renderIndex.Remove(visual);
    }

    private void ProcessPendingBindings()
    {
        if (_pendingBindings.Count == 0)
        {
            return;
        }

        foreach (var binding in _pendingBindings)
        {
            if (_dynamicUpdateIndex.TryGetVisuals(binding, out var initVisuals))
            {
                foreach (var v in initVisuals)
                {
                    v.MarkDynamicUpdateDirty();
                }
            }

            if (_measureIndex.TryGetVisuals(binding, out var measureVisuals))
            {
                foreach (var v in measureVisuals)
                {
                    v.MarkMeasureDirty();
                }
            }

            if (_arrangeIndex.TryGetVisuals(binding, out var arrangeVisuals))
            {
                foreach (var v in arrangeVisuals)
                {
                    v.MarkArrangeDirty();
                }
            }

            if (_renderIndex.TryGetVisuals(binding, out var renderVisuals))
            {
                foreach (var v in renderVisuals)
                {
                    v.MarkRenderDirty();
                }
            }
        }

        _pendingBindings.Clear();
        _renderRequested = true;
    }

    private void Render()
    {
        EnsureFocusInScope();

        _inlineLiveRegionTopRow = null;
        _renderFrameIndex++;

        var wantsCursor = TryGetDesiredCursor(out var cursorX, out var cursorY);

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var height = Math.Max(1, _terminal.Size.Rows);

            Root.Measure(new Size(width, height));
            Root.Arrange(new Rectangle(0, 0, width, height));

            var buffer = EnsureRenderBuffer(width, height);
            buffer.Clear(Root.GetTheme().ForegroundTextStyle());
            Root.RenderTree(buffer);
            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            _fullscreenHost!.Render(buffer, wantsCursor, cursorX, cursorY);
            return;
        }

        {
            var width = Math.Max(1, _terminal.Size.Columns);

            Root.Measure(new Size(width, int.MaxValue / 4));
            Root.Arrange(new Rectangle(0, 0, width, Root.DesiredSize.Height));

            var buffer = EnsureRenderBuffer(width, Math.Max(1, Root.DesiredSize.Height));
            buffer.Clear(Root.GetTheme().ForegroundTextStyle());
            Root.RenderTree(buffer);
            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            _inlineHost!.Render(buffer, wantsCursor, cursorX, cursorY);
            _inlineLiveRegionTopRow = _inlineHost.LiveRegionTopRow;
        }
    }

    private bool TryGetDesiredCursor(out int x, out int y)
    {
        var focused = _focusedElement;
        if (focused is Input.ICursorProvider provider && provider.TryGetCursorCell(out x, out y))
        {
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private CellBuffer EnsureRenderBuffer(int width, int height)
    {
        var existing = _renderBuffer;
        if (existing is not null && existing.Width == width && existing.Height == height)
        {
            return existing;
        }

        _renderBuffer = new CellBuffer(width, height);
        return _renderBuffer;
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
            UpdateHover(null);
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
                _pointerCapture = null;
                return;
            }

            var translatedY = mouseEvent.Y - topRow.Value;
            uiY = translatedY;
            localY = translatedY;

            if (_pointerCapture is null)
            {
                if ((uint)translatedY >= (uint)height)
                {
                    UpdateHover(null);
                    return;
                }

                hitTarget = inputRoot.HitTest(mouseEvent.X, translatedY);
                target = hitTarget;
            }
            else
            {
                // When a pointer is captured, keep dispatching events to the captured element even if the
                // pointer leaves the live region. This avoids "stuck" captures and prevents hover effects
                // on other controls while dragging.
                hitTarget = (uint)translatedY < (uint)height ? inputRoot.HitTest(mouseEvent.X, translatedY) : null;
                target = _pointerCapture;
            }
        }

        if (target is null)
        {
            UpdateHover(null);
            return;
        }

        // While capturing, keep hover on the captured element to avoid hover state "leaking" to other visuals.
        UpdateHover(_pointerCapture ?? hitTarget);

        while (target is not null && (!target.IsVisible || !target.IsEnabled))
        {
            if (ReferenceEquals(target, _pointerCapture))
            {
                _pointerCapture = null;
                target = hitTarget;
                UpdateHover(hitTarget);
                continue;
            }

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
                if (mouseEvent.Button == TerminalMouseButton.Left)
                {
                    _pointerCapture = target;
                }
                target.RaiseEvent(Visual.PointerPressedEvent, args);
                break;
            case TerminalMouseKind.Up:
                target.RaiseEvent(Visual.PointerReleasedEvent, args);
                if (mouseEvent.Button == TerminalMouseButton.Left)
                {
                    _pointerCapture = null;
                    // Refresh hover after releasing capture.
                    UpdateHover(hitTarget);
                }
                break;
            case TerminalMouseKind.Wheel:
                target.RaiseEvent(Visual.PointerWheelEvent, args);
                break;
        }
    }

    private void UpdateHover(Visual? hitTarget)
    {
        var hoveredLeaf = hitTarget;
        while (hoveredLeaf is not null && (!hoveredLeaf.IsVisible || !hoveredLeaf.IsEnabled))
        {
            hoveredLeaf = hoveredLeaf.Parent;
        }

        _hoveredPath ??= new List<Visual>(8);
        _hoveredPathScratch ??= new List<Visual>(8);

        _hoveredPathScratch.Clear();
        for (var v = hoveredLeaf; v is not null; v = v.Parent)
        {
            _hoveredPathScratch.Add(v);
        }

        if (ReferenceEquals(_hoveredElement, hoveredLeaf) && _hoveredPath.Count == _hoveredPathScratch.Count)
        {
            var same = true;
            for (var i = 0; i < _hoveredPath.Count; i++)
            {
                if (!ReferenceEquals(_hoveredPath[i], _hoveredPathScratch[i]))
                {
                    same = false;
                    break;
                }
            }

            if (same)
            {
                return;
            }
        }

        for (var i = 0; i < _hoveredPath.Count; i++)
        {
            var v = _hoveredPath[i];
            if (!_hoveredPathScratch.Contains(v))
            {
                v.IsHovered = false;
            }
        }

        for (var i = 0; i < _hoveredPathScratch.Count; i++)
        {
            var v = _hoveredPathScratch[i];
            if (!_hoveredPath.Contains(v))
            {
                v.IsHovered = true;
            }
        }

        _hoveredElement = hoveredLeaf;
        (_hoveredPath, _hoveredPathScratch) = (_hoveredPathScratch, _hoveredPath);
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

internal sealed class DependencyIndex
{
    private readonly Dictionary<Binding, HashSet<Visual>> _bindingToVisuals = new(BindingReferenceComparer.Instance);
    private readonly Dictionary<Visual, HashSet<Binding>> _visualToBindings = new();

    public void Update(Visual visual, IReadOnlyCollection<Binding> dependencies)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (!_visualToBindings.TryGetValue(visual, out var old))
        {
            old = new HashSet<Binding>(BindingReferenceComparer.Instance);
            _visualToBindings.Add(visual, old);
        }
        else if (old.SetEquals(dependencies))
        {
            return;
        }

        // Remove old bindings no longer present.
        foreach (var binding in old)
        {
            if (Contains(dependencies, binding))
            {
                continue;
            }

            if (_bindingToVisuals.TryGetValue(binding, out var visuals))
            {
                visuals.Remove(visual);
                if (visuals.Count == 0)
                {
                    _bindingToVisuals.Remove(binding);
                }
            }
        }

        // Add new bindings.
        foreach (var binding in dependencies)
        {
            if (old.Contains(binding))
            {
                continue;
            }

            if (!_bindingToVisuals.TryGetValue(binding, out var visuals))
            {
                visuals = new HashSet<Visual>();
                _bindingToVisuals.Add(binding, visuals);
            }

            visuals.Add(visual);
        }

        old.Clear();
        foreach (var binding in dependencies)
        {
            old.Add(binding);
        }
    }

    public void Remove(Visual visual)
    {
        if (!_visualToBindings.TryGetValue(visual, out var bindings))
        {
            return;
        }

        foreach (var binding in bindings)
        {
            if (_bindingToVisuals.TryGetValue(binding, out var visuals))
            {
                visuals.Remove(visual);
                if (visuals.Count == 0)
                {
                    _bindingToVisuals.Remove(binding);
                }
            }
        }

        _visualToBindings.Remove(visual);
    }

    public bool TryGetVisuals(Binding binding, out HashSet<Visual> visuals)
        => _bindingToVisuals.TryGetValue(binding, out visuals!);

    private static bool Contains(IReadOnlyCollection<Binding> bindings, Binding binding)
    {
        if (bindings is HashSet<Binding> set)
        {
            return set.Contains(binding);
        }

        foreach (var b in bindings)
        {
            if (BindingReferenceComparer.Instance.Equals(b, binding))
            {
                return true;
            }
        }

        return false;
    }
}
