// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents a strongly-typed reference to a bindable property on an object instance.
/// </summary>
public readonly record struct Binding(object Owner, BindingAccessor Accessor)
{
    /// <summary>
    /// Gets a value indicating whether this binding is empty.
    /// </summary>
    public bool IsEmpty => Owner == null || Accessor == null;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => Accessor.IsReadOnly;
}

/// <summary>
/// Represents a strongly-typed reference to a bindable property on an object instance.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
public readonly record struct Binding<T>(object Owner, BindingAccessor<T> Accessor)
{
    /// <summary>
    /// Gets a value indicating whether this binding is empty.
    /// </summary>
    public bool IsEmpty => Owner == null || Accessor == null;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => Accessor.IsReadOnly;

    /// <summary>
    /// Sets the bound property value on the binding owner.
    /// </summary>
    /// <param name="value">The value to set.</param>
    public void SetValue(T value)
    {
        if (Accessor.Setter is null) throw new InvalidOperationException($"The binding {Accessor.Name} is read-only.");
        Accessor.Setter(Owner, value);
    }

    /// <summary>
    /// Gets the bound property value from the binding owner.
    /// </summary>
    /// <returns>The current value.</returns>
    public T GetValue() => Accessor.Getter(Owner);

    /// <summary>
    /// Converts a typed binding to a non-generic <see cref="Binding"/>.
    /// </summary>
    /// <param name="binding">The typed binding.</param>
    public static implicit operator Binding(Binding<T> binding) => new(binding.Owner, binding.Accessor);
}
