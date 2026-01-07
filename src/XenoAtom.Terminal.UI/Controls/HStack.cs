// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class HStack : Panel
{
    public HStack()
    {
    }

    public HStack(params Visual[] children)
    {
        AddRange(children);
    }

    [Bindable]
    public partial int Spacing { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0;
        var height = 0;
        var spacing = Math.Max(0, Spacing);

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var remainingWidth = Math.Max(0, availableSize.Width - width);
            child.Measure(new Size(remainingWidth, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            if (i + 1 < Children.Count)
            {
                width += spacing;
            }
        }

        return new Size(Math.Min(availableSize.Width, width), height);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var x = finalRect.X;
        var spacing = Math.Max(0, Spacing);
        var childCount = Children.Count;
        if (childCount == 0)
        {
            return;
        }

        var totalSpacing = spacing * Math.Max(0, childCount - 1);
        var fixedWidth = 0;
        var stretchCount = 0;
        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                stretchCount++;
            }
            else
            {
                fixedWidth += child.DesiredSize.Width;
            }
        }

        var remaining = Math.Max(0, finalRect.Width - fixedWidth - totalSpacing);
        var stretchWidth = stretchCount > 0 ? remaining / stretchCount : 0;
        var stretchRemainder = stretchCount > 0 ? remaining % stretchCount : 0;
        var stretchIndex = 0;

        foreach (var child in Children)
        {
            var w = child.DesiredSize.Width;
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                w = stretchWidth + (stretchIndex < stretchRemainder ? 1 : 0);
                stretchIndex++;
            }

            child.Arrange(new Rectangle(x, finalRect.Y, w, finalRect.Height));
            x += w + spacing;
        }
    }
}
