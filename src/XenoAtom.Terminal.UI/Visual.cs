// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Animation;

namespace XenoAtom.Terminal.UI;

public abstract partial class Visual : DispatcherObject, IVisualElement
{
    private Dictionary<object, Delegate?>? _handlers;
    private List<KeyBinding>? _keyBindings;
    private Dictionary<object, object?>? _environment;
    private List<Action<Visual>>? _dynamicUpdates;
    private List<Collections.IDynamicUpdateResettable>? _dynamicUpdateLists;

    private Size _desiredSizeWithoutMargin;

    private bool _dynamicUpdatesDirty;
    private bool _measureDirty = true;
    private bool _arrangeDirty = true;
    private HashSet<Binding>? _dynamicUpdateDeps;
    private HashSet<Binding>? _measureDeps;
    private HashSet<Binding>? _arrangeDeps;
    private HashSet<Binding>? _renderDeps;

    private bool _hasLastMeasure;
    private LayoutConstraints _lastMeasureConstraints;
    private bool _hasLastArrange;
    private Rectangle _lastArrangeRect;

    public Visual? Parent { get; private set; }

    public Rectangle Bounds { get; protected set; }

    public Size DesiredSize { get; private set; }

    public SizeHints MeasureHints { get; private set; }

    internal Size DesiredSizeWithoutMargin => _desiredSizeWithoutMargin;

    public TerminalApp? App { get; private set; }

    public bool Focusable { get; protected init; }

    private sealed class __Invalidation__BindingAccessor : BindingAccessor
    {
        public static __Invalidation__BindingAccessor Instance { get; } = new();

        private __Invalidation__BindingAccessor() : base(string.Intern("$invalidate$"))
        {
        }

        public override object? GetValue(object instance) => null;

        public override void SetValue(object instance, object? value)
        {
        }
    }

    protected void Invalidate()
    {
        VerifyAccess();
        MarkMeasureDirty();
        BindingManager.Current.NotifyValueChanged(this, __Invalidation__BindingAccessor.Instance);
    }

    protected Visual()
    {
        _isVisible = true;
        _isEnabled = true;
        _maxWidth = int.MaxValue;
        _maxHeight = int.MaxValue;
    }

    [Bindable]
    public partial HorizontalAlignment HorizontalAlignment { get; set; }

    [Bindable]
    public partial VerticalAlignment VerticalAlignment { get; set; }

    [Bindable]
    public partial int MinWidth { get; set; }

    [Bindable]
    public partial int MinHeight { get; set; }

    [Bindable]
    public partial int MaxWidth { get; set; }

    [Bindable]
    public partial int MaxHeight { get; set; }

    [Bindable]
    public partial Thickness Margin { get; set; }

    [Bindable]
    public partial bool IsVisible { get; set; }

    [Bindable]
    public partial bool IsEnabled { get; set; }

    [Bindable]
    public partial bool IsHovered { get; internal set; }

    partial void OnMinWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMinHeightChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMaxWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMaxHeightChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    public void AddKeyBinding(Input.TerminalKeyGesture gesture, Action action)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(action);
        _keyBindings ??= new List<KeyBinding>();
        for (var i = 0; i < _keyBindings.Count; i++)
        {
            var existing = _keyBindings[i];
            if (existing.Gesture.Equals(gesture))
            {
                _keyBindings[i] = new KeyBinding { Gesture = gesture, Action = action };
                return;
            }
        }

