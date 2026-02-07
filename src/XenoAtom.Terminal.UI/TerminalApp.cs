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
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Hosts a retained-mode visual tree and drives input, layout, rendering, and binding invalidation.
/// </summary>
public sealed partial class TerminalApp : DispatcherObject, IAsyncDisposable, IVisualElement
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<PendingAction> _pendingActions = new();
    private readonly TerminalInstance _terminal;
    private readonly TerminalAppOptions _options;
    private readonly WindowLayer? _windowLayer;
    private Visual? _activeTooltipWindow;
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
    private DebugOverlayMetrics? _debugOverlayMetrics;
    private int _renderFrameIndex;
    private Task? _runTask;
    private CellBuffer? _renderBuffer;
    private Visual? _focusedElement;
    private Func<TerminalRunningContext, ValueTask<TerminalLoopResult>>? _onUpdate;
    private TerminalRunningContext? _updateContext;
    private readonly AnsiBuilder _updateOutputBuilder = new(initialCapacity: 4096);
    private global::XenoAtom.Terminal.UI.Input.KeyGesture _exitGesture;
    private bool _inlineRemoveOnEnd;
    private Dictionary<string, AnsiStyle>? _previousMarkupStyles;

    private Popup? _contextMenuPopup;
    private Visual? _contextMenuFocusContext;

    private BindableList<Command>? _globalCommands;

    private long _lastTickTimestamp;
    private readonly KeyGesture[] _pendingSequence = new KeyGesture[4];
    private int _pendingSequenceCount;
    private long _pendingSequenceTimestamp;
    private Visual? _pendingSequenceFocus;

    private readonly List<IAnimatedVisual> _animatedVisuals = new();
    private long _nextAnimationTick = long.MaxValue;

    private readonly HashSet<Binding> _pendingBindingWrites = new(BindingReferenceComparer.Instance);

    private bool _pendingRenderHasLayoutImpact;
    private bool _pendingRenderDirtyRectValid;
    private Rectangle _pendingRenderDirtyRect;

    private int _lastRenderWidth;
    private int _lastRenderHeight;

    private static readonly AsyncLocal<int> UpdateCallbackDepth = new();

    private Task<TerminalLoopResult>? _pendingUpdateTask;

    private readonly record struct PendingAction(Action Action, bool CaptureFlowOutput);

    internal enum DependencyKind
    {
        DynamicUpdate = 0,
        PrepareChildren = 1,
        Measure = 2,
        Arrange = 3,
        Render = 4,
    }

    private readonly BindingDependencyIndex _dynamicUpdateIndex = new();
    private readonly BindingDependencyIndex _prepareChildrenIndex = new();
    private readonly BindingDependencyIndex _measureIndex = new();
    private readonly BindingDependencyIndex _arrangeIndex = new();
    private readonly BindingDependencyIndex _renderIndex = new();

    TerminalApp? IVisualElement.App => this;
    internal DebugOverlayMetrics? DebugOverlayMetrics => _debugOverlayMetrics;

    /// <summary>
    /// Gets the global commands registered on this application.
    /// </summary>
    public BindableList<Command> GlobalCommands => _globalCommands ??= new BindableList<Command>(this, "TerminalApp.GlobalCommands");

    /// <summary>
    /// Adds or replaces a global command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the command creates an ambiguous prefix conflict.</exception>
    public void AddGlobalCommand(Command command)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(command);
        command.Validate();

        var commands = GlobalCommands;

        // Avoid ambiguous routing: a sequence prefix must not be used as a standalone gesture in the same scope.
        if (command.Sequence is { } sequence)
        {
            var prefix = sequence[0];
            for (var i = 0; i < commands.Count; i++)
            {
                var existing = commands[i];
                if (existing.Gesture is { } g && g.Equals(prefix))
                {
                    throw new InvalidOperationException($"The gesture '{prefix}' is already registered as a standalone global command and cannot be used as a sequence prefix.");
                }
            }
        }
        else if (command.Gesture is { } gesture)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var existing = commands[i];
                if (existing.Sequence is { } existingSequence && existingSequence[0].Equals(gesture))
                {
                    throw new InvalidOperationException($"The gesture '{gesture}' is already registered as a global sequence prefix and cannot be used as a standalone command.");
                }
            }
        }

        for (var i = 0; i < commands.Count; i++)
        {
            if (string.Equals(commands[i].Id, command.Id, StringComparison.Ordinal))
            {
                commands[i] = command;
                return;
            }
        }

        commands.Add(command);
    }

    /// <summary>
    /// Removes a global command by id.
    /// </summary>
    /// <param name="id">The command id.</param>
    /// <returns><see langword="true"/> if a command was removed; otherwise <see langword="false"/>.</returns>
    public bool RemoveGlobalCommand(string id)
    {
        VerifyAccess();
        if (_globalCommands is null)
        {
            return false;
        }

        for (var i = 0; i < _globalCommands.Count; i++)
        {
            if (string.Equals(_globalCommands[i].Id, id, StringComparison.Ordinal))
            {
                _globalCommands.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalApp"/> class.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="terminal">The terminal instance to use. When <see langword="null"/>, uses <see cref="Terminal.Instance"/>.</param>
    /// <param name="options">Optional host configuration.</param>
    public TerminalApp(Visual root, TerminalInstance? terminal = null, TerminalAppOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _options = options ?? new TerminalAppOptions();
        if (_options.UpdateWaitDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The update wait duration cannot be negative.");
        }

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

            // When we wrap the user root with an internal WindowLayer, propagate the Theme so that
            // fullscreen clear/background uses the expected theme even if the user set it on the root visual.
            if (!ReferenceEquals(ContentRoot, _windowLayer))
            {
                ContentRoot.StyleEnvironment ??= new();
                _windowLayer.StyleEnvironment = ContentRoot.StyleEnvironment;
            }
        }
        else
        {
            Root = root;
        }

        if (_options.HostKind == TerminalHostKind.Inline && !Root.HasLocalStyle(Theme.Key))
        {
            Root.Style(Theme.Terminal);
        }

        if (!Root.HasLocalStyle(CultureStyle.Key))
        {
            Root.Style(CultureStyle.Default with { Culture = _options.Culture });
        }

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            _fullscreenHost = new FullscreenHost(_terminal);
        }
        else
        {
            _inlineHost = new InlineInteractiveHost(_terminal);
        }

        _exitGesture = _options.ExitGesture ?? GetDefaultExitGesture(_options.HostKind);

        AddGlobalCommand(new Command
        {
            Id = "TerminalApp.Quit",
            LabelMarkup = "Quit",
            DescriptionMarkup = "Quit the application.",
            Gesture = _exitGesture,
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => v.App?.Stop(),
        });
    }

    /// <summary>
    /// Gets the underlying terminal instance used by this app.
    /// </summary>
    public TerminalInstance Terminal => _terminal;

    /// <summary>
    /// Gets the actual root visual rendered by the app.
    /// </summary>
    public Visual Root { get; }

    /// <summary>
    /// Gets the user-provided content root visual.
    /// </summary>
    public Visual ContentRoot { get; }

    internal void SetUpdateCallback(Func<TerminalRunningContext, TerminalLoopResult> onUpdate)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        _onUpdate = ctx => new ValueTask<TerminalLoopResult>(onUpdate(ctx));
    }

    internal void SetUpdateCallback(Func<TerminalRunningContext, ValueTask<TerminalLoopResult>> onUpdate)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        _onUpdate = onUpdate;
    }

    internal bool InlineRemoveOnEnd => _inlineRemoveOnEnd;


    /// <summary>
    /// Gets the currently focused visual, or <see langword="null"/> if no element is focused.
    /// </summary>
    [Bindable]
    public Visual? FocusedElement
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __FocusedElement__BindingAccessor.Instance);
            return _focusedElement;
        }

        set
        {
            if (!ReferenceEquals(_focusedElement, value))
            {
                DetachFocus(_focusedElement);
                _focusedElement = value;
                AttachFocus(value);
                BindingManager.Current.NotifyValueChanged(this, __FocusedElement__BindingAccessor.Instance);
            }
        }
    }

    private void DetachFocus(Visual? element)
    {
        if (element is null) return;

        element.HasFocus = false;
        var next = element.Parent;
        while (next is not null)
        {
            next.HasFocusWithin = false;
            next = next.Parent;
        }
    }

    private void AttachFocus(Visual? element)
    {
        if (element is null) return;
        element.HasFocus = true;
        var next = element.Parent;
        while (next is not null)
        {
            next.HasFocusWithin = true;
            next = next.Parent;
        }
    }

    /// <summary>
    /// Posts an action to be executed on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _pendingActions.Enqueue(new PendingAction(action, CaptureFlowOutput: UpdateCallbackDepth.Value > 0));
        _wakeUp.Set();
    }

    /// <summary>
    /// Requests the app loop to stop.
    /// </summary>
    public void Stop() => _cts.Cancel();

    internal int PendingCommandSequenceCount => _pendingSequenceCount;

    internal KeyGesture GetPendingCommandSequenceGesture(int index) => _pendingSequence[index];

    /// <summary>
    /// Stops the app and releases resources.
    /// </summary>
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

    /// <summary>
    /// Writes a markup line in inline host mode.
    /// </summary>
    /// <param name="markup">The markup to write.</param>
    /// <exception cref="InvalidOperationException">Thrown when used outside of inline host mode.</exception>
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

    /// <summary>
    /// Appends a visual as flow output in inline host mode.
    /// </summary>
    /// <param name="block">The visual to render and append.</param>
    /// <exception cref="InvalidOperationException">Thrown when used outside of inline host mode.</exception>
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

        ThemedHost? themedHost = null;
        var renderRoot = block;
        if (_options.HostKind == TerminalHostKind.Inline && !block.HasLocalStyle(Theme.Key))
        {
            themedHost = new ThemedHost(block, Theme.Terminal);
            renderRoot = themedHost;
        }

        renderRoot.AttachToApp(this);
        try
        {
            renderRoot.Measure(new LayoutConstraints(0, width, 0, LayoutConstants.Infinite));
            renderRoot.Arrange(new Rectangle(0, 0, width, renderRoot.DesiredSize.Height));

            // Flow output can allocate (it's not per-frame). Keep it simple for now.
            var buffer = new CellBuffer(width, Math.Max(1, renderRoot.DesiredSize.Height));
            buffer.Clear(renderRoot.GetTheme().BaseTextStyle());
            renderRoot.RenderTree(buffer);
            _inlineHost.WriteMarkupLines(buffer.ToMarkupLines());
        }
        finally
        {
            renderRoot.DetachFromApp();
            if (themedHost is not null)
            {
                themedHost.Content = null;
            }
        }

        RequestRender();
    }

    /// <summary>
    /// Runs the app until it is stopped or the token is canceled.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the run.</param>
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        Run(cancellationToken);
        return Task.CompletedTask;
    }

    private TerminalScope _alternateScope;
    private TerminalScope _rawScope;
    private TerminalScope _echoScope;
    private TerminalScope _mouseScope;
    private TerminalScope _pasteScope;
    private TerminalScope _cursorScope;

    /// <summary>
    /// Starts a single-threaded run of this app without blocking the calling thread.
    /// Intended for deterministic unit tests.
    /// </summary>
    internal void BeginRun()
    {
        if (_runTask is not null)
        {
            throw new InvalidOperationException("The app is already running.");
        }

        _runTask = Task.CompletedTask;
        try
        {
            BeginRunCore(started: null);
        }
        catch
        {
            EndRunCore();
            _runTask = null;
            throw;
        }
    }

    /// <summary>
    /// Performs a single frame update without sleeping. Intended for deterministic tests.
    /// </summary>
    internal void Tick(long? timestamp = null)
    {
        VerifyAccess();

        _lastTickTimestamp = timestamp ?? Stopwatch.GetTimestamp();
        var metrics = _debugOverlayMetrics;
        metrics?.BeginTick(_lastTickTimestamp);

        CancelPendingSequenceIfTimedOut(_lastTickTimestamp);

        while (_pendingActions.TryDequeue(out var pending))
        {
            if (pending.CaptureFlowOutput)
            {
                ExecuteCapturedFlowOutput(pending.Action);
            }
            else
            {
                pending.Action();
            }
        }

        FlushCapturedFlowOutputIfNeeded();

        while (_terminal.TryReadEvent(out var ev))
        {
            HandleTerminalEvent(ev);
        }

        AdvanceAnimations(timestamp);

        long userUpdateTicks = 0;
        if (_onUpdate is not null && !_cts.IsCancellationRequested)
        {
            var hasResult = false;
            var result = TerminalLoopResult.Continue;

            if (_pendingUpdateTask is null)
            {
                var updateStart = metrics is null ? 0 : Stopwatch.GetTimestamp();
                _updateContext!.Timestamp = timestamp ?? Stopwatch.GetTimestamp();

                var previousDepth = UpdateCallbackDepth.Value;
                UpdateCallbackDepth.Value = previousDepth + 1;
                try
                {
                    using (_terminal.CaptureOutput(_updateOutputBuilder))
                    {
                        var resultTask = _onUpdate(_updateContext);
                        if (resultTask.IsCompletedSuccessfully)
                        {
                            result = resultTask.GetAwaiter().GetResult();
                            hasResult = true;
                        }
                        else
                        {
                            _pendingUpdateTask = resultTask.AsTask();
                        }
                    }
                }
                finally
                {
                    UpdateCallbackDepth.Value = previousDepth;
                }

                if (metrics is not null)
                {
                    userUpdateTicks = Math.Max(0, Stopwatch.GetTimestamp() - updateStart);
                }

                FlushCapturedFlowOutputIfNeeded();
            }
            else if (_pendingUpdateTask.IsCompleted)
            {
                result = _pendingUpdateTask.GetAwaiter().GetResult();
                _pendingUpdateTask = null;
                hasResult = true;
            }

            if (hasResult && result != TerminalLoopResult.Continue)
            {
                if (_options.HostKind == TerminalHostKind.Inline)
                {
                    _inlineRemoveOnEnd = result == TerminalLoopResult.Stop;
                }

                ProcessBindingWrites();
                _renderRequested = true;
                _cts.Cancel();
            }
        }

        ProcessBindingWrites();

        if (_renderRequested)
        {
            _renderRequested = false;
            Render();
        }

        metrics?.EndTick(_lastTickTimestamp, userUpdateTicks);
    }

    /// <summary>
    /// Ends a run started by <see cref="BeginRun"/>.
    /// </summary>
    internal void EndRun()
    {
        if (_runTask is null)
        {
            return;
        }

        try
        {
            EndRunCore();
        }
        finally
        {
            _runTask = null;
        }
    }

    /// <summary>
    /// Runs the terminal app synchronously on the current thread.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the run.</param>
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

        try
        {
            BeginRunCore(started);

            while (!token.IsCancellationRequested)
            {
                Tick();
                var updateWaitDuration = _options.UpdateWaitDuration;
                if (updateWaitDuration > TimeSpan.Zero)
                {
                    Thread.Sleep(updateWaitDuration);
                }
            }
        }
        finally
        {
            EndRunCore();
        }
    }

    private void BeginRunCore(ManualResetEventSlim? started)
    {
        Dispatcher.BindToCurrentThread(this);
        started?.Set();

        // Ensure semantic markup tokens (e.g. [primary]) are available for Terminal.WriteMarkupLine during this run.
        // Restore in EndRunCore to avoid leaking theme state to other terminal usages.
        _previousMarkupStyles = _terminal.MarkupStyles;
        _terminal.MarkupStyles = Root.GetTheme().GetMarkupStyles();

        Root.AttachToApp(this);
        BindingManager.Current.ValueChanged += OnValueChanged;
        _updateContext = new TerminalRunningContext(this, _terminal, _options.HostKind);
        _inlineRemoveOnEnd = false;
        _pendingUpdateTask = null;
        _updateOutputBuilder.Clear();

        if (!_terminal.IsInputRunning)
        {
            _terminal.StartInput(new TerminalInputOptions { TreatControlCAsInput = true });
        }

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            _alternateScope = _terminal.UseAlternateScreen();
        }

        _rawScope = _terminal.UseRawMode(_options.RawMode);
        if (_options.DisableInputEcho)
        {
            _echoScope = _terminal.SetInputEcho(false);
        }

        _cursorScope = _terminal.HideCursor();

        if (_options.EnableMouse)
        {
            _mouseScope = _terminal.EnableMouseInput(_options.MouseMode);
        }

        if (_options.EnableBracketedPaste)
        {
            _pasteScope = _terminal.EnableBracketedPasteInput();
        }

        EnsureInitialFocus();
        RequestRender();
    }

    private void EndRunCore()
    {
        try
        {
            _pendingUpdateTask = null;
            _updateOutputBuilder.Clear();

            _terminal.MarkupStyles = _previousMarkupStyles;
            _previousMarkupStyles = null;

            _pasteScope.Dispose();
            _mouseScope.Dispose();
            _cursorScope.Dispose();
            _echoScope.Dispose();
            _rawScope.Dispose();
            _alternateScope.Dispose();

            _pasteScope = default;
            _mouseScope = default;
            _cursorScope = default;
            _echoScope = default;
            _rawScope = default;
            _alternateScope = default;

            BindingManager.Current.ValueChanged -= OnValueChanged;
            Root.DetachFromApp();
        }
        finally
        {
            Dispatcher.DetachFromThread(this);
        }
    }

    private void ExecuteCapturedFlowOutput(Action action)
    {
        using (_terminal.CaptureOutput(_updateOutputBuilder))
        {
            action();
        }
    }

    private void FlushCapturedFlowOutputIfNeeded()
    {
        if (_updateOutputBuilder.Length == 0)
        {
            return;
        }

        if (_options.HostKind == TerminalHostKind.Inline)
        {
            _inlineHost?.PrepareForUserUpdate();
            _terminal.WriteAtomic((TextWriter w) => w.Write(_updateOutputBuilder.UnsafeAsSpan()));
            _renderRequested = true;
        }

        _updateOutputBuilder.Clear();
    }

    internal void RequestRender()
    {
        _renderRequested = true;
        _wakeUp.Set();
    }

    internal void RequestAnimation()
    {
        _nextAnimationTick = 0;
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

        // Tooltips are non-interactive overlays and should not remain visible while opening other windows.
        if (_activeTooltipWindow is not null && !ReferenceEquals(_activeTooltipWindow, window))
        {
            _windowLayer.RemoveWindow(_activeTooltipWindow);
            _activeTooltipWindow = null;
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

    internal void ShowTooltipWindow(Visual tooltipWindow)
    {
        ArgumentNullException.ThrowIfNull(tooltipWindow);
        VerifyAccess();

        if (_windowLayer is null)
        {
            throw new InvalidOperationException("Showing tooltips is only supported in fullscreen apps.");
        }

        if (_activeTooltipWindow is not null && !ReferenceEquals(_activeTooltipWindow, tooltipWindow))
        {
            _windowLayer.RemoveWindow(_activeTooltipWindow);
        }

        _activeTooltipWindow = tooltipWindow;
        ShowWindow(tooltipWindow);
    }

    internal void CloseTooltipWindow(Visual tooltipWindow)
    {
        ArgumentNullException.ThrowIfNull(tooltipWindow);
        VerifyAccess();

        if (_windowLayer is null)
        {
            return;
        }

        if (ReferenceEquals(_activeTooltipWindow, tooltipWindow))
        {
            _activeTooltipWindow = null;
        }

        _windowLayer.RemoveWindow(tooltipWindow);
    }

    internal bool CloseWindow(Visual window)
    {
        ArgumentNullException.ThrowIfNull(window);
        VerifyAccess();

        if (_windowLayer is null)
        {
            throw new InvalidOperationException("Closing dialogs/windows is only supported in fullscreen apps.");
        }

        if (ReferenceEquals(_activeTooltipWindow, window))
        {
            _activeTooltipWindow = null;
        }

        return _windowLayer.RemoveWindow(window);
    }

    internal Popup ShowContextMenu(Visual target, IEnumerable<MenuItem> items, int uiX, int uiY)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(items);
        VerifyAccess();

        if (_windowLayer is null)
        {
            throw new InvalidOperationException("Showing context menus is only supported in fullscreen apps.");
        }

        if (_contextMenuPopup is not null)
        {
            _contextMenuPopup.Close();
            _contextMenuPopup = null;
        }

        var menuItems = items as IReadOnlyList<MenuItem> ?? items.ToArray();
        if (menuItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot show an empty context menu.");
        }

        var focusContext = FocusedElement;
        _contextMenuFocusContext = focusContext;

        var popup = ContextMenuService.CreatePopup(target, menuItems, uiX, uiY);
        _contextMenuPopup = popup;

        popup.Closed((_, _) =>
        {
            if (ReferenceEquals(_contextMenuPopup, popup))
            {
                _contextMenuPopup = null;
            }

            var toRestore = _contextMenuFocusContext;
            _contextMenuFocusContext = null;
            if (toRestore is not null && ReferenceEquals(toRestore.App, this))
            {
                Focus(toRestore);
            }
        });

        popup.Show();
        return popup;
    }

    /// <summary>
    /// Sets focus to the specified visual.
    /// </summary>
    /// <param name="visual">The visual to focus, or <see langword="null"/> to clear focus.</param>
    public void Focus(Visual? visual)
    {
        VerifyAccess();
        FocusedElement = visual;
        RequestRender();
    }

    internal void RegisterAnimatedVisual(IAnimatedVisual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        if (!_animatedVisuals.Contains(visual))
        {
            _animatedVisuals.Add(visual);
        }
        _nextAnimationTick = 0;
    }

    internal void UnregisterAnimatedVisual(IAnimatedVisual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _animatedVisuals.Remove(visual);
        _nextAnimationTick = 0;
    }

    private void AdvanceAnimations(long? timestamp = null)
    {
        if (_animatedVisuals.Count == 0)
        {
            _nextAnimationTick = long.MaxValue;
            return;
        }

        var now = timestamp ?? Stopwatch.GetTimestamp();
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
        // Don't record bindings for visuals not yet attached to the app (e.g. in initializers)
        if (binding.Owner is IVisualElement visual)
        {
            if (visual.App is null)
            {
                return;
            }
        }

        _pendingBindingWrites.Add(binding);
    }

    internal void UpdateBindingReadsForVisual(Visual visual, DependencyKind kind, IReadOnlyCollection<Binding> reads)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(reads);

        switch (kind)
        {
            case DependencyKind.DynamicUpdate:
                _dynamicUpdateIndex.UpdateBindingReadsForVisual(visual, reads);
                break;
            case DependencyKind.PrepareChildren:
                _prepareChildrenIndex.UpdateBindingReadsForVisual(visual, reads);
                break;
            case DependencyKind.Measure:
                _measureIndex.UpdateBindingReadsForVisual(visual, reads);
                break;
            case DependencyKind.Arrange:
                _arrangeIndex.UpdateBindingReadsForVisual(visual, reads);
                break;
            case DependencyKind.Render:
                _renderIndex.UpdateBindingReadsForVisual(visual, reads);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    internal void UnregisterDependencies(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _dynamicUpdateIndex.Remove(visual);
        _prepareChildrenIndex.Remove(visual);
        _measureIndex.Remove(visual);
        _arrangeIndex.Remove(visual);
        _renderIndex.Remove(visual);
    }

    private void ProcessBindingWrites()
    {
        if (_pendingBindingWrites.Count == 0)
        {
            return;
        }

        var metrics = _debugOverlayMetrics;

        foreach (var binding in _pendingBindingWrites)
        {
            if (_dynamicUpdateIndex.TryGetVisuals(binding, out var initVisuals))
            {
                foreach (var v in initVisuals)
                {
                    v.MarkDynamicUpdateDirty();
                    _pendingRenderHasLayoutImpact = true;
                }
            }

            if (_prepareChildrenIndex.TryGetVisuals(binding, out var prepareVisuals))
            {
                foreach (var v in prepareVisuals)
                {
                    v.MarkPrepareChildrenDirty();
                    _pendingRenderHasLayoutImpact = true;
                }
            }

            if (_measureIndex.TryGetVisuals(binding, out var measureVisuals))
            {
                foreach (var v in measureVisuals)
                {
                    v.MarkMeasureDirty();
                    _pendingRenderHasLayoutImpact = true;
                }
            }

            if (_arrangeIndex.TryGetVisuals(binding, out var arrangeVisuals))
            {
                foreach (var v in arrangeVisuals)
                {
                    v.MarkArrangeDirty();
                    _pendingRenderHasLayoutImpact = true;
                }
            }

            if (_renderIndex.TryGetVisuals(binding, out var renderVisuals))
            {
                foreach (var v in renderVisuals)
                {
                    var bounds = v.Bounds;
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        continue;
                    }

                    // Expand by 1 cell horizontally to reduce artifacts with wide glyphs clipped at region boundaries.
                    var x = Math.Max(0, bounds.X - 1);
                    var right = Math.Min(LayoutConstants.MaxFinite, bounds.Right + 1);
                    var expanded = new Rectangle(x, bounds.Y, Math.Max(0, right - x), bounds.Height);
                    metrics?.AddDirtyRect(expanded);

                    if (!_pendingRenderDirtyRectValid)
                    {
                        _pendingRenderDirtyRect = expanded;
                        _pendingRenderDirtyRectValid = true;
                    }
                    else
                    {
                        _pendingRenderDirtyRect = Rectangle.Union(_pendingRenderDirtyRect, expanded);
                    }
                }
            }
        }

        _pendingBindingWrites.Clear();
        _renderRequested = true;
    }

    private void Render()
    {
        EnsureFocusInScope();

        _inlineLiveRegionTopRow = null;
        _renderFrameIndex++;
        var metrics = _debugOverlayMetrics;
        var renderStartTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();
        metrics?.BeginRenderFrame(_renderFrameIndex);

        if (_options.HostKind == TerminalHostKind.Fullscreen)
        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var height = Math.Max(1, _terminal.Size.Rows);

            if (metrics is not null)
            {
                var t0 = Stopwatch.GetTimestamp();
                Root.Measure(new LayoutConstraints(0, width, 0, height));
                metrics.RenderMeasureTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);

                t0 = Stopwatch.GetTimestamp();
                Root.Arrange(new Rectangle(0, 0, width, height));
                metrics.RenderArrangeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
            }
            else
            {
                Root.Measure(new LayoutConstraints(0, width, 0, height));
                Root.Arrange(new Rectangle(0, 0, width, height));
            }

            var layoutProducedWrites = _pendingBindingWrites.Count > 0;

            // Ensure any bindings updated during layout are processed before rendering (e.g. Bounds from Arrange)
            ProcessBindingWrites();

            var wantsCursor = TryGetDesiredCursor(out var cursorX, out var cursorY);

            var buffer = EnsureRenderBuffer(width, height);
            var baseStyle = Root.GetTheme().BaseTextStyle();

            var fullRepaint =
                _debugOverlayVisible ||
                width != _lastRenderWidth ||
                height != _lastRenderHeight ||
                layoutProducedWrites ||
                _pendingRenderHasLayoutImpact ||
                !_pendingRenderDirtyRectValid;

            _lastRenderWidth = width;
            _lastRenderHeight = height;
            metrics?.SetFullRepaint(fullRepaint);
            if (metrics is not null)
            {
                metrics.SetRepaintRect(fullRepaint ? new Rectangle(0, 0, width, height) : ClampToViewport(_pendingRenderDirtyRect, width, height));
            }

            if (fullRepaint)
            {
                buffer.Clear(baseStyle);
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    Root.RenderTree(buffer);
                    metrics.RenderTreeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                }
                else
                {
                    Root.RenderTree(buffer);
                }
            }
            else
            {
                var rect = ClampToViewport(_pendingRenderDirtyRect, width, height);
                buffer.PushClip(rect);
                buffer.ClearCurrentClip(baseStyle);
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    Root.RenderTree(buffer);
                    metrics.RenderTreeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                }
                else
                {
                    Root.RenderTree(buffer);
                }
                buffer.PopClip();
            }

            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            if (metrics is not null)
            {
                var t0 = Stopwatch.GetTimestamp();
                _fullscreenHost!.Render(buffer, wantsCursor, cursorX, cursorY);
                metrics.RenderHostTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                metrics.EndRenderFrame(renderStartTimestamp, Stopwatch.GetTimestamp());
            }
            else
            {
                _fullscreenHost!.Render(buffer, wantsCursor, cursorX, cursorY);
            }
            _pendingRenderHasLayoutImpact = false;
            _pendingRenderDirtyRectValid = false;
            return;
        }

        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var viewportHeight = Math.Max(1, _terminal.Size.Rows);
            var stretchRootToViewport = Root.VerticalAlignment == Align.Stretch;

            if (metrics is not null)
            {
                var t0 = Stopwatch.GetTimestamp();
                Root.Measure(new LayoutConstraints(0, width, 0, stretchRootToViewport ? viewportHeight : LayoutConstants.Infinite));
                metrics.RenderMeasureTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);

                t0 = Stopwatch.GetTimestamp();
                var arrangeHeight = stretchRootToViewport ? viewportHeight : Root.DesiredSize.Height;
                Root.Arrange(new Rectangle(0, 0, width, arrangeHeight));
                metrics.RenderArrangeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
            }
            else
            {
                Root.Measure(new LayoutConstraints(0, width, 0, stretchRootToViewport ? viewportHeight : LayoutConstants.Infinite));
                var arrangeHeight = stretchRootToViewport ? viewportHeight : Root.DesiredSize.Height;
                Root.Arrange(new Rectangle(0, 0, width, arrangeHeight));
            }

            var layoutProducedWrites = _pendingBindingWrites.Count > 0;

            // Ensure any bindings updated during layout are processed before rendering (e.g. Bounds from Arrange)
            ProcessBindingWrites();

            var wantsCursor = TryGetDesiredCursor(out var cursorX, out var cursorY);

            var bufferHeight = stretchRootToViewport ? viewportHeight : Math.Max(1, Root.DesiredSize.Height);
            var buffer = EnsureRenderBuffer(width, bufferHeight);
            var baseStyle = Root.GetTheme().BaseTextStyle();

            var fullRepaint =
                _debugOverlayVisible ||
                width != _lastRenderWidth ||
                layoutProducedWrites ||
                _pendingRenderHasLayoutImpact ||
                !_pendingRenderDirtyRectValid;

            _lastRenderWidth = width;
            _lastRenderHeight = viewportHeight;
            metrics?.SetFullRepaint(fullRepaint);
            if (metrics is not null)
            {
                metrics.SetRepaintRect(fullRepaint ? new Rectangle(0, 0, width, buffer.Height) : ClampToViewport(_pendingRenderDirtyRect, width, buffer.Height));
            }

            if (fullRepaint)
            {
                buffer.Clear(baseStyle);
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    Root.RenderTree(buffer);
                    metrics.RenderTreeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                }
                else
                {
                    Root.RenderTree(buffer);
                }
            }
            else
            {
                var rect = ClampToViewport(_pendingRenderDirtyRect, width, buffer.Height);
                buffer.PushClip(rect);
                buffer.ClearCurrentClip(baseStyle);
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    Root.RenderTree(buffer);
                    metrics.RenderTreeTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                }
                else
                {
                    Root.RenderTree(buffer);
                }
                buffer.PopClip();
            }

            if (_debugOverlayVisible)
            {
                RenderDebugOverlay(buffer);
            }

            if (metrics is not null)
            {
                var t0 = Stopwatch.GetTimestamp();
                _inlineHost!.Render(buffer, wantsCursor, cursorX, cursorY);
                metrics.RenderHostTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                metrics.EndRenderFrame(renderStartTimestamp, Stopwatch.GetTimestamp());
            }
            else
            {
                _inlineHost!.Render(buffer, wantsCursor, cursorX, cursorY);
            }
            _inlineLiveRegionTopRow = _inlineHost.LiveRegionTopRow;

            _pendingRenderHasLayoutImpact = false;
            _pendingRenderDirtyRectValid = false;
        }
    }

    private static Rectangle ClampToViewport(Rectangle rect, int width, int height)
    {
        var x0 = Math.Clamp(rect.X, 0, width);
        var y0 = Math.Clamp(rect.Y, 0, height);
        var x1 = Math.Clamp(rect.Right, 0, width);
        var y1 = Math.Clamp(rect.Bottom, 0, height);
        return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
    }

    private bool TryGetDesiredCursor(out int x, out int y)
    {
        var focused = FocusedElement;
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
        var metrics = _debugOverlayMetrics;

        static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
        static string FormatMs(long ticks) => ToMs(ticks).ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture);
        static string FormatFps(double fps) => fps <= 0 ? "-" : fps.ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture);

        var dirtyText = "Dirty: <none>";
        if (metrics is not null && metrics.HasDirtyRect)
        {
            var r = metrics.DirtyRect;
            dirtyText = $"Dirty: ({r.X},{r.Y}) {r.Width}x{r.Height}";
        }

        var repaintText = "Repaint: <unknown>";
        if (metrics is not null && metrics.HasRepaintRect)
        {
            var r = metrics.RepaintRect;
            repaintText = $"Repaint: ({r.X},{r.Y}) {r.Width}x{r.Height}";
        }

        var lines = new List<string>(16)
        {
            $"Frame: {_renderFrameIndex}  FPS: {FormatFps(metrics?.Fps ?? 0)}",
            $"Tick: {FormatMs(metrics?.TickTotalTicks ?? 0)}ms  Update: {FormatMs(metrics?.TickUserUpdateTicks ?? 0)}ms",
            $"Top: Measure {FormatMs(metrics?.RenderMeasureTicks ?? 0)}ms  Arrange {FormatMs(metrics?.RenderArrangeTicks ?? 0)}ms  Render {FormatMs(metrics?.RenderTreeTicks ?? 0)}ms  Host {FormatMs(metrics?.RenderHostTicks ?? 0)}ms",
            $"Top: Total {FormatMs(metrics?.RenderTotalTicks ?? 0)}ms",
            $"Calls: DynamicUpdate {(metrics?.DynamicUpdate.Calls ?? 0)} ({FormatMs(metrics?.DynamicUpdate.Ticks ?? 0)}ms)",
            $"Calls: Prepare {(metrics?.PrepareChildren.Calls ?? 0)} ({FormatMs(metrics?.PrepareChildren.Ticks ?? 0)}ms)",
            $"Calls: Measure {(metrics?.Measure.Calls ?? 0)} ({FormatMs(metrics?.Measure.Ticks ?? 0)}ms)  Cache {(metrics?.MeasureCacheHits ?? 0)}",
            $"Calls: Arrange {(metrics?.Arrange.Calls ?? 0)} ({FormatMs(metrics?.Arrange.Ticks ?? 0)}ms)  Cache {(metrics?.ArrangeCacheHits ?? 0)}",
            $"Calls: Render {(metrics?.RenderOverride.Calls ?? 0)} ({FormatMs(metrics?.RenderOverride.Ticks ?? 0)}ms)  ClipSkips {(metrics?.RenderClipSkips ?? 0)}",
            repaintText + (metrics is not null && metrics.FullRepaint ? "  (full repaint)" : string.Empty),
            dirtyText + (metrics is not null && metrics.FullRepaint ? "  (full repaint)" : string.Empty),
            $"Diff: {(metrics?.DiffOutputChars ?? 0)} chars  {(metrics?.DiffCellsTouched ?? 0)} cells  full={((metrics?.DiffForceFull ?? false) ? "yes" : "no")}",
            $"Focus: {(focus is null ? "<none>" : focus.GetType().Name)}",
            $"Hover: {(hover is null ? "<none>" : hover.GetType().Name)}",
        };

        var contentWidth = 0;
        foreach (var line in lines)
        {
            contentWidth = Math.Max(contentWidth, TerminalTextUtility.GetWidth(line.AsSpan()));
        }

        var width = Math.Min(maxWidth, Math.Max(3, contentWidth + 2));
        var height = Math.Min(maxHeight, Math.Max(3, lines.Count + 2));
        if (width < 3 || height < 3)
        {
            return;
        }

        var borderStyle = theme.BorderStyle(focused: true) | TextStyle.Bold;

        // The overlay fills with blank glyphs; ensure we write an explicit foreground to avoid inheriting colors
        // from the underlay when rendering the overlay text (which preserves the filled cell style).
        var backgroundStyle = theme.ForegroundTextStyle() | TextStyle.Dim;
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

        for (var i = 0; i < lines.Count && i + 1 < bottom; i++)
        {
            buffer.WriteText(1, 1 + i, lines[i].AsSpan(), Style.None);
        }
    }

    private bool DispatchKeyEvent(TerminalKeyEvent keyEvent, bool routeCommands = true)
    {
        EnsureFocusInScope();

        var args = new KeyEventArgs { RawEvent = keyEvent };

        if (routeCommands && TryHandleCommandShortcut(args))
        {
            return true;
        }

        if (FocusedElement is null || !FocusedElement.IsEnabled || !FocusedElement.IsVisible)
        {
            return false;
        }

        FocusedElement.RaiseEvent(Visual.KeyDownEvent, args);
        return args.Handled;
    }

    private static KeyGesture ToGesture(TerminalKeyEvent keyEvent)
        => keyEvent.Key != TerminalKey.Unknown
            ? new KeyGesture(keyEvent.Key, keyEvent.Modifiers)
            : new KeyGesture(keyEvent.Char ?? '\0', keyEvent.Modifiers);

    private bool TryHandleCommandShortcut(KeyEventArgs args)
    {
        var keyEvent = args.RawEvent;
        var allowGlobalCommands = ShouldConsiderGlobalCommands();

        // If a sequence is active, only consider sequence continuation (or cancellation).
        if (_pendingSequenceCount > 0)
        {
            if (keyEvent.Key == TerminalKey.Escape)
            {
                CancelPendingSequence();
                args.Handled = true;
                return true;
            }

            return TryContinueSequence(args, allowGlobalCommands);
        }

        // Single-stroke command routing uses the same focus-walk semantics as key bindings.
        if (TryExecuteGestureCommand(keyEvent, allowGlobalCommands))
        {
            args.Handled = true;
            return true;
        }

        // No direct match: check for sequence prefixes.
        var gesture = ToGesture(keyEvent);
        if (TryStartSequence(gesture, allowGlobalCommands))
        {
            args.Handled = true;
            return true;
        }

        return false;
    }

    private bool TryExecuteGestureCommand(TerminalKeyEvent keyEvent, bool allowGlobalCommands)
    {
        for (var v = FocusedElement; v is not null; v = v.Parent)
        {
            var commands = v.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd.Gesture is not { } gesture)
                {
                    continue;
                }

                if (!gesture.Matches(keyEvent))
                {
                    continue;
                }

                if (!cmd.IsVisibleFor(v) || !cmd.CanExecuteFor(v))
                {
                    return true; // gesture matched but is disabled/hidden in this context; treat as handled.
                }

                cmd.Execute(v);
                return true;
            }
        }

        if (!allowGlobalCommands)
        {
            return false;
        }

        // Global commands are evaluated last. The target is the focused element when possible.
        var globalTarget = FocusedElement ?? Root;
        if (_globalCommands is not null)
        {
            for (var i = 0; i < _globalCommands.Count; i++)
            {
                var cmd = _globalCommands[i];
                if (cmd.Gesture is not { } gesture)
                {
                    continue;
                }

                if (!gesture.Matches(keyEvent))
                {
                    continue;
                }

                if (!cmd.IsVisibleFor(globalTarget) || !cmd.CanExecuteFor(globalTarget))
                {
                    return true;
                }

                cmd.Execute(globalTarget);
                return true;
            }
        }

        return false;
    }

    private bool TryStartSequence(in KeyGesture firstGesture, bool allowGlobalCommands)
    {
        // Prefix detection uses the same ordering as execution: focused chain first, then globals.
        if (!IsSequencePrefix(firstGesture, allowGlobalCommands))
        {
            return false;
        }

        _pendingSequence[0] = firstGesture;
        _pendingSequenceCount = 1;
        _pendingSequenceTimestamp = _lastTickTimestamp;
        _pendingSequenceFocus = FocusedElement;
        return true;
    }

    private bool TryContinueSequence(KeyEventArgs args, bool allowGlobalCommands)
    {
        if (_pendingSequenceCount >= _pendingSequence.Length)
        {
            CancelPendingSequence();
            return false;
        }

        var next = ToGesture(args.RawEvent);
        _pendingSequence[_pendingSequenceCount++] = next;
        _pendingSequenceTimestamp = _lastTickTimestamp;

        var prefix = _pendingSequence.AsSpan(0, _pendingSequenceCount);

        if (TryExecuteMatchingSequence(prefix, allowGlobalCommands, out var handled))
        {
            args.Handled = handled;
            return handled;
        }

        // If the prefix matches at least one command, keep waiting.
        if (HasSequenceWithPrefix(prefix, allowGlobalCommands))
        {
            args.Handled = true;
            return true;
        }

        // No match: exit sequence mode and let normal key routing run for this key.
        CancelPendingSequence();
        return false;
    }

    private bool TryExecuteMatchingSequence(ReadOnlySpan<KeyGesture> prefix, bool allowGlobalCommands, out bool handled)
    {
        handled = false;

        for (var v = FocusedElement; v is not null; v = v.Parent)
        {
            var commands = v.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count != prefix.Length)
                {
                    continue;
                }

                if (!SequenceMatches(sequence, prefix))
                {
                    continue;
                }

                CancelPendingSequence();

                if (!cmd.IsVisibleFor(v) || !cmd.CanExecuteFor(v))
                {
                    handled = true;
                    return true;
                }

                cmd.Execute(v);
                handled = true;
                return true;
            }
        }

        if (!allowGlobalCommands)
        {
            return false;
        }

        var globalTarget = FocusedElement ?? Root;
        if (_globalCommands is not null)
        {
            for (var i = 0; i < _globalCommands.Count; i++)
            {
                var cmd = _globalCommands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count != prefix.Length)
                {
                    continue;
                }

                if (!SequenceMatches(sequence, prefix))
                {
                    continue;
                }

                CancelPendingSequence();

                if (!cmd.IsVisibleFor(globalTarget) || !cmd.CanExecuteFor(globalTarget))
                {
                    handled = true;
                    return true;
                }

                cmd.Execute(globalTarget);
                handled = true;
                return true;
            }
        }

        return false;
    }

    private bool IsSequencePrefix(in KeyGesture gesture, bool allowGlobalCommands)
    {
        for (var v = FocusedElement; v is not null; v = v.Parent)
        {
            var commands = v.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count > 0 && sequence[0].Matches(gesture) && cmd.IsVisibleFor(v))
                {
                    return true;
                }
            }
        }

        if (!allowGlobalCommands)
        {
            return false;
        }

        var globalTarget = FocusedElement ?? Root;
        if (_globalCommands is not null)
        {
            for (var i = 0; i < _globalCommands.Count; i++)
            {
                var cmd = _globalCommands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count > 0 && sequence[0].Matches(gesture) && cmd.IsVisibleFor(globalTarget))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasSequenceWithPrefix(ReadOnlySpan<KeyGesture> prefix, bool allowGlobalCommands)
    {
        for (var v = FocusedElement; v is not null; v = v.Parent)
        {
            var commands = v.Commands;
            for (var i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count < prefix.Length)
                {
                    continue;
                }

                if (SequenceMatchesPrefix(sequence, prefix) && cmd.IsVisibleFor(v))
                {
                    return true;
                }
            }
        }

        if (!allowGlobalCommands)
        {
            return false;
        }

        var globalTarget = FocusedElement ?? Root;
        if (_globalCommands is not null)
        {
            for (var i = 0; i < _globalCommands.Count; i++)
            {
                var cmd = _globalCommands[i];
                if (cmd.Sequence is not { } sequence)
                {
                    continue;
                }

                if (sequence.Count < prefix.Length)
                {
                    continue;
                }

                if (SequenceMatchesPrefix(sequence, prefix) && cmd.IsVisibleFor(globalTarget))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ShouldConsiderGlobalCommands()
    {
        // While a modal root (e.g. a popup or dialog) is active, global commands should not execute "behind" it.
        // Commands can still be surfaced and executed within the modal itself by registering them on that subtree.
        return ReferenceEquals(GetInputRoot(), Root);
    }

    private static bool SequenceMatches(KeySequence sequence, ReadOnlySpan<KeyGesture> gestures)
    {
        if (sequence.Count != gestures.Length)
        {
            return false;
        }

        for (var i = 0; i < gestures.Length; i++)
        {
            if (!sequence[i].Matches(gestures[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SequenceMatchesPrefix(KeySequence sequence, ReadOnlySpan<KeyGesture> prefix)
    {
        for (var i = 0; i < prefix.Length; i++)
        {
            if (!sequence[i].Matches(prefix[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void CancelPendingSequence()
    {
        _pendingSequenceCount = 0;
        _pendingSequenceTimestamp = 0;
        _pendingSequenceFocus = null;
    }

    private void CancelPendingSequenceIfFocusChanged()
    {
        if (_pendingSequenceCount == 0)
        {
            return;
        }

        if (!ReferenceEquals(_pendingSequenceFocus, FocusedElement))
        {
            CancelPendingSequence();
        }
    }

    private void CancelPendingSequenceIfTimedOut(long now)
    {
        if (_pendingSequenceCount == 0)
        {
            return;
        }

        // Default timeout for v1: 1.5 seconds.
        if (Stopwatch.GetElapsedTime(_pendingSequenceTimestamp, now) > TimeSpan.FromMilliseconds(1500))
        {
            CancelPendingSequence();
        }
    }

    private void EnsureInitialFocus()
    {
        EnsureFocusInScope();
    }

    private void FocusNext()
    {
        var scope = GetFocusScopeRoot();
        var focusables = EnumerateFocusables(scope).ToList();
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
        var focusables = EnumerateFocusables(scope).ToList();
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
        // Cancel multi-stroke sequences when a non-key event happens. This avoids leaving the app in a “prefix pending”
        // state if the user interacts with the UI using the mouse or the terminal is resized.
        if (ev is not TerminalKeyEvent)
        {
            CancelPendingSequence();
        }

        if (ev is TerminalResizeEvent)
        {
            _fullscreenHost?.Reset();
            _inlineHost?.HandleResize();
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

        CancelPendingSequenceIfFocusChanged();

        if (_options.ToggleDebugOverlayGesture.Matches(keyEvent))
        {
            _debugOverlayVisible = !_debugOverlayVisible;
            _debugOverlayMetrics = _debugOverlayVisible ? new DebugOverlayMetrics() : null;
            _fullscreenHost?.SetMetricsSink(_debugOverlayMetrics);
            _inlineHost?.SetMetricsSink(_debugOverlayMetrics);
            RequestRender();
            return;
        }

        var activeModal = FindActiveModalRoot(Root);

        if (_exitGesture.Matches(keyEvent))
        {
            // Allow controls to handle the exit gesture (e.g. close transient popups) before exiting the app.
            if (!DispatchKeyEvent(keyEvent, routeCommands: false))
            {
                _cts.Cancel();
            }
            return;
        }

        if (keyEvent.Key == TerminalKey.Tab)
        {
            CancelPendingSequence();
            // Transient popups should close on Tab before focus moves in the underlying UI.
            if (activeModal is Popup popup && popup.CloseOnTab)
            {
                popup.Close();
            }

            // Give the focused control (and its command chain) a chance to handle Tab.
            // This enables cases like DataGrid navigation and editors that want Tab insertion.
            if (DispatchKeyEvent(keyEvent))
            {
                return;
            }

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

        _ = DispatchKeyEvent(keyEvent);
    }

    private static global::XenoAtom.Terminal.UI.Input.KeyGesture GetDefaultExitGesture(TerminalHostKind hostKind)
        => hostKind == TerminalHostKind.Fullscreen
            ? new global::XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlQ, TerminalModifiers.Ctrl)
            : new global::XenoAtom.Terminal.UI.Input.KeyGesture(TerminalKey.Escape);

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
            while (focusTarget is not null && (!focusTarget.Focusable || !focusTarget.IsEnabled || !focusTarget.IsVisible))
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

                if (mouseEvent.Kind == TerminalMouseKind.Down
                    && mouseEvent.Button == TerminalMouseButton.Right
                    && !args.Handled)
                {
                    TryShowContextMenu(target, args.UiX, args.UiY);
                }
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

    private void TryShowContextMenu(Visual hitTarget, int uiX, int uiY)
    {
        VerifyAccess();

        if (_windowLayer is null)
        {
            return;
        }

        var activeModal = FindActiveModalRoot(Root);
        if (activeModal is not null && !ReferenceEquals(activeModal, Root))
        {
            // When a context menu is already open, a right-click closes it instead of opening a nested menu.
            if (_contextMenuPopup is not null && ReferenceEquals(activeModal, _contextMenuPopup))
            {
                _contextMenuPopup.Close();
            }

            return;
        }

        IReadOnlyList<MenuItem>? menuItems = null;

        // First, try an explicit factory in the hovered chain (nearest wins).
        for (var v = hitTarget; v is not null; v = v.Parent)
        {
            if (v.ContextMenuFactory is not { } factory)
            {
                continue;
            }

            var produced = factory(v);
            if (produced is null)
            {
                return;
            }

            menuItems = produced as IReadOnlyList<MenuItem> ?? produced.ToArray();
            break;
        }

        if (menuItems is null)
        {
            // Fallback: discover commands for the context menu surface.
            var commands = new List<ResolvedCommand>();
            CommandQuery.Collect(this, hitTarget, CommandPresentation.ContextMenu, commands);
            if (commands.Count == 0)
            {
                return;
            }

            var list = new List<MenuItem>(commands.Count);
            for (var i = 0; i < commands.Count; i++)
            {
                var resolved = commands[i];
                var cmd = resolved.Command;

                var item = new MenuItem(new Markup(cmd.LabelMarkup), cmd)
                {
                    CommandTarget = resolved.Target,
                    IsEnabled = resolved.IsEnabled,
                };

                list.Add(item);
            }

            menuItems = list;
        }

        if (menuItems.Count == 0)
        {
            return;
        }

        ShowContextMenu(hitTarget, menuItems, uiX, uiY);
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
            if (_options.InitialFocusMode == InitialFocusMode.None)
            {
                return;
            }

            // Prefer a visual explicitly marked for initial focus. This allows apps to define their
            // focus target declaratively (e.g. focus a sidebar list instead of a search box).
            FocusedElement = EnumerateFocusables(scopeRoot).FirstOrDefault(v => v.AutoFocus)
                ?? EnumerateFocusables(scopeRoot).FirstOrDefault();
            if (FocusedElement is not null)
            {
                RequestRender();
            }
        }
    }

    private static IEnumerable<Visual> EnumerateFocusables(Visual root)
    {
        // Prefer focusing leaf controls over container controls (e.g. focus a TreeView inside a ScrollViewer rather
        // than the ScrollViewer itself). Containers remain reachable via Tab because we still yield them after their
        // descendants.
        for (var i = 0; i < root.GetChildrenCount(); i++)
        {
            var child = root.GetChildUnsafe(i);
            foreach (var nested in EnumerateFocusables(child))
            {
                yield return nested;
            }
        }

        if (root.Focusable && root.IsVisible && root.IsEnabled)
        {
            yield return root;
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

internal sealed class BindingDependencyIndex
{
    private readonly Dictionary<Binding, HashSet<Visual>> _bindingToVisuals = new(BindingReferenceComparer.Instance);
    private readonly Dictionary<Visual, HashSet<Binding>> _visualToBindings = new();

    public void UpdateBindingReadsForVisual(Visual visual, IReadOnlyCollection<Binding> reads)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(reads);

        if (!_visualToBindings.TryGetValue(visual, out var old))
        {
            old = new HashSet<Binding>(BindingReferenceComparer.Instance);
            _visualToBindings.Add(visual, old);
        }
        else if (old.SetEquals(reads))
        {
            return;
        }

        // Remove old bindings no longer present.
        foreach (var binding in old)
        {
            if (Contains(reads, binding))
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
        foreach (var binding in reads)
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
        foreach (var binding in reads)
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
