// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record TabControlStyle : IStyle<TabControlStyle>
{
    public static TabControlStyle Default { get; } = new();

    public static StyleKey<TabControlStyle> Key { get; } = new("TabControlStyle", Default);

    public bool ShowBorder { get; init; } = true;

    public Thickness TabPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    public CellStyle? StripStyle { get; init; }
    public CellStyle? TabStyle { get; init; }
    public CellStyle? TabHoveredStyle { get; init; }
    public CellStyle? TabPressedStyle { get; init; }
    public CellStyle? TabSelectedStyle { get; init; }
    public CellStyle? TabDisabledStyle { get; init; }

    public CellStyle ResolveStripStyle(Theme theme) => StripStyle ?? theme.BaseTextStyle();

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
