// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Renders a simple line chart for a series of values.
/// </summary>
public sealed partial class LineChart : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineChart"/> class.
    /// </summary>
    public LineChart()
    {
        Values = new BindableList<double>(this, "LineCharValues");
    }

    /// <summary>
    /// Gets the data values to render.
    /// </summary>
    [Bindable]
    public BindableList<double> Values { get; }

    /// <summary>
    /// Gets or sets the minimum value for the chart scale.
    /// </summary>
    [Bindable]
    public partial double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for the chart scale.
    /// </summary>
    [Bindable]
    public partial double? Maximum { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var count = Values?.Count ?? 0;
        if (count <= 0)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        var width = Math.Min(availableSize.Width, Math.Max(1, count));
        var height = Math.Min(availableSize.Height, Math.Max(1, 4));
        return SizeHints.Fixed(new Size(width, height));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;
    }

    /// <inheritdoc />
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
        var style = Get<LineChartStyle>();
        var point = style.PointGlyph;
        var pointStyle = style.ResolvePointStyle(theme);

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
            var y = rect.Y + (int)Math.Round((1.0 - t) * (height - 1));
            buffer.SetCell(rect.X + x, y, point, pointStyle);
        }
    }
}
