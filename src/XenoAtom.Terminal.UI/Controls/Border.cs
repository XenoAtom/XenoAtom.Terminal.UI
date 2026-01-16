// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Border : ContentVisual
{
    public Border()
    {
    }

    public Border(Visual content)
    {
        Content = content;
    }

    public Border(Func<Visual> contentFactory)
    {
        ArgumentNullException.ThrowIfNull(contentFactory);
        this.Content(contentFactory);
    }

    [Bindable]
    public partial Thickness Padding { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = Padding;
        var padH = LayoutConstants.ClampFinite(Math.Max(0, padding.Horizontal));
        var padV = LayoutConstants.ClampFinite(Math.Max(0, padding.Vertical));

        var maxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - 2 - padH);
        var maxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - 2 - padV);

        var childConstraints = new LayoutConstraints(0, maxW, 0, maxH);

        var content = Content;
        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(childConstraints);

        var addW = LayoutConstants.ClampFinite(2 + padH);
        var addH = LayoutConstants.ClampFinite(2 + padV);

        var minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
        var minH = LayoutConstants.ClampFinite(contentHints.Min.Height + addH);
        var natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
        var natH = LayoutConstants.ClampFinite(contentHints.Natural.Height + addH);

        int maxWidth, maxHeight;
        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxWidth = LayoutConstants.Infinite;
        }
        else
        {
            maxWidth = LayoutConstants.ClampOrInfinite(contentHints.Max.Width + addW);
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxHeight = LayoutConstants.Infinite;
        }
        else
        {
            maxHeight = LayoutConstants.ClampOrInfinite(contentHints.Max.Height + addH);
        }

        return SizeHints.Flex(
            new Size(minW, minH),
            new Size(natW, natH),
            new Size(maxWidth, maxHeight),
            contentHints.FlexGrowX,
            contentHints.FlexGrowY,
            contentHints.FlexShrinkX,
            contentHints.FlexShrinkY).Normalize();
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var padding = Padding;
        var padH = Math.Max(0, padding.Horizontal);
        var padV = Math.Max(0, padding.Vertical);

        var content = Content;
        if (content is not null)
        {
            var inner = new Rectangle(
                finalRect.X + 1 + padding.Left,
                finalRect.Y + 1 + padding.Top,
                Math.Max(0, finalRect.Width - 2 - padH),
                Math.Max(0, finalRect.Height - 2 - padV));

            content.Arrange(inner);
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
        var glyphs = theme.Lines;
        var style = theme.BorderStyle(focused: false);
        var clearStyle = CellStyle.None;

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        // Fill background.
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), clearStyle);
            }
        }

        buffer.SetCell(left, top, glyphs.TopLeft, style);
        buffer.SetCell(right, top, glyphs.TopRight, style);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
        buffer.SetCell(right, bottom, glyphs.BottomRight, style);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, style);
            buffer.SetCell(x, bottom, glyphs.Horizontal, style);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, style);
            buffer.SetCell(right, y, glyphs.Vertical, style);
        }
    }
}
