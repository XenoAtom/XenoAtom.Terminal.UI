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

    public event Action<object, string>? ValueChanged;

    public T GetValue<T>(object owner, ref T backingField, BindingAccessor<T> accessor)
    {
        _ = owner;
        _ = accessor;
        return backingField;
    }

    public void SetValue<T>(object owner, ref T backingField, T value, BindingAccessor<T> accessor)
    {
        _ = accessor;
        if (EqualityComparer<T>.Default.Equals(backingField, value))
        {
            return;
        }

        backingField = value;
        ValueChanged?.Invoke(owner, accessor.Name);
    }
}
