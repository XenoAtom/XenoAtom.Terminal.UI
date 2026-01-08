// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record TableStyle : IStyle<TableStyle>
{
    public static TableStyle Default { get; } = new();

    public static StyleKey<TableStyle> Key { get; } = new("TableStyle", Default);

    public static TableStyle Minimal { get; } = Default with
    {
        ShowOuterBorder = false,
        ShowVerticalLines = false,
        ShowRowSeparators = false,
    };

    public static TableStyle Grid { get; } = Default with
    {
        ShowOuterBorder = true,
        ShowVerticalLines = true,
        ShowRowSeparators = true,
    };

    public static TableStyle RoundedGrid { get; } = Grid with
    {
        Glyphs = LineGlyphs.Rounded,
    };

    public static TableStyle DoubleGrid { get; } = Grid with
    {
        Glyphs = LineGlyphs.Double,
    };

    public bool ShowOuterBorder { get; init; } = true;

    public bool ShowVerticalLines { get; init; } = true;

    public bool ShowRowSeparators { get; init; }

    public bool ShowHeaderSeparator { get; init; } = true;

    public Thickness CellPadding { get; init; } = new(1, 0, 1, 0);

    public LineGlyphs? Glyphs { get; init; }

    public CellStyle? CellStyle { get; init; }

    public CellStyle? HeaderStyle { get; init; }

    public CellStyle? BorderStyle { get; init; }

    public CellStyle ResolveCellStyle(Theme theme)
        => CellStyle ?? theme.BaseTextStyle();

    public CellStyle ResolveHeaderStyle(Theme theme)
        => HeaderStyle ?? (ResolveCellStyle(theme) | TextStyle.Bold);

    public CellStyle ResolveBorderStyle(Theme theme, bool focused)
        => BorderStyle ?? theme.BorderStyle(focused);
}
