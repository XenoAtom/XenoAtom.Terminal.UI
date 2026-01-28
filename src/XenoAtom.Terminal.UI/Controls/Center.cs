// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Centers its content within the available bounds.
/// </summary>
public sealed partial class Center : ContentVisual
{
    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var content = Content;
        if (content is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        return content.Measure(constraints);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var content = Content;
        if (content is null)
        {
            return;
        }

        var w = Math.Min(finalRect.Width, content.DesiredSize.Width);
        var h = Math.Min(finalRect.Height, content.DesiredSize.Height);
        var x = finalRect.X + Math.Max(0, (finalRect.Width - w) / 2);
        var y = finalRect.Y + Math.Max(0, (finalRect.Height - h) / 2);

        content.Arrange(new Rectangle(x, y, w, h));
    }
}
