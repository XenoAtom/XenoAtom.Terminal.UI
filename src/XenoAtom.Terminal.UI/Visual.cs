// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.ComponentModel;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Collections;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Base class for all UI elements in XenoAtom.Terminal.UI.
/// </summary>
/// <remarks>
/// A <see cref="Visual"/> participates in:
/// <list type="bullet">
/// <item><description>Layout via <see cref="Measure(in LayoutConstraints)"/> and <see cref="Arrange(Rectangle)"/>.</description></item>
/// <item><description>Rendering into the cell buffer.</description></item>
/// <item><description>Input routing (keyboard/mouse) when enabled and/or focused.</description></item>
/// </list>
/// Visuals are retained-mode objects: you build a tree of visuals and the framework updates layout/rendering as tracked bindings change.
/// </remarks>
public abstract partial class Visual : DispatcherObject, IVisualElement
{
    private static long s_nextGraphicsRenderId;

    private readonly ulong _graphicsRenderId = (ulong)Interlocked.Increment(ref s_nextGraphicsRenderId);
    private Dictionary<object, Delegate?>? _handlers;
    private Dictionary<object, Delegate?>? _handledEventHandlers;
    private BindableList<Command>? _commands;
    internal Dictionary<object, object?>? StyleEnvironment;
    private Dictionary<BindingAccessor, ComputedPropertyRecipe>? _computedProperties;
    private List<Action<Visual>>? _dynamicUpdates;

    private Size _lastDesiredSizeWithoutMargin;

    private bool _hasComputedPropertyRunner;
    private bool _dynamicUpdatesDirty;
    private bool _prepareChildrenDirty = true;
    private bool _measureDirty = true;
    private bool _arrangeDirty = true;
    private bool _isHitTestVisible = true;
    private HashSet<Binding>? _dynamicUpdateDeps;
    private HashSet<Binding>? _prepareChildrenDeps;
    private HashSet<Binding>? _measureDeps;
    private HashSet<Binding>? _arrangeDeps;
    private HashSet<Binding>? _renderDeps;
    private HashSet<Binding>? _graphicsRenderDeps;
    private int _graphicsRenderableSubtreeCount;

    private bool _hasLastMeasure;
    private LayoutConstraints _lastMeasureConstraints;
    private bool _hasLastArrange;
    private Rectangle _lastArrangeRect;

    /// <summary>
    /// Gets the parent visual, or <c>null</c> if this visual is the root of a tree.
    /// </summary>
    public Visual? Parent { get; private set; }

    /// <summary>
    /// Gets the arranged bounds of this visual, in cell coordinates, relative to its parent.
    /// </summary>
    [Bindable]
    public partial Rectangle Bounds { get; private set; }

    /// <summary>
    /// Gets the desired size computed during the last measure pass.
    /// </summary>
    public Size DesiredSize { get; private set; }

    /// <summary>
    /// Gets the last measure hints computed during <see cref="Measure(in LayoutConstraints)"/>.
    /// </summary>
    public SizeHints MeasureHints { get; private set; }

    /// <summary>
    /// Gets the owning <see cref="TerminalApp"/> when this visual is attached to an application.
    /// </summary>
    public TerminalApp? App { get; private set; }

    internal ulong GraphicsRenderId => _graphicsRenderId;

    internal int GraphicsRenderableSubtreeCount => _graphicsRenderableSubtreeCount;

