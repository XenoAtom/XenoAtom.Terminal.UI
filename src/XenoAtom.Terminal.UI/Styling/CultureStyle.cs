// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Provides culture configuration for formatting values (for example when converting numbers to strings).
/// </summary>
/// <remarks>
/// <para>
/// The active culture can be set per application (see <see cref="TerminalAppOptions.Culture"/>) and can be overridden
/// for a visual subtree by setting this style in the environment:
/// </para>
/// <code>
/// root.Style(new CultureStyle { Culture = CultureInfo.GetCultureInfo("fr-FR") });
/// </code>
/// <para>
/// Controls and helpers that need culture-aware formatting typically use <see cref="Visual.ToStringValue{T}"/> or
/// <see cref="Visual.ToStringObject(object?,string?)"/> to centralize formatting behavior.
/// </para>
/// </remarks>
public sealed record CultureStyle : IStyle<CultureStyle>
{
    /// <summary>
    /// Gets the default culture style.
    /// </summary>
    public static CultureStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="CultureStyle"/>.
    /// </summary>
    public static StyleKey<CultureStyle> Key { get; } = new(nameof(CultureStyle), Default);

    /// <summary>
    /// Gets the culture used for formatting values.
    /// </summary>
    /// <remarks>
    /// When not set explicitly by the host, the default is <see cref="CultureInfo.InvariantCulture"/>.
    /// </remarks>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;
}