        _keyBindings.Add(new KeyBinding { Gesture = gesture, Action = action });
    }

    internal bool TryHandleKeyBinding(KeyEventArgs e)
    {
        if (_keyBindings is null)
        {
            return false;
        }

        for (var i = 0; i < _keyBindings.Count; i++)
        {
            var binding = _keyBindings[i];
            if (binding.Gesture.Matches(e.RawEvent))
            {
                binding.Action();
                e.Handled = true;
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

    internal int GetChildrenCount() => ChildrenCount;

    internal Visual GetChildUnsafe(int index) => GetChild(index);

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
    }

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
    }

    internal void AttachCollectionChild(Visual child) => AttachChild(child);

    internal void DetachCollectionChild(Visual child) => DetachChild(child);

    public void Set<T>(T value) where T : IStyle<T> => Set(T.Key, value);

    public void Set<T>(StyleKey<T> key, T value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        _environment ??= new Dictionary<object, object?>();
        _environment[key] = value;
        BindingManager.Current.NotifyValueChanged(this, key.DependencyAccessor);
    }

    public T Get<T>() where T : IStyle<T> => Get(T.Key);

    public T Get<T>(StyleKey<T> key)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);

        Visual? root = null;

        for (var v = this; v is not null; v = v.Parent)
        {
            root = v;
            if (v._environment is not null && v._environment.TryGetValue(key, out var boxed))
            {
                BindingManager.Current.RegisterRead(v, key.DependencyAccessor);
                return boxed is T typed ? typed : key.DefaultValue;
            }
        }

        BindingManager.Current.RegisterRead(root ?? this, key.DependencyAccessor);
        return key.DefaultValue;
    }

    internal bool HasLocal<T>(StyleKey<T> key)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(key);
        return _environment is not null && _environment.ContainsKey(key);
    }

    public Theme GetTheme() => Get<Theme>();

    public void RegisterDynamicUpdate(Action<Visual> configure)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(configure);
        _dynamicUpdates ??= new List<Action<Visual>>();
        _dynamicUpdates.Add(configure);
        _dynamicUpdatesDirty = true;
    }

    internal void RegisterDynamicUpdateList(Collections.IDynamicUpdateResettable list)
    {
        _dynamicUpdateLists ??= new List<Collections.IDynamicUpdateResettable>();
        if (!_dynamicUpdateLists.Contains(list))
        {
            _dynamicUpdateLists.Add(list);
        }
    }

    internal void AttachToApp(TerminalApp app)
    {
        using var _ = BindingManager.Current.DisableReadTracking();
        App = app;
        OnAttachedToApp(app);

        if (this is IAnimatedVisual animated)
        {
            app.RegisterAnimatedVisual(animated);
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

    internal void DetachFromApp()
    {
        using var _ = BindingManager.Current.DisableReadTracking();
        var app = App;
        if (app is null)
        {
            return;
        }

        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            if (child.App is not null)
            {
                child.DetachFromApp();
            }
        }

        if (this is IAnimatedVisual animated)
        {
            app.UnregisterAnimatedVisual(animated);
        }

        app.UnregisterDependencies(this);

        App = null;
        OnDetachedFromApp(app);
    }

    protected virtual void OnAttachedToApp(TerminalApp app) { }

    protected virtual void OnDetachedFromApp(TerminalApp app) { }

    public void Measure(Size availableSize)
        => Measure(LayoutConstraints.FromMaxSize(availableSize));

    public SizeHints Measure(in LayoutConstraints constraints)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();

        if (!_measureDirty && _hasLastMeasure && constraints.Equals(_lastMeasureConstraints))
        {
            return MeasureHints;
        }

        using var session = BindingManager.Current.StartTracking();
        var margin = Margin;
        var innerConstraints = ApplyMeasureConstraints(Deflate(constraints, margin));

        var hints = MeasureCore(innerConstraints).Normalize();
        hints = ClampHintsToConstraints(hints, innerConstraints);

        _desiredSizeWithoutMargin = hints.Natural;

        // Inflate hints by margin for the parent's perspective.
        var inflatedHints = Inflate(hints, margin).Normalize();
        inflatedHints = ClampHintsToConstraints(inflatedHints, constraints);

        MeasureHints = inflatedHints;
        DesiredSize = inflatedHints.Natural;

        if (UnionDependencies(ref _measureDeps, session.Dependencies) && App is not null)
        {
            App.UpdateDependencies(this, TerminalApp.DependencyKind.Measure, _measureDeps!);
        }
        _measureDirty = false;
        _hasLastMeasure = true;
        _lastMeasureConstraints = constraints;

        return MeasureHints;
    }

    public void Arrange(Rectangle finalRect)
    {
        VerifyAccess();
        EnsureDynamicUpdatesApplied();

        if (!_arrangeDirty && _hasLastArrange && finalRect.Equals(_lastArrangeRect))
        {
            return;
        }

        using var session = BindingManager.Current.StartTracking();
        var margin = Margin;
        var innerSlot = Deflate(finalRect, margin);
        var arrangedRect = ApplyArrangeConstraints(innerSlot);
        Bounds = arrangedRect;
        ArrangeCore(arrangedRect);
        if (UnionDependencies(ref _arrangeDeps, session.Dependencies) && App is not null)
        {
            App.UpdateDependencies(this, TerminalApp.DependencyKind.Arrange, _arrangeDeps!);
        }
        _arrangeDirty = false;
        _hasLastArrange = true;
        _lastArrangeRect = finalRect;
    }

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

        var growX = HorizontalAlignment == HorizontalAlignment.Stretch ? 1 : 0;
        var growY = VerticalAlignment == VerticalAlignment.Stretch ? 1 : 0;
        var shrinkX = natural.Width > min.Width ? 1 : 0;
        var shrinkY = natural.Height > min.Height ? 1 : 0;

        return SizeHints.Flex(min, natural, new Size(maxW, maxH), growX: growX, growY: growY, shrinkX: shrinkX, shrinkY: shrinkY).Normalize();
    }

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
        var horizontal = thickness.Horizontal;
        var vertical = thickness.Vertical;

        int minW, minH, natW, natH, maxW, maxH;
        try
        {
            checked
            {
                minW = LayoutConstants.ClampFinite(hints.Min.Width + horizontal);
                minH = LayoutConstants.ClampFinite(hints.Min.Height + vertical);

                natW = LayoutConstants.ClampFinite(hints.Natural.Width + horizontal);
                natH = LayoutConstants.ClampFinite(hints.Natural.Height + vertical);

                maxW = hints.Max.Width == LayoutConstants.Infinite
                    ? LayoutConstants.Infinite
                    : LayoutConstants.ClampFinite(hints.Max.Width + horizontal);
                maxH = hints.Max.Height == LayoutConstants.Infinite
                    ? LayoutConstants.Infinite
                    : LayoutConstants.ClampFinite(hints.Max.Height + vertical);
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while inflating SizeHints by margin.", ex);
        }

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
        if (HorizontalAlignment == HorizontalAlignment.Stretch && MaxWidth == LayoutConstants.Infinite)
        {
            maxW = LayoutConstants.Infinite;
            growX = Math.Max(growX, 1);
            shrinkX = Math.Max(shrinkX, 1);
        }

        if (VerticalAlignment == VerticalAlignment.Stretch && MaxHeight == LayoutConstants.Infinite)
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

        var desired = _desiredSizeWithoutMargin;
        var desiredW = Math.Clamp(desired.Width, Math.Max(0, MinWidth), maxW);
        var desiredH = Math.Clamp(desired.Height, Math.Max(0, MinHeight), maxH);

        var slotW = Math.Max(0, slot.Width);
        var slotH = Math.Max(0, slot.Height);

        var w = HorizontalAlignment == HorizontalAlignment.Stretch ? slotW : Math.Min(slotW, desiredW);
        var h = VerticalAlignment == VerticalAlignment.Stretch ? slotH : Math.Min(slotH, desiredH);

        w = Math.Clamp(w, 0, maxW == int.MaxValue ? w : maxW);
        h = Math.Clamp(h, 0, maxH == int.MaxValue ? h : maxH);

        var x = slot.X;
        var y = slot.Y;

        if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            x += (slotW - w) / 2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            x += slotW - w;
        }

        if (VerticalAlignment == VerticalAlignment.Center)
        {
            y += (slotH - h) / 2;
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
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
        EnsureDynamicUpdatesApplied();

        bool visible;
        using (var session = BindingManager.Current.StartTracking())
        {
            visible = IsVisible;
            if (visible)
            {
                buffer.PushClip(Bounds);
                RenderOverride(buffer);
                buffer.PopClip();
            }

            if (ReplaceDependencies(ref _renderDeps, session.Dependencies) && App is not null)
            {
                App.UpdateDependencies(this, TerminalApp.DependencyKind.Render, _renderDeps!);
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

    protected virtual void RenderOverride(CellBuffer buffer)
    {
        _ = buffer;
    }

    public IEnumerable<Visual> EnumerateVisualsDepthFirst()
    {
        VerifyAccess();
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

    public Visual? HitTest(int x, int y)
    {
        VerifyAccess();
        if (!IsVisible || !Bounds.Contains(x, y))
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

        if (_dynamicUpdateLists is not null)
        {
            for (var i = 0; i < _dynamicUpdateLists.Count; i++)
            {
                _dynamicUpdateLists[i].ResetForDynamicUpdate();
            }
        }

        _dynamicUpdatesDirty = false;

        using var initScope = BindingManager.Current.BeginDynamicUpdate(this);
        using var session = BindingManager.Current.StartTracking();
        var app = App;

        app?.SetVisualBeingDynamicallyInitialized(this);
        try
        {
            for (var i = 0; i < _dynamicUpdates.Count; i++)
            {
                _dynamicUpdates[i](this);
            }
        }
        finally
        {
            app?.SetVisualBeingDynamicallyInitialized(null);
        }

        if (ReplaceDependencies(ref _dynamicUpdateDeps, session.Dependencies) && App is not null)
        {
            App.UpdateDependencies(this, TerminalApp.DependencyKind.DynamicUpdate, _dynamicUpdateDeps!);
        }
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

    internal void MarkDynamicUpdateDirty()
    {
        _dynamicUpdatesDirty = true;
        _dynamicUpdateDeps = null;
        MarkMeasureDirty();
    }

    internal void MarkMeasureDirty()
    {
        _measureDirty = true;
        _arrangeDirty = true;
        _hasLastMeasure = false;
        _hasLastArrange = false;
        _measureDeps = null;
        _arrangeDeps = null;

        Parent?.MarkMeasureDirtyFromChild();
    }

    internal void MarkArrangeDirty()
    {
        _arrangeDirty = true;
        _hasLastArrange = false;
        _arrangeDeps = null;

        Parent?.MarkArrangeDirtyFromChild();
    }

    internal void MarkRenderDirty()
    {
        // Rendering is currently full-frame, so we only request a redraw from the app.
        // Layout caching uses measure/arrange dirtiness; render dirtiness is tracked by TerminalApp.
    }

    private void MarkMeasureDirtyFromChild()
    {
        if (_measureDirty && _arrangeDirty)
        {
            return;
        }

        _measureDirty = true;
        _arrangeDirty = true;
        _hasLastMeasure = false;
        _hasLastArrange = false;
        _measureDeps = null;
        _arrangeDeps = null;

        Parent?.MarkMeasureDirtyFromChild();
    }

    private void MarkArrangeDirtyFromChild()
    {
        if (_arrangeDirty)
        {
            return;
        }

        _arrangeDirty = true;
        _hasLastArrange = false;
        _arrangeDeps = null;

        Parent?.MarkArrangeDirtyFromChild();
    }

    protected void AddHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        _handlers ??= new Dictionary<object, Delegate?>();

        if (_handlers.TryGetValue(routedEvent, out var existing))
        {
            _handlers[routedEvent] = Delegate.Combine(existing, handler);
        }
        else
        {
            _handlers.Add(routedEvent, handler);
        }
    }

    protected void RemoveHandler<TArgs>(RoutedEvent<TArgs> routedEvent, EventHandler<TArgs> handler)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (_handlers is null)
        {
            return;
        }

        if (!_handlers.TryGetValue(routedEvent, out var existing))
        {
            return;
        }

        var updated = Delegate.Remove(existing, handler);
        if (updated is null)
        {
            _handlers.Remove(routedEvent);
        }
        else
        {
            _handlers[routedEvent] = updated;
        }
    }

    protected internal void RaiseEvent<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs args)
        where TArgs : EventArgs
    {
        ArgumentNullException.ThrowIfNull(routedEvent);
        ArgumentNullException.ThrowIfNull(args);

        if (args is RoutedEventArgs routedArgs)
        {
            routedArgs.OriginalSource ??= this;
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
            InvokeHandlers(routedEvent, args);
            return;
        }

        if ((strategy & RoutingStrategy.Preview) != 0)
        {
            foreach (var v in chain)
            {
                v.InvokeHandlers(routedEvent, args);
                if (args is RoutedEventArgs { Handled: true })
                {
                    return;
                }
            }
        }

        if ((strategy & RoutingStrategy.Bubble) != 0)
        {
            for (var i = chain.Count - 1; i >= 0; i--)
            {
                chain[i].InvokeHandlers(routedEvent, args);
                if (args is RoutedEventArgs { Handled: true })
                {
                    return;
                }
            }
        }
    }

    private void InvokeHandlers<TArgs>(RoutedEvent<TArgs> routedEvent, TArgs args)
        where TArgs : EventArgs
    {
        if (args is RoutedEventArgs routedArgs)
        {
            routedArgs.Source = this;
        }

        routedEvent.Dispatch(this, args);

        if (_handlers is not null && _handlers.TryGetValue(routedEvent, out var existing) && existing is EventHandler<TArgs> handler)
        {
            handler(this, args);
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnKeyDown(KeyEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnTextInput(TextInputEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnPaste(PasteEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerMoved(PointerEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerPressed(PointerEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerReleased(PointerEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerWheel(PointerEventArgs e) { }
}
