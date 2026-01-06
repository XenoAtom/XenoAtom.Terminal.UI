// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Center : Visuals.Visual
{
    private Visuals.Visual? _child;

    public Visuals.Visual? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value))
            {
                return;
            }

            if (_child is not null)
            {
                throw new InvalidOperationException("Center currently only supports setting Child once.");
            }

            _child = value;
            if (value is not null)
            {
                AttachChild(value);
            }

            App?.RequestRender();
        }
    }

    protected override int ChildrenCount => _child is null ? 0 : 1;

    protected override Visuals.Visual GetChild(int index)
        => index == 0 && _child is not null ? _child : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_child is null)
        {
            return default;
        }

        _child.Measure(availableSize);
        return _child.DesiredSize;
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        if (_child is null)
        {
            return;
        }

        var w = Math.Min(finalRect.Width, _child.DesiredSize.Width);
        var h = Math.Min(finalRect.Height, _child.DesiredSize.Height);
        var x = finalRect.X + Math.Max(0, (finalRect.Width - w) / 2);
        var y = finalRect.Y + Math.Max(0, (finalRect.Height - h) / 2);

        _child.Arrange(new Rectangle(x, y, w, h));
    }
}
