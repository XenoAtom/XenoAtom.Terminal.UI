// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Threading;
using XenoAtom.Terminal.UI.Animation;

namespace XenoAtom.Terminal.UI;

public abstract partial class Visual : DispatcherObject
{
    private Dictionary<object, Delegate?>? _handlers;
    private List<KeyBinding>? _keyBindings;
    private Dictionary<object, object?>? _environment;
    private List<Action<Visual>>? _initializers;

    private Size _desiredSizeWithoutMargin;

    private bool _initializersDirty;
    private bool _measureDirty = true;
    private bool _arrangeDirty = true;
    private bool _renderDirty = true;
    private HashSet<Binding>? _initializerDeps;
    private HashSet<Binding>? _measureDeps;
    private HashSet<Binding>? _arrangeDeps;
    private HashSet<Binding>? _renderDeps;

    public Visual? Parent { get; private set; }

    public Rectangle Bounds { get; protected set; }

    public Size DesiredSize { get; private set; }

    internal Size DesiredSizeWithoutMargin => _desiredSizeWithoutMargin;

    public TerminalApp? App { get; private set; }

    public bool Focusable { get; protected init; }

    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;

    [Bindable]
    public HorizontalAlignment HorizontalAlignment
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __HorizontalAlignment__BindingAccessor.Instance);
            return _horizontalAlignment;
        }
        set
        {
            if (_horizontalAlignment == value)
            {
                return;
            }

            _horizontalAlignment = value;
            BindingManager.Current.NotifyValueChanged(this, __HorizontalAlignment__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public VerticalAlignment VerticalAlignment
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __VerticalAlignment__BindingAccessor.Instance);
            return _verticalAlignment;
        }
        set
        {
            if (_verticalAlignment == value)
            {
                return;
            }

            _verticalAlignment = value;
            BindingManager.Current.NotifyValueChanged(this, __VerticalAlignment__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    private int _minWidth;
    private int _minHeight;
    private int _maxWidth = int.MaxValue;
    private int _maxHeight = int.MaxValue;

    [Bindable]
    public int MinWidth
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __MinWidth__BindingAccessor.Instance);
            return _minWidth;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_minWidth == value)
            {
                return;
            }

            _minWidth = value;
            BindingManager.Current.NotifyValueChanged(this, __MinWidth__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public int MinHeight
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __MinHeight__BindingAccessor.Instance);
            return _minHeight;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_minHeight == value)
            {
                return;
            }

            _minHeight = value;
            BindingManager.Current.NotifyValueChanged(this, __MinHeight__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public int MaxWidth
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __MaxWidth__BindingAccessor.Instance);
            return _maxWidth;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_maxWidth == value)
            {
                return;
            }

            _maxWidth = value;
            BindingManager.Current.NotifyValueChanged(this, __MaxWidth__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public int MaxHeight
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __MaxHeight__BindingAccessor.Instance);
            return _maxHeight;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            if (_maxHeight == value)
            {
                return;
            }

            _maxHeight = value;
            BindingManager.Current.NotifyValueChanged(this, __MaxHeight__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _isHovered;

    private Thickness _margin;

    [Bindable]
    public Thickness Margin
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __Margin__BindingAccessor.Instance);
            return _margin;
        }
        set
        {
            if (_margin == value)
            {
                return;
            }

            _margin = value;
            BindingManager.Current.NotifyValueChanged(this, __Margin__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public bool IsVisible
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __IsVisible__BindingAccessor.Instance);
            return _isVisible;
        }
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            BindingManager.Current.NotifyValueChanged(this, __IsVisible__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public bool IsEnabled
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __IsEnabled__BindingAccessor.Instance);
            return _isEnabled;
        }
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            BindingManager.Current.NotifyValueChanged(this, __IsEnabled__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    [Bindable]
    public bool IsHovered
    {
        get
        {
            BindingManager.Current.RegisterRead(this, __IsHovered__BindingAccessor.Instance);
            return _isHovered;
        }
        internal set
        {
            if (_isHovered == value)
            {
                return;
            }

            _isHovered = value;
            BindingManager.Current.NotifyValueChanged(this, __IsHovered__BindingAccessor.Instance);
            App?.RequestRender();
        }
    }

    public void AddKeyBinding(Input.TerminalKeyGesture gesture, Action action)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(action);
        _keyBindings ??= new List<KeyBinding>();
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

    public Theme GetTheme() => Get<Theme>();

    public void Initialize(Action<Visual> configure)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(configure);
        _initializers ??= new List<Action<Visual>>();
        _initializers.Add(configure);
        _initializersDirty = true;
        App?.RequestRender();
    }

    internal void AttachToApp(TerminalApp app)
    {
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

        App = null;
        OnDetachedFromApp(app);
    }

    protected virtual void OnAttachedToApp(TerminalApp app) { }

    protected virtual void OnDetachedFromApp(TerminalApp app) { }

    public void Measure(Size availableSize)
    {
        VerifyAccess();
        EnsureInitialized();

        using var session = BindingManager.Current.StartTracking();
        var margin = Margin;
        var availableWithoutMargin = Deflate(availableSize, margin);
        var innerAvailable = ApplyMeasureConstraints(availableWithoutMargin);
        _desiredSizeWithoutMargin = MeasureOverride(innerAvailable);
        _desiredSizeWithoutMargin = ApplyMinMaxConstraints(_desiredSizeWithoutMargin);
        DesiredSize = Inflate(_desiredSizeWithoutMargin, margin);
        StoreDependencies(ref _measureDeps, session.Dependencies);
        _measureDirty = false;
    }

    public void Arrange(Rectangle finalRect)
    {
        VerifyAccess();
        EnsureInitialized();

        using var session = BindingManager.Current.StartTracking();
        var margin = Margin;
        var innerSlot = Deflate(finalRect, margin);
        var arrangedRect = ApplyArrangeConstraints(innerSlot);
        Bounds = arrangedRect;
        ArrangeOverride(arrangedRect);
        StoreDependencies(ref _arrangeDeps, session.Dependencies);
        _arrangeDirty = false;
    }

    protected virtual Size MeasureOverride(Size availableSize)
    {
        var width = 0;
        var height = 0;

        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected virtual void ArrangeOverride(Rectangle finalRect)
    {
        for (var i = 0; i < ChildrenCount; i++)
        {
            var child = GetChild(i);
            child.Arrange(finalRect);
        }
    }

    private Size ApplyMeasureConstraints(Size availableSize)
    {
        var w = Math.Max(0, availableSize.Width);
        var h = Math.Max(0, availableSize.Height);

        var maxW = Math.Max(0, MaxWidth);
        var maxH = Math.Max(0, MaxHeight);
        if (maxW != int.MaxValue)
        {
            w = Math.Min(w, maxW);
        }

        if (maxH != int.MaxValue)
        {
            h = Math.Min(h, maxH);
        }

        return new Size(w, h);
    }

    private Size ApplyMinMaxConstraints(Size size)
    {
        var minW = Math.Max(0, MinWidth);
        var minH = Math.Max(0, MinHeight);
        var maxW = Math.Max(Math.Max(0, MaxWidth), minW);
        var maxH = Math.Max(Math.Max(0, MaxHeight), minH);

        var w = Math.Clamp(size.Width, minW, maxW);
        var h = Math.Clamp(size.Height, minH, maxH);
        return new Size(w, h);
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
        EnsureInitialized();

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

            StoreDependencies(ref _renderDeps, session.Dependencies);
            _renderDirty = false;
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

    internal void PropagateBindingChanged(Binding binding)
    {
        var stack = new Stack<Visual>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var v = stack.Pop();
            if (v._initializerDeps is not null && v._initializerDeps.Contains(binding))
            {
                v._initializersDirty = true;
            }

            if (!v._measureDirty && v._measureDeps is not null && v._measureDeps.Contains(binding))
            {
                v._measureDirty = true;
            }

            if (!v._arrangeDirty && v._arrangeDeps is not null && v._arrangeDeps.Contains(binding))
            {
                v._arrangeDirty = true;
            }

            if (!v._renderDirty && v._renderDeps is not null && v._renderDeps.Contains(binding))
            {
                v._renderDirty = true;
            }

            for (var i = v.ChildrenCount - 1; i >= 0; i--)
            {
                stack.Push(v.GetChild(i));
            }
        }
    }

    private void EnsureInitialized()
    {
        if (!_initializersDirty || _initializers is null)
        {
            return;
        }

        _initializersDirty = false;
        _measureDirty = true;
        _arrangeDirty = true;
        _renderDirty = true;

        using var session = BindingManager.Current.StartTracking();
        for (var i = 0; i < _initializers.Count; i++)
        {
            _initializers[i](this);
        }

        StoreDependencies(ref _initializerDeps, session.Dependencies);
    }

    private static void StoreDependencies(ref HashSet<Binding>? target, IReadOnlyCollection<Binding> dependencies)
    {
        if (target is null)
        {
            target = new HashSet<Binding>(BindingReferenceComparer.Instance);
        }
        else
        {
            target.Clear();
        }

        foreach (var dep in dependencies)
        {
            target.Add(dep);
        }
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
