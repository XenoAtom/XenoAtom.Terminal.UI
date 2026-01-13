// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class DockLayout : Visual
{
    public DockLayout()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    [Bindable]
    public partial Visual? Top { get; set; }

    [Bindable]
    public partial Visual? Bottom { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var top = Top;
        var bottom = Bottom;
        var content = Content;

        var maxW = constraints.MaxWidth;
        var remainingMaxH = constraints.MaxHeight;

        var topHints = top is null ? SizeHints.Fixed(Size.Zero) : top.Measure(new LayoutConstraints(0, maxW, 0, remainingMaxH));
        if (remainingMaxH != LayoutConstants.Infinite)
        {
            remainingMaxH = Math.Max(0, remainingMaxH - topHints.Natural.Height);
        }

        var bottomHints = bottom is null ? SizeHints.Fixed(Size.Zero) : bottom.Measure(new LayoutConstraints(0, maxW, 0, remainingMaxH));
        if (remainingMaxH != LayoutConstants.Infinite)
        {
            remainingMaxH = Math.Max(0, remainingMaxH - bottomHints.Natural.Height);
        }

        var contentHints = content is null ? SizeHints.Fixed(Size.Zero) : content.Measure(new LayoutConstraints(0, maxW, 0, remainingMaxH));

        var minW = Math.Max(topHints.Min.Width, Math.Max(contentHints.Min.Width, bottomHints.Min.Width));
        var natW = Math.Max(topHints.Natural.Width, Math.Max(contentHints.Natural.Width, bottomHints.Natural.Width));

        var maxWInf = LayoutConstants.IsInfinite(topHints.Max.Width) || LayoutConstants.IsInfinite(contentHints.Max.Width) || LayoutConstants.IsInfinite(bottomHints.Max.Width);
        var maxWidth = maxWInf
            ? LayoutConstants.Infinite
            : Math.Max(topHints.Max.Width, Math.Max(contentHints.Max.Width, bottomHints.Max.Width));

        var minH = topHints.Min.Height + contentHints.Min.Height + bottomHints.Min.Height;
        var natH = topHints.Natural.Height + contentHints.Natural.Height + bottomHints.Natural.Height;

        int maxHeight;
        if (LayoutConstants.IsInfinite(topHints.Max.Height) || LayoutConstants.IsInfinite(contentHints.Max.Height) || LayoutConstants.IsInfinite(bottomHints.Max.Height))
        {
            maxHeight = LayoutConstants.Infinite;
        }
        else
        {
            maxHeight = LayoutConstants.ClampOrInfinite(topHints.Max.Height + contentHints.Max.Height + bottomHints.Max.Height);
        }

        return SizeHints.Flex(
            new Size(LayoutConstants.ClampFinite(minW), LayoutConstants.ClampFinite(minH)),
            new Size(LayoutConstants.ClampFinite(natW), LayoutConstants.ClampFinite(natH)),
            new Size(maxWidth, maxHeight),
            growX: Math.Max(topHints.FlexGrowX, Math.Max(contentHints.FlexGrowX, bottomHints.FlexGrowX)),
            growY: contentHints.FlexGrowY,
            shrinkX: Math.Max(topHints.FlexShrinkX, Math.Max(contentHints.FlexShrinkX, bottomHints.FlexShrinkX)),
            shrinkY: contentHints.FlexShrinkY).Normalize();
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var y = finalRect.Y;
        var remainingHeight = finalRect.Height;

        var top = Top;
        if (top is not null)
        {
            var h = Math.Min(remainingHeight, top.DesiredSize.Height);
            top.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, h));
            y += h;
            remainingHeight -= h;
        }

        var bottomHeight = 0;
        var bottom = Bottom;
        if (bottom is not null)
        {
            bottomHeight = Math.Min(remainingHeight, bottom.DesiredSize.Height);
            bottom.Arrange(new Rectangle(finalRect.X, finalRect.Y + finalRect.Height - bottomHeight, finalRect.Width, bottomHeight));
            remainingHeight -= bottomHeight;
        }

        var content = Content;
        if (content is not null)
        {
            content.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, Math.Max(0, remainingHeight)));
        }
    }

    protected override int ChildrenCount
    {
        get
        {
            var count = 0;
            if (_top is not null) count++;
            if (_content is not null) count++;
            if (_bottom is not null) count++;
            return count;
        }
    }

    protected override Visual GetChild(int index)
    {
        if ((uint)index >= (uint)ChildrenCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var i = index;
        if (_top is not null)
        {
            if (i == 0) return _top;
            i--;
        }

        if (_content is not null)
        {
            if (i == 0) return _content;
            i--;
        }

        if (_bottom is not null)
        {
            return _bottom;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
