// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

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

    public ProgressBar()
    {
    }

    [Bindable]
    public partial double Value { get; set; }

    [Bindable]
    public partial string? Label { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Max(10, Math.Min(availableSize.Width, 30));
        return new Size(width, 1);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var progressStyle = GetEnvironmentValue(ProgressBarStyle.Key);

        var value = Math.Clamp(Value, 0.0, 1.0);
        var percent = (int)Math.Round(value * 100.0);

        var label = Label;
        var prefix = string.IsNullOrEmpty(label) ? string.Empty : $"{label} ";
        var prefixWidth = TerminalTextUtility.GetWidth(prefix.AsSpan());

        var showPercent = progressStyle.ShowPercentage;
        var percentText = showPercent ? $"{percent,3}%" : string.Empty;
        var percentWidth = showPercent ? TerminalTextUtility.GetWidth(percentText.AsSpan()) : 0;

        buffer.WriteText(rect.X, rect.Y, prefix.AsSpan(), CellStyle.None);

        var borderStyle = progressStyle.ResolveBorder(theme);
        var filledStyle = progressStyle.ResolveFilled(theme);
        var unfilledStyle = progressStyle.ResolveUnfilled(theme);

        var barStartX = rect.X + prefixWidth;
        var barEndX = rect.X + rect.Width - percentWidth;
        if (showPercent && barEndX > barStartX)
        {
            barEndX = Math.Max(barStartX, barEndX - 1);
        }

        if (showPercent && percentWidth > 0)
        {
            buffer.WriteText(rect.X + Math.Max(0, rect.Width - percentWidth), rect.Y, percentText.AsSpan(), CellStyle.None);
        }

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

    private static void RenderSolid(CellBuffer buffer, int y, int x, int width, double value, Rune fill, Rune track, CellStyle fillStyle, CellStyle trackStyle)
    {
        var filled = (int)Math.Round(width * value);
        filled = Math.Clamp(filled, 0, width);

        for (var i = 0; i < width; i++)
        {
            buffer.SetCell(x + i, y, i < filled ? fill : track, i < filled ? fillStyle : trackStyle);
        }
    }

    private static void RenderSegmented(CellBuffer buffer, int y, int x, int width, double value, Rune fullFill, Rune track, CellStyle fillStyle, CellStyle trackStyle)
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
