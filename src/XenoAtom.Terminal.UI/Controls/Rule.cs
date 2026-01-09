// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Rule : Visual
{
    [Bindable]
    public partial Orientation Orientation { get; set; }

    [Bindable]
    public partial Visual? StartLabel { get; set; }

    [Bindable]
    public partial Visual? CenterLabel { get; set; }

    [Bindable]
    public partial Visual? EndLabel { get; set; }

    protected override int ChildrenCount
        => (_startLabel is null ? 0 : 1) + (_centerLabel is null ? 0 : 1) + (_endLabel is null ? 0 : 1);

    protected override Visual GetChild(int index)
    {
        if (_startLabel is not null)
        {
            if (index == 0) return _startLabel;
            index--;
        }

        if (_centerLabel is not null)
        {
            if (index == 0) return _centerLabel;
            index--;
        }

        if (_endLabel is not null)
        {
            if (index == 0) return _endLabel;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var style = Get<RuleStyle>();
        var pad = Math.Max(0, style.LabelPadding);

        var start = StartLabel;
        var center = CenterLabel;
        var end = EndLabel;

        start?.Measure(new Size(int.MaxValue / 4, 1));
        center?.Measure(new Size(int.MaxValue / 4, 1));
        end?.Measure(new Size(int.MaxValue / 4, 1));

        if (Orientation == Orientation.Vertical)
        {
            var maxLabel = Math.Max(GetLabelWidth(start), Math.Max(GetLabelWidth(center), GetLabelWidth(end)));
            var requiredWidth = Math.Max(1, maxLabel == 0 ? 1 : (maxLabel + (pad * 2)));
            return new Size(Math.Min(availableSize.Width, requiredWidth), availableSize.Height);
        }

        return new Size(availableSize.Width, Math.Min(availableSize.Height, 1));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        if (finalRect.Width <= 0 || finalRect.Height <= 0)
        {
            return;
        }

        var style = Get<RuleStyle>();
        var pad = Math.Max(0, style.LabelPadding);

        if (Orientation == Orientation.Vertical)
        {
            ArrangeVertical(finalRect, pad);
            return;
        }

        ArrangeHorizontal(finalRect, pad);
    }

    private void ArrangeHorizontal(Rectangle rect, int pad)
    {
        var y = rect.Y;
        var width = rect.Width;
        if (width <= 0)
        {
            return;
        }

        var startTotal = GetTotalWidth(StartLabel, width, pad);
        var endTotal = GetTotalWidth(EndLabel, width, pad);
        var centerTotal = GetTotalWidth(CenterLabel, width, pad);

        ArrangeLabelLeft(StartLabel, rect.X, y, startTotal, pad);
        ArrangeLabelRight(EndLabel, rect.X, y, width, endTotal, pad);

        if (CenterLabel is not null)
        {
            if (centerTotal <= 0)
            {
                CenterLabel.Arrange(new Rectangle(rect.X, y, 0, 1));
                return;
            }

            var desiredX = rect.X + Math.Max(0, (width - centerTotal) / 2);
            var minX = rect.X + startTotal;
            var maxX = rect.X + Math.Max(0, width - endTotal - centerTotal);
            if (maxX < minX)
            {
                CenterLabel.Arrange(new Rectangle(rect.X, y, 0, 1));
                return;
            }

            var x = Math.Clamp(desiredX, minX, maxX);
            CenterLabel.Arrange(new Rectangle(x + pad, y, Math.Max(0, centerTotal - (pad * 2)), 1));
        }
    }

    private void ArrangeVertical(Rectangle rect, int pad)
    {
        var height = rect.Height;
        var width = rect.Width;
        if (height <= 0 || width <= 0)
        {
            return;
        }

        ArrangeVerticalLabel(StartLabel, rect, pad, rect.Y);
        ArrangeVerticalLabel(CenterLabel, rect, pad, rect.Y + (height - 1) / 2);
        ArrangeVerticalLabel(EndLabel, rect, pad, rect.Y + height - 1);
    }

    private static void ArrangeVerticalLabel(Visual? label, Rectangle rect, int pad, int y)
    {
        if (label is null)
        {
            return;
        }

        var total = Math.Min(rect.Width, label.DesiredSize.Width + (pad * 2));
        if (total <= 0)
        {
            label.Arrange(new Rectangle(rect.X, y, 0, 1));
            return;
        }

        var startX = rect.X + Math.Max(0, (rect.Width - total) / 2);
        label.Arrange(new Rectangle(startX + pad, y, Math.Max(0, total - (pad * 2)), 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<RuleStyle>();
        var pad = Math.Max(0, style.LabelPadding);
        var glyphs = style.ResolveGlyphs(theme);
        var lineStyle = style.ResolveLineStyle(theme);

        if (Orientation == Orientation.Vertical)
        {
            var x = rect.X + (rect.Width / 2);
            for (var y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                buffer.SetCell(x, y, glyphs.Vertical, lineStyle);
            }

            RenderVerticalLabelGap(buffer, rect, StartLabel, pad, rect.Y);
            RenderVerticalLabelGap(buffer, rect, CenterLabel, pad, rect.Y + (rect.Height - 1) / 2);
            RenderVerticalLabelGap(buffer, rect, EndLabel, pad, rect.Y + rect.Height - 1);
            return;
        }

        var y0 = rect.Y;
        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, y0, glyphs.Horizontal, lineStyle);
        }

        RenderHorizontalLabelGap(buffer, rect, StartLabel, pad);
        RenderHorizontalLabelGap(buffer, rect, CenterLabel, pad);
        RenderHorizontalLabelGap(buffer, rect, EndLabel, pad);
    }

    private static void RenderHorizontalLabelGap(CellBuffer buffer, Rectangle rect, Visual? label, int pad)
    {
        if (label is null || label.Bounds.Width <= 0)
        {
            return;
        }

        var startX = Math.Max(rect.X, label.Bounds.X - pad);
        var endX = Math.Min(rect.X + rect.Width, label.Bounds.X + label.Bounds.Width + pad);
        var style = CellStyle.None | TextStyle.Bold;

        for (var x = startX; x < endX; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), style);
        }
    }

    private static void RenderVerticalLabelGap(CellBuffer buffer, Rectangle rect, Visual? label, int pad, int y)
    {
        if (label is null || label.Bounds.Width <= 0)
        {
            return;
        }

        var startX = Math.Max(rect.X, label.Bounds.X - pad);
        var endX = Math.Min(rect.X + rect.Width, label.Bounds.X + label.Bounds.Width + pad);
        var style = CellStyle.None | TextStyle.Bold;

        for (var x = startX; x < endX; x++)
        {
            buffer.SetCell(x, y, new Rune(' '), style);
        }
    }

    private static void ArrangeLabelLeft(Visual? label, int x0, int y, int totalWidth, int pad)
    {
        if (label is null)
        {
            return;
        }

        if (totalWidth < pad * 2)
        {
            label.Arrange(new Rectangle(x0, y, 0, 1));
            return;
        }

        label.Arrange(new Rectangle(x0 + pad, y, Math.Max(0, totalWidth - (pad * 2)), 1));
    }

    private static void ArrangeLabelRight(Visual? label, int x0, int y, int width, int totalWidth, int pad)
    {
        if (label is null)
        {
            return;
        }

        if (totalWidth < pad * 2)
        {
            label.Arrange(new Rectangle(x0 + width, y, 0, 1));
            return;
        }

        var startX = x0 + Math.Max(0, width - totalWidth);
        label.Arrange(new Rectangle(startX + pad, y, Math.Max(0, totalWidth - (pad * 2)), 1));
    }

    private static int GetTotalWidth(Visual? label, int availableWidth, int pad)
    {
        if (label is null)
        {
            return 0;
        }

        var total = label.DesiredSize.Width + (pad * 2);
        return Math.Min(availableWidth, total);
    }

    private static int GetLabelWidth(Visual? label)
        => label is null ? 0 : label.DesiredSize.Width;
}
