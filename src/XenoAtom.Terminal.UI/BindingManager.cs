// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Central binding manager used by generated bindable property accessors.
/// </summary>
public sealed class BindingManager
{
    public static BindingManager Current { get; } = new();

    public event Action<Binding>? ValueChanged;

    [ThreadStatic]
    private static TrackingContext? _tracking;

    public T GetValue<T>(object owner, ref T backingField, BindingAccessor<T> accessor)
    {
        _tracking?.RegisterRead(owner, accessor);
        return backingField;
    }

    public void SetValue<T>(object owner, ref T backingField, T value, BindingAccessor<T> accessor)
    {
        _ = accessor;
        if (EqualityComparer<T>.Default.Equals(backingField, value))
        {
            return;
        }

        if (owner is Visual { App: { } app })
        {
            app.Dispatcher.VerifyAccess();
        }

        backingField = value;
        ValueChanged?.Invoke(new Binding(owner, accessor));
    }

    public TrackingSession StartTracking()
    {
        var previous = _tracking;
        var current = new TrackingContext();
        _tracking = current;
        return new TrackingSession(previous, current.Dependencies);
    }

    public void RegisterRead(object owner, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _tracking?.RegisterRead(owner, GetNameAccessor(name));
    }

    public void NotifyValueChanged(object owner, string name)
    {
        if (owner is Visual { App: { } app })
        {
            app.Dispatcher.VerifyAccess();
        }

        ArgumentNullException.ThrowIfNull(name);
        ValueChanged?.Invoke(new Binding(owner, GetNameAccessor(name)));
    }

    public void RegisterRead(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _tracking?.RegisterRead(owner, accessor);
    }

    public void NotifyValueChanged(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        if (owner is Visual { App: { } app })
        {
            app.Dispatcher.VerifyAccess();
        }

        ValueChanged?.Invoke(new Binding(owner, accessor));
    }

    public readonly struct TrackingSession : IDisposable
    {
        private readonly TrackingContext? _previous;

        internal TrackingSession(object? previous, IReadOnlyCollection<Binding> dependencies)
        {
            _previous = (TrackingContext?)previous;
            Dependencies = dependencies;
        }

        public IReadOnlyCollection<Binding> Dependencies { get; }

        public void Dispose()
        {
            _tracking = _previous;
        }
    }

    private sealed class TrackingContext
    {
        private readonly HashSet<Binding> _dependencies = new(BindingReferenceComparer.Instance);

        public IReadOnlyCollection<Binding> Dependencies => _dependencies;

        public void RegisterRead(object owner, BindingAccessor accessor)
        {
            _dependencies.Add(new Binding(owner, accessor));
        }
    }

    private readonly Dictionary<string, BindingAccessor> _nameAccessors = new(StringComparer.Ordinal);

    private BindingAccessor GetNameAccessor(string name)
    {
        lock (_nameAccessors)
        {
            if (_nameAccessors.TryGetValue(name, out var accessor))
            {
                return accessor;
            }

            accessor = new NameBindingAccessor(name);
            _nameAccessors.Add(name, accessor);
            return accessor;
        }
    }

    private sealed class NameBindingAccessor : BindingAccessor
    {
        public NameBindingAccessor(string name) : base(name)
        {
        }

        public override object? GetValue(object instance) => throw new NotSupportedException();

        public override void SetValue(object instance, object? value) => throw new NotSupportedException();
    }
}
