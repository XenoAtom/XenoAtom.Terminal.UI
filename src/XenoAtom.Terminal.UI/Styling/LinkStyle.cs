// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.Link"/>.
/// </summary>
public sealed record LinkStyle : IStyle<LinkStyle>
{
    /// <summary>
    /// Gets the default link style.
    /// </summary>
    public static LinkStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for links.
    /// </summary>
    public static StyleKey<LinkStyle> Key { get; } = new("LinkStyle", Default);

    /// <summary>
    /// Gets the normal style.
    /// </summary>
    public CellStyle? Normal { get; init; }

    /// <summary>
    /// Gets the hovered style.
    /// </summary>
    public CellStyle? Hovered { get; init; }

    /// <summary>
    /// Gets the focused style.
    /// </summary>
    public CellStyle? Focused { get; init; }

    /// <summary>
    /// Gets the disabled style.
    /// </summary>
    public CellStyle? Disabled { get; init; }

    /// <summary>
    /// Resolves the link style for the given state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the link is enabled.</param>
    /// <param name="focused">Whether the link is focused.</param>
    /// <param name="hovered">Whether the link is hovered.</param>
    /// <returns>The resolved cell style.</returns>
    public CellStyle Resolve(Theme theme, bool enabled, bool focused, bool hovered)
    {
        if (!enabled)
        {
            if (Disabled is { } d)
            {
                return d;
            }

            var disabled = theme.ForegroundTextStyle() | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return disabled;
        }

        var baseStyle = theme.ForegroundTextStyle() | TextStyle.Underline;
        if (theme.Accent is { } accent)
        {
            baseStyle = baseStyle.WithForeground(accent);
        }

        if (focused)
        {
            return Focused ?? (baseStyle | TextStyle.Bold);
        }

        if (hovered)
        {
            return Hovered ?? (baseStyle | TextStyle.Bold);
        }

        return Normal ?? baseStyle;
    }
}
