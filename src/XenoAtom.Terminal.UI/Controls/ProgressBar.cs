// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
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
    public partial Visual? Label { get; set; }

    protected override int ChildrenCount => _label is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _label is not null ? _label : throw new ArgumentOutOfRangeException(nameof(index));

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var progressStyle = Get<ProgressBarStyle>();

        var showPercent = progressStyle.ShowPercentage;
        var percentWidth = showPercent ? 4 : 0; // "100%"

        var label = Label;
        var labelWidth = 0;
        if (label is not null)
        {
            label.Measure(new Size(LayoutConstants.Infinite, 1));
            labelWidth = label.DesiredSize.Width;
            if (labelWidth > 0)
            {
                labelWidth += 1; // space after label
            }
        }

        var minBarWidth = 10;
        var desiredWidth = labelWidth + minBarWidth + percentWidth;
        var width = Math.Min(availableSize.Width, Math.Max(minBarWidth, desiredWidth));
        return SizeHints.Fixed(new Size(width, 1));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var rect = finalRect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var progressStyle = Get<ProgressBarStyle>();
        var percentWidth = progressStyle.ShowPercentage ? 4 : 0; // "100%"
        var label = Label;
        if (label is null)
        {
            return;
        }

        var available = Math.Max(0, rect.Width - percentWidth);
        var labelDesired = Math.Min(available, label.DesiredSize.Width);
        if (labelDesired <= 0 || available <= 0)
        {
            return;
        }

        label.Arrange(new Rectangle(rect.X, rect.Y, labelDesired, 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var progressStyle = Get<ProgressBarStyle>();

        var value = Math.Clamp(Value, 0.0, 1.0);
        var percent = (int)Math.Round(value * 100.0);

        var label = Label;
        var prefixWidth = 0;
        if (label is not null)
        {
            prefixWidth = label.Bounds.Width;
            if (prefixWidth > 0 && prefixWidth < rect.Width)
            {
                prefixWidth += 1; // space after label
            }
        }

        var showPercent = progressStyle.ShowPercentage;
        var percentText = showPercent ? $"{percent,3}%" : string.Empty;
        var percentWidth = showPercent ? TerminalTextUtility.GetWidth(percentText.AsSpan()) : 0;

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

        if (label is not null && prefixWidth > 0 && label.Bounds.Width > 0 && rect.Width > label.Bounds.Width)
        {
            buffer.SetCell(rect.X + label.Bounds.Width, rect.Y, new Rune(' '), CellStyle.None);
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
