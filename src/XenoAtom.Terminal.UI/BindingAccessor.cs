// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Describes how to get/set a bindable property from an object instance.
/// </summary>
public abstract class BindingAccessor
{
    protected BindingAccessor(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }

    public abstract object? GetValue(object instance);

    public abstract void SetValue(object instance, object? value);
}

/// <summary>
/// Describes how to get/set a bindable property from an object instance.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
public abstract class BindingAccessor<T> : BindingAccessor
{
    protected BindingAccessor(string name, Func<object, T> getter, Action<object, T> setter) : base(name)
    {
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        Setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public Func<object, T> Getter { get; }

    public Action<object, T> Setter { get; }

    public override object? GetValue(object instance) => Getter(instance);

    public override void SetValue(object instance, object? value) => Setter(instance, (T)value!);
}
