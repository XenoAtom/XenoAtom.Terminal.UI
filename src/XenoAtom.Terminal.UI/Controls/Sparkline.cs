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
/// Renders a one-line sparkline for a sequence of values.
/// </summary>
public sealed partial class Sparkline : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Sparkline"/> class.
    /// </summary>
    public Sparkline()
    {
        Values = new BindableList<double>(this, nameof(Values));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sparkline"/> class with values.
    /// </summary>
    /// <param name="values">The values to render.</param>
    public Sparkline(IEnumerable<double> values) : this()
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var value in values)
        {
            Values.Add(value);
        }
    }

    /// <summary>
    /// Gets the values to render.
    /// </summary>
    [Bindable]
    public BindableList<double> Values { get; }

    /// <summary>
    /// Gets or sets the minimum value for scaling.
    /// </summary>
    [Bindable]
    public partial double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for scaling.
    /// </summary>
    [Bindable]
    public partial double? Maximum { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var count = Values.Count;
        return SizeHints.Fixed(constraints.Clamp(new Size(Math.Max(0, count), 1)));
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
        if (values.Count == 0)
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
        var style = GetStyle<SparklineStyle>();
        var glyphs = style.Glyphs;
        var cellStyle = style.Resolve(theme);

        var width = rect.Width;
        var count = values.Count;

        for (var x = 0; x < width; x++)
        {
            // Downsample by taking the max in the bucket to preserve spikes.
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
            var level = (int)Math.Round(t * 7.0);
            var rune = glyphs.GetLevel(level);
            buffer.SetCell(rect.X + x, rect.Y, rune, cellStyle);
        }
    }
}
