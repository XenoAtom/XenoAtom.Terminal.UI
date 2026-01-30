// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Describes how to get/set a bindable property from an object instance.
/// </summary>
public abstract class BindingAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BindingAccessor"/> class.
    /// </summary>
    /// <param name="name">The bindable property name.</param>
    protected BindingAccessor(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets the bindable property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public abstract bool IsReadOnly { get; }

    /// <summary>
    /// Gets the property value from the specified instance.
    /// </summary>
    /// <param name="instance">The property owner.</param>
    /// <returns>The current value.</returns>
    public abstract object? GetValueAsObject(object instance);

    /// <summary>
    /// Sets the property value on the specified instance.
    /// </summary>
    /// <param name="instance">The property owner.</param>
    /// <param name="value">The value to set.</param>
    public abstract void SetValueAsObject(object instance, object? value);
}

/// <summary>
/// Describes how to get/set a bindable property from an object instance.
/// </summary>
/// <typeparam name="T">The property type.</typeparam>
public class BindingAccessor<T> : BindingAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BindingAccessor{T}"/> class.
    /// </summary>
    /// <param name="name">The bindable property name.</param>
    /// <param name="getter">A delegate that reads the value from an instance.</param>
    /// <param name="setter">A delegate that writes the value to an instance.</param>
    public BindingAccessor(string name, Func<object, T> getter, Action<object, T>? setter) : base(name)
    {
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
        Setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    /// <summary>
    /// Gets the delegate used to read the value.
    /// </summary>
    public Func<object, T> Getter { get; }

    /// <summary>
    /// Gets the delegate used to write the value.
    /// </summary>
    public Action<object, T>? Setter { get; }
    
    /// <inheritdoc />
    public override bool IsReadOnly => Setter is null;

    /// <summary>
    /// Gets the value of the property or field from the specified object instance.
    /// </summary>
    /// <param name="instance">The object instance from which to retrieve the value. Must not be null.</param>
    /// <returns>The value of type T obtained from the specified instance.</returns>
    public T GetValue(object instance) => Getter(instance);

    /// <summary>
    /// Sets the value of the property on the specified object instance.
    /// </summary>
    /// <param name="instance">The object instance whose property value is to be set.</param>
    /// <param name="value">The value to assign to the property.</param>
    /// <exception cref="InvalidOperationException">Thrown if the property is read-only.</exception>
    public void SetValue(object instance, T value)
    {
        if (Setter is null) throw new InvalidOperationException($"The property '{Name}' is read-only.");
        Setter(instance, (T)value!);
    }
    
    /// <inheritdoc />
    public override object? GetValueAsObject(object instance) => Getter(instance);

    /// <inheritdoc />
    public override void SetValueAsObject(object instance, object? value)
    {
        if (Setter is null) throw new InvalidOperationException($"The property '{Name}' is read-only.");
        Setter(instance, (T)value!);
    }
}