    /// <summary>
    /// Gets a value indicating whether this visual can receive focus.
    /// </summary>
    [Bindable]
    public partial bool Focusable { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether this visual should be preferred as the initial focus target.
    /// </summary>
    /// <remarks>
    /// When <see cref="TerminalAppOptions.InitialFocusMode"/> is <see cref="InitialFocusMode.FirstFocusable"/>,
    /// <see cref="TerminalApp"/> selects the first visible, enabled visual with <see cref="AutoFocus"/> set.
    /// If none are found, the first focusable element in the focus scope is used.
    /// </remarks>
    [Bindable]
    public partial bool AutoFocus { get; set; }

    /// <summary>
    /// Invalidates this visual so that it will be re-measured/arranged/rendered as needed.
    /// </summary>
    [Obsolete("Manual invalidation is not supported. Make state changes via bindable properties so the app can invalidate automatically.", error: true)]
    protected void Invalidate() => throw new NotSupportedException();

    /// <summary>
    /// Initializes a new instance of the <see cref="Visual"/> class.
    /// </summary>
    protected Visual()
    {
        _isVisible = true;
        _isEnabled = true;
        _maxWidth = int.MaxValue;
        _maxHeight = int.MaxValue;
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of this visual within its layout slot.
    /// </summary>
    /// <remarks>
    /// This value is interpreted by the parent layout during arrange. In particular, <see cref="Align.Stretch"/> requests
    /// taking all available width.
    /// </remarks>
    [Bindable]
    public partial Align HorizontalAlignment { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment of this visual within its layout slot.
    /// </summary>
    /// <remarks>
    /// This value is interpreted by the parent layout during arrange. In particular, <see cref="Align.Stretch"/> requests
    /// taking all available height.
    /// </remarks>
    [Bindable]
    public partial Align VerticalAlignment { get; set; }

    /// <summary>
    /// Gets or sets the minimum desired width of this visual, in cells.
    /// </summary>
    [Bindable]
    public partial int MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the minimum desired height of this visual, in cells.
    /// </summary>
    [Bindable]
    public partial int MinHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum desired width of this visual, in cells.
    /// </summary>
    [Bindable]
    public partial int MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum desired height of this visual, in cells.
    /// </summary>
    [Bindable]
    public partial int MaxHeight { get; set; }

    /// <summary>
    /// Gets or sets the outer margin around this visual.
    /// </summary>
    /// <remarks>
    /// Margins are handled by the parent layout and contribute to the layout slot size and position of this visual.
    /// </remarks>
    [Bindable]
    public partial Thickness Margin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this visual is visible.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="false"/>, the visual is not rendered and does not participate in layout or input.
    /// </remarks>
    [Bindable]
    public partial bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this visual is enabled.
    /// </summary>
    /// <remarks>
    /// Disabled visuals typically do not receive input and may render using a disabled style.
    /// </remarks>
    [Bindable]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Gets a value indicating whether this visual currently has keyboard focus.
    /// </summary>
    [Bindable]
    public partial bool HasFocus { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this visual, or any of its descendants, currently has focus.
    /// </summary>
    [Bindable]
    public partial bool HasFocusWithin { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether this visual participates in hit testing.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="false"/>, the visual is treated as “transparent” for pointer hit testing and input
    /// routing. This is useful for non-interactive overlays such as tooltips.
    /// </remarks>
    public bool IsHitTestVisible
    {
        get => _isHitTestVisible;
        set
        {
            VerifyAccess();
            _isHitTestVisible = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the pointer is currently hovering this visual.
    /// </summary>
    [Bindable]
    public partial bool IsHovered { get; internal set; }

    partial void OnIsHoveredChanged(bool value) => OnHoveredChanged(value);

    partial void OnMinWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMinHeightChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMaxWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMaxHeightChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    /// <summary>
    /// Adds a key binding to this visual.
    /// </summary>
    /// <remarks>
     /// Key bindings are evaluated during key input routing. When the gesture matches, the action is invoked and the event is handled.
    /// </remarks>
    /// <param name="gesture">The key gesture.</param>
    /// <param name="action">The action to invoke when the gesture is triggered.</param>
    public void AddKeyBinding(Input.KeyGesture gesture, Action action)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(action);

        // Key bindings are represented internally as commands with Presentation.None so they participate in shortcut routing,
        // while remaining hidden from command UI surfaces.
        AddCommand(new Command
        {
            Id = $"KeyBinding:{gesture}",
            LabelMarkup = string.Empty,
            Gesture = gesture,
            Presentation = CommandPresentation.None,
            Importance = CommandImportance.Tertiary,
            Execute = _ => action(),
        });
    }

    /// <summary>
    /// Gets the commands registered on this visual.
    /// </summary>
    public BindableList<Command> Commands => _commands ??= new BindableList<Command>(this, "Visual.Commands");

    /// <summary>
    /// Gets or sets an optional factory used to build a context menu for this visual.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a context menu is requested (for example via right-click in a fullscreen app), the framework looks for the nearest
    /// visual in the hovered chain that provides a <see cref="ContextMenuFactory"/>.
    /// </para>
    /// <para>
    /// If no factory is provided, the framework falls back to command discovery using <see cref="CommandPresentation.ContextMenu"/>.
    /// </para>
    /// </remarks>
    public Func<Visual, IEnumerable<MenuItem>>? ContextMenuFactory { get; set; }

    /// <summary>
    /// Adds or replaces a command on this visual.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="command"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is invalid.</exception>
    public void AddCommand(Command command)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(command);
        command.Validate();

        var commands = Commands;

        // Avoid ambiguous routing: a sequence prefix must not be used as a standalone gesture in the same scope.
        // This keeps single-stroke bindings simple and prevents timeout-based disambiguation.
        if (command.Sequence is { } sequence)
        {
            var prefix = sequence[0];
            for (var i = 0; i < commands.Count; i++)
            {
                var existing = commands[i];
                if (existing.Gesture is { } g && g.Equals(prefix))
                {
                    throw new InvalidOperationException($"The gesture '{prefix}' is already registered as a standalone command in this scope and cannot be used as a sequence prefix.");
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
                    throw new InvalidOperationException($"The gesture '{gesture}' is already registered as a sequence prefix in this scope and cannot be used as a standalone command.");
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
    /// Removes a command by id.
    /// </summary>
    /// <param name="id">The command id.</param>
    /// <returns><see langword="true"/> if a command was removed; otherwise <see langword="false"/>.</returns>
    public bool RemoveCommand(string id)
    {
        VerifyAccess();
        if (_commands is null)
        {
            return false;
        }

        for (var i = 0; i < _commands.Count; i++)
        {
            if (string.Equals(_commands[i].Id, id, StringComparison.Ordinal))
            {
                _commands.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the number of visual children in this <see cref="Visual"/>.
    /// </summary>
    protected virtual int ChildrenCount => 0;

    /// <summary>
    /// Gets the visual child at the specified <paramref name="index"/>.
    /// </summary>
    protected virtual Visual GetChild(int index) => throw new ArgumentOutOfRangeException(nameof(index));

    internal int GetChildrenCount()
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();
        return ChildrenCount;
    }

    internal Visual GetChildUnsafe(int index)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();
        return GetChild(index);
    }

    /// <summary>
    /// Attaches a child visual to this instance and connects it to the application if already attached.
    /// </summary>
    /// <param name="child">The child to attach.</param>
    protected void AttachChild(Visual child)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(child);
        if (child.Parent is not null)
        {
            throw new InvalidOperationException("The visual already has a parent.");
        }

        child.Parent = this;

        if (App is not null)
        {
            child.AttachToApp(App);
        }

        // The visual tree changed; parent layout caches are no longer valid.
        MarkMeasureDirty();
    }

    /// <summary>
    /// Detaches a child visual from this instance and disconnects it from the application if needed.
    /// </summary>
    /// <param name="child">The child to detach.</param>
    protected void DetachChild(Visual child)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(child);
        if (!ReferenceEquals(child.Parent, this))
        {
            throw new InvalidOperationException("The visual is not a child of this visual.");
        }

        if (App is not null)
        {
            child.DetachFromApp();
        }

        child.Parent = null;

        // The visual tree changed; parent layout caches are no longer valid.
        MarkMeasureDirty();
    }

    internal void AttachCollectionChild(Visual child) => AttachChild(child);

    internal void DetachCollectionChild(Visual child) => DetachChild(child);

    /// <summary>
    /// Sets a style value in the environment of this visual and returns it by type.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="value">The style value.</param>
    public void SetStyle<T>(T value) where T : IStyle<T> => SetStyle(T.Key, value);

    /// <summary>
    /// Sets a style value factory in the environment of this visual and returns it by type.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="value">The factory used to resolve the style value.</param>
    public void SetStyle<T>(Func<T> value) where T : IStyle<T> => SetStyle(T.Key, value);

    /// <summary>
    /// Sets a style binding in the environment of this visual and returns it by type.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="value">The binding used to resolve the style value.</param>
    public void SetStyle<T>(Binding<T> value) where T : IStyle<T> => SetStyle(T.Key, value);

    /// <summary>
    /// Sets a style value in the environment of this visual.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="key">The style key.</param>
    /// <param name="value">The style value.</param>
    public void SetStyle<T>(StyleKey<T> key, T value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);

        var oldSource = ResolveStyleSourceBeforeSet(key);

        StyleEnvironment ??= new Dictionary<object, object?>();
        StyleEnvironment[key] = value;

        NotifyStyleSourceChange(key, oldSource);
    }

    /// <summary>
    /// Sets a style value factory in the environment of this visual.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="key">The style key.</param>
    /// <param name="value">The factory used to resolve the style value.</param>
    public void SetStyle<T>(StyleKey<T> key, Func<T> value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var oldSource = ResolveStyleSourceBeforeSet(key);

        StyleEnvironment ??= new Dictionary<object, object?>();
        StyleEnvironment[key] = value;

        NotifyStyleSourceChange(key, oldSource);
    }

    /// <summary>
    /// Sets a style binding in the environment of this visual.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="key">The style key.</param>
    /// <param name="value">The binding used to resolve the style value.</param>
    public void SetStyle<T>(StyleKey<T> key, Binding<T> value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        if (value.IsEmpty)
        {
            throw new ArgumentException("The binding cannot be empty.", nameof(value));
        }

        SetStyle(key, value.GetValue);
    }

    /// <summary>
    /// Gets a style value from the environment, using the default key for the style type.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <returns>The resolved style value.</returns>
    public T GetStyle<T>() where T : IStyle<T> => GetStyle(T.Key);

    /// <summary>
    /// Gets a style value from the environment.
    /// </summary>
    /// <typeparam name="T">The style type.</typeparam>
    /// <param name="key">The style key.</param>
    /// <returns>The resolved style value.</returns>
    public T GetStyle<T>(StyleKey<T> key)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);

        Visual? root = null;

        for (var v = this; v is not null; v = v.Parent)
        {
            root = v;
            if (v.StyleEnvironment is not null && v.StyleEnvironment.TryGetValue(key, out var boxed))
            {
                BindingManager.Current.RegisterRead(v, key.BindingAccessor);
                return ResolveStyleValue(key, boxed);
            }
        }

        BindingManager.Current.RegisterRead(root ?? this, key.BindingAccessor);
        return key.DefaultValue;
    }

    /// <summary>
    /// Determines whether a local value is set for the specified style key.
    /// </summary>
    /// <typeparam name="T">The type of the value associated with the style key.</typeparam>
    /// <param name="key">The style key to check for a locally set value. Cannot be null.</param>
    /// <returns>true if a local value is set for the specified key; otherwise, false.</returns>
    public bool HasLocalStyle<T>(StyleKey<T> key)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        return StyleEnvironment is not null && StyleEnvironment.ContainsKey(key);
    }

    /// <summary>
    /// Gets the current theme resolved from the environment.
    /// </summary>
    public Theme GetTheme() => GetStyle<Theme>();

    private void NotifyStyleSourceChange<T>(StyleKey<T> key, Visual oldSource)
    {
        if (!ReferenceEquals(oldSource, this))
        {
            BindingManager.Current.NotifyValueChanged(oldSource, key.BindingAccessor);
        }

        BindingManager.Current.NotifyValueChanged(this, key.BindingAccessor);
    }

    private Visual ResolveStyleSourceBeforeSet<T>(StyleKey<T> key)
    {
        Visual? root = null;
        for (var v = this; v is not null; v = v.Parent)
        {
            root = v;
            if (v.StyleEnvironment is not null && v.StyleEnvironment.ContainsKey(key))
            {
                return v;
            }
        }

        return root ?? this;
    }

    private static T ResolveStyleValue<T>(StyleKey<T> key, object? boxed)
    {
        if (boxed is T typed)
        {
            return typed;
        }

        if (boxed is Func<T> factory)
        {
            var resolved = factory();
            return resolved is null ? key.DefaultValue : resolved;
        }

        return key.DefaultValue;
    }

    /// <summary>
    /// Gets the absolute bounds of this visual in the coordinate space of the visual tree root.
    /// </summary>
    /// <remarks>
    /// The returned rectangle is based on the arranged <see cref="Bounds"/> of this visual and its ancestors.
    /// </remarks>
    public Rectangle GetAbsoluteBounds()
    {
        VerifyAccess();

        var rect = Bounds;
        var x = rect.X;
        var y = rect.Y;
        for (var p = Parent; p is not null; p = p.Parent)
        {
            var pb = p.Bounds;
            x += pb.X;
            y += pb.Y;
        }

        return new Rectangle(x, y, rect.Width, rect.Height);
    }

    /// <summary>
    /// Registers a dynamic update callback for this visual.
    /// </summary>
    /// <remarks>
    /// Dynamic updates are evaluated by the framework; property reads performed inside the callback are tracked so that future changes
    /// re-trigger the update and any dependent layout/render passes.
    /// </remarks>
    /// <param name="configure">A callback invoked during the dynamic update pass.</param>
    public void RegisterDynamicUpdate(Action<Visual> configure)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(configure);
        _dynamicUpdates ??= new List<Action<Visual>>();
        _dynamicUpdates.Add(configure);
        _dynamicUpdatesDirty = true;
    }

    /// <summary>
    /// Installs or replaces a computed-property recipe for this visual.
    /// </summary>
    /// <remarks>
    /// This API is intended for source-generated fluent configuration. The callback is executed during the visual's dynamic
    /// update pass so bindable reads are tracked against the real upstream dependencies.
    /// </remarks>
    /// <param name="accessor">The accessor that identifies the target property.</param>
    /// <param name="apply">The callback that computes and applies the property value.</param>
    /// <param name="state">Opaque state captured for the callback.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetComputedProperty(BindingAccessor accessor, Action<Visual, object?> apply, object? state)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(apply);

        _computedProperties ??= new Dictionary<BindingAccessor, ComputedPropertyRecipe>();
        _computedProperties[accessor] = new ComputedPropertyRecipe
        {
            Accessor = accessor,
            State = state,
            Apply = apply,
        };

        EnsureComputedPropertyRunner();

        if (App is null)
        {
            using (BindingManager.Current.SuppressReadTracking())
            {
                apply(this, state);
            }
        }

        MarkDynamicUpdateDirty();
    }

    /// <summary>
    /// Removes a previously installed computed-property recipe from this visual.
    /// </summary>
    /// <param name="accessor">The accessor that identifies the target property.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void ClearComputedProperty(BindingAccessor accessor)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(accessor);

        if (_computedProperties is null || !_computedProperties.Remove(accessor))
        {
            return;
        }

        if (_computedProperties.Count == 0)
        {
            _computedProperties = null;
        }

        MarkDynamicUpdateDirty();
    }

    internal bool HasComputedProperty(BindingAccessor accessor)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(accessor);
        return _computedProperties is not null && _computedProperties.ContainsKey(accessor);
    }

