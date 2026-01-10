// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SparklineStyle : IStyle<SparklineStyle>
{
    public static SparklineStyle Default { get; } = new();

    public static StyleKey<SparklineStyle> Key { get; } = new("SparklineStyle", Default);

    public SparklineGlyphs Glyphs { get; init; } = SparklineGlyphs.Blocks8;

    public CellStyle? Style { get; init; }

    public CellStyle Resolve(Theme theme)
    {
        if (Style is { } s)
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

