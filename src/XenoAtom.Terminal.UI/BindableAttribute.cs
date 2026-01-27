// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Marks a partial auto-property as a bindable property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class BindableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="Visual"/>-typed bindable property should be treated as a
    /// logical/template value rather than a direct visual child.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, when a <see cref="Visual"/> declares a bindable property whose type is also a <see cref="Visual"/>,
    /// the source generator treats it as a child relationship and generates attach/detach logic in the setter.
    /// </para>
    /// <para>
    /// Set <see cref="NoVisualAttach"/> to <see langword="true"/> when the value is used as an input to a template or is
    /// forwarded to an internal visual (for example, a <c>Content</c> property that is hosted by an internal
    /// <see cref="Controls.ScrollViewer"/>). In that case, the control should attach the visual explicitly (typically
    /// from <c>PrepareChildren</c>) and not via the generated setter.
    /// </para>
    /// </remarks>
    public bool NoVisualAttach { get; set; }
}
