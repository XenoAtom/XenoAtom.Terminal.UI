// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Group : Visual
{
    [Bindable]
    public partial Thickness Padding { get; set; }

    [Bindable]
    public partial Visual? TopLeftText { get; set; }

    [Bindable]
    public partial Visual? TopRightText { get; set; }

    [Bindable]
    public partial Visual? BottomLeftText { get; set; }

    [Bindable]
    public partial Visual? BottomRightText { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    protected override int ChildrenCount
        => (_topLeftText is null ? 0 : 1)
            + (_topRightText is null ? 0 : 1)
            + (_bottomLeftText is null ? 0 : 1)
            + (_bottomRightText is null ? 0 : 1)
            + (_content is null ? 0 : 1);

    protected override Visual GetChild(int index)
    {
        if (_topLeftText is not null)
        {
            if (index == 0) return _topLeftText;
            index--;
        }

        if (_topRightText is not null)
        {
            if (index == 0) return _topRightText;
            index--;
        }

        if (_bottomLeftText is not null)
        {
            if (index == 0) return _bottomLeftText;
            index--;
        }

        if (_bottomRightText is not null)
        {
            if (index == 0) return _bottomRightText;
            index--;
        }

        if (_content is not null)
        {
            if (index == 0) return _content;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var innerWidth = Math.Max(0, availableSize.Width - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableSize.Height - 2 - padding.Vertical);

        var content = Content;
        content?.Measure(new Size(innerWidth, innerHeight));

        var desiredWidth = 2 + padding.Horizontal + (content?.DesiredSize.Width ?? 0);
        var desiredHeight = 2 + padding.Vertical + (content?.DesiredSize.Height ?? 0);

        var topLeft = TopLeftText;
        var topRight = TopRightText;
        var bottomLeft = BottomLeftText;
        var bottomRight = BottomRightText;

        if (topLeft is not null) topLeft.Measure(new Size(int.MaxValue / 4, 1));
        if (topRight is not null) topRight.Measure(new Size(int.MaxValue / 4, 1));
        if (bottomLeft is not null) bottomLeft.Measure(new Size(int.MaxValue / 4, 1));
        if (bottomRight is not null) bottomRight.Measure(new Size(int.MaxValue / 4, 1));

        var topRequired = GetLabelWidth(topLeft) + GetLabelWidth(topRight);
        var bottomRequired = GetLabelWidth(bottomLeft) + GetLabelWidth(bottomRight);
        var labelRequired = Math.Max(topRequired, bottomRequired);
        if (labelRequired > 0)
        {
            desiredWidth = Math.Max(desiredWidth, 2 + labelRequired);
        }

        return new Size(Math.Min(availableSize.Width, desiredWidth), Math.Min(availableSize.Height, desiredHeight));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        ArrangeLabels(finalRect);

        var content = Content;
        if (content is null)
        {
            return;
        }

        var padding = Padding;
        var inner = new Rectangle(
            finalRect.X + 1 + padding.Left,
            finalRect.Y + 1 + padding.Top,
            Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
            Math.Max(0, finalRect.Height - 2 - padding.Vertical));

        content.Arrange(inner);
    }

    private void ArrangeLabels(Rectangle finalRect)
    {
        var innerWidth = Math.Max(0, finalRect.Width - 2);
        if (innerWidth <= 0 || finalRect.Height <= 0)
        {
            return;
        }

        var innerLeft = finalRect.X + 1;
        var topY = finalRect.Y;
        var bottomY = finalRect.Y + finalRect.Height - 1;

        ArrangeLabelLeft(TopLeftText, innerLeft, topY, innerWidth);
        ArrangeLabelRight(TopRightText, innerLeft, topY, innerWidth);
        ArrangeLabelLeft(BottomLeftText, innerLeft, bottomY, innerWidth);
        ArrangeLabelRight(BottomRightText, innerLeft, bottomY, innerWidth);
    }

    private static void ArrangeLabelLeft(Visual? label, int innerLeft, int y, int innerWidth)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth < 2)
        {
            label.Arrange(new Rectangle(innerLeft, y, 0, 1));
            return;
        }

        label.Arrange(new Rectangle(innerLeft + 1, y, Math.Max(0, totalWidth - 2), 1));
    }

    private static void ArrangeLabelRight(Visual? label, int innerLeft, int y, int innerWidth)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth < 2)
        {
            label.Arrange(new Rectangle(innerLeft, y, 0, 1));
            return;
        }

        var startX = innerLeft + Math.Max(0, innerWidth - totalWidth);
        label.Arrange(new Rectangle(startX + 1, y, Math.Max(0, totalWidth - 2), 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var focused = false;
        for (var v = App?.FocusedElement; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, this))
            {
                focused = true;
                break;
            }
        }

        var theme = GetTheme();
        var glyphs = theme.Lines;
        var borderStyle = theme.BorderStyle(focused);

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        if (rect.Width >= 2 && rect.Height >= 2)
        {
            buffer.SetCell(left, top, glyphs.TopLeft, borderStyle);
            buffer.SetCell(right, top, glyphs.TopRight, borderStyle);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, borderStyle);
            buffer.SetCell(right, bottom, glyphs.BottomRight, borderStyle);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
                buffer.SetCell(x, bottom, glyphs.Horizontal, borderStyle);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, borderStyle);
                buffer.SetCell(right, y, glyphs.Vertical, borderStyle);
            }
        }

        var labelStyle = CellStyle.None | TextStyle.Bold;
        var innerWidth = Math.Max(0, rect.Width - 2);
        if (innerWidth > 0)
        {
            var innerLeft = rect.X + 1;
            RenderLabelLeft(buffer, TopLeftText, innerLeft, top, innerWidth, labelStyle);
            RenderLabelRight(buffer, TopRightText, innerLeft, top, innerWidth, labelStyle);
            RenderLabelLeft(buffer, BottomLeftText, innerLeft, bottom, innerWidth, labelStyle);
            RenderLabelRight(buffer, BottomRightText, innerLeft, bottom, innerWidth, labelStyle);
        }
    }

    private static void RenderLabelLeft(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, CellStyle style)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth <= 0)
        {
            return;
        }

        for (var i = 0; i < totalWidth; i++)
        {
            buffer.SetCell(innerLeft + i, y, new Rune(' '), style);
        }
    }

    private static void RenderLabelRight(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, CellStyle style)
    {
        if (label is null)
        {
            return;
        }

        var totalWidth = Math.Min(innerWidth, label.DesiredSize.Width + 2);
        if (totalWidth <= 0)
        {
            return;
        }

        var startX = innerLeft + Math.Max(0, innerWidth - totalWidth);
        for (var i = 0; i < totalWidth; i++)
        {
            buffer.SetCell(startX + i, y, new Rune(' '), style);
        }
    }

    private static int GetLabelWidth(Visual? label)
        => label is null ? 0 : label.DesiredSize.Width + 2;
}
