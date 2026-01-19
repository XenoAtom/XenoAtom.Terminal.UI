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
/// Represents a bordered group with optional corner labels and content.
/// </summary>
public sealed partial class Group : Visual
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class.
    /// </summary>
    public Group()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Group"/> class with top-left label.
    /// </summary>
    /// <param name="topLeftText">The top-left label visual.</param>
    public Group(Visual topLeftText)
    {
        TopLeftText = topLeftText;
    }
    
    /// <summary>
    /// Gets or sets the content padding inside the group border.
    /// </summary>
    [Bindable]
    public partial Thickness Padding { get; set; }

    /// <summary>
    /// Gets or sets the top-left label visual.
    /// </summary>
    [Bindable]
    public partial Visual? TopLeftText { get; set; }

    /// <summary>
    /// Gets or sets the top-right label visual.
    /// </summary>
    [Bindable]
    public partial Visual? TopRightText { get; set; }

    /// <summary>
    /// Gets or sets the bottom-left label visual.
    /// </summary>
    [Bindable]
    public partial Visual? BottomLeftText { get; set; }

    /// <summary>
    /// Gets or sets the bottom-right label visual.
    /// </summary>
    [Bindable]
    public partial Visual? BottomRightText { get; set; }

    /// <summary>
    /// Gets or sets the content visual.
    /// </summary>
    [Bindable]
    public partial Visual? Content { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount
        => (_topLeftText is null ? 0 : 1)
            + (_topRightText is null ? 0 : 1)
            + (_bottomLeftText is null ? 0 : 1)
            + (_bottomRightText is null ? 0 : 1)
            + (_content is null ? 0 : 1);

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = Padding;
        var padLeft = Math.Max(0, padding.Left);
        var padRight = Math.Max(0, padding.Right);
        var padTop = Math.Max(0, padding.Top);
        var padBottom = Math.Max(0, padding.Bottom);
        var padH = LayoutConstants.ClampFinite(padLeft + padRight);
        var padV = LayoutConstants.ClampFinite(padTop + padBottom);

        var innerMaxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - 2 - padH);
        var innerMaxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - 2 - padV);

        var innerConstraints = new LayoutConstraints(0, innerMaxW, 0, innerMaxH);

        var content = Content;
        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(innerConstraints);

        var addW = LayoutConstants.ClampFinite(2 + padH);
        var addH = LayoutConstants.ClampFinite(2 + padV);

        var minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
        var minH = LayoutConstants.ClampFinite(contentHints.Min.Height + addH);
        var natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
        var natH = LayoutConstants.ClampFinite(contentHints.Natural.Height + addH);

        var topLeft = TopLeftText;
        var topRight = TopRightText;
        var bottomLeft = BottomLeftText;
        var bottomRight = BottomRightText;

        var labelConstraints = new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1);
        if (topLeft is not null) topLeft.Measure(labelConstraints);
        if (topRight is not null) topRight.Measure(labelConstraints);
        if (bottomLeft is not null) bottomLeft.Measure(labelConstraints);
        if (bottomRight is not null) bottomRight.Measure(labelConstraints);

        var topRequired = GetLabelWidth(topLeft) + GetLabelWidth(topRight);
        var bottomRequired = GetLabelWidth(bottomLeft) + GetLabelWidth(bottomRight);
        var labelRequired = Math.Max(topRequired, bottomRequired);
        if (labelRequired > 0)
        {
            var minLabelW = LayoutConstants.ClampFinite(2 + labelRequired);
            minW = Math.Max(minW, minLabelW);
            natW = Math.Max(natW, minLabelW);
        }

        int maxW, maxH;
        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxW = LayoutConstants.Infinite;
        }
        else
        {
            maxW = LayoutConstants.ClampOrInfinite(contentHints.Max.Width + addW);
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxH = LayoutConstants.Infinite;
        }
        else
        {
            maxH = LayoutConstants.ClampOrInfinite(contentHints.Max.Height + addH);
        }

        if (labelRequired > 0 && maxW != LayoutConstants.Infinite)
        {
            maxW = Math.Max(maxW, LayoutConstants.ClampOrInfinite(2 + labelRequired));
        }

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxW, maxH),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        ArrangeLabels(finalRect);

        var content = Content;
        if (content is null)
        {
            return;
        }

        var padding = Padding;
        var padLeft = Math.Max(0, padding.Left);
        var padRight = Math.Max(0, padding.Right);
        var padTop = Math.Max(0, padding.Top);
        var padBottom = Math.Max(0, padding.Bottom);
        var padH = padLeft + padRight;
        var padV = padTop + padBottom;
        var inner = new Rectangle(
            finalRect.X + 1 + padLeft,
            finalRect.Y + 1 + padTop,
            Math.Max(0, finalRect.Width - 2 - padH),
            Math.Max(0, finalRect.Height - 2 - padV));

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

    /// <inheritdoc />
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
        var groupStyle = Get<GroupStyle>();
        var glyphs = groupStyle.Glyphs ?? theme.Lines;
        var borderStyle = groupStyle.ResolveBorderStyle(theme, focused);

        var backgroundStyle = groupStyle.ResolveBackgroundStyle();

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        if (backgroundStyle is { } bgStyle)
        {
            for (var y = top; y <= bottom; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), bgStyle);
                }
            }
        }

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

        var labelStyle = groupStyle.ResolveLabelBackgroundStyle();
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

    private static void RenderLabelLeft(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, Style style)
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

    private static void RenderLabelRight(CellBuffer buffer, Visual? label, int innerLeft, int y, int innerWidth, Style style)
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
