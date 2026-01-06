// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class HStack : Panel
{
    [Bindable]
    public partial int Spacing { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var width = 0;
        var height = 0;
        var spacing = Math.Max(0, Spacing);

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var remainingWidth = Math.Max(0, availableSize.Width - width);
            child.Measure(new CellSize(remainingWidth, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            if (i + 1 < Children.Count)
            {
                width += spacing;
            }
        }

        return new CellSize(Math.Min(availableSize.Width, width), height);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;

        var x = finalRect.X;
        var spacing = Math.Max(0, Spacing);

        foreach (var child in Children)
        {
            var w = child.DesiredSize.Width;
            child.Arrange(new CellRect(x, finalRect.Y, w, finalRect.Height));
            x += w + spacing;
        }
    }
}

