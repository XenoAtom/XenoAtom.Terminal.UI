// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents a strongly-typed reference to a bindable property on an object instance.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
public readonly record struct Binding<T>(object Owner, BindingAccessor<T> Accessor)
{
}

