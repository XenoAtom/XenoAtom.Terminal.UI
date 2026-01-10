// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Sparkline : Visual
{
    [Bindable]
    public partial IReadOnlyList<double>? Values { get; set; }

    [Bindable]
    public partial double? Minimum { get; set; }

    [Bindable]
    public partial double? Maximum { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = Values?.Count ?? 0;
        var width = Math.Min(availableSize.Width, count);
        return new Size(Math.Max(0, width), Math.Min(availableSize.Height, 1));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
    }

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
        var style = Get<SparklineStyle>();
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

