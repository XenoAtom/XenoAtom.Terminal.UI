// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Central binding manager used by generated bindable property accessors.
/// </summary>
/// <remarks>
/// There is one binding manager per thread (see <see cref="Current"/>): dependency tracking, deferred actions, read/write suppression
/// and <see cref="ValueChanged"/> are local to the calling thread, which is the UI thread of the app running on it.
/// </remarks>
public sealed class BindingManager
{
    [ThreadStatic]
    private static BindingManager? _current;

    // Two-way binding synchronizations are keyed by source owner, not by thread, so they are shared by all threads.
    private static readonly ConditionalWeakTable<object, SourceOwnerSubscriptions> _boundSubscriptions = new();

    /// <summary>
    /// Gets the binding manager of the calling thread, creating it on first access.
    /// </summary>
    public static BindingManager Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _current ?? CreateForCurrentThread();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BindingManager CreateForCurrentThread() => _current = new BindingManager();

    private BindingManager()
    {
        EnableDependencyLoopCheck = true; // On by default for now
    }

    /// <summary>
    /// Raised when a bindable value has changed and UI invalidation needs to be performed.
    /// </summary>
    /// <remarks>Only changes made on the thread of this binding manager are raised.</remarks>
    public event Action<Binding>? ValueChanged;


    /// <summary>
    /// Gets or sets a value indicating whether dependency loop checks are enabled during dependency tracking.
    /// </summary>
    /// <remarks>This setting applies to the binding manager of the thread it is set on.</remarks>
    public bool EnableDependencyLoopCheck { get; set; }

    private TrackingContext? _tracking;
    private TrackingContext? _freeList;
    private Queue<Action>? _deferredActions;
    private bool _isFlushingDeferredActions;

    private int _suppressReadTrackingCount;
    private int _suppressWriteTrackingCount;

    /// <summary>
    /// Gets a value indicating whether a dependency-tracking scope is currently active.
    /// </summary>
    public bool IsTracking => _tracking is not null;

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

        VerifyWriteAccess(owner, accessor);

        backingField = value;

        // Ensure the visual element is loaded to avoid tracking writes on unloaded elements
        if (owner is IVisualElement { App: null })
        {
            return true;
        }

        if (Volatile.Read(ref _suppressWriteTrackingCount) == 0)
        {
            if (_tracking is null || _tracking.RegisterWrite(owner, accessor))
            {
                RaiseValueChanged(new Binding(owner, accessor));
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
    public TrackingSession StartTracking([CallerMemberName] string? name = null)
    {
        var previous = _tracking;
        var current = NewTrackingContext();
        current.Name = name;
        current.Next = previous;
        _tracking = current;
        return new TrackingSession(current);
    }

    private TrackingContext NewTrackingContext()
    {
        if (_freeList is null)
        {
            return new TrackingContext(this);
        }
        var context = _freeList;
        _freeList = _freeList.Next;
        context.Next = null;
        return context;
    }

    private void ReleaseTrackingContext(TrackingContext? context)
    {
        if (context is null)
        {
            return;
        }
        var previous = context.Next;
        context.Reset();
        context.Next = _freeList;
        _freeList = context;
        _tracking = previous;

        if (_tracking is null)
        {
            FlushDeferredActions();
        }
    }

    /// <summary>
    /// Runs an action immediately when no tracking scope is active, or defers it until the outermost tracking scope completes.
    /// </summary>
    /// <param name="action">The action to run.</param>
    public void RunAfterTracking(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!IsTracking)
        {
            action();
            return;
        }

        (_deferredActions ??= new Queue<Action>()).Enqueue(action);
    }

    /// <summary>
    /// Suppresses dependency read tracking for the duration of the session.
    /// </summary>
    /// <returns>A disposable session that restores read tracking on dispose.</returns>
    public SuppressReadTrackingSession SuppressReadTracking()
    {
        Interlocked.Increment(ref _suppressReadTrackingCount);
        return new SuppressReadTrackingSession(this);
    }

    /// <summary>
    /// Enables or disables dependency read tracking.
    /// </summary>
    /// <param name="readTracking"><see langword="true"/> to enable read tracking; <see langword="false"/> to disable it.</param>
    /// <remarks>
    /// Use <see cref="SuppressReadTracking"/> for scoped suppression.
    /// </remarks>
    [Obsolete("Use SuppressReadTracking() in a using scope instead.", error: false)]
    public void SetReadTracking(bool readTracking)
    {
        if (readTracking)
        {
            ReleaseSuppression(ref _suppressReadTrackingCount);
        }
        else
        {
            Interlocked.Increment(ref _suppressReadTrackingCount);
        }
    }

    /// <summary>
    /// Suppresses write tracking notifications for the duration of the session.
    /// </summary>
    /// <remarks>This method increments the internal counter for suppressed notifications. It is important to
    /// ensure that the corresponding session is disposed of to re-enable notifications.</remarks>
    /// <returns>A new instance of <see cref="SuppressWriteTrackingSession"/> that manages the suppression of write tracking
    /// notifications.</returns>
    public SuppressWriteTrackingSession SuppressWriteTracking()
    {
        Interlocked.Increment(ref _suppressWriteTrackingCount);
        return new SuppressWriteTrackingSession(this);
    }

    /// <summary>
    /// Represents a session that suppresses dependency read tracking until disposed.
    /// </summary>
    public readonly struct SuppressReadTrackingSession : IDisposable
    {
        private readonly BindingManager? _manager;

        internal SuppressReadTrackingSession(BindingManager manager)
        {
            _manager = manager;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_manager is null)
            {
                return;
            }

            ReleaseSuppression(ref _manager._suppressReadTrackingCount);
        }
    }

