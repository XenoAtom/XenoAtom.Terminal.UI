// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.ObjectModel;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Central binding manager used by generated bindable property accessors.
/// </summary>
public sealed class BindingManager
{
    /// <summary>
    /// Gets the singleton instance of the binding manager.
    /// </summary>
    public static BindingManager Current { get; } = new();

    /// <summary>
    /// Raised when a bindable value has changed and UI invalidation needs to be performed.
    /// </summary>
    public event Action<Binding>? ValueChanged;

    [ThreadStatic]
    private static TrackingContext? _tracking;

    [ThreadStatic]
    private static object? _dynamicUpdateOwner;

    internal object? DynamicUpdateOwner => _dynamicUpdateOwner;

    [ThreadStatic]
    private static int _suppressNotifications;

    /// <summary>
    /// Gets the current value of a bindable property.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="owner">The owner instance.</param>
    /// <param name="backingField">A reference to the generated backing field.</param>
    /// <param name="accessor">The accessor describing the property.</param>
    /// <returns>The current value.</returns>
    public T GetValue<T>(object owner, ref T backingField, BindingAccessor<T> accessor)
    {
        RegisterRead(owner, accessor);
        return backingField;
    }

    /// <summary>
    /// Sets the current value of a bindable property.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="owner">The owner instance.</param>
    /// <param name="backingField">A reference to the generated backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="accessor">The accessor describing the property.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
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

        // Ensure the visual element is loaded to avoid tracking writes on unloaded elements
        if (owner is IVisualElement { App: null })
        {
            return true;
        }
        
        if (_suppressNotifications == 0)
        {
            if (_tracking is null || _tracking.RegisterWrite(owner, accessor))
            {
                ValueChanged?.Invoke(new Binding(owner, accessor));
            }
        }
        return true;
    }

    /// <summary>
    /// Starts a dependency tracking scope for binding reads.
    /// </summary>
    /// <remarks>
    /// Tracking is thread-local and is typically used by <see cref="TerminalApp"/> to record which bindings were read
    /// during dynamic updates, layout and rendering.
    /// </remarks>
    /// <returns>A disposable session that restores the previous tracking context on dispose.</returns>
    public TrackingSession StartTracking()
    {
        var previous = _tracking;
        var current = new TrackingContext();
        _tracking = current;
        return new TrackingSession(previous, current.Reads, current.Writes);
    }

    /// <summary>
    /// Disables dependency read tracking for the duration of the returned session.
    /// </summary>
    /// <returns>A disposable session that restores the previous tracking context on dispose.</returns>
    public TrackingSession DisableReadTracking()
    {
        var previous = _tracking;
        _tracking = null;
        return new TrackingSession(previous, ReadOnlySet<Binding>.Empty, ReadOnlySet<Binding>.Empty);
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

    /// <summary>
    /// Registers a binding read for dependency tracking.
    /// </summary>
    /// <param name="owner">The owner instance.</param>
    /// <param name="accessor">The accessor describing the property.</param>
    public void RegisterRead(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        if (owner is Threading.DispatcherObject dispatcherObject)
        {
            dispatcherObject.VerifyAccess();
        }

        // Ensure the visual element is loaded to avoid tracking reads on unloaded elements
        if (owner is IVisualElement { App: null })
        {
            return;
        }
        
        _tracking?.RegisterRead(owner, accessor);
    }

    /// <summary>
    /// Notifies the UI that a value has changed without touching a generated backing field.
    /// </summary>
    /// <param name="owner">The owner instance.</param>
    /// <param name="accessor">The accessor describing the property.</param>
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

    /// <summary>
    /// Represents a dependency tracking scope for binding reads.
    /// </summary>
    public readonly struct TrackingSession : IDisposable
    {
        private readonly TrackingContext? _previous;

        internal TrackingSession(object? previous, IReadOnlySet<Binding> reads, IReadOnlySet<Binding> writes)
        {
            _previous = (TrackingContext?)previous;
            Reads = reads;
            Writes = writes;
        }

        /// <summary>
        /// Gets the dependencies that were read during the tracking session.
        /// </summary>
        public IReadOnlySet<Binding> Reads { get; }

        /// <summary>
        /// Gets the set of bindings that are written by this operation.
        /// </summary>
        public IReadOnlySet<Binding> Writes { get; }

        /// <summary>
        /// Restores the previous tracking context.
        /// </summary>
        public void Dispose()
        {
            _tracking = _previous;
        }
    }

    private sealed class TrackingContext
    {
        private readonly HashSet<Binding> _reads = new(BindingReferenceComparer.Instance);
        private readonly HashSet<Binding> _writes = new(BindingReferenceComparer.Instance);

        public IReadOnlySet<Binding> Reads => _reads;

        public IReadOnlySet<Binding> Writes => _writes;

        public void RegisterRead(object owner, BindingAccessor accessor)
        {
            var binding = new Binding(owner, accessor);
            // If we are writing the same binding, we don't track the read to avoid cycles
            if (!_writes.Contains(binding))
            {
                _reads.Add(binding);
            }
        }

        public bool RegisterWrite(object owner, BindingAccessor accessor)
        {
            var binding = new Binding(owner, accessor);
            // If we are both reading and then writing the same binding, we won't track the writes to avoid cycles
            if (_reads.Contains(binding))
            {
                return false;
            }
            _writes.Add(binding);
            return true;
        }
    }
}
