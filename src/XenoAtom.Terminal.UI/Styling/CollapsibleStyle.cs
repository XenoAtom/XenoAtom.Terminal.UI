// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Collapsible"/>.
/// </summary>
public sealed record CollapsibleStyle : IStyle<CollapsibleStyle>
{
    /// <summary>
    /// Gets the default collapsible style.
    /// </summary>
    public static CollapsibleStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="CollapsibleStyle"/>.
    /// </summary>
    public static StyleKey<CollapsibleStyle> Key { get; } = new("CollapsibleStyle", Default);

    /// <summary>
    /// Gets the number of spaces between the expand/collapse glyph and the header content.
    /// </summary>
    public int SpaceBetweenGlyphAndHeader { get; init; } = 1;

    /// <summary>
    /// Gets the number of rows between header and content when expanded.
    /// </summary>
    public int ContentSpacing { get; init; }

    /// <summary>
    /// Gets the glyph used for expanded state.
    /// </summary>
    public Rune ExpandedGlyph { get; init; } = new('▾');

    /// <summary>
    /// Gets the glyph used for collapsed state.
    /// </summary>
    public Rune CollapsedGlyph { get; init; } = new('▸');

    /// <summary>
    /// Gets the optional style for a normal header.
    /// </summary>
    public Style? Header { get; init; }

    /// <summary>
    /// Gets the optional style for a hovered header.
    /// </summary>
    public Style? HeaderHovered { get; init; }

    /// <summary>
    /// Gets the optional style for a pressed header.
    /// </summary>
    public Style? HeaderPressed { get; init; }

    /// <summary>
    /// Gets the optional style for a focused header.
    /// </summary>
    public Style? HeaderFocused { get; init; }

    /// <summary>
    /// Gets the optional style for a disabled header.
    /// </summary>
    public Style? HeaderDisabled { get; init; }

    /// <summary>
    /// Resolves the header style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the control is enabled.</param>
    /// <param name="focused">Whether the control is focused.</param>
    /// <param name="hovered">Whether the header is hovered.</param>
    /// <param name="pressed">Whether the header is pressed.</param>
    public Style ResolveHeader(Theme theme, bool enabled, bool focused, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = Header ?? ResolveDefaultHeader(theme);

        if (!enabled)
        {
            if (HeaderDisabled is { } disabled)
            {
                return disabled | TextStyle.Dim;
            }

            if (theme.Disabled is { } disabledFg)
            {
                normal = normal.WithForeground(disabledFg);
            }

            return normal | TextStyle.Dim;
        }

        if (pressed)
        {
            return HeaderPressed ?? ResolveDefaultPressed(theme, normal);
        }

        var style = normal;
        if (hovered)
        {
            style = HeaderHovered ?? ResolveDefaultHovered(theme, style);
        }

        if (focused)
        {
            style = HeaderFocused ?? ResolveDefaultFocused(theme, style);
        }

        return style;
    }

    private static Style ResolveDefaultHeader(Theme theme)
        => theme.ForegroundTextStyle() | TextStyle.Bold;

    private static Style ResolveDefaultHovered(Theme theme, Style normal)
    {
        if ((theme.ControlFillHover ?? theme.SurfaceAlt) is { } bg)
        {
            return normal.WithBackground(bg);
        }

        return normal;
    }

    private static Style ResolveDefaultPressed(Theme theme, Style normal)
    {
        if (theme.Selection is { } bg)
        {
            return normal.WithBackground(bg) | TextStyle.Bold;
        }

        return normal | TextStyle.Bold;
    }

    private static Style ResolveDefaultFocused(Theme theme, Style normal)
    {
        if ((theme.Selection ?? theme.ControlFillPressed ?? theme.ControlFillHover ?? theme.SurfaceAlt) is { } bg)
        {
            normal = normal.WithBackground(bg);
        }

        if ((theme.FocusBorder ?? theme.Accent) is { } fg)
        {
            normal = normal.WithForeground(fg);
        }

        return normal | TextStyle.Underline;
    }
}
