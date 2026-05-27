// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
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
    private readonly Func<Rune, bool> _wideRuneResolver;
    private readonly WindowLayer? _windowLayer;
    private Visual? _activeTooltipWindow;
    private readonly InlineInteractiveHost? _inlineHost;
    private readonly FullscreenHost? _fullscreenHost;
    private readonly GraphicsCommandBuffer _graphicsCommands = new();
    private readonly GraphicsRenderContext _graphicsRenderContext;
    private readonly Dictionary<Visual, IGraphicsRenderableVisual> _graphicsRenderableVisuals = new();
    private readonly AsyncAutoResetEvent _wakeUp = new();
    private readonly AutoResetEvent _wakeSignal = new(false);
    private readonly CancellationTokenSource _cts = new();
    private readonly ITerminalLoopClock _loopClock;
    private readonly ITerminalLoopWaitBackend _loopWaitBackend;

    private bool _renderRequested = true;
    private Visual? _pointerCapture;
    private Visual? _hoveredElement;
    private readonly List<Visual> _focusablesScratch = new(16);
    private List<Visual>? _hoveredPath;
    private List<Visual>? _hoveredPathScratch;
    private int? _inlineLiveRegionTopRow;
    private bool _debugOverlayVisible;
    private DebugOverlayMetrics? _debugOverlayMetrics;
    private int _renderFrameIndex;
    private Task? _runTask;
    private CellBuffer? _renderBuffer;
    private readonly CellBufferRegionSnapshot _debugOverlaySnapshot = new();
    private Visual? _focusedElement;
    private Visual? _selectionOwnerElement;
    private ISelectionOwner? _selectionOwner;
    private Func<TerminalRunningContext, ValueTask<TerminalLoopResult>>? _onUpdate;
    private TerminalRunningContext? _updateContext;
    private readonly AnsiBuilder _updateOutputBuilder = new(initialCapacity: 4096);
    private bool _inlineRemoveOnEnd;
    private Dictionary<string, AnsiStyle>? _previousMarkupStyles;

    private Popup? _contextMenuPopup;

    private BindableList<Command>? _globalCommands;

    private long _lastTickTimestamp;
    private int _wakeRequested;
    private int _pendingWakeReasons;
    private readonly KeyGesture[] _pendingSequence = new KeyGesture[4];
    private int _pendingSequenceCount;
    private long _pendingSequenceTimestamp;
    private Visual? _pendingSequenceFocus;

    private readonly List<IAnimatedVisual> _animatedVisuals = new();
    private long _nextAnimationTick = long.MaxValue;
    private long _nextActiveUpdateTick = long.MaxValue;

    private readonly HashSet<Binding> _pendingBindingWrites = new(BindingReferenceComparer.Instance);

    private bool _pendingRenderHasLayoutImpact;
    private bool _layoutStabilizationRequested;
    private bool _pendingRenderDirtyRectValid;
    private Rectangle _pendingRenderDirtyRect;
    private bool _forceNextFullRepaint;
    private bool _isRendering;
    private bool _deferredFullRepaintRequest;
    private bool _graphicsDisplayListDirty;

    private int _lastRenderWidth;
    private int _lastRenderHeight;

    private static readonly AsyncLocal<int> UpdateCallbackDepth = new();
    private static readonly TimeSpan DefaultActiveFrameInterval = TimeSpan.FromMilliseconds(15);

    private Task<TerminalLoopResult>? _pendingUpdateTask;
    private readonly System.Collections.Concurrent.ConcurrentQueue<TerminalEvent> _pendingTerminalEvents = new();
    private CancellationTokenSource? _inputRelayCts;
    private Task? _inputRelayTask;
    private ExceptionDispatchInfo? _inputRelayFailure;
    private IDisposable? _wideRuneResolverScope;

    private readonly record struct PendingAction(Action Action, bool CaptureFlowOutput);

    internal enum DependencyKind
    {
        DynamicUpdate = 0,
        PrepareChildren = 1,
        Measure = 2,
        Arrange = 3,
        Render = 4,
        GraphicsRender = 5,
    }

    private enum SceneRenderMode
    {
        None = 0,
        Dirty = 1,
        Full = 2,
    }

    private readonly record struct DebugOverlayLayout(Rectangle Rect, List<string> Lines, Style BorderStyle, Style BackgroundStyle);

    private readonly BindingDependencyIndex _dynamicUpdateIndex = new();
    private readonly BindingDependencyIndex _prepareChildrenIndex = new();
    private readonly BindingDependencyIndex _measureIndex = new();
    private readonly BindingDependencyIndex _arrangeIndex = new();
    private readonly BindingDependencyIndex _renderIndex = new();
    private readonly BindingDependencyIndex _graphicsRenderIndex = new();

    TerminalApp? IVisualElement.App => this;
    internal DebugOverlayMetrics? DebugOverlayMetrics => _debugOverlayMetrics;

    /// <summary>
    /// Gets the predicate used to widen additional runes to two terminal cells for this app.
    /// </summary>
    public Func<Rune, bool> WideRuneResolver => _wideRuneResolver;

    /// <summary>
    /// Gets the command id used by the built-in application quit command.
    /// </summary>
    /// <remarks>
    /// Applications can use this id to replace or re-register the default quit command through
    /// <see cref="GlobalCommands"/>, <see cref="AddGlobalCommand(Command)"/>, and <see cref="RemoveGlobalCommand(string)"/>.
    /// </remarks>
    public static string DefaultQuitCommandId { get; } = "TerminalApp.Quit";

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
        : this(root, terminal, options, loopClock: null, loopWaitBackend: null)
    {
    }

    internal TerminalApp(Visual root, TerminalInstance? terminal, TerminalAppOptions? options, ITerminalLoopClock? loopClock, ITerminalLoopWaitBackend? loopWaitBackend)
    {
        ArgumentNullException.ThrowIfNull(root);
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _options = options ?? new TerminalAppOptions();
        _wideRuneResolver = _options.WideRuneResolver ?? TerminalWideRuneResolvers.Default;
        _loopClock = loopClock ?? StopwatchTerminalLoopClock.Instance;
        _loopWaitBackend = loopWaitBackend ?? TerminalLoopWaitBackendFactory.CreateDefault(_loopClock);
        _graphicsRenderContext = new GraphicsRenderContext(_graphicsCommands);
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

        var exitGesture = _options.ExitGesture ?? GetDefaultExitGesture(_options.HostKind);

        AddGlobalCommand(new Command
        {
            Id = DefaultQuitCommandId,
            LabelMarkup = "Quit",
            DescriptionMarkup = "Quit the application.",
            Gesture = exitGesture,
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

    /// <summary>
    /// Gets a value indicating whether this app has a graphics presenter that can present for the current terminal.
    /// </summary>
    /// <remarks>
    /// Graphics-capable visuals can use this value to decide whether to render fallback text/content instead of emitting
    /// graphics display-list commands.
    /// </remarks>
    public bool IsGraphicsPresentationEnabled
    {
        get
        {
            var presenter = _options.GraphicsPresenter;
            return presenter is not null && presenter.CanPresent(_terminal.Graphics.Capabilities);
        }
    }

    internal GraphicsCommandBuffer GraphicsCommands => _graphicsCommands;

    internal int GraphicsRenderableVisualCount => _graphicsRenderableVisuals.Count;

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

                if (_selectionOwnerElement is not null && value is not null
                    && !IsInScope(value, _selectionOwnerElement)
                    && !IsInScope(_selectionOwnerElement, value))
                {
                    ClearSelectionOwner();
                }
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
        SignalWakeUp(TerminalLoopWakeReason.Post);
    }

    internal Visual? SelectionOwnerElement => _selectionOwnerElement;

    internal void ClearSelectionOwnerIfMatches(Visual visual)
    {
        if (ReferenceEquals(_selectionOwnerElement, visual))
        {
            ClearSelectionOwner();
        }
    }

    private bool TryCopyActiveSelection()
    {
        if (_selectionOwner is null || !_selectionOwner.HasSelection)
        {
            return false;
        }

        if (!_selectionOwner.TryCopySelection(out var text) || string.IsNullOrEmpty(text))
        {
            return false;
        }

        Terminal.Clipboard.TrySetText(text);
        return true;
    }

    private void ClearSelectionOwner()
    {
        if (_selectionOwner is null)
        {
            _selectionOwnerElement = null;
            return;
        }

        _selectionOwner.ClearSelection();
        _selectionOwner = null;
        _selectionOwnerElement = null;
        RequestRender();
    }

    private void SetSelectionOwner(Visual? element, ISelectionOwner? owner)
    {
        if (owner is not null && !owner.IsSelectable)
        {
            owner = null;
            element = null;
        }

        if (ReferenceEquals(_selectionOwnerElement, element))
        {
            return;
        }

        if (_selectionOwner is not null)
        {
            _selectionOwner.ClearSelection();
        }

        _selectionOwnerElement = element;
        _selectionOwner = owner;
        RequestRender();
    }

    private void UpdateSelectionOwnerFromPointer(Visual? target)
    {
        if (target is null)
        {
            ClearSelectionOwner();
            return;
        }

        for (var v = target; v is not null; v = v.Parent)
        {
            if (v is ISelectionOwner owner && owner.IsSelectable && v.IsVisible && v.IsEnabled)
            {
                SetSelectionOwner(v, owner);
                return;
            }
        }

        ClearSelectionOwner();
    }

    /// <summary>
    /// Requests the app loop to stop.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
        SignalWakeUp(TerminalLoopWakeReason.Shutdown);
    }

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
        _options.GraphicsPresenter?.Dispose();
        _loopWaitBackend.Dispose();
        _wakeSignal.Dispose();
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
            var buffer = new CellBuffer(width, Math.Max(1, renderRoot.DesiredSize.Height), _wideRuneResolver);
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
    /// <exception cref="InvalidOperationException">Thrown when the terminal input relay terminates unexpectedly while the app is running.</exception>
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
        Interlocked.Exchange(ref _wakeRequested, 0);
        ThrowIfInputRelayFailed();

        _lastTickTimestamp = timestamp ?? _loopClock.GetTimestamp();
        var metrics = _debugOverlayMetrics;
        metrics?.BeginTick(_lastTickTimestamp);
        var wakeReasons = (TerminalLoopWakeReason)Interlocked.Exchange(ref _pendingWakeReasons, 0);
        if (wakeReasons != TerminalLoopWakeReason.None)
        {
            metrics?.RecordWake(wakeReasons);
        }

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

        while (TryDequeueTerminalEvent(out var ev))
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
                var updateStart = metrics is null ? 0 : _loopClock.GetTimestamp();
                _updateContext!.Timestamp = timestamp ?? _loopClock.GetTimestamp();

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
                            AttachPendingUpdateWake(_pendingUpdateTask);
                        }
                    }
                }
                finally
                {
                    UpdateCallbackDepth.Value = previousDepth;
                }

                if (metrics is not null)
                {
                    userUpdateTicks = Math.Max(0, _loopClock.GetTimestamp() - updateStart);
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

        if (_debugOverlayVisible)
        {
            _renderRequested = true;
        }

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
    /// <exception cref="InvalidOperationException">Thrown when the terminal input relay terminates unexpectedly while the app is running.</exception>
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
            StartInputRelay(token);

            while (!token.IsCancellationRequested)
            {
                Tick();
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var now = _loopClock.GetTimestamp();
                var deadline = ComputeNextRunDeadline(now);
                if (deadline <= now)
                {
                    if (deadline != long.MaxValue)
                    {
                        _debugOverlayMetrics?.RecordLateDeadline(Math.Max(0, now - deadline));
                        Interlocked.Or(ref _pendingWakeReasons, (int)TerminalLoopWakeReason.Deadline);
                    }
                    continue;
                }

                if (Volatile.Read(ref _wakeRequested) != 0)
                {
                    continue;
                }

                DrainWakeSignal();
                if (Volatile.Read(ref _wakeRequested) != 0)
                {
                    continue;
                }

                var waitResult = _loopWaitBackend.WaitUntil(deadline, _wakeSignal, token);
                if (waitResult == TerminalLoopWaitResult.Canceled)
                {
                    break;
                }

                if (waitResult == TerminalLoopWaitResult.Deadline)
                {
                    Interlocked.Or(ref _pendingWakeReasons, (int)TerminalLoopWakeReason.Deadline);
                }
            }
        }
        finally
        {
            StopInputRelay();
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
        _wideRuneResolverScope = TerminalTextUtility.PushWideRuneResolver(_wideRuneResolver);
        _options.GraphicsPresenter?.Reset();

        Root.AttachToApp(this);
        BindingManager.Current.ValueChanged += OnValueChanged;
        _updateContext = new TerminalRunningContext(this, _terminal, _options.HostKind);
        _inlineRemoveOnEnd = false;
        _pendingUpdateTask = null;
        _nextActiveUpdateTick = long.MaxValue;
        _updateOutputBuilder.Clear();
        _inputRelayFailure = null;

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
            _nextActiveUpdateTick = long.MaxValue;
            _updateOutputBuilder.Clear();

            try
            {
                // Could fail if the terminal was disposed
                _terminal.MarkupStyles = _previousMarkupStyles;
            }
            catch
            {
                // ignore
            }

            _previousMarkupStyles = null;

            _options.GraphicsPresenter?.Reset();

            _wideRuneResolverScope?.Dispose();
            _wideRuneResolverScope = null;

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

    private void RequestRender()
    {
        _renderRequested = true;
        SignalWakeUp(TerminalLoopWakeReason.Render);
    }

    internal void RequestLayoutStabilization()
    {
        _layoutStabilizationRequested = true;
        _pendingRenderHasLayoutImpact = true;
        RequestRender();
    }

    /// <summary>
    /// Requests a render pass so graphics-only visuals can refresh their display-list commands.
    /// </summary>
    /// <remarks>
    /// This does not invalidate layout or mark any text cells dirty. It is intended for real-time graphics sources whose
    /// latest frame changed while their cell footprint stayed the same.
    /// </remarks>
    public void RequestGraphicsRender()
    {
        VerifyAccess();
        _graphicsDisplayListDirty = true;
        RequestRender();
    }

    /// <summary>
    /// Requests a full text repaint on the next render pass.
    /// </summary>
    /// <remarks>
    /// Graphics presenters can use this after clearing streamed graphics regions so the underlying text/background cells
    /// are restored on the following frame.
    /// </remarks>
    public void RequestFullRender()
    {
        VerifyAccess();
        _fullscreenHost?.Reset();
        if (_isRendering)
        {
            _deferredFullRepaintRequest = true;
        }
        else
        {
            _forceNextFullRepaint = true;
        }

        RequestRender();
    }

    internal void RequestAnimation()
    {
        _nextAnimationTick = 0;
        SignalWakeUp(TerminalLoopWakeReason.Animation);
    }

    private bool TryDequeueTerminalEvent(out TerminalEvent ev)
    {
        if (_inputRelayTask is not null)
        {
            return _pendingTerminalEvents.TryDequeue(out ev!);
        }

        return _terminal.TryReadEvent(out ev!);
    }

    private void StartInputRelay(CancellationToken token)
    {
        if (_inputRelayTask is not null)
        {
            return;
        }

        _inputRelayFailure = null;
        _inputRelayCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var relayToken = _inputRelayCts.Token;
        _inputRelayTask = Task.Run(async () =>
        {
            try
            {
                while (!relayToken.IsCancellationRequested)
                {
                    var ev = await _terminal.ReadEventAsync(relayToken).ConfigureAwait(false);
                    _pendingTerminalEvents.Enqueue(ev);
                    SignalWakeUp(TerminalLoopWakeReason.Input);
                }
            }
            catch (Exception ex)
            {
                if (!relayToken.IsCancellationRequested)
                {
                    FailInputRelay(ex);
                }
            }
        }, CancellationToken.None);
    }

    private void StopInputRelay()
    {
        _inputRelayCts?.Cancel();

        if (_inputRelayTask is not null)
        {
            try
            {
                _inputRelayTask.GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is OperationCanceledException or ChannelClosedException)
            {
                // Ignore shutdown races while stopping the relay.
            }
        }

        _inputRelayTask = null;
        _inputRelayCts?.Dispose();
        _inputRelayCts = null;

        while (_pendingTerminalEvents.TryDequeue(out _))
        {
        }
    }

    private void FailInputRelay(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Interlocked.CompareExchange(ref _inputRelayFailure, ExceptionDispatchInfo.Capture(exception), comparand: null);
        SignalWakeUp(TerminalLoopWakeReason.Input);
    }

    private void ThrowIfInputRelayFailed()
    {
        var failure = Interlocked.Exchange(ref _inputRelayFailure, null);
        if (failure is null)
        {
            return;
        }

        throw new InvalidOperationException(
            "The terminal input relay terminated unexpectedly, so the application can no longer receive terminal events.",
            failure.SourceException);
    }

    private void AttachPendingUpdateWake(Task<TerminalLoopResult> pendingTask)
    {
        pendingTask.ContinueWith(static (_, state) =>
        {
            ((TerminalApp)state!).SignalWakeUp(TerminalLoopWakeReason.AsyncUpdate);
        }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void SignalWakeUp(TerminalLoopWakeReason reason)
    {
        Interlocked.Or(ref _pendingWakeReasons, (int)reason);
        Volatile.Write(ref _wakeRequested, 1);
        _wakeUp.Set();
        try
        {
            _wakeSignal.Set();
        }
        catch (ObjectDisposedException)
        {
            // A late background completion can race with disposal during shutdown.
        }
    }

    private void DrainWakeSignal()
    {
        while (_wakeSignal.WaitOne(0))
        {
        }
    }

    private long ComputeNextRunDeadline(long now)
    {
        if (_options.LoopMode == TerminalLoopMode.Auto)
        {
            if (_nextAnimationTick == 0)
            {
                return now;
            }

            var activeDeadline = long.MaxValue;
            if (_onUpdate is not null && _pendingUpdateTask is null)
            {
                var activeFrameTicks = TerminalLoopScheduler.ToStopwatchTicks(DefaultActiveFrameInterval, _loopClock.Frequency);
                activeDeadline = TerminalLoopScheduler.ComputeNextActiveDeadline(_lastTickTimestamp, now, _nextActiveUpdateTick, activeFrameTicks);
                _nextActiveUpdateTick = activeDeadline;
            }
            else
            {
                _nextActiveUpdateTick = long.MaxValue;
            }

            var animationDeadline = _nextAnimationTick == long.MaxValue
                ? long.MaxValue
                : (_nextAnimationTick <= now ? now : _nextAnimationTick);

            if (activeDeadline == long.MaxValue)
            {
                return animationDeadline;
            }

            if (animationDeadline == long.MaxValue)
            {
                return activeDeadline;
            }

            return Math.Min(activeDeadline, animationDeadline);
        }

        var pollingSliceTicks = TerminalLoopScheduler.ToStopwatchTicks(_options.UpdateWaitDuration, _loopClock.Frequency);
        return TerminalLoopScheduler.ComputePollingDeadline(now, _nextAnimationTick, pollingSliceTicks);
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
        => ShowWindow(window, ownerWindow: null);

    internal void ShowWindow(Visual window, Visual? ownerWindow)
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

        _windowLayer.AddWindow(window, ownerWindow);

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
        var ownerWindow = tooltipWindow is TooltipWindow tooltip ? ResolveWindowOwner(tooltip.Anchor) : null;
        ShowWindow(tooltipWindow, ownerWindow);
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

        CloseOwnedWindows(window);
        return _windowLayer.RemoveWindow(window);
    }

    internal Visual? ResolveWindowOwner(Visual? visual)
    {
        if (_windowLayer is null || visual is null)
        {
            return null;
        }

        for (var current = visual; current is not null; current = current.Parent)
        {
            if (!ReferenceEquals(current.Parent, _windowLayer))
            {
                continue;
            }

            return ReferenceEquals(current, _windowLayer.Content) ? null : current;
        }

        return null;
    }

    private void CloseOwnedWindows(Visual owner)
    {
        if (_windowLayer is null)
        {
            return;
        }

        var ownedWindows = _windowLayer.GetOwnedWindows(owner);
        for (var i = 0; i < ownedWindows.Length; i++)
        {
            switch (ownedWindows[i])
            {
                case Popup popup:
                    popup.Close();
                    break;
                case Dialog dialog:
                    dialog.Close();
                    break;
                case TooltipWindow tooltip:
                    CloseTooltipWindow(tooltip);
                    break;
                default:
                    _windowLayer.RemoveWindow(ownedWindows[i]);
                    break;
            }
        }
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

        var popup = ContextMenuService.CreatePopup(target, menuItems, uiX, uiY);
        _contextMenuPopup = popup;

        popup.Closed((_, _) =>
        {
            if (ReferenceEquals(_contextMenuPopup, popup))
            {
                _contextMenuPopup = null;
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

    internal void RegisterGraphicsRenderableVisual(Visual visual, IGraphicsRenderableVisual graphics)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(graphics);

        if (!_graphicsRenderableVisuals.TryAdd(visual, graphics))
        {
            return;
        }

        for (var current = visual; current is not null; current = current.Parent)
        {
            current.IncrementGraphicsRenderableSubtreeCount();
        }

        _graphicsDisplayListDirty = true;
    }

    internal void UnregisterGraphicsRenderableVisual(Visual visual, IGraphicsRenderableVisual graphics)
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(graphics);

        if (!_graphicsRenderableVisuals.TryGetValue(visual, out var registered) || !ReferenceEquals(registered, graphics))
        {
            return;
        }

        _graphicsRenderableVisuals.Remove(visual);
        for (var current = visual; current is not null; current = current.Parent)
        {
            current.DecrementGraphicsRenderableSubtreeCount();
        }

        _graphicsRenderIndex.Remove(visual);
        _graphicsDisplayListDirty = true;
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
                var visualChanged = visual.AdvanceAnimation(now);
                changed |= visualChanged;
                if (visualChanged)
                {
                    if (visual is Visual renderVisual)
                    {
                        AddRenderDirtyRect(renderVisual);
                    }
                    else
                    {
                        _pendingRenderDirtyRectValid = false;
                    }
                }
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
            case DependencyKind.GraphicsRender:
                _graphicsRenderIndex.UpdateBindingReadsForVisual(visual, reads);
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
        _graphicsRenderIndex.Remove(visual);
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
                    AddRenderDirtyRect(v);
                }
            }

            if (_graphicsRenderIndex.TryGetVisuals(binding, out var graphicsVisuals))
            {
                _graphicsDisplayListDirty |= graphicsVisuals.Count > 0;
            }
        }

        _pendingBindingWrites.Clear();
        _renderRequested = true;
    }

    private void AddRenderDirtyRect(Visual visual)
    {
        var bounds = visual.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // Expand by 1 cell horizontally to reduce artifacts with wide glyphs clipped at region boundaries.
        var x = Math.Max(0, bounds.X - 1);
        var right = Math.Min(LayoutConstants.MaxFinite, bounds.Right + 1);
        var expanded = new Rectangle(x, bounds.Y, Math.Max(0, right - x), bounds.Height);

        _debugOverlayMetrics?.AddSceneDirtyRect(expanded);

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

    private void RunRootLayoutPasses(
        in LayoutConstraints constraints,
        int arrangeWidth,
        int arrangeHeight,
        bool arrangeHeightFromDesiredSize,
        DebugOverlayMetrics? metrics,
        out bool layoutProducedWrites)
    {
        const int MaxLayoutPasses = 4;

        layoutProducedWrites = _pendingRenderHasLayoutImpact;
        var measureTicks = 0L;
        var arrangeTicks = 0L;

        for (var pass = 0; pass < MaxLayoutPasses; pass++)
        {
            // Some virtualized controls can discover during arrange that their measured extent changed (for example
            // a realized document block whose attached size differs from its cached measured size). Those controls
            // request a same-frame stabilization pass so rendering does not expose the intermediate layout.
            _layoutStabilizationRequested = false;

            if (metrics is not null)
            {
                var t0 = Stopwatch.GetTimestamp();
                Root.Measure(constraints);
                measureTicks += Math.Max(0, Stopwatch.GetTimestamp() - t0);

                t0 = Stopwatch.GetTimestamp();
                var finalHeight = arrangeHeightFromDesiredSize ? Root.DesiredSize.Height : arrangeHeight;
                Root.Arrange(new Rectangle(0, 0, arrangeWidth, finalHeight));
                arrangeTicks += Math.Max(0, Stopwatch.GetTimestamp() - t0);
            }
            else
            {
                Root.Measure(constraints);
                var finalHeight = arrangeHeightFromDesiredSize ? Root.DesiredSize.Height : arrangeHeight;
                Root.Arrange(new Rectangle(0, 0, arrangeWidth, finalHeight));
            }

            var passProducedWrites = _pendingBindingWrites.Count > 0;
            layoutProducedWrites |= passProducedWrites;
            // Ensure any bindings updated during layout are processed before rendering (e.g. Bounds from Arrange).
            if (passProducedWrites)
            {
                ProcessBindingWrites();
            }

            if (!_layoutStabilizationRequested)
            {
                break;
            }
        }

        if (metrics is not null)
        {
            metrics.RenderMeasureTicks = measureTicks;
            metrics.RenderArrangeTicks = arrangeTicks;
        }
    }

    private void Render()
    {
        _isRendering = true;
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

            var buffer = EnsureRenderBuffer(width, height);
            var baseStyle = Root.GetTheme().BaseTextStyle();
            var layoutRequired =
                _forceNextFullRepaint ||
                width != _lastRenderWidth ||
                height != _lastRenderHeight ||
                _pendingRenderHasLayoutImpact;

            var layoutProducedWrites = false;
            if (layoutRequired)
            {
                RunRootLayoutPasses(
                    new LayoutConstraints(0, width, 0, height),
                    width,
                    height,
                    arrangeHeightFromDesiredSize: false,
                    metrics,
                    out layoutProducedWrites);
            }

            var wantsCursor = TryGetDesiredCursor(out var cursorX, out var cursorY);
            var dirtyRect = _pendingRenderDirtyRectValid ? ClampToViewport(_pendingRenderDirtyRect, width, height) : default;
            if (dirtyRect.Width > 0 && dirtyRect.Height > 0)
            {
                metrics?.AddSceneDirtyRect(dirtyRect);
            }

            var fullRepaint =
                _forceNextFullRepaint ||
                width != _lastRenderWidth ||
                height != _lastRenderHeight ||
                layoutProducedWrites ||
                _pendingRenderHasLayoutImpact;

            var sceneRenderMode = DetermineSceneRenderMode(fullRepaint, dirtyRect);

            _lastRenderWidth = width;
            _lastRenderHeight = height;
            UpdateSceneMetrics(metrics, sceneRenderMode, sceneRenderMode == SceneRenderMode.Full ? new Rectangle(0, 0, width, height) : dirtyRect);
            metrics?.FinalizeSceneFrame(renderStartTimestamp);
            metrics?.SetOverlayFrame(_debugOverlayVisible, overlayOnlyFrame: _debugOverlayVisible && sceneRenderMode == SceneRenderMode.None);

            RenderScene(buffer, baseStyle, metrics, sceneRenderMode, dirtyRect);
            var viewportBounds = new Rectangle(0, 0, width, height);
            CollectGraphicsCommands(viewportBounds, metrics);
            var textRepaintBounds = GetTextRepaintBounds(sceneRenderMode, viewportBounds, dirtyRect);
            var graphicsContext = CreateGraphicsPresentContext(viewportBounds, TerminalHostKind.Fullscreen, sceneRenderMode, textRepaintBounds);
            var bufferedGraphicsPresenter = _options.GraphicsPresenter as IBufferedTerminalGraphicsPresenter;
            var hasGraphicsFrameOutput = HasPendingGraphicsOutput(bufferedGraphicsPresenter, graphicsContext, metrics);

            var overlayComposited = _debugOverlayVisible && ComposeDebugOverlay(buffer, metrics);
            try
            {
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    _fullscreenHost!.Render(buffer, wantsCursor, cursorX, cursorY, hasGraphicsFrameOutput, writer => PresentGraphicsCommands(bufferedGraphicsPresenter!, graphicsContext, writer, metrics));
                    if (bufferedGraphicsPresenter is null)
                    {
                        PresentGraphicsCommands(graphicsContext, metrics);
                    }
                    metrics.RenderHostTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                    metrics.EndRenderFrame(renderStartTimestamp, Stopwatch.GetTimestamp());
                }
                else
                {
                    _fullscreenHost!.Render(buffer, wantsCursor, cursorX, cursorY, hasGraphicsFrameOutput, writer => PresentGraphicsCommands(bufferedGraphicsPresenter!, graphicsContext, writer, metrics: null));
                    if (bufferedGraphicsPresenter is null)
                    {
                        PresentGraphicsCommands(graphicsContext, metrics: null);
                    }
                }
            }
            finally
            {
                if (overlayComposited)
                {
                    _debugOverlaySnapshot.Restore(buffer);
                }
            }

            _pendingRenderHasLayoutImpact = false;
            _pendingRenderDirtyRectValid = false;
            CompleteRenderInvalidationState();
            return;
        }

        {
            var width = Math.Max(1, _terminal.Size.Columns);
            var viewportHeight = Math.Max(1, _terminal.Size.Rows);
            var stretchRootToViewport = Root.VerticalAlignment == Align.Stretch;

            var layoutRequired =
                _forceNextFullRepaint ||
                width != _lastRenderWidth ||
                viewportHeight != _lastRenderHeight ||
                _pendingRenderHasLayoutImpact;

            var layoutProducedWrites = false;
            if (layoutRequired)
            {
                RunRootLayoutPasses(
                    new LayoutConstraints(0, width, 0, stretchRootToViewport ? viewportHeight : LayoutConstants.Infinite),
                    width,
                    viewportHeight,
                    arrangeHeightFromDesiredSize: !stretchRootToViewport,
                    metrics,
                    out layoutProducedWrites);
            }

            var wantsCursor = TryGetDesiredCursor(out var cursorX, out var cursorY);

            var bufferHeight = stretchRootToViewport ? viewportHeight : Math.Max(1, Root.DesiredSize.Height);
            var buffer = EnsureRenderBuffer(width, bufferHeight);
            var baseStyle = Root.GetTheme().BaseTextStyle();
            var dirtyRect = _pendingRenderDirtyRectValid ? ClampToViewport(_pendingRenderDirtyRect, width, buffer.Height) : default;
            if (dirtyRect.Width > 0 && dirtyRect.Height > 0)
            {
                metrics?.AddSceneDirtyRect(dirtyRect);
            }

            var fullRepaint =
                _forceNextFullRepaint ||
                width != _lastRenderWidth ||
                viewportHeight != _lastRenderHeight ||
                layoutProducedWrites ||
                _pendingRenderHasLayoutImpact;

            var sceneRenderMode = DetermineSceneRenderMode(fullRepaint, dirtyRect);

            _lastRenderWidth = width;
            _lastRenderHeight = viewportHeight;
            UpdateSceneMetrics(metrics, sceneRenderMode, sceneRenderMode == SceneRenderMode.Full ? new Rectangle(0, 0, width, buffer.Height) : dirtyRect);
            metrics?.FinalizeSceneFrame(renderStartTimestamp);
            metrics?.SetOverlayFrame(_debugOverlayVisible, overlayOnlyFrame: _debugOverlayVisible && sceneRenderMode == SceneRenderMode.None);

            RenderScene(buffer, baseStyle, metrics, sceneRenderMode, dirtyRect);
            var viewportBounds = new Rectangle(0, 0, width, buffer.Height);
            CollectGraphicsCommands(viewportBounds, metrics);
            var textRepaintBounds = GetTextRepaintBounds(sceneRenderMode, viewportBounds, dirtyRect);
            var graphicsViewportBounds = new Rectangle(0, _inlineHost!.LiveRegionTopRow.GetValueOrDefault(), width, buffer.Height);
            var graphicsContext = CreateGraphicsPresentContext(graphicsViewportBounds, TerminalHostKind.Inline, sceneRenderMode, textRepaintBounds);
            var bufferedGraphicsPresenter = _options.GraphicsPresenter as IBufferedTerminalGraphicsPresenter;
            var hasGraphicsFrameOutput = HasPendingGraphicsOutput(bufferedGraphicsPresenter, graphicsContext, metrics);

            var overlayComposited = _debugOverlayVisible && ComposeDebugOverlay(buffer, metrics);
            try
            {
                if (metrics is not null)
                {
                    var t0 = Stopwatch.GetTimestamp();
                    _inlineHost.Render(buffer, wantsCursor, cursorX, cursorY, hasGraphicsFrameOutput, writer => PresentGraphicsCommands(bufferedGraphicsPresenter!, graphicsContext, writer, metrics));
                    if (bufferedGraphicsPresenter is null)
                    {
                        PresentGraphicsCommands(graphicsContext, metrics);
                    }
                    metrics.RenderHostTicks = Math.Max(0, Stopwatch.GetTimestamp() - t0);
                    metrics.EndRenderFrame(renderStartTimestamp, Stopwatch.GetTimestamp());
                }
                else
                {
                    _inlineHost.Render(buffer, wantsCursor, cursorX, cursorY, hasGraphicsFrameOutput, writer => PresentGraphicsCommands(bufferedGraphicsPresenter!, graphicsContext, writer, metrics: null));
                    if (bufferedGraphicsPresenter is null)
                    {
                        PresentGraphicsCommands(graphicsContext, metrics: null);
                    }
                }
            }
            finally
            {
                if (overlayComposited)
                {
                    _debugOverlaySnapshot.Restore(buffer);
                }
            }

            _inlineLiveRegionTopRow = _inlineHost.LiveRegionTopRow;

            _pendingRenderHasLayoutImpact = false;
            _pendingRenderDirtyRectValid = false;
            CompleteRenderInvalidationState();
        }

        _isRendering = false;
    }

    private void CompleteRenderInvalidationState()
    {
        _forceNextFullRepaint = _deferredFullRepaintRequest;
        _deferredFullRepaintRequest = false;
        _isRendering = false;
    }

    private static SceneRenderMode DetermineSceneRenderMode(bool fullRepaint, in Rectangle dirtyRect)
    {
        if (fullRepaint)
        {
            return SceneRenderMode.Full;
        }

        return dirtyRect.Width > 0 && dirtyRect.Height > 0
            ? SceneRenderMode.Dirty
            : SceneRenderMode.None;
    }

    private static void UpdateSceneMetrics(DebugOverlayMetrics? metrics, SceneRenderMode sceneRenderMode, in Rectangle repaintRect)
    {
        if (metrics is null)
        {
            return;
        }

        metrics.SetSceneFullRepaint(sceneRenderMode == SceneRenderMode.Full);
        metrics.SetSceneRepaintRect(sceneRenderMode == SceneRenderMode.None ? default : repaintRect);
    }

    private void RenderScene(CellBuffer buffer, Style baseStyle, DebugOverlayMetrics? metrics, SceneRenderMode sceneRenderMode, in Rectangle dirtyRect)
    {
        switch (sceneRenderMode)
        {
            case SceneRenderMode.None:
                return;
            case SceneRenderMode.Full:
                buffer.Clear(baseStyle);
                break;
            case SceneRenderMode.Dirty:
                buffer.PushClip(dirtyRect);
                buffer.ClearCurrentClip(baseStyle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sceneRenderMode));
        }

        try
        {
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
        finally
        {
            if (sceneRenderMode == SceneRenderMode.Dirty)
            {
                buffer.PopClip();
            }
        }
    }

    private void CollectGraphicsCommands(in Rectangle viewportBounds, DebugOverlayMetrics? metrics)
    {
        _graphicsRenderContext.BeginFrame();

        if (Root.GraphicsRenderableSubtreeCount <= 0)
        {
            _graphicsDisplayListDirty = false;
            metrics?.RecordGraphicsCommandCollection(0, 0);
            return;
        }

        if (metrics is not null)
        {
            var t0 = Stopwatch.GetTimestamp();
            CollectGraphicsCommandsRecursive(Root, viewportBounds);
            metrics.RecordGraphicsCommandCollection(_graphicsCommands.Count, Math.Max(0, Stopwatch.GetTimestamp() - t0));
        }
        else
        {
            CollectGraphicsCommandsRecursive(Root, viewportBounds);
        }

        _graphicsDisplayListDirty = false;
    }

    private void CollectGraphicsCommandsRecursive(Visual visual, in Rectangle clipBounds)
    {
        if (visual.GraphicsRenderableSubtreeCount <= 0)
        {
            return;
        }

        if (!visual.IsVisible)
        {
            return;
        }

        var bounds = visual.Bounds;
        var effectiveClip = Intersect(clipBounds, bounds);
        if (effectiveClip.Width <= 0 || effectiveClip.Height <= 0)
        {
            return;
        }

        var childrenCount = visual.GetChildrenCount();

        if (_graphicsRenderableVisuals.TryGetValue(visual, out var graphics))
        {
            using var session = BindingManager.Current.StartTracking();
            _graphicsRenderContext.BeginVisual(visual.GraphicsRenderId, effectiveClip);
            graphics.RenderGraphics(_graphicsRenderContext);
            visual.UpdateGraphicsRenderDependencies(session.Reads);
        }

        for (var i = 0; i < childrenCount; i++)
        {
            var child = visual.GetChildUnsafe(i);
            CollectGraphicsCommandsRecursive(child, effectiveClip);
        }
    }

    private TerminalGraphicsPresentContext CreateGraphicsPresentContext(in Rectangle viewportBounds, TerminalHostKind hostKind, SceneRenderMode sceneRenderMode, in Rectangle textRepaintBounds)
        => new(
            this,
            _terminal,
            hostKind,
            viewportBounds,
            _renderFrameIndex,
            textRepaintBounds,
            ToGraphicsTextFrameKind(sceneRenderMode));

    private bool HasPendingGraphicsOutput(IBufferedTerminalGraphicsPresenter? presenter, TerminalGraphicsPresentContext context, DebugOverlayMetrics? metrics)
    {
        metrics?.RecordGraphicsPresenter(_options.GraphicsPresenter, presenter is not null);
        if (presenter is null)
        {
            metrics?.RecordGraphicsHasPendingOutput(false, 0);
            return false;
        }

        if (metrics is null)
        {
            return presenter.HasPendingOutput(_graphicsCommands, context);
        }

        var t0 = Stopwatch.GetTimestamp();
        var hasPendingOutput = presenter.HasPendingOutput(_graphicsCommands, context);
        metrics.RecordGraphicsHasPendingOutput(hasPendingOutput, Math.Max(0, Stopwatch.GetTimestamp() - t0));
        metrics.RecordGraphicsPresenter(_options.GraphicsPresenter, buffered: true);
        return hasPendingOutput;
    }

    private void PresentGraphicsCommands(TerminalGraphicsPresentContext context, DebugOverlayMetrics? metrics)
    {
        var presenter = _options.GraphicsPresenter;
        if (presenter is null)
        {
            return;
        }

        var cancellationToken = _cts.IsCancellationRequested ? CancellationToken.None : _cts.Token;
        var t0 = metrics is not null ? Stopwatch.GetTimestamp() : 0;
        try
        {
            var presentTask = presenter.PresentAsync(_graphicsCommands, context, cancellationToken);
            if (presentTask.IsCompletedSuccessfully)
            {
                presentTask.GetAwaiter().GetResult();
            }
            else
            {
                presentTask.AsTask().GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            // The app is stopping or disposing. Graphics presentation is best-effort during shutdown and must not turn a
            // normal exit into an unhandled exception.
        }
        finally
        {
            if (metrics is not null)
            {
                metrics.RecordGraphicsPresentation(Math.Max(0, Stopwatch.GetTimestamp() - t0));
                metrics.RecordGraphicsPresenter(presenter, buffered: false);
            }
        }
    }

    private void PresentGraphicsCommands(IBufferedTerminalGraphicsPresenter presenter, TerminalGraphicsPresentContext context, AnsiWriter writer, DebugOverlayMetrics? metrics)
    {
        var cancellationToken = _cts.IsCancellationRequested ? CancellationToken.None : _cts.Token;
        var t0 = metrics is not null ? Stopwatch.GetTimestamp() : 0;
        try
        {
            var presentTask = presenter.PresentAsync(_graphicsCommands, context, writer, cancellationToken);
            if (presentTask.IsCompletedSuccessfully)
            {
                presentTask.GetAwaiter().GetResult();
            }
            else
            {
                presentTask.AsTask().GetAwaiter().GetResult();
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            // The app is stopping or disposing. Graphics presentation is best-effort during shutdown and must not turn a
            // normal exit into an unhandled exception.
        }
        finally
        {
            if (metrics is not null)
            {
                metrics.RecordGraphicsPresentation(Math.Max(0, Stopwatch.GetTimestamp() - t0));
                metrics.RecordGraphicsPresenter(presenter, buffered: true);
            }
        }
    }

    private static TerminalGraphicsTextFrameKind ToGraphicsTextFrameKind(SceneRenderMode sceneRenderMode) => sceneRenderMode switch
    {
        SceneRenderMode.None => TerminalGraphicsTextFrameKind.None,
        SceneRenderMode.Dirty => TerminalGraphicsTextFrameKind.Dirty,
        SceneRenderMode.Full => TerminalGraphicsTextFrameKind.Full,
        _ => throw new ArgumentOutOfRangeException(nameof(sceneRenderMode)),
    };

    private static Rectangle GetTextRepaintBounds(SceneRenderMode sceneRenderMode, Rectangle viewportBounds, Rectangle dirtyRect) => sceneRenderMode switch
    {
        SceneRenderMode.None => default,
        SceneRenderMode.Dirty => dirtyRect,
        SceneRenderMode.Full => new Rectangle(0, 0, viewportBounds.Width, viewportBounds.Height),
        _ => throw new ArgumentOutOfRangeException(nameof(sceneRenderMode)),
    };

    private static Rectangle Intersect(in Rectangle a, in Rectangle b)
    {
        var x0 = Math.Max(a.X, b.X);
        var y0 = Math.Max(a.Y, b.Y);
        var x1 = Math.Min(a.Right, b.Right);
        var y1 = Math.Min(a.Bottom, b.Bottom);
        return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
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

        _renderBuffer = new CellBuffer(width, height, _wideRuneResolver);
        return _renderBuffer;
    }

    private bool ComposeDebugOverlay(CellBuffer buffer, DebugOverlayMetrics? metrics)
    {
        if (!TryCreateDebugOverlayLayout(buffer, out var layout))
        {
            return false;
        }

        _debugOverlaySnapshot.Save(buffer, layout.Rect);
        var startTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();
        RenderDebugOverlay(buffer, layout);
        if (metrics is not null)
        {
            metrics.RecordOverlayComposition(layout.Rect, Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
        }

        return true;
    }

    private bool TryCreateDebugOverlayLayout(CellBuffer buffer, out DebugOverlayLayout layout)
    {
        var maxWidth = buffer.Width;
        var maxHeight = buffer.Height;
        if (maxWidth <= 0 || maxHeight <= 0)
        {
            layout = default;
            return false;
        }

        var theme = Root.GetTheme();
        var focus = FocusedElement;
        var hover = _hoveredElement;
        var metrics = _debugOverlayMetrics;
        var waitDiagnostics = _loopWaitBackend.GetDiagnosticsSnapshot();

        static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
        static string FormatMs(long ticks) => ToMs(ticks).ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture).PadLeft(6);
        static string FormatDurationMs(TimeSpan duration) => duration.TotalMilliseconds.ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture).PadLeft(6);
        static string FormatFps(double fps) => fps <= 0 ? "-" : fps.ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture);
        static string YesNo(bool value) => value ? "yes" : "no";
        static string FormatBytes(long bytes)
        {
            const double Kib = 1024.0;
            var value = (double)Math.Max(0, bytes);
            var suffix = "B";
            if (value >= Kib * Kib)
            {
                value /= Kib * Kib;
                suffix = "MiB";
            }
            else if (value >= Kib)
            {
                value /= Kib;
                suffix = "KiB";
            }

            return value >= 10 || suffix == "B"
                ? value.ToString("0", global::System.Globalization.CultureInfo.InvariantCulture) + suffix
                : value.ToString("0.0", global::System.Globalization.CultureInfo.InvariantCulture) + suffix;
        }

        static string FormatAge(long elapsedTicks)
        {
            var seconds = Math.Max(0, elapsedTicks) / (double)Stopwatch.Frequency;
            if (seconds < 1)
            {
                return $"{(seconds * 1000):0}ms ago";
            }

            if (seconds < 10)
            {
                return $"{seconds:0.0}s ago";
            }

            if (seconds < 60)
            {
                return $"{seconds:0}s ago";
            }

            var minutes = seconds / 60;
            if (minutes < 60)
            {
                return $"{minutes:0.0}m ago";
            }

            return $"{(minutes / 60):0.0}h ago";
        }

        var overlayNowTimestamp = Stopwatch.GetTimestamp();

        List<string> BuildLines(Rectangle overlayRect)
        {
            var dirtyText = "Scene: Dirty <none>";
            if (metrics is not null && metrics.SceneHasDirtyRect)
            {
                var r = metrics.SceneDirtyRect;
                dirtyText = $"Scene: Dirty ({r.X},{r.Y}) {r.Width}x{r.Height}";
            }

            var repaintText = "Scene: Repaint <none>";
            if (metrics is not null && metrics.SceneHasRepaintRect)
            {
                var r = metrics.SceneRepaintRect;
                repaintText = $"Scene: Repaint ({r.X},{r.Y}) {r.Width}x{r.Height}";
            }

            var overlayText = overlayRect.Width > 0 && overlayRect.Height > 0
                ? $"Overlay: ({overlayRect.X},{overlayRect.Y}) {overlayRect.Width}x{overlayRect.Height}  {FormatMs(metrics?.OverlayRenderTicks ?? 0)}ms"
                : $"Overlay: <clipped>  {FormatMs(metrics?.OverlayRenderTicks ?? 0)}ms";

            if (metrics?.OverlayOnlyFrame ?? false)
            {
                overlayText += "  overlay-only";
            }

            var lastSceneText = "Scene: Last <none yet>";
            if (metrics is not null && metrics.HasLastSceneUpdate)
            {
                var lastRepaintText = metrics.LastSceneHasRepaintRect
                    ? $"repaint ({metrics.LastSceneRepaintRect.X},{metrics.LastSceneRepaintRect.Y}) {metrics.LastSceneRepaintRect.Width}x{metrics.LastSceneRepaintRect.Height}"
                    : "repaint <none>";
                var lastDirtyText = metrics.LastSceneHasDirtyRect
                    ? $"dirty ({metrics.LastSceneDirtyRect.X},{metrics.LastSceneDirtyRect.Y}) {metrics.LastSceneDirtyRect.Width}x{metrics.LastSceneDirtyRect.Height}"
                    : "dirty <none>";
                lastSceneText = $"Scene: Last {lastRepaintText}  {lastDirtyText}  full={(metrics.LastSceneFullRepaint ? "yes" : "no")}  {FormatAge(overlayNowTimestamp - metrics.LastSceneUpdateTimestamp)}";
            }

            var lines = new List<string>
            {
                $"Frame: {_renderFrameIndex}  FPS: {FormatFps(metrics?.Fps ?? 0)}",
                $"Tick: {FormatMs(metrics?.TickTotalTicks ?? 0)}ms  Update: {FormatMs(metrics?.TickUserUpdateTicks ?? 0)}ms",
                $"Loop: {waitDiagnostics.BackendName}  YieldWin {FormatMs(waitDiagnostics.YieldWindowTicks)}ms  Overshoot avg/p95 {FormatMs(waitDiagnostics.AverageOvershootTicks)}/{FormatMs(waitDiagnostics.P95OvershootTicks)}ms",
                $"Loop: Yield avg {FormatMs(waitDiagnostics.AverageYieldTicks)}ms  Late {(metrics?.LateDeadlineCount ?? 0)}",
                $"Wake64: deadline {(metrics?.WakeDeadlineCount ?? 0)}  input {(metrics?.WakeInputCount ?? 0)}  render {(metrics?.WakeRenderCount ?? 0)}  anim {(metrics?.WakeAnimationCount ?? 0)}",
                $"Wake64: post {(metrics?.WakePostCount ?? 0)}  async {(metrics?.WakeAsyncUpdateCount ?? 0)}  stop {(metrics?.WakeShutdownCount ?? 0)}",
            };

            if (metrics is not null && (metrics.GraphicsPresenterConfigured || metrics.GraphicsCommandCount > 0 || metrics.GraphicsCollectTicks > 0 || metrics.LastGraphicsPresentTicks > 0))
            {
                var presenterName = metrics.GraphicsPresenterConfigured
                    ? (metrics.GraphicsPresenterName ?? "<unknown>")
                    : "<none>";
                var pendingText = metrics.GraphicsPresenterBuffered
                    ? $"{YesNo(metrics.GraphicsHasPendingOutput)}/{FormatMs(metrics.GraphicsHasPendingTicks)}ms"
                    : "n/a";
                lines.Add($"Gfx: {presenterName}  buffered={YesNo(metrics.GraphicsPresenterBuffered)}  cmds {metrics.GraphicsCommandCount}  collect {FormatMs(metrics.GraphicsCollectTicks)}ms  pending {pendingText}  present(prev) {FormatMs(metrics.LastGraphicsPresentTicks)}ms");

                if (metrics.HasGraphicsPresenterDiagnostics)
                {
                    var diagnostics = metrics.GraphicsPresenterDiagnostics;
                    var protocol = diagnostics.Protocol == TerminalGraphicsProtocol.None ? "-" : diagnostics.Protocol.ToString();
                    lines.Add($"GfxImg: {protocol}  last pres {FormatDurationMs(diagnostics.LastPresentationDuration)}ms  enc {diagnostics.LastEncodedFrameCount} in {FormatDurationMs(diagnostics.LastEncodeDuration)}ms  payload {FormatBytes(diagnostics.LastPayloadByteCount)}  drop {diagnostics.LastDroppedFrameCount}  cache h/m {diagnostics.LastCacheHitCount}/{diagnostics.LastCacheMissCount}");
                    lines.Add($"GfxImg: total pres {diagnostics.PresentationCount}  enc {diagnostics.EncodedFrameCount} total {FormatDurationMs(diagnostics.TotalEncodeDuration)}ms avg {FormatDurationMs(diagnostics.AverageEncodeDuration)}ms  encfps {FormatFps(diagnostics.EffectiveFramesPerSecond)}  payload {FormatBytes(diagnostics.PayloadByteCount)}  drop {diagnostics.DroppedFrameCount}  cache h/m/s {diagnostics.CacheHitCount}/{diagnostics.CacheMissCount}/{diagnostics.CacheStoreCount}");
                }
            }

            lines.AddRange([
                $"Top(prev): Measure {FormatMs(metrics?.LastRenderMeasureTicks ?? 0)}ms  Arrange {FormatMs(metrics?.LastRenderArrangeTicks ?? 0)}ms  Render {FormatMs(metrics?.LastRenderTreeTicks ?? 0)}ms",
                $"Top(prev): Overlay {FormatMs(metrics?.OverlayRenderTicks ?? 0)}ms  Host {FormatMs(metrics?.LastRenderHostTicks ?? 0)}ms  Total {FormatMs(metrics?.LastRenderTotalTicks ?? 0)}ms",
                $"Calls: DynamicUpdate {(metrics?.DynamicUpdate.Calls ?? 0)} ({FormatMs(metrics?.DynamicUpdate.Ticks ?? 0)}ms)",
                $"Calls: Prepare {(metrics?.PrepareChildren.Calls ?? 0)} ({FormatMs(metrics?.PrepareChildren.Ticks ?? 0)}ms)",
                $"Calls: Measure {(metrics?.Measure.Calls ?? 0)} ({FormatMs(metrics?.Measure.Ticks ?? 0)}ms)  Cache {(metrics?.MeasureCacheHits ?? 0)}",
                $"Calls: Arrange {(metrics?.Arrange.Calls ?? 0)} ({FormatMs(metrics?.Arrange.Ticks ?? 0)}ms)  Cache {(metrics?.ArrangeCacheHits ?? 0)}",
                $"Calls: Render {(metrics?.RenderOverride.Calls ?? 0)} ({FormatMs(metrics?.RenderOverride.Ticks ?? 0)}ms)  ClipSkips {(metrics?.RenderClipSkips ?? 0)}",
                repaintText,
                dirtyText,
                $"Scene: Full {((metrics?.SceneFullRepaint ?? false) ? "yes" : "no")}",
                lastSceneText,
                overlayText,
                $"HostDiff: {(metrics?.DiffOutputChars ?? 0)} chars  {(metrics?.DiffCellsTouched ?? 0)} cells  full={((metrics?.DiffForceFull ?? false) ? "yes" : "no")}",
                $"Focus: {(focus is null ? "<none>" : focus.GetType().Name)}",
                $"Hover: {(hover is null ? "<none>" : hover.GetType().Name)}",
            ]);

            return lines;
        }

        Rectangle overlayRect = default;
        List<string> lines = [];
        for (var pass = 0; pass < 4; pass++)
        {
            lines = BuildLines(overlayRect);

            var contentWidth = 0;
            foreach (var line in lines)
            {
                contentWidth = Math.Max(contentWidth, TerminalTextUtility.GetWidth(line.AsSpan()));
            }

            var width = Math.Min(maxWidth, Math.Max(3, contentWidth + 2));
            var height = Math.Min(maxHeight, Math.Max(3, lines.Count + 2));
            if (width < 3 || height < 3)
            {
                layout = default;
                return false;
            }

            var nextRect = new Rectangle(0, 0, width, height);
            if (nextRect == overlayRect)
            {
                overlayRect = nextRect;
                break;
            }

            overlayRect = nextRect;
        }

        var borderStyle = theme.BorderStyle(focused: true) | TextStyle.Bold;

        // The overlay fills with blank glyphs; ensure we write an explicit foreground to avoid inheriting colors
        // from the underlay when rendering the overlay text (which preserves the filled cell style).
        var backgroundStyle = theme.ForegroundTextStyle() | TextStyle.Dim;
        if (theme.Background is { } bg)
        {
            backgroundStyle = backgroundStyle.WithBackground(bg);
        }

        layout = new DebugOverlayLayout(overlayRect, lines, borderStyle, backgroundStyle);
        return true;
    }

    private void RenderDebugOverlay(CellBuffer buffer, in DebugOverlayLayout layout)
    {
        var rect = layout.Rect;
        var width = rect.Width;
        var height = rect.Height;
        var borderStyle = layout.BorderStyle;
        var backgroundStyle = layout.BackgroundStyle;
        var lines = layout.Lines;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                buffer.SetCell(rect.X + x, rect.Y + y, new Rune(' '), backgroundStyle);
            }
        }

        var right = rect.X + width - 1;
        var bottom = rect.Y + height - 1;

        buffer.SetCell(rect.X, rect.Y, new Rune('+'), borderStyle);
        buffer.SetCell(right, rect.Y, new Rune('+'), borderStyle);
        buffer.SetCell(rect.X, bottom, new Rune('+'), borderStyle);
        buffer.SetCell(right, bottom, new Rune('+'), borderStyle);

        for (var x = rect.X + 1; x < right; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune('-'), borderStyle);
            buffer.SetCell(x, bottom, new Rune('-'), borderStyle);
        }

        for (var y = rect.Y + 1; y < bottom; y++)
        {
            buffer.SetCell(rect.X, y, new Rune('|'), borderStyle);
            buffer.SetCell(right, y, new Rune('|'), borderStyle);
        }

        for (var i = 0; i < lines.Count && i + 1 < height - 1; i++)
        {
            buffer.WriteText(rect.X + 1, rect.Y + 1 + i, lines[i].AsSpan(), Style.None);
        }
    }

    private bool DispatchKeyEvent(TerminalKeyEvent keyEvent, bool routeCommands = true)
    {
        EnsureFocusInScope();

        var args = new KeyEventArgs { RawEvent = keyEvent };

        if ((keyEvent.Modifiers & TerminalModifiers.Ctrl) != 0 && keyEvent.Char is TerminalChar.CtrlC)
        {
            if (TryCopyActiveSelection())
            {
                return true;
            }
        }

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

                if (!cmd.RouteGesture)
                {
                    continue;
                }

                if (!gesture.Matches(keyEvent))
                {
                    continue;
                }

                if (!cmd.IsVisibleFor(v) || !cmd.CanExecuteFor(v))
                {
                    if (cmd.ConsumesGestureWhenUnavailable)
                    {
                        return true; // gesture matched but is disabled/hidden in this context; treat as handled.
                    }

                    continue;
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

                if (!cmd.RouteGesture)
                {
                    continue;
                }

                if (!gesture.Matches(keyEvent))
                {
                    continue;
                }

                if (!cmd.IsVisibleFor(globalTarget) || !cmd.CanExecuteFor(globalTarget))
                {
                    if (cmd.ConsumesGestureWhenUnavailable)
                    {
                        return true;
                    }

                    continue;
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
        _focusablesScratch.Clear();
        CollectFocusables(scope, _focusablesScratch);
        if (_focusablesScratch.Count == 0)
        {
            return;
        }

        if (FocusedElement is null || !_focusablesScratch.Contains(FocusedElement))
        {
            FocusFirstTabStop();
            return;
        }

        var index = _focusablesScratch.IndexOf(FocusedElement);
        FocusNextTabStop(index);
    }

    private void FocusPrevious()
    {
        var scope = GetFocusScopeRoot();
        _focusablesScratch.Clear();
        CollectFocusables(scope, _focusablesScratch);
        if (_focusablesScratch.Count == 0)
        {
            return;
        }

        if (FocusedElement is null || !_focusablesScratch.Contains(FocusedElement))
        {
            FocusLastTabStop();
            return;
        }

        var index = _focusablesScratch.IndexOf(FocusedElement);
        FocusPreviousTabStop(index);
    }

    private void FocusFirstTabStop()
    {
        for (var i = 0; i < _focusablesScratch.Count; i++)
        {
            if (_focusablesScratch[i].IsTabStop)
            {
                FocusedElement = _focusablesScratch[i];
                RequestRender();
                return;
            }
        }
    }

    private void FocusLastTabStop()
    {
        for (var i = _focusablesScratch.Count - 1; i >= 0; i--)
        {
            if (_focusablesScratch[i].IsTabStop)
            {
                FocusedElement = _focusablesScratch[i];
                RequestRender();
                return;
            }
        }
    }

    private void FocusNextTabStop(int currentIndex)
    {
        for (var offset = 1; offset <= _focusablesScratch.Count; offset++)
        {
            var candidate = _focusablesScratch[(currentIndex + offset) % _focusablesScratch.Count];
            if (candidate.IsTabStop)
            {
                FocusedElement = candidate;
                RequestRender();
                return;
            }
        }
    }

    private void FocusPreviousTabStop(int currentIndex)
    {
        for (var offset = 1; offset <= _focusablesScratch.Count; offset++)
        {
            var candidate = _focusablesScratch[(currentIndex - offset + _focusablesScratch.Count) % _focusablesScratch.Count];
            if (candidate.IsTabStop)
            {
                FocusedElement = candidate;
                RequestRender();
                return;
            }
        }
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

        if (TryHandleAppExitGesture(keyEvent))
        {
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

    private bool TryHandleAppExitGesture(TerminalKeyEvent keyEvent)
    {
        if (!TryGetDefaultQuitCommand(out var command))
        {
            return false;
        }

        if (command.Gesture is not { } gesture || !gesture.Matches(keyEvent))
        {
            return false;
        }

        // Allow controls to observe the raw gesture first (e.g. close a transient popup) before the app-level
        // quit command runs. The command itself stays replaceable at runtime via GlobalCommands.
        if (DispatchKeyEvent(keyEvent, routeCommands: false))
        {
            return true;
        }

        EnsureFocusInScope();
        var target = FocusedElement ?? Root;
        if (!command.IsVisibleFor(target) || !command.CanExecuteFor(target))
        {
            return command.ConsumesGestureWhenUnavailable;
        }

        command.Execute(target);
        return true;
    }

    private bool TryGetDefaultQuitCommand(out Command command)
    {
        var commands = _globalCommands;
        if (commands is not null)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                var candidate = commands[i];
                if (string.Equals(candidate.Id, DefaultQuitCommandId, StringComparison.Ordinal))
                {
                    command = candidate;
                    return true;
                }
            }
        }

        command = null!;
        return false;
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
        var activeMenuBar = inputRoot is Popup popup ? ResolveOwningMenuBar(popup) : null;

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

            if (_pointerCapture is null
                && activeMenuBar is not null
                && TryResolveMenuPopupHitTarget(activeMenuBar, mouseEvent.X, mouseEvent.Y, out var menuHitTarget)
                && menuHitTarget is not null)
            {
                hitTarget = menuHitTarget;
                target = menuHitTarget;
            }
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

        if (mouseEvent.Kind is TerminalMouseKind.Down or TerminalMouseKind.DoubleClick && mouseEvent.Button == TerminalMouseButton.Left)
        {
            UpdateSelectionOwnerFromPointer(target);
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

    private bool TryResolveMenuPopupHitTarget(Controls.MenuBar menuBar, int uiX, int uiY, out Visual? hitTarget)
    {
        hitTarget = menuBar.HitTestMenuInteraction(uiX, uiY);
        return hitTarget is not null;
    }

    private static Controls.MenuBar? ResolveOwningMenuBar(Popup popup)
    {
        for (var current = popup.Anchor; current is not null; current = current.Parent)
        {
            if (current is Controls.MenuBar menuBar)
            {
                return menuBar;
            }

            if (current is Popup parentPopup)
            {
                return ResolveOwningMenuBar(parentPopup);
            }
        }

        return null;
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
        var wakeAnimations = false;

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
                wakeAnimations |= v is IAnimatedVisual;
            }
        }

        for (var i = 0; i < _hoveredPathScratch.Count; i++)
        {
            var v = _hoveredPathScratch[i];
            if (!_hoveredPath.Contains(v))
            {
                v.IsHovered = true;
                wakeAnimations |= v is IAnimatedVisual;
            }
        }

        _hoveredElement = hoveredLeaf;
        (_hoveredPath, _hoveredPathScratch) = (_hoveredPathScratch, _hoveredPath);

        // Hover-driven animated visuals can stay dormant while idle, so wake the scheduler when
        // the hovered path changes instead of forcing them to poll every tick.
        if (wakeAnimations)
        {
            RequestAnimation();
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
            if (_options.InitialFocusMode == InitialFocusMode.None)
            {
                return;
            }

            _focusablesScratch.Clear();
            CollectFocusables(scopeRoot, _focusablesScratch);
            if (_focusablesScratch.Count == 0)
            {
                return;
            }

            // Prefer a visual explicitly marked for initial focus. This allows apps to define their
            // focus target declaratively (e.g. focus a sidebar list instead of a search box).
            var focused = _focusablesScratch[0];
            for (var i = 0; i < _focusablesScratch.Count; i++)
            {
                if (_focusablesScratch[i].AutoFocus)
                {
                    focused = _focusablesScratch[i];
                    break;
                }
            }

            FocusedElement = focused;
            if (FocusedElement is not null)
            {
                RequestRender();
            }
        }
    }

    private static void CollectFocusables(Visual root, List<Visual> focusables)
    {
        // Prefer focusing leaf controls over container controls (e.g. focus a TreeView inside a ScrollViewer rather
        // than the ScrollViewer itself). Containers that are tab stops remain reachable via Tab because we still yield
        // them after their descendants.
        for (var i = 0; i < root.GetChildrenCount(); i++)
        {
            var child = root.GetChildUnsafe(i);
            CollectFocusables(child, focusables);
        }

        if (root.Focusable && root.IsVisible && root.IsEnabled)
        {
            focusables.Add(root);
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
