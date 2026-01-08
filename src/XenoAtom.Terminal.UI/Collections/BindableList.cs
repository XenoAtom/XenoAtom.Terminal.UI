// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections;
using System.ComponentModel;

namespace XenoAtom.Terminal.UI.Collections;

/// <summary>
/// A list that participates in the binding dependency tracking system.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class BindableList<T> : IList<T>, IReadOnlyList<T>, IInitializerResettable
{
    private readonly object _owner;
    private readonly BindingAccessor _accessor;
    private readonly List<T> _items;
    private readonly Action<T>? _onAdding;
    private readonly Action<T>? _onRemoving;
    private bool _touchedDuringInitialization;

    public BindableList(object owner, string name, Action<T>? onAdding = null, Action<T>? onRemoving = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        ArgumentException.ThrowIfNullOrEmpty(name);
        _accessor = new ListBindingAccessor(string.Intern("$list$" + name));
        _items = new List<T>();
        _onAdding = onAdding;
        _onRemoving = onRemoving;
    }

    internal object Owner => _owner;

    internal BindingAccessor Accessor => _accessor;

    public int Count
    {
        get
        {
            BindingManager.Current.RegisterRead(_owner, _accessor);
            return _items.Count;
        }
    }

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            BindingManager.Current.RegisterRead(_owner, _accessor);
            return _items[index];
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var old = _items[index];
            if (ReferenceEquals(old, value))
            {
                return;
            }

            TrackInitializerMutation();
            _onRemoving?.Invoke(old);
            _onAdding?.Invoke(value);

            _items[index] = value;
            BindingManager.Current.NotifyValueChanged(_owner, _accessor);
        }
    }

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        TrackInitializerMutation();
        _onAdding?.Invoke(item);
        _items.Add(item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    public void AddRange(params T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length == 0)
        {
            return;
        }

        TrackInitializerMutation();
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            ArgumentNullException.ThrowIfNull(item);
            _onAdding?.Invoke(item);
            _items.Add(item);
        }

        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        TrackInitializerMutation();
        if (_onRemoving is not null)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                _onRemoving(_items[i]);
            }
        }

        _items.Clear();
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    public bool Contains(T item)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        _items.CopyTo(array, arrayIndex);
    }

    public List<T>.Enumerator GetEnumerator()
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(T item)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        TrackInitializerMutation();
        _onAdding?.Invoke(item);
        _items.Insert(index, item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    public bool Remove(T item)
    {
        var index = _items.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        var item = _items[index];
        TrackInitializerMutation();
        _onRemoving?.Invoke(item);
        _items.RemoveAt(index);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }

        TrackInitializerMutation();
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    void IInitializerResettable.ResetForReinitialize() => ResetForReinitialize();

    internal void ResetForReinitialize()
    {
        if (!_touchedDuringInitialization || _items.Count == 0)
        {
            _touchedDuringInitialization = false;
            return;
        }

        if (_onRemoving is not null)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                _onRemoving(_items[i]);
            }
        }

        _items.Clear();
        _touchedDuringInitialization = false;
    }

    private void TrackInitializerMutation()
    {
        // If this list is mutated while the owning visual is executing initializers,
        // mark it so we can reset it before re-running those initializers.
        if (!_touchedDuringInitialization && ReferenceEquals(BindingManager.Current.InitializingOwner, _owner))
        {
            _touchedDuringInitialization = true;
            if (_owner is Visual v)
            {
                v.RegisterInitializerList(this);
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public T[] ToArray()
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.ToArray();
    }

    private sealed class ListBindingAccessor : BindingAccessor
    {
        public ListBindingAccessor(string name) : base(name)
        {
        }

        public override object? GetValue(object instance) => throw new NotSupportedException();

        public override void SetValue(object instance, object? value) => throw new NotSupportedException();
    }
}
