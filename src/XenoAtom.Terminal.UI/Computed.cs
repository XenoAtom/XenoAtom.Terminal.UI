// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

public sealed class Computed<T> : IDisposable
{
    private readonly Func<T> _compute;
    private HashSet<BindingDependency> _deps = new(BindingDependencyComparer.Instance);
    private bool _isDirty = true;
    private bool _isDisposed;
    private T? _value;

    public Computed(Func<T> compute)
    {
        _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        BindingManager.Current.ValueChanged += OnBindingChanged;
    }

    public event Action? Invalidated;

    public T Value
    {
        get
        {
            EnsureNotDisposed();
            if (_isDirty)
            {
                Recompute();
            }
            return _value!;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        BindingManager.Current.ValueChanged -= OnBindingChanged;
        _deps.Clear();
        _value = default;
    }

    private void Recompute()
    {
        using var session = BindingManager.Current.StartTracking();
        var newValue = _compute();

        _deps.Clear();
        foreach (var dep in session.Dependencies)
        {
            _deps.Add(dep);
        }

        _value = newValue;
        _isDirty = false;
    }

    private void OnBindingChanged(object owner, string name)
    {
        if (_isDisposed || _isDirty)
        {
            return;
        }

        if (_deps.Contains(new BindingDependency(owner, name)))
        {
            _isDirty = true;
            Invalidated?.Invoke();
        }
    }

    private void EnsureNotDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(Computed<T>));
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

