// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Wraps a delegate to avoid the C# "property invocation" syntax ambiguity (e.g. <c>obj.MyDelegate(...)</c>),
/// while still allowing ergonomic assignment and binding.
/// </summary>
/// <typeparam name="TDelegate">The delegate type.</typeparam>
/// <remarks>
/// <para>
/// This type is primarily used for bindable properties whose type is a delegate (e.g. validators, formatters).
/// In C#, when a property is itself a delegate, the syntax <c>obj.Property(...)</c> attempts to invoke that delegate,
/// which prevents fluent extension methods with the same name from being used.
/// </para>
/// <para>
/// By wrapping the delegate, <c>obj.Property(...)</c> is no longer treated as a delegate invocation, allowing the fluent
/// extension method resolution to pick the generated bindable fluent API instead.
/// </para>
/// </remarks>
public readonly record struct Delegator<TDelegate>(TDelegate? Invoke) where TDelegate : Delegate
{
    /// <summary>
    /// Gets a value indicating whether this instance does not contain a delegate.
    /// </summary>
    public bool IsEmpty => Invoke is null;

    /// <summary>
    /// Implicitly wraps a delegate into a <see cref="Delegator{TDelegate}"/>.
    /// </summary>
    /// <param name="value">The delegate value.</param>
    public static implicit operator Delegator<TDelegate>(TDelegate? value) => new(value);

    /// <summary>
    /// Implicitly unwraps the delegate stored in a <see cref="Delegator{TDelegate}"/>.
    /// </summary>
    /// <param name="value">The delegator value.</param>
    public static implicit operator TDelegate?(Delegator<TDelegate> value) => value.Invoke;
}

