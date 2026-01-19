// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Specifies glyphs used by bar charts.
/// </summary>
/// <param name="Full">The full-height glyph.</param>
/// <param name="Partials">Partial-height glyphs.</param>
public readonly record struct BarChartGlyphs(Rune Full, SparklineGlyphs Partials)
{
    /// <summary>
    /// Gets a glyph set based on block characters.
    /// </summary>
    public static BarChartGlyphs Blocks { get; } = new(new Rune(0x2588), SparklineGlyphs.Blocks8);
}

/// <summary>
/// Defines styling for bar chart controls.
/// </summary>
public sealed record BarChartStyle : IStyle<BarChartStyle>
{
    /// <summary>
    /// Gets the default bar chart style.
    /// </summary>
    public static BarChartStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for bar charts.
    /// </summary>
    public static StyleKey<BarChartStyle> Key { get; } = new("BarChartStyle", Default);

    /// <summary>
    /// Gets the glyphs used for the bars.
    /// </summary>
    public BarChartGlyphs Glyphs { get; init; } = BarChartGlyphs.Blocks;

    /// <summary>
    /// Gets the optional fill style for bars.
    /// </summary>
    public Style? FillStyle { get; init; }

    /// <summary>
    /// Resolves the fill style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public Style ResolveFill(Theme theme)
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