    /// <summary>
    /// Represents a session that temporarily suppresses write tracking notifications, allowing batch updates or changes
    /// to occur without triggering notification events until the session is disposed.
    /// </summary>
    /// <remarks>This struct implements IDisposable. To ensure that write tracking notifications are properly
    /// managed, use SuppressWriteTrackingSession within a using statement or explicitly call Dispose when the session
    /// is complete. Failing to dispose the session may result in notifications remaining suppressed longer than
    /// intended.</remarks>
    public readonly struct SuppressWriteTrackingSession : IDisposable
    {
        private readonly BindingManager? _manager;

        internal SuppressWriteTrackingSession(BindingManager manager)
        {
            _manager = manager;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_manager is null)
            {
                return;
            }

            ReleaseSuppression(ref _manager._suppressWriteTrackingCount);
        }
    }

    /// <summary>
    /// Registers a binding read for dependency tracking.
    /// </summary>
    /// <param name="owner">The owner instance.</param>
    /// <param name="accessor">The accessor describing the property.</param>
    public void RegisterRead(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        var dispatcherObject = owner as Threading.DispatcherObject;
        dispatcherObject?.VerifyAccess();

        if (Volatile.Read(ref _suppressReadTrackingCount) > 0)
        {
            return;
        }

        // Ensure the visual element is loaded to avoid tracking reads on unloaded elements
        if (owner is IVisualElement { App: null })
        {
            return;
        }

        var tracking = _tracking;
        if (tracking is null)
        {
            return;
        }

        if (dispatcherObject is not null && dispatcherObject.OwnerDispatcher is null)
        {
            AcquireDispatcherOwnership(dispatcherObject);
        }

        tracking.RegisterRead(owner, accessor);
    }

    /// <summary>
    /// Tags a free-standing <see cref="Threading.DispatcherObject"/> (e.g. <see cref="State{T}"/>) with the dispatcher of the app
    /// that tracks a read of it, so that later accesses from other threads are rejected while that app runs.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AcquireDispatcherOwnership(Threading.DispatcherObject dispatcherObject)
    {
        // Visual elements get their dispatcher from the app they are attached to.
        if (dispatcherObject is IVisualElement)
        {
            return;
        }

        var dispatcher = Threading.Dispatcher.Current;
        if (dispatcher.IsRunning)
        {
            dispatcherObject.TryAcquireDispatcher(dispatcher);
        }
    }

    /// <summary>
    /// Notifies the UI that a value has changed without touching a generated backing field.
    /// </summary>
    /// <param name="owner">The owner instance.</param>
    /// <param name="accessor">The accessor describing the property.</param>
    public void NotifyValueChanged(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        VerifyWriteAccess(owner, accessor);

        if (Volatile.Read(ref _suppressWriteTrackingCount) == 0)
        {
            RaiseValueChanged(new Binding(owner, accessor));
        }
    }

    /// <summary>
    /// Registers a bound target so source writes synchronize the local bindable value before it is read again.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="targetOwner">The target owner instance.</param>
    /// <param name="targetAccessor">The accessor describing the target property.</param>
    /// <param name="sourceBinding">The source binding to observe.</param>
    /// <param name="applyValue">The callback that applies a synchronized value to the target.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterBoundValue<T>(object targetOwner, BindingAccessor<T> targetAccessor, Binding<T> sourceBinding, Action<object, T> applyValue)
    {
        ArgumentNullException.ThrowIfNull(targetOwner);
        ArgumentNullException.ThrowIfNull(targetAccessor);
        ArgumentNullException.ThrowIfNull(applyValue);

        if (sourceBinding.IsEmpty)
        {
            return;
        }

        var subscriptions = _boundSubscriptions.GetOrCreateValue(sourceBinding.Owner);
        subscriptions.Add(sourceBinding.Accessor, new BoundValueSubscription<T>(targetOwner, targetAccessor, sourceBinding.Accessor.Getter, applyValue));
    }

    /// <summary>
    /// Removes a previously registered bound target synchronization.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="targetOwner">The target owner instance.</param>
    /// <param name="targetAccessor">The accessor describing the target property.</param>
    /// <param name="sourceBinding">The source binding to stop observing.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void UnregisterBoundValue<T>(object targetOwner, BindingAccessor<T> targetAccessor, Binding<T> sourceBinding)
    {
        ArgumentNullException.ThrowIfNull(targetOwner);
        ArgumentNullException.ThrowIfNull(targetAccessor);

        if (sourceBinding.IsEmpty)
        {
            return;
        }

        if (!_boundSubscriptions.TryGetValue(sourceBinding.Owner, out var subscriptions))
        {
            return;
        }

        subscriptions.Remove(sourceBinding.Accessor, targetOwner, targetAccessor);
    }

    private void RaiseValueChanged(Binding binding)
    {
        PropagateBoundValues(binding);
        ValueChanged?.Invoke(binding);
    }

    private static void VerifyWriteAccess(object owner, BindingAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(accessor);

        // Only DispatcherObject owners carry a thread affinity (see DispatcherObject). Plain owners cannot be checked:
        // with one dispatcher per thread there is no way to know which app, if any, observes them.
        if (owner is Threading.DispatcherObject dispatcherObject && !dispatcherObject.CheckAccess())
        {
            throw new InvalidOperationException(
                $"Invalid thread access for bindable update `{owner.GetType().Name}.{accessor.Name}`. " +
                "Use TerminalApp.Dispatcher.InvokeAsync(...) to update bound state on the UI thread.");
        }
    }

    private void PropagateBoundValues(Binding binding)
    {
        if (!_boundSubscriptions.TryGetValue(binding.Owner, out var subscriptions))
        {
            return;
        }

        subscriptions.Propagate(binding);
    }

    private static void ReleaseSuppression(ref int counter)
    {
        while (true)
        {
            var current = Volatile.Read(ref counter);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref counter, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private void FlushDeferredActions()
    {
        if (_isFlushingDeferredActions || _deferredActions is null || _deferredActions.Count == 0)
        {
            return;
        }

        _isFlushingDeferredActions = true;
        try
        {
            while (_tracking is null && _deferredActions.Count > 0)
            {
                var action = _deferredActions.Dequeue();
                action();
            }
        }
        finally
        {
            _isFlushingDeferredActions = false;
            if (_deferredActions is not null && _deferredActions.Count == 0)
            {
                _deferredActions = null;
            }
        }
    }

    /// <summary>
    /// Represents a dependency tracking scope for binding reads.
    /// </summary>
    public readonly struct TrackingSession : IDisposable
    {
        private readonly TrackingContext? _context;

        internal TrackingSession(object? context)
        {
            _context = (TrackingContext?)context;
        }

        /// <summary>
        /// Gets the dependencies that were read during the tracking session.
        /// </summary>
        public IReadOnlySet<Binding> Reads => _context is null ? ReadOnlySet<Binding>.Empty : _context.Reads;

        /// <summary>
        /// Gets the set of bindings that are written by this operation.
        /// </summary>
        public IReadOnlySet<Binding> Writes => _context is null ? ReadOnlySet<Binding>.Empty : _context.Writes;

        /// <summary>
        /// Restores the previous tracking context.
        /// </summary>
        public void Dispose()
        {
            _context?.Owner.ReleaseTrackingContext(_context);
        }
    }

    private sealed class TrackingContext
    {
        private readonly HashSet<Binding> _reads = new(BindingReferenceComparer.Instance);
        private readonly HashSet<Binding> _writes = new(BindingReferenceComparer.Instance);

        public TrackingContext(BindingManager owner)
        {
            Owner = owner;
        }

        public BindingManager Owner { get; }

        public TrackingContext? Next { get; set; }

        public string? Name { get; set; }

        public IReadOnlySet<Binding> Reads => _reads;

        public IReadOnlySet<Binding> Writes => _writes;

        public void Reset()
        {
            Name = null;
            Next = null;
            _reads.Clear();
            _writes.Clear();
        }

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
                throw new InvalidOperationException($"Cannot read and then write `{owner.GetType().Name}.{accessor.Name}` within a same tracking context");
            }

            if (Owner.EnableDependencyLoopCheck)
            {
                var next = Next;
                while (next is not null)
                {
                    if (next._reads.Contains(binding))
                    {
                        throw new InvalidOperationException($"Dependency loop detected when writing `{owner.GetType().Name}.{accessor.Name}` which was already read in an upper tracking context `{next.Name ?? "unnamed"}`");
                    }
                    next = next.Next;
                }
            }

            _writes.Add(binding);
            return true;
        }
    }

    private abstract class BoundValueSubscription
    {
        public abstract bool IsAlive { get; }

        public abstract bool Matches(object targetOwner, BindingAccessor targetAccessor);

        public abstract bool TryApply(object sourceOwner);
    }

    private sealed class BoundValueSubscription<T> : BoundValueSubscription
    {
        private readonly WeakReference<object> _targetOwner;
        private readonly BindingAccessor<T> _targetAccessor;
        private readonly Func<object, T> _getSourceValue;
        private readonly Action<object, T> _applyValue;

        public BoundValueSubscription(object targetOwner, BindingAccessor<T> targetAccessor, Func<object, T> getSourceValue, Action<object, T> applyValue)
        {
            _targetOwner = new WeakReference<object>(targetOwner);
            _targetAccessor = targetAccessor;
            _getSourceValue = getSourceValue;
            _applyValue = applyValue;
        }

        public override bool IsAlive => _targetOwner.TryGetTarget(out _);

        public override bool Matches(object targetOwner, BindingAccessor targetAccessor)
        {
            if (!_targetOwner.TryGetTarget(out var currentTargetOwner))
            {
                return false;
            }

            return ReferenceEquals(currentTargetOwner, targetOwner) && ReferenceEquals(_targetAccessor, targetAccessor);
        }

        public override bool TryApply(object sourceOwner)
        {
            if (!_targetOwner.TryGetTarget(out var targetOwner))
            {
                return false;
            }

            T value;
            using (Current.SuppressReadTracking())
            {
                value = _getSourceValue(sourceOwner);
            }

            _applyValue(targetOwner, value);
            return true;
        }
    }

    private sealed class SourceOwnerSubscriptions
    {
        // Shared by all threads (see _boundSubscriptions): guard the dictionary and its lists, but apply values outside the lock.
        private readonly Lock _lock = new();
        private readonly Dictionary<BindingAccessor, List<BoundValueSubscription>> _subscriptionsByAccessor = new();

        public void Add(BindingAccessor accessor, BoundValueSubscription subscription)
        {
            ArgumentNullException.ThrowIfNull(accessor);
            ArgumentNullException.ThrowIfNull(subscription);

            lock (_lock)
            {
                if (!_subscriptionsByAccessor.TryGetValue(accessor, out var subscriptions))
                {
                    subscriptions = [];
                    _subscriptionsByAccessor.Add(accessor, subscriptions);
                }

                subscriptions.Add(subscription);
            }
        }

        public void Remove(BindingAccessor accessor, object targetOwner, BindingAccessor targetAccessor)
        {
            ArgumentNullException.ThrowIfNull(accessor);
            ArgumentNullException.ThrowIfNull(targetOwner);
            ArgumentNullException.ThrowIfNull(targetAccessor);

            lock (_lock)
            {
                if (!_subscriptionsByAccessor.TryGetValue(accessor, out var subscriptions))
                {
                    return;
                }

                CleanupSubscriptions(subscriptions, static (subscription, state) =>
                {
                    var (owner, accessor) = ((object Owner, BindingAccessor Accessor))state;
                    return !subscription.IsAlive || subscription.Matches(owner, accessor);
                }, (targetOwner, targetAccessor));

                if (subscriptions.Count == 0)
                {
                    _subscriptionsByAccessor.Remove(accessor);
                }
            }
        }

        public void Propagate(Binding binding)
        {
            BoundValueSubscription[] snapshot;
            lock (_lock)
            {
                if (!_subscriptionsByAccessor.TryGetValue(binding.Accessor, out var subscriptions) || subscriptions.Count == 0)
                {
                    return;
                }

                snapshot = subscriptions.ToArray();
            }

            var requiresCleanup = false;

            foreach (var subscription in snapshot)
            {
                requiresCleanup |= !subscription.TryApply(binding.Owner);
            }

            if (!requiresCleanup)
            {
                return;
            }

            lock (_lock)
            {
                if (!_subscriptionsByAccessor.TryGetValue(binding.Accessor, out var subscriptions))
                {
                    return;
                }

                CleanupSubscriptions<object?>(subscriptions, static (subscription, _) => !subscription.IsAlive, null);
                if (subscriptions.Count == 0)
                {
                    _subscriptionsByAccessor.Remove(binding.Accessor);
                }
            }
        }

        private static void CleanupSubscriptions<TState>(
            List<BoundValueSubscription> subscriptions,
            Func<BoundValueSubscription, TState, bool> shouldRemove,
            TState state)
        {
            for (var i = subscriptions.Count - 1; i >= 0; i--)
            {
                if (shouldRemove(subscriptions[i], state))
                {
                    subscriptions.RemoveAt(i);
                }
            }
        }
    }
}
