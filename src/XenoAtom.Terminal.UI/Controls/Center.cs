// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Center : Visual
{
    [Bindable]
    public partial Visual? Content { get; set; }

    protected override int ChildrenCount => _content is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = Content;
        if (content is null)
        {
            return default;
        }

        content.Measure(availableSize);
        return content.DesiredSize;
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

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
