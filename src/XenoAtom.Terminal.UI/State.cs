// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// A bindable container for a value, useful for passing state between visuals.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed partial class State<T> : Threading.DispatcherObject
{
    /// <summary>
    /// Initializes a new instance of the State class with the specified value and optional name.
    /// </summary>
    /// <param name="value">The value to be stored in the state.</param>
    /// <param name="name">The name associated with the state. If not specified, the caller member name is used.</param>
    public State(T value, [CallerMemberName] string? name = null)
    {
        _value = value;
        Name = name;
        //this.Value(1);
    }

    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets or sets the current value of the object.
    /// </summary>
    [Bindable]
    public partial T Value { get; set; }

    /// <summary>
    /// Defines an implicit conversion from a State&lt;T> instance to its underlying value of type T.
    /// </summary>
    /// <remarks>This operator enables seamless use of State&lt;T> objects in contexts where a value of type T is
    /// expected. If the State&lt;T> instance is null, the behavior depends on the implementation of the Value property and
    /// the type T.</remarks>
    /// <param name="value">The State&lt;T> instance to convert.</param>
    public static implicit operator T(State<T> value) => value.Value;

    /// <summary>
    /// Converts a State&lt;T> instance to a Binding&lt;T> implicitly.
    /// </summary>
    /// <remarks>This implicit conversion allows a State&lt;T> to be used wherever a Binding&lt;T> is expected,
    /// enabling seamless integration in data binding scenarios.</remarks>
    /// <param name="state">The state object to convert to a binding. Cannot be null.</param>
    public static implicit operator Binding<T>(State<T> state) => new(state, __Value__BindingAccessor.Instance);

    /// <summary>
    /// Converts a State&lt;T> instance to a Binding, enabling implicit binding of state values.
    /// </summary>
    /// <remarks>This implicit conversion allows State&lt;T> objects to be used wherever a Binding is expected,
    /// simplifying data binding scenarios.</remarks>
    /// <param name="state">The state object to be converted to a Binding. Cannot be null.</param>
    public static implicit operator Binding(State<T> state) => new(state, __Value__BindingAccessor.Instance);

    /// <inheritdoc/>
    public override string? ToString() => Value?.ToString();
}
