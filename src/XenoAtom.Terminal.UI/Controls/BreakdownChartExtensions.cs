// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides convenience extension methods for building <see cref="BreakdownChart"/> instances.
/// </summary>
public static partial class BreakdownChartExtensions
{
    /// <summary>
    /// Appends a segment to a breakdown.
    /// </summary>
    /// <param name="breakdown">The breakdown chart to update.</param>
    /// <param name="value">The segment value.</param>
    /// <param name="label">The optional segment label visual.</param>
    /// <param name="color">The optional segment color.</param>
    /// <param name="tooltip">The optional tooltip visual displayed when hovered.</param>
    /// <returns>The same breakdown chart instance for chaining.</returns>
    public static BreakdownChart Segment(this BreakdownChart breakdown, double value, Visual? label = null, Color? color = null, Visual? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(breakdown);

        var segment = new BreakdownSegment
        {
            Value = value,
            Label = label,
            Color = color,
            Tooltip = tooltip,
        };

        breakdown.Segments.Add(segment);
        return breakdown;
    }

    /// <summary>
    /// Sets how leftover space is distributed along each compact legend line.
    /// </summary>
    /// <param name="breakdown">The breakdown chart to update.</param>
    /// <param name="justify">The legend justification.</param>
    /// <returns>The same breakdown chart instance for chaining.</returns>
    public static BreakdownChart LegendJustify(this BreakdownChart breakdown, WrapJustify justify)
    {
        ArgumentNullException.ThrowIfNull(breakdown);

        var style = breakdown.GetStyle<BreakdownStyle>();
        breakdown.Style(style with { LegendJustify = justify });
        return breakdown;
    }
}
