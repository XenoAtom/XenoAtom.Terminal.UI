// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Marks a property as participating in fluent configuration code generation.
/// </summary>
/// <remarks>
/// <para>
/// When a property is annotated with <see cref="FluentAttribute"/>, the source generator emits an extension
/// method that assigns the property and returns the same instance for chaining.
/// </para>
/// <para>
/// This attribute is intended for non-visual types that are not part of the binding system, such as prompt options.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FluentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FluentAttribute"/> class.
    /// </summary>
    public FluentAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentAttribute"/> class with a custom method name.
    /// </summary>
    /// <param name="methodName">The name of the fluent extension method to generate.</param>
    public FluentAttribute(string methodName)
    {
        MethodName = methodName;
    }

    /// <summary>
    /// Gets the optional fluent extension method name to generate.
    /// </summary>
    public string? MethodName { get; }
}

