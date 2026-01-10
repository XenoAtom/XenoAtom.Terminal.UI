// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record MenuListStyle : IStyle<MenuListStyle>
{
    public static MenuListStyle Default { get; } = new();

    public static StyleKey<MenuListStyle> Key { get; } = new("MenuListStyle", Default);

    public Thickness Padding { get; init; } = new(Left: 1, Top: 1, Right: 1, Bottom: 1);

    public int SpaceBetweenIconAndText { get; init; } = 1;

    public int SpaceBetweenTextAndShortcut { get; init; } = 2;

    public Rune SubmenuGlyph { get; init; } = new('›');

    public CellStyle? ItemStyle { get; init; }
    public CellStyle? SelectedStyle { get; init; }
    public CellStyle? HoveredStyle { get; init; }
    public CellStyle? DisabledStyle { get; init; }
    public CellStyle? SeparatorStyle { get; init; }

    public CellStyle ResolveItemStyle(Theme theme, bool enabled, bool selected, bool hovered)
    {
        var baseStyle = theme.ForegroundTextStyle();

        if (!enabled)
        {
            var disabled = baseStyle | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                disabled = disabled.WithForeground(c);
            }
            return DisabledStyle ?? disabled;
        }

        if (selected)
        {
            if (SelectedStyle is { } s)
            {
                return s;
            }

            var style = baseStyle | TextStyle.Bold;
            if (theme.SurfaceAlt is { } bg)
            {
                style = style.WithBackground(bg);
            }
            return style;
        }

        if (hovered)
        {
            return HoveredStyle ?? (baseStyle | TextStyle.Bold);
        }

        return ItemStyle ?? baseStyle;
    }

    public CellStyle ResolveSeparatorStyle(Theme theme)
    {
        if (SeparatorStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle() | TextStyle.Dim;
        if (theme.Disabled is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }
}

