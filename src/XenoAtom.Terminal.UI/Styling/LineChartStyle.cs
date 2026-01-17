// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.LineChart"/>.
/// </summary>
public sealed record LineChartStyle : IStyle<LineChartStyle>
{
    /// <summary>
    /// Gets the default line chart style.
    /// </summary>
    public static LineChartStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for line charts.
    /// </summary>
    public static StyleKey<LineChartStyle> Key { get; } = new("LineChartStyle", Default);

    /// <summary>
    /// Gets the glyph used for data points.
    /// </summary>
    public Rune PointGlyph { get; init; } = new Rune(0x2022); // U+2022

    /// <summary>
    /// Gets the optional point style.
    /// </summary>
    public CellStyle? PointStyle { get; init; }

    /// <summary>
    /// Resolves the point style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
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
