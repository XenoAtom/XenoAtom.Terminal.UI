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

        var barWidth = Math.Max(0, rect.Width - prefixWidth - 6);
        var filled = (int)Math.Round(barWidth * value);

        buffer.WriteText(rect.X, rect.Y, prefix.AsSpan(), CellStyle.None);
        var borderStyle = progressStyle.ResolveBorder(theme);
        var filledStyle = progressStyle.ResolveFilled(theme);
        var unfilledStyle = progressStyle.ResolveUnfilled(theme);

        buffer.WriteText(rect.X + prefixWidth, rect.Y, "[".AsSpan(), borderStyle);

        for (var i = 0; i < barWidth; i++)
        {
            buffer.SetCell(rect.X + prefixWidth + 1 + i, rect.Y, new Rune(i < filled ? progressStyle.FillGlyph : progressStyle.TrackGlyph), i < filled ? filledStyle : unfilledStyle);
        }

        buffer.WriteText(rect.X + prefixWidth + 1 + barWidth, rect.Y, "]".AsSpan(), borderStyle);

        var percentText = $"{percent,3}%";
        buffer.WriteText(rect.X + Math.Max(0, rect.Width - 4), rect.Y, percentText.AsSpan(), CellStyle.None);
    }
}