    internal void AttachToApp(TerminalApp app)
    {
        using (BindingManager.Current.SuppressReadTracking())
        {
            App = app;

            // A visual may be detached and later re-attached (e.g. popups, dialogs, or moved subtrees).
            // Cached layout/render state must not survive across attachments, otherwise we can skip Arrange/Measure
            // and keep stale bounds (e.g. a Popup re-used with a different placement).
            _measureDirty = true;
            _arrangeDirty = true;
            _prepareChildrenDirty = true;
            _hasLastMeasure = false;
            _hasLastArrange = false;
            _measureDeps = null;
            _arrangeDeps = null;
            _prepareChildrenDeps = null;
            _dynamicUpdateDeps = null;
            _renderDeps = null;
            _graphicsRenderDeps = null;
            if (_dynamicUpdates is not null)
            {
                _dynamicUpdatesDirty = true;
            }

            OnAttachedToApp(app);

            if (this is IAnimatedVisual animated)
            {
                app.RegisterAnimatedVisual(animated);
            }

            if (this is IGraphicsRenderableVisual graphics)
            {
                app.RegisterGraphicsRenderableVisual(this, graphics);
            }

            for (var i = 0; i < ChildrenCount; i++)
            {
                var child = GetChild(i);
                if (child.App is null)
                {
                    child.AttachToApp(app);
                }
            }
        }
    }

