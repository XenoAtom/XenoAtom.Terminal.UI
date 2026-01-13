// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Rule : Visual
{
    public Rule()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

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

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<RuleStyle>();
        var pad = Math.Max(0, style.LabelPadding);

        var start = StartLabel;
        var center = CenterLabel;
        var end = EndLabel;

        var labelConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        start?.Measure(labelConstraints);
        center?.Measure(labelConstraints);
        end?.Measure(labelConstraints);

        var startTotal = GetTotalWidth(start, LayoutConstants.Infinite, pad);
        var endTotal = GetTotalWidth(end, LayoutConstants.Infinite, pad);
        var centerTotal = GetTotalWidth(center, LayoutConstants.Infinite, pad);

        var required = Math.Max(1, startTotal + endTotal + centerTotal);
        var min = new Size(LayoutConstants.ClampFinite(required), 1);
        var natural = min;
        var max = new Size(LayoutConstants.Infinite, 1);
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        if (finalRect.Width <= 0 || finalRect.Height <= 0)
        {
            return;
        }

        var style = Get<RuleStyle>();
        var pad = Math.Max(0, style.LabelPadding);

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
