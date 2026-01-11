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
    [Bindable]
    public partial Visual? Top { get; set; }

    [Bindable]
    public partial Visual? Bottom { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var topHeight = 0;
        var bottomHeight = 0;
        var width = 0;

        var top = Top;
        if (top is not null)
        {
            top.Measure(new Size(availableSize.Width, availableSize.Height));
            topHeight = top.DesiredSize.Height;
            width = Math.Max(width, top.DesiredSize.Width);
        }

        var bottom = Bottom;
        if (bottom is not null)
        {
            bottom.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - topHeight)));
            bottomHeight = bottom.DesiredSize.Height;
            width = Math.Max(width, bottom.DesiredSize.Width);
        }

        var content = Content;
        if (content is not null)
        {
            content.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - topHeight - bottomHeight)));
            width = Math.Max(width, content.DesiredSize.Width);
        }

        var height = Math.Min(availableSize.Height, topHeight + bottomHeight + (content?.DesiredSize.Height ?? 0));
        return SizeHints.Fixed(new Size(Math.Min(availableSize.Width, width), height));
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
