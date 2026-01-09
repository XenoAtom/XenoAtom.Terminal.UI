// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SelectStyle : IStyle<SelectStyle>
{
    public static SelectStyle Default { get; } = new();

    public static StyleKey<SelectStyle> Key { get; } = new("SelectStyle", Default);

    public Thickness Padding { get; init; } = new(Left: 1, Top: 0, Right: 2, Bottom: 0);

    public bool ShowBorder { get; init; }

    public Rune ArrowGlyph { get; init; } = new('▾');

    public CellStyle? NormalStyle { get; init; }

    public CellStyle? HoverStyle { get; init; }

    public CellStyle? FocusedStyle { get; init; }

    public CellStyle? DisabledStyle { get; init; }

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
