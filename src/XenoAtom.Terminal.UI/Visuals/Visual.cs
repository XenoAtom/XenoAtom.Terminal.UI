// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Visuals;

public abstract partial class Visual : BindableObject
{
    private readonly List<Visual> _children = new();
    private Dictionary<object, Delegate?>? _handlers;
    private List<KeyBinding>? _keyBindings;
    private Dictionary<object, object?>? _environment;

    public Visual? Parent { get; private set; }

    public IReadOnlyList<Visual> Children => _children;

    public Rectangle Bounds { get; protected set; }

    public Size DesiredSize { get; private set; }

    public TerminalApp? App { get; private set; }

    public bool Focusable { get; protected init; }

    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _isHovered;

    public bool IsVisible
    {
        get
        {
            BindingManager.Current.RegisterRead(this, nameof(IsVisible));
            return _isVisible;
        }
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            BindingManager.Current.NotifyValueChanged(this, nameof(IsVisible));
            App?.RequestRender();
        }
    }

    public bool IsEnabled
    {
        get
        {
            BindingManager.Current.RegisterRead(this, nameof(IsEnabled));
            return _isEnabled;
        }
        set
        {
            if (_isEnabled == value)
            {
                return;
            }

            _isEnabled = value;
            BindingManager.Current.NotifyValueChanged(this, nameof(IsEnabled));
            App?.RequestRender();
        }
    }

    public bool IsHovered
    {
        get
        {
            BindingManager.Current.RegisterRead(this, nameof(IsHovered));
            return _isHovered;
        }
        internal set
        {
            if (_isHovered == value)
            {
                return;
            }

            _isHovered = value;
            BindingManager.Current.NotifyValueChanged(this, nameof(IsHovered));
            App?.RequestRender();
        }
    }

    public void AddKeyBinding(Input.TerminalKeyGesture gesture, Action action)
    {
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

    public void AddChild(Visual child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (child.Parent is not null)
        {
            throw new InvalidOperationException("The visual already has a parent.");
        }

        _children.Add(child);
        child.Parent = this;

        if (App is not null)
        {
            child.AttachToApp(App);
        }
    }

    public void SetEnvironmentValue<T>(EnvironmentKey<T> key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _environment ??= new Dictionary<object, object?>();
        _environment[key] = value;
        BindingManager.Current.NotifyValueChanged(this, key.DependencyName);
    }

    public T GetEnvironmentValue<T>(EnvironmentKey<T> key)
    {
        ArgumentNullException.ThrowIfNull(key);

        Visual? root = null;

        for (var v = this; v is not null; v = v.Parent)
        {
            root = v;
            if (v._environment is not null && v._environment.TryGetValue(key, out var boxed))
            {
                BindingManager.Current.RegisterRead(v, key.DependencyName);
                return boxed is T typed ? typed : key.DefaultValue;
            }
        }

        BindingManager.Current.RegisterRead(root ?? this, key.DependencyName);
        return key.DefaultValue;
    }

    public Theme GetTheme() => GetEnvironmentValue(Theme.Key);

    protected void ClearChildren()
    {
        if (_children.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (App is not null)
            {
                child.DetachFromApp();
            }
            child.Parent = null;
        }

        _children.Clear();
    }

    protected bool BringChildToFront(Visual child)
    {
        ArgumentNullException.ThrowIfNull(child);

        var index = _children.IndexOf(child);
        if (index < 0 || index == _children.Count - 1)
        {
            return false;
        }

        _children.RemoveAt(index);
        _children.Add(child);
        App?.RequestRender();
        return true;
    }

    internal void AttachToApp(TerminalApp app)
    {
        App = app;
        OnAttachedToApp(app);
        foreach (var child in _children)
        {
            child.AttachToApp(app);
        }
    }

    internal void DetachFromApp()
    {
        var app = App;
        if (app is null)
        {
            return;
        }

        foreach (var child in _children)
        {
            child.DetachFromApp();
        }

        App = null;
        OnDetachedFromApp(app);
    }

    protected virtual void OnAttachedToApp(TerminalApp app) { }

    protected virtual void OnDetachedFromApp(TerminalApp app) { }

    public void Measure(Size availableSize)
    {
        DesiredSize = MeasureOverride(availableSize);
    }

    public void Arrange(Rectangle finalRect)
    {
        Bounds = finalRect;
        ArrangeOverride(finalRect);
    }

    protected virtual Size MeasureOverride(Size availableSize)
    {
        var width = 0;
        var height = 0;

        foreach (var child in _children)
        {
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected virtual void ArrangeOverride(Rectangle finalRect)
    {
        foreach (var child in _children)
        {
            child.Arrange(finalRect);
        }
    }

    internal void RenderTree(CellBuffer buffer)
    {
        if (!IsVisible)
        {
            return;
        }

        buffer.PushClip(Bounds);
        RenderOverride(buffer);
        foreach (var child in _children)
        {
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
        yield return this;

        foreach (var child in _children)
        {
            foreach (var nested in child.EnumerateVisualsDepthFirst())
            {
                yield return nested;
            }
        }
    }

    public Visual? HitTest(int x, int y)
    {
        if (!IsVisible || !Bounds.Contains(x, y))
        {
            return null;
        }

        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var hit = _children[i].HitTest(x, y);
            if (hit is not null)
            {
                return hit;
            }
        }

        return this;
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
