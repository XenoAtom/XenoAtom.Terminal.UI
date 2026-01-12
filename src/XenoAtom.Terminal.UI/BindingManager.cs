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

    [ThreadStatic]
    private static object? _dynamicUpdateOwner;

    internal object? DynamicUpdateOwner => _dynamicUpdateOwner;

    [ThreadStatic]
    private static int _suppressNotifications;

    public T GetValue<T>(object owner, ref T backingField, BindingAccessor<T> accessor)
    {
        if (owner is Threading.DispatcherObject dispatcherObject)
        {
            dispatcherObject.VerifyAccess();
        }

        _tracking?.RegisterRead(owner, accessor);
        return backingField;
    }

    public bool SetValue<T>(object owner, ref T backingField, T value, BindingAccessor<T> accessor)
    {
        _ = accessor;
        if (EqualityComparer<T>.Default.Equals(backingField, value))
        {
            return false;
        }

        if (owner is Threading.DispatcherObject dispatcherObject)
        {
            dispatcherObject.VerifyAccess();
        }

        backingField = value;
        if (_suppressNotifications == 0)
        {
            ValueChanged?.Invoke(new Binding(owner, accessor));
        }
        return true;
    }

    public TrackingSession StartTracking()
    {
        var previous = _tracking;
        var current = new TrackingContext();
        _tracking = current;
        return new TrackingSession(previous, current.Dependencies);
    }

    public TrackingSession DisableReadTracking()
    {
        var previous = _tracking;
        _tracking = null;
        return new TrackingSession(previous, Array.Empty<Binding>());
    }

    internal DynamicUpdateSession BeginDynamicUpdate(object owner)
    {
        var previous = _dynamicUpdateOwner;
        _dynamicUpdateOwner = owner;
        return new DynamicUpdateSession(previous);
    }

    internal NotificationSuppressionSession SuppressNotifications()
    {
        _suppressNotifications++;
        return new NotificationSuppressionSession();
    }

    internal readonly struct NotificationSuppressionSession : IDisposable
    {
        public void Dispose()
        {
            if (_suppressNotifications > 0)
            {
                _suppressNotifications--;
            }
        }
    }

    internal readonly struct DynamicUpdateSession : IDisposable
    {
        private readonly object? _previous;

        public DynamicUpdateSession(object? previous) => _previous = previous;

        public void Dispose() => _dynamicUpdateOwner = _previous;
    }

    public void RegisterRead(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        if (owner is Threading.DispatcherObject dispatcherObject)
        {
            dispatcherObject.VerifyAccess();
        }

        _tracking?.RegisterRead(owner, accessor);
    }

    public void NotifyValueChanged(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        if (owner is Threading.DispatcherObject dispatcherObject)
        {
            dispatcherObject.VerifyAccess();
        }

        if (_suppressNotifications == 0)
        {
            ValueChanged?.Invoke(new Binding(owner, accessor));
        }
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
}
