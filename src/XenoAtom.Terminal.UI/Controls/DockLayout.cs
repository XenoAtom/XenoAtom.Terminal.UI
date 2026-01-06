// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.


// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class DockLayout : Visuals.Visual
{
    private Visuals.Visual? _top;
    private Visuals.Visual? _bottom;
    private Visuals.Visual? _content;

    public Visuals.Visual? Top
    {
        get => _top;
        set => SetOnce(ref _top, value);
    }

    public Visuals.Visual? Bottom
    {
        get => _bottom;
        set => SetOnce(ref _bottom, value);
    }

    public Visuals.Visual? Content
    {
        get => _content;
        set => SetOnce(ref _content, value);
    }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var topHeight = 0;
        var bottomHeight = 0;

        if (_top is not null)
        {
            _top.Measure(new CellSize(availableSize.Width, availableSize.Height));
            topHeight = _top.DesiredSize.Height;
        }

        if (_bottom is not null)
        {
            _bottom.Measure(new CellSize(availableSize.Width, Math.Max(0, availableSize.Height - topHeight)));
            bottomHeight = _bottom.DesiredSize.Height;
        }

        if (_content is not null)
        {
            _content.Measure(new CellSize(availableSize.Width, Math.Max(0, availableSize.Height - topHeight - bottomHeight)));
        }

        var height = Math.Min(availableSize.Height, topHeight + bottomHeight + (_content?.DesiredSize.Height ?? 0));
        return new CellSize(availableSize.Width, height);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;

        var y = finalRect.Y;
        var remainingHeight = finalRect.Height;

        if (_top is not null)
        {
            var h = Math.Min(remainingHeight, _top.DesiredSize.Height);
            _top.Arrange(new CellRect(finalRect.X, y, finalRect.Width, h));
            y += h;
            remainingHeight -= h;
        }

        var bottomHeight = 0;
        if (_bottom is not null)
        {
            bottomHeight = Math.Min(remainingHeight, _bottom.DesiredSize.Height);
            _bottom.Arrange(new CellRect(finalRect.X, finalRect.Y + finalRect.Height - bottomHeight, finalRect.Width, bottomHeight));
            remainingHeight -= bottomHeight;
        }

        if (_content is not null)
        {
            _content.Arrange(new CellRect(finalRect.X, y, finalRect.Width, Math.Max(0, remainingHeight)));
        }
    }

    private void SetOnce(ref Visuals.Visual? field, Visuals.Visual? value)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        if (field is not null)
        {
            throw new InvalidOperationException("DockLayout currently only supports setting each slot once.");
        }

        field = value;
        if (value is not null)
        {
            AddChild(value);
        }

        App?.RequestRender();
    }
}

