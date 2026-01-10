// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record MenuBarStyle : IStyle<MenuBarStyle>
{
    public static MenuBarStyle Default { get; } = new();

    public static StyleKey<MenuBarStyle> Key { get; } = new("MenuBarStyle", Default);

    public Thickness Padding { get; init; } = new(Left: 1, Top: 0, Right: 1, Bottom: 0);

    public Thickness ItemPadding { get; init; } = new(Left: 2, Top: 0, Right: 2, Bottom: 0);

    public int ItemSpacing { get; init; } = 0;

    public CellStyle? BarStyle { get; init; }
    public CellStyle? ItemStyle { get; init; }
    public CellStyle? ItemHoverStyle { get; init; }
    public CellStyle? ItemOpenStyle { get; init; }
    public CellStyle? ItemSelectedStyle { get; init; }
    public CellStyle? ItemDisabledStyle { get; init; }

    public CellStyle ResolveBarStyle(Theme theme)
    {
        if (BarStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        else if (theme.Surface is { } bg2)
        {
            style = style.WithBackground(bg2);
        }

        return style;
    }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool open, bool selected, bool hovered)
    {
        if (!enabled)
        {
            var disabled = theme.ForegroundTextStyle() | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return ItemDisabledStyle ?? disabled;
        }

        if (open)
        {
            return ItemOpenStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (selected)
        {
            return ItemSelectedStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        if (hovered)
        {
            return ItemHoverStyle ?? (theme.BorderStyle(focused: true) | TextStyle.Bold);
        }

        return ItemStyle ?? theme.ForegroundTextStyle();
    }
}

