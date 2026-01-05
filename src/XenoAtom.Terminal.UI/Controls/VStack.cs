// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public sealed partial class VStack : Panel
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
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
            if (i + 1 < Children.Count)
            {
                height += spacing;
            }
        }

        return new CellSize(Math.Min(availableSize.Width, width), height);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;

        var y = finalRect.Y;
        var spacing = Math.Max(0, Spacing);

        foreach (var child in Children)
        {
            var h = child.DesiredSize.Height;
            child.Arrange(new CellRect(finalRect.X, y, finalRect.Width, h));
            y += h + spacing;
        }
    }
}

