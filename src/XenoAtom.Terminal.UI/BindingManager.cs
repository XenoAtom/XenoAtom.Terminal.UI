// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Central binding manager used by generated bindable property accessors.
/// </summary>
public sealed class BindingManager
{
    public static BindingManager Current { get; } = new();

    public event Action<object, string>? ValueChanged;

    [ThreadStatic]
    private static TrackingContext? _tracking;

    public T GetValue<T>(object owner, ref T backingField, BindingAccessor<T> accessor)
    {
        _tracking?.RegisterRead(owner, accessor.Name);
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
        ValueChanged?.Invoke(owner, accessor.Name);
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
        _tracking?.RegisterRead(owner, name);
    }

    public void NotifyValueChanged(object owner, string name)
    {
        if (owner is Visual { App: { } app })
        {
            app.Dispatcher.VerifyAccess();
        }

        ValueChanged?.Invoke(owner, name);
    }

    public readonly struct TrackingSession : IDisposable
    {
        private readonly TrackingContext? _previous;

        internal TrackingSession(object? previous, IReadOnlyCollection<BindingDependency> dependencies)
        {
            _previous = (TrackingContext?)previous;
            Dependencies = dependencies;
        }

        public IReadOnlyCollection<BindingDependency> Dependencies { get; }

        public void Dispose()
        {
            _tracking = _previous;
        }
    }

    private sealed class TrackingContext
    {
        private readonly HashSet<BindingDependency> _dependencies = new(BindingDependencyComparer.Instance);

        public IReadOnlyCollection<BindingDependency> Dependencies => _dependencies;

        public void RegisterRead(object owner, string name)
        {
            _dependencies.Add(new BindingDependency(owner, name));
        }
    }

    private sealed class BindingDependencyComparer : IEqualityComparer<BindingDependency>
    {
        public static BindingDependencyComparer Instance { get; } = new();

        public bool Equals(BindingDependency x, BindingDependency y)
            => ReferenceEquals(x.Owner, y.Owner) && ReferenceEquals(x.Name, y.Name);

        public int GetHashCode(BindingDependency obj)
            => HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Owner), RuntimeHelpers.GetHashCode(obj.Name));
    }
}
