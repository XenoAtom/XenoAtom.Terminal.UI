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

    public T GetValue<T>(ref T backingField, BindingAccessor<T> accessor)
    {
        _ = accessor;
        return backingField;
    }

    public void SetValue<T>(ref T backingField, T value, BindingAccessor<T> accessor)
    {
        _ = accessor;
        backingField = value;
    }
}

