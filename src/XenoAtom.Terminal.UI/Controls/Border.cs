// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Border : Visuals.Visual
{
    private Visuals.Visual? _child;

    [Bindable]
    public partial Thickness Padding { get; set; }

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
                throw new InvalidOperationException("Border currently only supports setting Child once.");
            }

            _child = value;
            if (value is not null)
            {
                AddChild(value);
            }

            App?.RequestRender();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var innerWidth = Math.Max(0, availableSize.Width - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableSize.Height - 2 - padding.Vertical);

        if (_child is not null)
        {
            _child.Measure(new Size(innerWidth, innerHeight));
        }

        var desiredWidth = 2 + padding.Horizontal + (_child?.DesiredSize.Width ?? 0);
        var desiredHeight = 2 + padding.Vertical + (_child?.DesiredSize.Height ?? 0);

        return new Size(Math.Min(availableSize.Width, desiredWidth), desiredHeight);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var padding = Padding;

        if (_child is not null)
        {
            var inner = new Rectangle(
                finalRect.X + 1 + padding.Left,
                finalRect.Y + 1 + padding.Top,
                Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
                Math.Max(0, finalRect.Height - 2 - padding.Vertical));

            _child.Arrange(inner);
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
        var surface = theme.SurfaceStyle();

        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        // Fill background.
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        buffer.SetCell(left, top, new Rune(glyphs.TopLeft), style);
        buffer.SetCell(right, top, new Rune(glyphs.TopRight), style);
        buffer.SetCell(left, bottom, new Rune(glyphs.BottomLeft), style);
        buffer.SetCell(right, bottom, new Rune(glyphs.BottomRight), style);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, new Rune(glyphs.Horizontal), style);
            buffer.SetCell(x, bottom, new Rune(glyphs.Horizontal), style);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, new Rune(glyphs.Vertical), style);
            buffer.SetCell(right, y, new Rune(glyphs.Vertical), style);
        }
    }
}
