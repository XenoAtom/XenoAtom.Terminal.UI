// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Select{T}"/>.
/// </summary>
public sealed record SelectStyle : IStyle<SelectStyle>
{
    /// <summary>
    /// Gets the default select style.
    /// </summary>
    public static SelectStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SelectStyle"/>.
    /// </summary>
    public static StyleKey<SelectStyle> Key { get; } = new("SelectStyle", Default);

    /// <summary>
    /// Gets the padding applied around the select content.
    /// </summary>
    public Thickness Padding { get; init; } = new(Left: 1, Top: 0, Right: 2, Bottom: 0);

    /// <summary>
    /// Gets the glyph used to render the dropdown arrow.
    /// </summary>
    public Rune ArrowGlyph { get; init; } = new('▾');

    /// <summary>
    /// Gets the optional style used for the normal state.
    /// </summary>
    public CellStyle? NormalStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the hovered state.
    /// </summary>
    public CellStyle? HoverStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the focused state.
    /// </summary>
    public CellStyle? FocusedStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the disabled state.
    /// </summary>
    public CellStyle? DisabledStyle { get; init; }

    /// <summary>
    /// Resolves the select style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the control is enabled.</param>
    /// <param name="focused">Whether the control is focused.</param>
    /// <param name="hovered">Whether the control is hovered.</param>
    public CellStyle ResolveStyle(Theme theme, bool enabled, bool focused, bool hovered)
    {
        if (!enabled && DisabledStyle is { } disabled)
        {
            return disabled;
        }

        if (focused && FocusedStyle is { } focus)
        {
            return focus;
        }

        if (hovered && HoverStyle is { } hover)
        {
            return hover;
        }

        if (NormalStyle is { } normal)
        {
            return normal;
        }

        var style = CellStyle.None;
        if (!enabled)
        {
            style |= TextStyle.Dim;
        }
        else if (focused)
        {
            style |= TextStyle.Bold;
        }

        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }

        return style;
    }
}
