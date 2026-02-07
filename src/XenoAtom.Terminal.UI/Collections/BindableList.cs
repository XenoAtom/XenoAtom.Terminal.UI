// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI.Collections;

/// <summary>
/// A list that participates in the binding dependency tracking system.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class BindableList<T> : IList<T>, IReadOnlyList<T>, IDynamicUpdateResettable
{
    private readonly object _owner;
    private readonly BindingAccessor _accessor;
    private readonly List<T> _items;
    private readonly Action<T>? _onAdding;
    private readonly Action<T>? _onRemoving;
    private int _version;
    private bool _touchedDuringInitialization;

    /// <summary>
    /// Initializes a new instance of the BindableList class, optionally assigning a name to the list for identification
    /// purposes.
    /// </summary>
    /// <remarks>The name parameter can be used to distinguish between multiple BindableList instances,
    /// especially when binding or tracking lists in data-driven scenarios. If a name is not provided, a unique
    /// identifier ensures that each list remains distinct.</remarks>
    /// <param name="name">An optional name used to identify the list. If null, a unique identifier is generated automatically.</param>
    public BindableList(string? name = null)
    {
        _owner = this;
        _accessor = new ListBindingAccessor(name is null
            ? "$list$" + Guid.NewGuid().ToString("N")
            : string.Intern("$list$" + name));
        _items = new List<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BindableList{T}"/> class.
    /// </summary>
    /// <param name="owner">The object that owns this list (used for dependency tracking).</param>
    /// <param name="name">A stable name used to identify this list for binding tracking.</param>
    /// <param name="onAdding">Optional callback invoked when an item is attached to the list.</param>
    /// <param name="onRemoving">Optional callback invoked when an item is detached from the list.</param>
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

    /// <summary>
    /// Gets a monotonically increasing version number that changes whenever the list is mutated.
    /// </summary>
    public int Version
    {
        get
        {
            BindingManager.Current.RegisterRead(_owner, _accessor);
            return _version;
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            BindingManager.Current.RegisterRead(_owner, _accessor);
            return _items.Count;
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public T this[int index]
    {
        get
        {
            BindingManager.Current.RegisterRead(_owner, _accessor);
            return _items[index];
        }
        set
        {
            if (typeof(T).IsClass)
            {
                ArgumentNullException.ThrowIfNull(value);
            }

            var old = _items[index];
            if (ReferenceEquals(old, value))
            {
                return;
            }

            TrackMutation();
            _onRemoving?.Invoke(old);
            _onAdding?.Invoke(value);

            _items[index] = value;
            BindingManager.Current.NotifyValueChanged(_owner, _accessor);
        }
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        if (typeof(T).IsClass)
        {
            ArgumentNullException.ThrowIfNull(item);
        }

        TrackMutation();
        _onAdding?.Invoke(item);
        _items.Add(item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    /// <summary>
    /// Adds a sequence of items to the list.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        TrackMutation();
        foreach(var item in items)
        {
            if (typeof(T).IsClass)
            {
                ArgumentNullException.ThrowIfNull(item);
            }

            _onAdding?.Invoke(item);
            _items.Add(item);
        }

        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    /// <summary>
    /// Adds an array of items to the list.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(params T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Length == 0)
        {
            return;
        }

        TrackMutation();
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (typeof(T).IsClass)
            {
                ArgumentNullException.ThrowIfNull(item);
            }

            _onAdding?.Invoke(item);
            _items.Add(item);
        }

        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        TrackMutation();
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

    /// <inheritdoc />
    public bool Contains(T item)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        _items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public List<T>.Enumerator GetEnumerator()
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(T item)
    {
        BindingManager.Current.RegisterRead(_owner, _accessor);
        return _items.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, T item)
    {
        if (typeof(T).IsClass)
        {
            ArgumentNullException.ThrowIfNull(item);
        }

        TrackMutation();
        _onAdding?.Invoke(item);
        _items.Insert(index, item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        var item = _items[index];
        TrackMutation();
        _onRemoving?.Invoke(item);
        _items.RemoveAt(index);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    /// <summary>
    /// Moves an item from one index to another.
    /// </summary>
    /// <param name="oldIndex">The old index.</param>
    /// <param name="newIndex">The new index.</param>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex)
        {
            return;
        }

        TrackMutation();
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
        BindingManager.Current.NotifyValueChanged(_owner, _accessor);
    }

    void IDynamicUpdateResettable.ResetForDynamicUpdate() => ResetForDynamicUpdate();

    internal void ResetForDynamicUpdate()
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
        unchecked
        {
            _version++;
        }
        _touchedDuringInitialization = false;
    }

    private void TrackMutation()
    {
        unchecked
        {
            _version++;
        }
    }

    /// <summary>
    /// Copies the items to a new array.
    /// </summary>
    /// <remarks>
    /// This method is hidden from IntelliSense and exists primarily to support generated code and tests.
    /// </remarks>
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

        public override bool IsReadOnly => false;

        public override object? GetValueAsObject(object instance) => throw new NotSupportedException();

        public override void SetValueAsObject(object instance, object? value) => throw new NotSupportedException();
    }
}
