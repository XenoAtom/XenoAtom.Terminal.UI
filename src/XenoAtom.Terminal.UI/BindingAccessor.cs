// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Describes how to get/set a bindable property from an object instance.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
public abstract class BindingAccessor<T>
{
    protected BindingAccessor(string name, Func<object, T> getter, Action<object, T> setter)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        Setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public string Name { get; }

    public Func<object, T> Getter { get; }

    public Action<object, T> Setter { get; }
}

