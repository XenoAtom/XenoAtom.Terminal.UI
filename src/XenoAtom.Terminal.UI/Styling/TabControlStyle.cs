// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.TabControl"/>.
/// </summary>
public sealed record TabControlStyle : IStyle<TabControlStyle>
{
    /// <summary>
    /// Gets the default tab control style.
    /// </summary>
    public static TabControlStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TabControlStyle"/>.
    /// </summary>
    public static StyleKey<TabControlStyle> Key { get; } = new("TabControlStyle", Default);

    /// <summary>
    /// Gets the padding applied around each tab header.
    /// </summary>
    public Thickness TabPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    /// <summary>
    /// Gets the optional style for the tab strip background.
    /// </summary>
    public CellStyle? StripStyle { get; init; }

    /// <summary>
    /// Gets the optional base style for a tab header.
    /// </summary>
    public CellStyle? TabStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a hovered tab header.
    /// </summary>
    public CellStyle? TabHoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a pressed tab header.
    /// </summary>
    public CellStyle? TabPressedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a selected tab header.
    /// </summary>
    public CellStyle? TabSelectedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for a disabled tab header.
    /// </summary>
    public CellStyle? TabDisabledStyle { get; init; }

    /// <summary>
    /// Resolves the strip style for the provided <paramref name="theme"/>.
    /// </summary>
    public CellStyle ResolveStripStyle(Theme theme) => StripStyle ?? theme.BaseTextStyle();

    /// <summary>
    /// Resolves the tab style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the tab is enabled.</param>
    /// <param name="focused">Whether the tab control is focused.</param>
    /// <param name="selected">Whether the tab is selected.</param>
    /// <param name="hovered">Whether the tab is hovered.</param>
    /// <param name="pressed">Whether the tab is pressed.</param>
    public CellStyle ResolveTabStyle(Theme theme, bool enabled, bool focused, bool selected, bool hovered, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var normal = TabStyle ?? theme.SurfaceStyle();

        if (!enabled)
        {
            var disabled = normal | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return TabDisabledStyle ?? disabled;
        }

        if (pressed)
        {
            return TabPressedStyle ?? ResolveDefaultPressed(theme, normal);
        }

        if (selected)
        {
            var resolved = TabSelectedStyle ?? ResolveDefaultSelected(theme, normal);
            if (focused)
            {
                resolved = ResolveDefaultFocused(theme, resolved);
            }
            return resolved;
        }

        if (hovered)
        {
            return TabHoveredStyle ?? ResolveDefaultHovered(theme, normal);
        }

        return normal;
    }

    private static CellStyle ResolveDefaultHovered(Theme theme, CellStyle normal)
    {
        if (theme.SurfaceAlt is { } hoverBg)
        {
            normal = normal.WithBackground(hoverBg);
        }

        return normal | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultPressed(Theme theme, CellStyle normal)
    {
        if (theme.Selection is { } selectionBg)
        {
            normal = normal.WithBackground(selectionBg);
        }

        return normal | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultSelected(Theme theme, CellStyle normal)
    {
        var style = normal | TextStyle.Bold;
        if (theme.Accent is { } accent)
        {
            style = style.WithForeground(accent);
        }
        return style;
    }

    private static CellStyle ResolveDefaultFocused(Theme theme, CellStyle style)
    {
        if (theme.FocusBorder is { } focus)
        {
            style = style.WithForeground(focus);
        }

        return style | TextStyle.Underline;
    }
}
