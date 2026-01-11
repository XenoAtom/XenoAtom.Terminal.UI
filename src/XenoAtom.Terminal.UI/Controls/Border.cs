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
    [Bindable]
    public partial Thickness Padding { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = Padding;

        var maxW = constraints.MaxWidth == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxWidth - 2 - padding.Horizontal);
        var maxH = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - 2 - padding.Vertical);

        var childConstraints = new LayoutConstraints(0, maxW, 0, maxH);

        var content = Content;
        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(childConstraints);

        int addW, addH;
        try
        {
            checked
            {
                addW = 2 + padding.Horizontal;
                addH = 2 + padding.Vertical;
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Border padding/border contribution.", ex);
        }

        int minW, minH, natW, natH, maxWidth, maxHeight;
        try
        {
            checked
            {
                minW = LayoutConstants.ClampFinite(contentHints.Min.Width + addW);
                minH = LayoutConstants.ClampFinite(contentHints.Min.Height + addH);

                natW = LayoutConstants.ClampFinite(contentHints.Natural.Width + addW);
                natH = LayoutConstants.ClampFinite(contentHints.Natural.Height + addH);
            }
        }
        catch (OverflowException ex)
        {
            throw new LayoutException("Overflow while computing Border Min/Natural size.", ex);
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Width))
        {
            maxWidth = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxWidth = LayoutConstants.ClampOrInfinite(checked(contentHints.Max.Width + addW));
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Border Max.Width.", ex);
            }
        }

        if (LayoutConstants.IsInfinite(contentHints.Max.Height))
        {
            maxHeight = LayoutConstants.Infinite;
        }
        else
        {
            try
            {
                maxHeight = LayoutConstants.ClampOrInfinite(checked(contentHints.Max.Height + addH));
            }
            catch (OverflowException ex)
            {
                throw new LayoutException("Overflow while computing Border Max.Height.", ex);
            }
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

        var content = Content;
        if (content is not null)
        {
            var inner = new Rectangle(
                finalRect.X + 1 + padding.Left,
                finalRect.Y + 1 + padding.Top,
                Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
                Math.Max(0, finalRect.Height - 2 - padding.Vertical));

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