    internal void DetachFromApp()
    {
        using (BindingManager.Current.SuppressReadTracking())
        {
            var app = App;
            if (app is null)
            {
                return;
            }

            // Remove the focus from an element that has been detached
            if (HasFocus)
            {
                app.FocusedElement = null;
            }

            if (ReferenceEquals(app.SelectionOwnerElement, this))
            {
                app.ClearSelectionOwnerIfMatches(this);
            }
            
            for (var i = 0; i < ChildrenCount; i++)
            {
                var child = GetChild(i);
                if (child.App is not null)
                {
                    child.DetachFromApp();
                }
            }

            // Reset cached state so a detached subtree is always considered out-of-date if it is re-attached later.
            _prepareChildrenDirty = true;
            _measureDirty = true;
            _arrangeDirty = true;
            _hasLastMeasure = false;
            _hasLastArrange = false;
            _prepareChildrenDeps = null;
            _measureDeps = null;
            _arrangeDeps = null;
            _renderDeps = null;
            _graphicsRenderDeps = null;

            if (this is IGraphicsRenderableVisual graphics)
            {
                app.UnregisterGraphicsRenderableVisual(this, graphics);
            }

            if (this is IAnimatedVisual animated)
            {
                app.UnregisterAnimatedVisual(animated);
            }

            app.UnregisterDependencies(this);

            App = null;
            OnDetachedFromApp(app);
        }
    }

    /// <summary>
    /// Called when this visual is attached to a <see cref="TerminalApp"/>.
    /// </summary>
    /// <param name="app">The owning application.</param>
    protected virtual void OnAttachedToApp(TerminalApp app) { }

    /// <summary>
    /// Called when this visual is detached from a <see cref="TerminalApp"/>.
    /// </summary>
    /// <param name="app">The owning application.</param>
    protected virtual void OnDetachedFromApp(TerminalApp app) { }

    /// <summary>
    /// Synchronizes internal children from bindable properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls that forward user-facing properties into internal visuals should do the forwarding here rather than
    /// from bindable property callbacks. Property reads performed during this method are tracked so that future
    /// changes automatically trigger a refresh before layout/rendering.
    /// </para>
    /// <para>
    /// Implementations should be idempotent and only apply changes when the target child differs (for example, via
    /// <see cref="object.ReferenceEquals(object?, object?)"/> checks).
    /// </para>
    /// </remarks>
    protected virtual void PrepareChildren()
    {
    }

    private void EnsureChildrenPrepared()
    {
        if (!_prepareChildrenDirty)
        {
            return;
        }

        var metrics = App?.DebugOverlayMetrics;
        var startTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();

        using (var session = BindingManager.Current.StartTracking())
        {
            PrepareChildren();
            if (metrics is not null)
            {
                metrics.RecordPrepareChildren(Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
            }

            if (UnionDependencies(ref _prepareChildrenDeps, session.Reads) && App is not null)
            {
                App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.PrepareChildren, _prepareChildrenDeps!);
            }

            _prepareChildrenDirty = false;
        }
    }

    /// <summary>
    /// Measures this visual using the provided available size.
    /// </summary>
    /// <remarks>
    /// This overload is a convenience wrapper that converts the size into unbounded constraints as needed.
    /// Prefer calling <see cref="Measure(in LayoutConstraints)"/> when you need explicit constraints.
    /// </remarks>
    /// <param name="availableSize">The available size.</param>
    public void Measure(Size availableSize)
        => Measure(LayoutConstraints.FromMaxSize(availableSize));

    /// <summary>
    /// Measures this visual under the provided layout constraints.
    /// </summary>
    /// <remarks>
    /// The result is a finite <see cref="SizeHints"/> value. “Fill” behavior is represented by flex and/or alignment during arrange,
    /// not by returning an infinite desired size.
    /// </remarks>
    /// <param name="constraints">The layout constraints.</param>
    /// <returns>The computed size hints.</returns>
    public SizeHints Measure(in LayoutConstraints constraints)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();

        if (!_measureDirty && _hasLastMeasure && constraints.Equals(_lastMeasureConstraints))
        {
            App?.DebugOverlayMetrics?.RecordMeasureCacheHit();
            return MeasureHints;
        }

        var previousDesiredWithoutMargin = _lastDesiredSizeWithoutMargin;

        var metrics = App?.DebugOverlayMetrics;
        var startTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();

        SizeHints measureHints;
        using (var session = BindingManager.Current.StartTracking())
        {
            if (!IsVisible)
            {
                _lastDesiredSizeWithoutMargin = Size.Zero;
                measureHints = SizeHints.Fixed(Size.Zero);
                MeasureHints = measureHints;
                DesiredSize = Size.Zero;
            }
            else
            {
                var margin = Margin;
                var innerConstraints = ApplyMeasureConstraints(Deflate(constraints, margin));

                var hints = MeasureCore(innerConstraints).Normalize();
                hints = ClampHintsToConstraints(hints, innerConstraints);

                _lastDesiredSizeWithoutMargin = hints.Natural;

                // Inflate hints by margin for the parent's perspective.
                var inflatedHints = Inflate(hints, margin).Normalize();
                inflatedHints = ClampHintsToConstraints(inflatedHints, constraints);

                measureHints = inflatedHints;
                MeasureHints = measureHints;
                DesiredSize = inflatedHints.Natural;
            }

            if (!previousDesiredWithoutMargin.Equals(_lastDesiredSizeWithoutMargin))
            {
                MarkArrangeDirtyLocal();
            }

            if (UnionDependencies(ref _measureDeps, session.Reads) && App is not null)
            {
                App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.Measure, _measureDeps!);
            }

