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
/// Represents a horizontal progress bar.
/// </summary>
public sealed partial class ProgressBar : Visual
{
    private static readonly Rune[] SegmentGlyphs =
    [
        new Rune(' '),        // 0/8
        new Rune(0x258F),     // ▏ 1/8
        new Rune(0x258E),     // ▎ 2/8
        new Rune(0x258D),     // ▍ 3/8
        new Rune(0x258C),     // ▌ 4/8
        new Rune(0x258B),     // ▋ 5/8
        new Rune(0x258A),     // ▊ 6/8
        new Rune(0x2589),     // ▉ 7/8
        new Rune(0x2588),     // █ 8/8
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressBar"/> class.
    /// </summary>
    public ProgressBar()
    {
        HorizontalAlignment = Align.Stretch;
    }

    /// <summary>
    /// Gets or sets the progress value in the range [0..1].
    /// </summary>
    [Bindable]
    public partial double Value { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var progressStyle = GetStyle<ProgressBarStyle>();

        var minBarWidth = 10;
        var required = minBarWidth;

        var showFrame = progressStyle.ShowFrame || progressStyle.Variant == ProgressBarVariant.Bracketed;
        if (showFrame)
        {
            required += 2;
        }

        var min = new Size(required, 1);
        var natural = min;
        var max = new Size(LayoutConstants.Infinite, 1);

        return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
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
        if (rect.Width <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var progressStyle = GetStyle<ProgressBarStyle>();

        var value = Math.Clamp(Value, 0.0, 1.0);

        var borderStyle = progressStyle.ResolveBorder(theme);
        var filledStyle = progressStyle.ResolveFilled(theme);
        var unfilledStyle = progressStyle.ResolveUnfilled(theme);

        var barStartX = rect.X;
        var barEndX = rect.X + rect.Width;

        var showFrame = progressStyle.ShowFrame || progressStyle.Variant == ProgressBarVariant.Bracketed;
        if (showFrame && barEndX - barStartX >= 2)
        {
            buffer.SetCell(barStartX, rect.Y, progressStyle.FrameLeftGlyph, borderStyle);
            buffer.SetCell(barEndX - 1, rect.Y, progressStyle.FrameRightGlyph, borderStyle);
            barStartX++;
            barEndX--;
        }

        var barWidth = Math.Max(0, barEndX - barStartX);
        if (barWidth <= 0)
        {
            return;
        }

        if (progressStyle.Variant == ProgressBarVariant.Segmented)
        {
            RenderSegmented(buffer, rect.Y, barStartX, barWidth, value, progressStyle.FillGlyph, progressStyle.TrackGlyph, filledStyle, unfilledStyle);
            return;
        }

        if (progressStyle.Variant == ProgressBarVariant.Shaded)
        {
            RenderSolid(buffer, rect.Y, barStartX, barWidth, value, new Rune(0x2593), new Rune(0x2591), filledStyle, unfilledStyle);
            return;
        }

        RenderSolid(buffer, rect.Y, barStartX, barWidth, value, progressStyle.FillGlyph, progressStyle.TrackGlyph, filledStyle, unfilledStyle);
    }

    private static void RenderSolid(CellBuffer buffer, int y, int x, int width, double value, Rune fill, Rune track, Style fillStyle, Style trackStyle)
    {
        var filled = (int)Math.Round(width * value);
        filled = Math.Clamp(filled, 0, width);

        for (var i = 0; i < width; i++)
        {
            buffer.SetCell(x + i, y, i < filled ? fill : track, i < filled ? fillStyle : trackStyle);
        }
    }

    private static void RenderSegmented(CellBuffer buffer, int y, int x, int width, double value, Rune fullFill, Rune track, Style fillStyle, Style trackStyle)
    {
        value = Math.Clamp(value, 0.0, 1.0);
        var scaled = value * width;
        var whole = (int)Math.Floor(scaled);
        var frac = scaled - whole;

        whole = Math.Clamp(whole, 0, width);
        var remainder = (int)Math.Round(frac * 8.0);
        remainder = Math.Clamp(remainder, 0, 8);

        for (var i = 0; i < width; i++)
        {
            buffer.SetCell(x + i, y, track, trackStyle);
        }

        for (var i = 0; i < whole; i++)
        {
            buffer.SetCell(x + i, y, fullFill, fillStyle);
        }

        if (whole < width && remainder > 0)
        {
            buffer.SetCell(x + whole, y, SegmentGlyphs[remainder], fillStyle);
        }
    }
}
