// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a bar chart for a list of numeric values.
/// </summary>
public sealed partial class BarChart : Visual
{
    /// <summary>
    /// Gets or sets the values displayed by the chart.
    /// </summary>
    [Bindable]
    public partial IReadOnlyList<double>? Values { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum value used for normalization.
    /// </summary>
    [Bindable]
    public partial double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum value used for normalization.
    /// </summary>
    [Bindable]
    public partial double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets the chart orientation.
    /// </summary>
    [Bindable]
    public partial Orientation Orientation { get; set; }

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var count = Values?.Count ?? 0;
        if (count <= 0)
        {
            return SizeHints.Fixed(default);
        }

        var natural = Orientation == Orientation.Vertical
            ? new Size(Math.Max(1, count), 4)
            : new Size(10, Math.Max(1, count));

        return SizeHints.Fixed(constraints.Clamp(natural));
    }

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var values = Values;
        if (values is null || values.Count == 0)
        {
            return;
        }

        var min = Minimum;
        var max = Maximum;

        if (min is null || max is null)
        {
            var computedMin = double.PositiveInfinity;
            var computedMax = double.NegativeInfinity;

            for (var i = 0; i < values.Count; i++)
            {
                var v = values[i];
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    continue;
                }

                computedMin = Math.Min(computedMin, v);
                computedMax = Math.Max(computedMax, v);
            }

            if (double.IsInfinity(computedMin) || double.IsInfinity(computedMax))
            {
                return;
            }

            min ??= computedMin;
            max ??= computedMax;
        }

        var minV = min.GetValueOrDefault();
        var maxV = max.GetValueOrDefault();
        if (maxV <= minV)
        {
            maxV = minV + 1.0;
        }

        var theme = GetTheme();
        var style = Get<BarChartStyle>();
        var glyphs = style.Glyphs;
        var fillStyle = style.ResolveFill(theme);

        if (Orientation == Orientation.Horizontal)
        {
            RenderHorizontal(buffer, rect, values, minV, maxV, glyphs.Full, fillStyle);
        }
        else
        {
            RenderVertical(buffer, rect, values, minV, maxV, glyphs, fillStyle);
        }
    }

    private static void RenderHorizontal(CellBuffer buffer, Rectangle rect, IReadOnlyList<double> values, double minV, double maxV, Rune full, CellStyle style)
    {
        var count = Math.Min(values.Count, rect.Height);
        for (var row = 0; row < count; row++)
        {
            var v = values[row];
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                v = minV;
            }

            var t = (v - minV) / (maxV - minV);
            t = Math.Clamp(t, 0.0, 1.0);
            var cells = (int)Math.Round(t * rect.Width);

            var y = rect.Y + row;
            for (var x = 0; x < cells && x < rect.Width; x++)
            {
                buffer.SetCell(rect.X + x, y, full, style);
            }
        }
    }

    private static void RenderVertical(CellBuffer buffer, Rectangle rect, IReadOnlyList<double> values, double minV, double maxV, BarChartGlyphs glyphs, CellStyle style)
    {
        var width = rect.Width;
        var height = rect.Height;
        var count = values.Count;

        for (var x = 0; x < width; x++)
        {
            var start = (x * count) / width;
            var end = ((x + 1) * count) / width;
            if (end <= start)
            {
                end = Math.Min(count, start + 1);
            }

            var sample = double.NegativeInfinity;
            for (var i = start; i < end; i++)
            {
                var v = values[i];
                if (double.IsNaN(v) || double.IsInfinity(v))
                {
                    continue;
                }
                sample = Math.Max(sample, v);
            }

            if (double.IsNegativeInfinity(sample))
            {
                sample = minV;
            }

            var t = (sample - minV) / (maxV - minV);
            t = Math.Clamp(t, 0.0, 1.0);

            var scaled = t * height;
            var fullCells = Math.Min(height, (int)Math.Floor(scaled));
            var frac = scaled - fullCells;
            var partialLevel = (int)Math.Round(frac * 7.0);

            for (var i = 0; i < fullCells; i++)
            {
                buffer.SetCell(rect.X + x, rect.Bottom - 1 - i, glyphs.Full, style);
            }

            if (fullCells < height && partialLevel > 0)
            {
                buffer.SetCell(rect.X + x, rect.Bottom - 1 - fullCells, glyphs.Partials.GetLevel(partialLevel), style);
            }
        }
    }
}
