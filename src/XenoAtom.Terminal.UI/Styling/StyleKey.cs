// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Identifies a style entry in a theme/environment.
/// </summary>
/// <typeparam name="T">The style type.</typeparam>
public sealed class StyleKey<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StyleKey{T}"/> class.
    /// </summary>
    /// <param name="name">The style name.</param>
    /// <param name="defaultValue">The default style value.</param>
    public StyleKey(string name, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = string.Intern(name);
        BindingAccessor = new EnvironmentBindingAccessor(string.Intern("$env$" + Name));
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Gets the style name.
    /// </summary>
    public string Name { get; }

    internal BindingAccessor BindingAccessor { get; }

    /// <summary>
    /// Gets the default style value.
    /// </summary>
    public T DefaultValue { get; }

    /// <inheritdoc />
    public override string ToString() => Name;

    private sealed class EnvironmentBindingAccessor : BindingAccessor
    {
        public EnvironmentBindingAccessor(string name) : base(name)
        {
        }

        public override bool IsReadOnly => false;

        public override object? GetValueAsObject(object instance) => throw new NotSupportedException();

        public override void SetValueAsObject(object instance, object? value) => throw new NotSupportedException();
    }
}
