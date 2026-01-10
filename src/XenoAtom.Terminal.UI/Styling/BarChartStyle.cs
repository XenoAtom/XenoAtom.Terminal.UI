// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct BarChartGlyphs(Rune Full, SparklineGlyphs Partials)
{
    public static BarChartGlyphs Blocks { get; } = new(new Rune(0x2588), SparklineGlyphs.Blocks8);
}

public sealed record BarChartStyle : IStyle<BarChartStyle>
{
    public static BarChartStyle Default { get; } = new();

    public static StyleKey<BarChartStyle> Key { get; } = new("BarChartStyle", Default);

    public BarChartGlyphs Glyphs { get; init; } = BarChartGlyphs.Blocks;

    public CellStyle? FillStyle { get; init; }

    public CellStyle ResolveFill(Theme theme)
    {
        if (FillStyle is { } s)
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