            _measureDirty = false;
            _hasLastMeasure = true;
            _lastMeasureConstraints = constraints;
        }

        if (metrics is not null)
        {
            metrics.RecordMeasure(Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
        }
         
        return measureHints;
    }

    /// <summary>
    /// Arranges this visual into the provided final rectangle.
    /// </summary>
    /// <param name="finalRect">The arranged rectangle.</param>
    public void Arrange(Rectangle finalRect)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();

        if (!_arrangeDirty && _hasLastArrange && finalRect.Equals(_lastArrangeRect))
        {
            App?.DebugOverlayMetrics?.RecordArrangeCacheHit();
            return;
        }

        var metrics = App?.DebugOverlayMetrics;
        var startTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();

        using (var session = BindingManager.Current.StartTracking())
        {
            if (!IsVisible)
            {
                Bounds = new Rectangle(finalRect.X, finalRect.Y, 0, 0);
            }
            else
            {
                var margin = Margin;
                var innerSlot = Deflate(finalRect, margin);
                var arrangedRect = ApplyArrangeConstraints(innerSlot);
                arrangedRect = PrepareArrangeBounds(arrangedRect);
                Bounds = arrangedRect;
                ArrangeCore(arrangedRect);
            }
            if (UnionDependencies(ref _arrangeDeps, session.Reads) && App is not null)
            {
                App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.Arrange, _arrangeDeps!);
            }

            _arrangeDirty = false;
            _hasLastArrange = true;
            _lastArrangeRect = finalRect;
        }

        if (metrics is not null)
        {
            metrics.RecordArrange(Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
        }
    }

    /// <summary>
    /// Prepares the final layout rectangle for arranging the element's content. Derived classes can override this
    /// method to modify the arrangement rectangle before layout is performed.
    /// </summary>
    /// <remarks>Override this method in a derived class to customize how the arrangement rectangle is
    /// prepared prior to layout. This can be used to adjust margins, alignment, or other layout considerations before
    /// the element is arranged.</remarks>
    /// <param name="finalRect">The rectangle that defines the final area within which the element should be arranged.</param>
    /// <returns>A rectangle representing the area to be used for arranging the element's content. By default, returns the input
    /// rectangle unchanged.</returns>
    protected virtual Rectangle PrepareArrangeBounds(in Rectangle finalRect) => finalRect;

    /// <summary>
    /// Performs the core measure logic for this visual.
    /// </summary>
    /// <remarks>
    /// The default implementation measures all children (if any) and computes max natural size. Controls typically override this
    /// to provide their intrinsic sizing behavior.
    /// </remarks>
    /// <param name="constraints">The layout constraints.</param>
    /// <returns>The computed size hints.</returns>
    protected virtual SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var width = 0;
        var height = 0;

        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            var childHints = child.Measure(constraints);
            width = Math.Max(width, childHints.Natural.Width);
            height = Math.Max(height, childHints.Natural.Height);
        }

        var natural = new Size(
            Math.Clamp(width, 0, LayoutConstants.MaxFinite),
            Math.Clamp(height, 0, LayoutConstants.MaxFinite));

        var min = new Size(
            Math.Clamp(MinWidth, 0, natural.Width),
            Math.Clamp(MinHeight, 0, natural.Height));

        var maxW = MaxWidth == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Clamp(MaxWidth, natural.Width, LayoutConstants.MaxFinite);
        var maxH = MaxHeight == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Clamp(MaxHeight, natural.Height, LayoutConstants.MaxFinite);

        var growX = HorizontalAlignment == Align.Stretch ? 1 : 0;
        var growY = VerticalAlignment == Align.Stretch ? 1 : 0;
        var shrinkX = natural.Width > min.Width ? 1 : 0;
        var shrinkY = natural.Height > min.Height ? 1 : 0;

        return SizeHints.Flex(min, natural, new Size(maxW, maxH), growX: growX, growY: growY, shrinkX: shrinkX, shrinkY: shrinkY).Normalize();
    }

    /// <summary>
    /// Performs the core arrange logic for this visual.
    /// </summary>
    /// <remarks>
    /// The default implementation arranges all children into the same final rectangle. Containers override this
    /// to position children according to their layout policy.
    /// </remarks>
    /// <param name="finalRect">The arranged rectangle.</param>
    protected virtual void ArrangeCore(in Rectangle finalRect)
    {
        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            child.Arrange(finalRect);
        }
    }

    private static LayoutConstraints Deflate(in LayoutConstraints constraints, Thickness thickness)
    {
        var minW = Math.Max(0, constraints.MinWidth - thickness.Horizontal);
        var minH = Math.Max(0, constraints.MinHeight - thickness.Vertical);

        var maxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - thickness.Horizontal);
        var maxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - thickness.Vertical);

        return new LayoutConstraints(minW, maxW, minH, maxH);
    }

    private static SizeHints Inflate(SizeHints hints, Thickness thickness)
    {
        var horizontal = Math.Max(0, thickness.Horizontal);
        var vertical = Math.Max(0, thickness.Vertical);

        var minW = LayoutConstants.ClampFinite(hints.Min.Width + horizontal);
        var minH = LayoutConstants.ClampFinite(hints.Min.Height + vertical);

        var natW = LayoutConstants.ClampFinite(hints.Natural.Width + horizontal);
        var natH = LayoutConstants.ClampFinite(hints.Natural.Height + vertical);

        var maxW = hints.Max.Width == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(hints.Max.Width + horizontal);
        var maxH = hints.Max.Height == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(hints.Max.Height + vertical);

        return hints with
        {
            Min = new Size(minW, minH),
            Natural = new Size(natW, natH),
            Max = new Size(maxW, maxH),
        };
    }

    private LayoutConstraints ApplyMeasureConstraints(in LayoutConstraints constraints)
    {
        var minW = Math.Max(0, constraints.MinWidth);
        var minH = Math.Max(0, constraints.MinHeight);

        var maxW = constraints.MaxWidth;
        var maxH = constraints.MaxHeight;

        var ownMinW = Math.Max(0, MinWidth);
        var ownMinH = Math.Max(0, MinHeight);
        var ownMaxW = Math.Max(0, MaxWidth);
        var ownMaxH = Math.Max(0, MaxHeight);

        minW = Math.Max(minW, ownMinW);
        minH = Math.Max(minH, ownMinH);

        if (maxW != LayoutConstants.Infinite)
        {
            maxW = Math.Min(maxW, ownMaxW);
        }
        else if (ownMaxW != LayoutConstants.Infinite)
        {
            maxW = ownMaxW;
        }

        if (maxH != LayoutConstants.Infinite)
        {
            maxH = Math.Min(maxH, ownMaxH);
        }
        else if (ownMaxH != LayoutConstants.Infinite)
        {
            maxH = ownMaxH;
        }

        return new LayoutConstraints(minW, maxW, minH, maxH);
    }

    private SizeHints ClampHintsToConstraints(SizeHints hints, in LayoutConstraints constraints)
    {
        var normalized = hints.Normalize();

        var min = constraints.Clamp(normalized.Min);
        var nat = constraints.Clamp(normalized.Natural);
        nat = new Size(Math.Max(min.Width, nat.Width), Math.Max(min.Height, nat.Height));

        var maxW = normalized.Max.Width;
        if (maxW != LayoutConstants.Infinite)
        {
            var maxWLimit = constraints.MaxWidth == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : constraints.MaxWidth;
            maxW = Math.Clamp(maxW, nat.Width, maxWLimit);
        }

        var maxH = normalized.Max.Height;
        if (maxH != LayoutConstants.Infinite)
        {
            var maxHLimit = constraints.MaxHeight == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : constraints.MaxHeight;
            maxH = Math.Clamp(maxH, nat.Height, maxHLimit);
        }

        var growX = normalized.FlexGrowX;
        var growY = normalized.FlexGrowY;
        var shrinkX = normalized.FlexShrinkX;
        var shrinkY = normalized.FlexShrinkY;

        // Treat Stretch as willingness to grow on that axis (bounded by MaxWidth/MaxHeight).
        if (HorizontalAlignment == Align.Stretch && MaxWidth == LayoutConstants.Infinite)
        {
            maxW = LayoutConstants.Infinite;
            growX = Math.Max(growX, 1);
            shrinkX = Math.Max(shrinkX, 1);
        }

        if (VerticalAlignment == Align.Stretch && MaxHeight == LayoutConstants.Infinite)
        {
            maxH = LayoutConstants.Infinite;
            growY = Math.Max(growY, 1);
            shrinkY = Math.Max(shrinkY, 1);
        }

        // Ensure min/natural are finite.
        if (min.Width >= LayoutConstants.Infinite || min.Height >= LayoutConstants.Infinite ||
            nat.Width >= LayoutConstants.Infinite || nat.Height >= LayoutConstants.Infinite)
        {
            throw new LayoutException($"Measure produced an infinite Min/Natural size for {GetType().Name}. Min={min} Natural={nat} Constraints={constraints}");
        }

        return new SizeHints
        {
            Min = min,
            Natural = nat,
            Max = new Size(maxW, maxH),
            FlexGrowX = growX,
            FlexGrowY = growY,
            FlexShrinkX = shrinkX,
            FlexShrinkY = shrinkY,
        }.Normalize();
    }

    private Rectangle ApplyArrangeConstraints(Rectangle slot)
    {
        var maxW = Math.Max(Math.Max(0, MaxWidth), MinWidth);
        var maxH = Math.Max(Math.Max(0, MaxHeight), MinHeight);

        var desired = _lastDesiredSizeWithoutMargin;
        var desiredW = Math.Clamp(desired.Width, Math.Max(0, MinWidth), maxW);
        var desiredH = Math.Clamp(desired.Height, Math.Max(0, MinHeight), maxH);

        var slotW = Math.Max(0, slot.Width);
        var slotH = Math.Max(0, slot.Height);

        var w = HorizontalAlignment == Align.Stretch ? slotW : Math.Min(slotW, desiredW);
        var h = VerticalAlignment == Align.Stretch ? slotH : Math.Min(slotH, desiredH);

        w = Math.Clamp(w, 0, maxW == int.MaxValue ? w : maxW);
        h = Math.Clamp(h, 0, maxH == int.MaxValue ? h : maxH);

        var x = slot.X;
        var y = slot.Y;

        if (HorizontalAlignment == Align.Center)
        {
            x += (slotW - w) / 2;
        }
        else if (HorizontalAlignment == Align.End)
        {
            x += slotW - w;
        }

        if (VerticalAlignment == Align.Center)
        {
            y += (slotH - h) / 2;
        }
        else if (VerticalAlignment == Align.End)
        {
            y += slotH - h;
        }

        return new Rectangle(x, y, w, h);
    }

    private static Size Inflate(Size size, Thickness thickness)
        => new(
            Math.Max(0, size.Width + thickness.Horizontal),
            Math.Max(0, size.Height + thickness.Vertical));

    private static Size Deflate(Size size, Thickness thickness)
        => new(
            Math.Max(0, size.Width - thickness.Horizontal),
            Math.Max(0, size.Height - thickness.Vertical));

    private static Rectangle Deflate(Rectangle rect, Thickness thickness)
        => new(
            rect.X + thickness.Left,
            rect.Y + thickness.Top,
            Math.Max(0, rect.Width - thickness.Horizontal),
            Math.Max(0, rect.Height - thickness.Vertical));

    internal void RenderTree(CellBuffer buffer)
    {
        VerifyAccess();
        if (!buffer.ClipIntersects(Bounds))
        {
            App?.DebugOverlayMetrics?.RecordRenderClipSkip();
            return;
        }

        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();

        var metrics = App?.DebugOverlayMetrics;
        bool visible;
        using (var session = BindingManager.Current.StartTracking())
        {
            visible = IsVisible;
            if (visible)
            {
                buffer.PushClip(Bounds);
                if (metrics is not null)
                {
                    var startTimestamp = Stopwatch.GetTimestamp();
                    RenderOverride(buffer);
                    metrics.RecordRenderOverride(Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
                }
                else
                {
                    RenderOverride(buffer);
                }
                buffer.PopClip();
            }

            if (ReplaceDependencies(ref _renderDeps, session.Reads) && App is not null)
            {
                App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.Render, _renderDeps!);
            }
        }

        if (!visible)
        {
            return;
        }

        buffer.PushClip(Bounds);
        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            child.RenderTree(buffer);
        }
        buffer.PopClip();
    }

    /// <summary>
    /// Renders the visual into the provided buffer.
    /// </summary>
    /// <remarks>
    /// Implementations should render within <see cref="Bounds"/>. Clipping is handled by the framework.
    /// </remarks>
    /// <param name="buffer">The target cell buffer.</param>
    protected virtual void RenderOverride(CellBuffer buffer)
    {
        _ = buffer;
    }

    /// <summary>
    /// Enumerates this visual and its descendants depth-first.
    /// </summary>
    /// <returns>A depth-first enumeration of visuals.</returns>
    public IEnumerable<Visual> EnumerateVisualsDepthFirst()
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();
        yield return this;

        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            foreach (var nested in child.EnumerateVisualsDepthFirst())
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// Returns the deepest visible visual that contains the specified point.
    /// </summary>
    /// <param name="x">The x coordinate in this visual coordinate space.</param>
    /// <param name="y">The y coordinate in this visual coordinate space.</param>
    /// <returns>The visual under the point, or <c>null</c> if none.</returns>
    public Visual? HitTest(int x, int y)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();
        EnsureChildrenPrepared();
        if (!IsVisible || !_isHitTestVisible || !Bounds.Contains(x, y))
        {
            return null;
        }

        for (var i = ChildrenCount - 1; i >= 0; i--)
        {
            var hit = GetChild(i).HitTest(x, y);
            if (hit is not null)
            {
                return hit;
            }
        }

        return this;
    }

    private void EnsureDynamicUpdatesApplied()
    {
        if (!_dynamicUpdatesDirty || _dynamicUpdates is null)
        {
            return;
        }

        _dynamicUpdatesDirty = false;

        var metrics = App?.DebugOverlayMetrics;
        var startTimestamp = metrics is null ? 0 : Stopwatch.GetTimestamp();

        using (var session = BindingManager.Current.StartTracking())
        {
            var app = App;

            for (var i = 0; i < _dynamicUpdates.Count; i++)
            {
                _dynamicUpdates[i](this);
            }

            OnDynamicUpdated();

            if (ReplaceDependencies(ref _dynamicUpdateDeps, session.Reads) && App is not null)
            {
                App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.DynamicUpdate, _dynamicUpdateDeps!);
            }
        }

        if (metrics is not null)
        {
            metrics.RecordDynamicUpdate(Math.Max(0, Stopwatch.GetTimestamp() - startTimestamp));
        }
    }

    private void EnsureComputedPropertyRunner()
    {
        if (_hasComputedPropertyRunner)
        {
            return;
        }

        _dynamicUpdates ??= new List<Action<Visual>>();
        _dynamicUpdates.Insert(0, static visual => visual.RunComputedProperties());
        _hasComputedPropertyRunner = true;
    }

    private void RunComputedProperties()
    {
        if (_computedProperties is null || _computedProperties.Count == 0)
        {
            return;
        }

        var recipes = _computedProperties.Values.ToArray();
        for (var i = 0; i < recipes.Length; i++)
        {
            var recipe = recipes[i];
            recipe.Apply(this, recipe.State);
        }
    }

    /// <summary>
    /// Invoked when the dynamic state of the object has been updated.
    /// </summary>
    protected virtual void OnDynamicUpdated()
    {
    }

    private static bool ReplaceDependencies(ref HashSet<Binding>? target, IReadOnlyCollection<Binding> dependencies)
    {
        if (target is null)
        {
            target = new HashSet<Binding>(BindingReferenceComparer.Instance);
        }
        else if (target.SetEquals(dependencies))
        {
            return false;
        }

        target.Clear();

        foreach (var dep in dependencies)
        {
            target.Add(dep);
        }

        return true;
    }

    private static bool UnionDependencies(ref HashSet<Binding>? target, IReadOnlyCollection<Binding> dependencies)
    {
        if (target is null)
        {
            target = new HashSet<Binding>(BindingReferenceComparer.Instance);
            foreach (var dep in dependencies)
            {
                target.Add(dep);
            }
            return true;
        }

        var changed = false;
        foreach (var dep in dependencies)
        {
            changed |= target.Add(dep);
        }
        return changed;
    }

    internal void UpdateGraphicsRenderDependencies(IReadOnlyCollection<Binding> dependencies)
    {
        if (ReplaceDependencies(ref _graphicsRenderDeps, dependencies) && App is not null)
        {
            App.UpdateBindingReadsForVisual(this, TerminalApp.DependencyKind.GraphicsRender, _graphicsRenderDeps!);
        }
    }

    internal void IncrementGraphicsRenderableSubtreeCount()
    {
        _graphicsRenderableSubtreeCount++;
    }

    internal void DecrementGraphicsRenderableSubtreeCount()
    {
        if (_graphicsRenderableSubtreeCount > 0)
        {
            _graphicsRenderableSubtreeCount--;
        }
    }

    internal void MarkDynamicUpdateDirty()
    {
        _dynamicUpdatesDirty = true;
        _dynamicUpdateDeps = null;
        MarkMeasureDirty();
    }

    internal void MarkPrepareChildrenDirty()
    {
        _prepareChildrenDirty = true;
        _prepareChildrenDeps = null;

        // PrepareChildren can attach/detach children or bridge user-facing properties to internal visuals,
        // which affects subsequent layout. Invalidate layout caching so the refresh is observed immediately.
        MarkMeasureDirty();
    }

    // NOT USED FOR NOW
    //private void MarkMeasureDirtyUpAndDown()
    //{
    //    MarkMeasureDirty();
    //    MarkDirtyDown();
    //}

    // NOT USED FOR NOW
    //private void MarkDirtyDown()
    //{
    //    MarkMeasureDirtyLocal();
    //    for (var i = 0; i < ChildrenCount; i++)
    //    {
    //        var child = GetChild(i);
    //        child.MarkDirtyDown();
    //    }
    //}

    private void MarkMeasureDirtyLocal()
    {
        _measureDirty = true;
        _hasLastMeasure = false;
        _measureDeps = null;
        MarkArrangeDirtyLocal();
    }

    internal void MarkMeasureDirty()
    {
        // This is disabled for now, as it seems that we can get into a situation where a parent is not marked as dirty but a local child is.
        // In that case, it would stop propagating the dirty state up, leading to incorrect layout.

        //if (_measureDirty && !_hasLastMeasure && _measureDeps is null && _arrangeDirty && !_hasLastArrange && _arrangeDeps is null)
        //{
        //    return;
        //}

        MarkMeasureDirtyLocal();
        Parent?.MarkMeasureDirty();
    }

    internal void MarkArrangeDirtyLocal()
    {
        _arrangeDirty = true;
        _hasLastArrange = false;
        _arrangeDeps = null;
    }
    
    internal void MarkArrangeDirty()
    {
        MarkArrangeDirtyLocal();
        Parent?.MarkArrangeDirty();
    }
    
    internal void MarkRenderDirty()
    {
        // Rendering is currently full-frame, so we only request a redraw from the app.
        // Layout caching uses measure/arrange dirtiness; render dirtiness is tracked by TerminalApp.
    }

    /// <summary>
    /// Registers a handler for a routed event on this visual.
    /// </summary>
    /// <typeparam name="TArgs">The routed event args type.</typeparam>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="handler">The handler to add.</param>
    protected void AddHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
        where TArgs : EventArgs
        => AddHandler(routedEvent, handler, handledEventsToo: false);

    /// <summary>
    /// Registers a handler for a routed event on this visual.
    /// </summary>
    /// <typeparam name="TArgs">The routed event args type.</typeparam>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="handler">The handler to add.</param>
    /// <param name="handledEventsToo">When <see langword="true"/>, invokes the handler even after the event has been marked handled.</param>
    protected void AddHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler, bool handledEventsToo)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = handledEventsToo
            ? _handledEventHandlers ??= new Dictionary<object, Delegate?>()
            : _handlers ??= new Dictionary<object, Delegate?>();

        if (handlers.TryGetValue(routedEvent, out var existing))
        {
            handlers[routedEvent] = Delegate.Combine(existing, handler);
        }
        else
        {
            handlers.Add(routedEvent, handler);
        }
    }

    /// <summary>
    /// Unregisters a handler for a routed event on this visual.
    /// </summary>
    /// <typeparam name="TArgs">The routed event args type.</typeparam>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="handler">The handler to remove.</param>
    protected void RemoveHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
        where TArgs : EventArgs
        => RemoveHandler(routedEvent, handler, handledEventsToo: false);

    /// <summary>
    /// Unregisters a handler for a routed event on this visual.
    /// </summary>
    /// <typeparam name="TArgs">The routed event args type.</typeparam>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="handler">The handler to remove.</param>
    /// <param name="handledEventsToo">When <see langword="true"/>, removes from the handled-events-too handler list.</param>
    protected void RemoveHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler, bool handledEventsToo)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = handledEventsToo ? _handledEventHandlers : _handlers;
        if (handlers is null)
        {
            return;
        }

        if (!handlers.TryGetValue(routedEvent, out var existing))
        {
            return;
        }

        var updated = Delegate.Remove(existing, handler);
        if (updated is null)
        {
            handlers.Remove(routedEvent);
        }
        else
        {
            handlers[routedEvent] = updated;
        }
    }

    /// <summary>
    /// Raises a routed event starting from this visual.
    /// </summary>
    /// <typeparam name="TArgs">The routed event args type.</typeparam>
    /// <param name="routedEvent">The routed event identifier.</param>
    /// <param name="args">The event arguments.</param>
    protected internal void RaiseEvent<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs args)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(args);

        if (BindingManager.Current.IsTracking)
        {
            BindingManager.Current.RunAfterTracking(() => RaiseEvent(routedEvent, args));
            return;
        }

        if (args is RoutedEventArgs routedArgs)
        {
            routedArgs.OriginalSource ??= this;
            routedArgs.RoutingPhase = RoutingPhase.None;
        }

        var chain = new List<Visual>();
        for (var v = this; v is not null; v = v.Parent)
        {
            chain.Add(v);
        }

        chain.Reverse();

        var strategy = routedEvent.RoutingStrategy;
        if (strategy == RoutingStrategy.Direct)
        {
            if (args is RoutedEventArgs directArgs)
            {
                directArgs.RoutingPhase = RoutingPhase.Direct;
            }

            InvokeHandlers(
                routedEvent,
                args,
                invokeRegular: args is not RoutedEventArgs { Handled: true },
                invokeHandledToo: true);
            return;
        }

        if ((strategy & RoutingStrategy.Preview) != 0)
        {
            foreach (var v in chain)
            {
                if (args is RoutedEventArgs previewArgs)
                {
                    previewArgs.RoutingPhase = RoutingPhase.Preview;
                }

                v.InvokeHandlers(
                    routedEvent,
                    args,
                    invokeRegular: args is not RoutedEventArgs { Handled: true },
                    invokeHandledToo: true);
            }
        }

        if ((strategy & RoutingStrategy.Bubble) != 0)
        {
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                if (args is RoutedEventArgs bubbleArgs)
                {
                    bubbleArgs.RoutingPhase = RoutingPhase.Bubble;
                }

                chain[i].InvokeHandlers(
                    routedEvent,
                    args,
                    invokeRegular: args is not RoutedEventArgs { Handled: true },
                    invokeHandledToo: true);
            }
        }
    }

    private void InvokeHandlers<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs args, bool invokeRegular, bool invokeHandledToo)
        where TArgs : EventArgs
    {
        // Disabled visuals do not participate in input routing. We still allow routing to proceed through
        // the visual tree so that enabled ancestors (e.g. a ScrollViewer) can react to input even when
        // the pointer is over a disabled child.
        if (!IsEnabled)
        {
            return;
        }

        if (args is RoutedEventArgs routedArgs)
        {
            routedArgs.Source = this;
        }

        if (invokeRegular)
        {
            routedEvent.Dispatch(this, args);

            if (_handlers is not null && _handlers.TryGetValue(routedEvent, out var existing) && existing is EventHandler<TArgs> handler)
            {
                handler(this, args);
            }
        }

        if (invokeHandledToo && _handledEventHandlers is not null && _handledEventHandlers.TryGetValue(routedEvent, out var handledExisting) && handledExisting is EventHandler<TArgs> handledHandler)
        {
            handledHandler(this, args);
        }
    }

    /// <summary>
    /// Called when a key down event is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnKeyDown(KeyEventArgs e) { }

    /// <summary>
    /// Called when the hovered state of this visual changes.
    /// </summary>
    /// <param name="value">The new hovered state.</param>
    protected virtual void OnHoveredChanged(bool value) { }

    /// <summary>
    /// Called when text input is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnTextInput(TextInputEventArgs e) { }

    /// <summary>
    /// Called when paste input is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnPaste(PasteEventArgs e) { }

    /// <summary>
    /// Called when a pointer move event is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerMoved(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer press event is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerPressed(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer release event is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerReleased(PointerEventArgs e) { }

    /// <summary>
    /// Called when a pointer wheel event is routed to this visual.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerWheel(PointerEventArgs e) { }

    /// <summary>
    /// Defines an implicit conversion from a factory function to a computed visual representation.
    /// </summary>
    /// <remarks>This operator allows a factory function to be used wherever a <see cref="Visual"/> is
    /// expected, enabling deferred or dynamic creation of visuals. The factory may be called multiple times depending
    /// on usage.</remarks>
    /// <param name="factory">A function that returns a <see cref="Visual"/> instance or <see langword="null"/>. The function is invoked to
    /// produce the visual when needed.</param>
    public static implicit operator Visual(Func<Visual?> factory) => new ComputedVisual(factory);

    /// <summary>
    /// Converts a state object containing a visual value to a visual instance.
    /// </summary>
    /// <remarks>This implicit conversion allows a State&lt;Visual?> to be used wherever a Visual is expected.
    /// The resulting Visual instance reflects the current value of the provided state and updates automatically when
    /// the state changes.</remarks>
    /// <param name="state">The state object that holds the current visual value to be converted. Cannot be null.</param>
    public static implicit operator Visual(State<Visual?> state) => new ComputedVisual(state);
}
