// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record LineChartStyle : IStyle<LineChartStyle>
{
    public static LineChartStyle Default { get; } = new();

    public static StyleKey<LineChartStyle> Key { get; } = new("LineChartStyle", Default);

    public Rune PointGlyph { get; init; } = new Rune(0x2022); // U+2022

    public CellStyle? PointStyle { get; init; }

    public CellStyle ResolvePointStyle(Theme theme)
    {
        if (PointStyle is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.Accent is { } accent)
        {
            style = style.WithForeground(accent);
        }
        return style;
    }
}
