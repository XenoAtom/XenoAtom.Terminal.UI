// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for the <see cref="Controls.BarChart"/> control.
/// </summary>
public sealed record BarChartStyle : IStyle<BarChartStyle>
{
    /// <summary>
    /// Gets the default bar chart style.
    /// </summary>
    public static BarChartStyle Default { get; } = new()
    {
        Padding = new Thickness(0),
        RowSpacing = 0,
        BarStyle = ProgressBarStyle.Segmented,
    };

    /// <summary>
    /// Gets the environment key for <see cref="BarChartStyle"/>.
    /// </summary>
    public static StyleKey<BarChartStyle> Key { get; } = new("BarChartStyle", Default);

    /// <summary>
    /// Gets the padding applied around the chart content.
    /// </summary>
    public Thickness Padding { get; init; }

    /// <summary>
    /// Gets the spacing between item rows.
    /// </summary>
    public int RowSpacing { get; init; }

    /// <summary>
    /// Gets an optional text block style applied to label cells.
    /// </summary>
    public TextBlockStyle? LabelTextStyle { get; init; }

    /// <summary>
    /// Gets an optional text block style applied to value cells.
    /// </summary>
    public TextBlockStyle? ValueTextStyle { get; init; }

    /// <summary>
    /// Gets the progress bar style used to render each bar row.
    /// </summary>
    public ProgressBarStyle BarStyle { get; init; } = ProgressBarStyle.Segmented;

    /// <summary>
    /// Gets an optional list of default bar colors.
    /// </summary>
    /// <remarks>
    /// When an item does not specify a bar color, the chart cycles through this list. When not provided, the chart
    /// falls back to theme tones (Primary/Success/Warning/Error).
    /// </remarks>
    public IReadOnlyList<Color?>? DefaultBarColors { get; init; }
}
