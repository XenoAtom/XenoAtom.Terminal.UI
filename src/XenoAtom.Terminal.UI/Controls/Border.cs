// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Border : Visual
{
    [Bindable]
    public partial Thickness Padding { get; set; }

    [Bindable]
    public partial Visual? Content { get; set; }

    protected override int ChildrenCount => _content is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _content is not null ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var innerWidth = Math.Max(0, availableSize.Width - 2 - padding.Horizontal);
        var innerHeight = Math.Max(0, availableSize.Height - 2 - padding.Vertical);

        var content = Content;
        if (content is not null)
        {
            content.Measure(new Size(innerWidth, innerHeight));
        }

        var desiredWidth = 2 + padding.Horizontal + (content?.DesiredSize.Width ?? 0);
        var desiredHeight = 2 + padding.Vertical + (content?.DesiredSize.Height ?? 0);

        return new Size(Math.Min(availableSize.Width, desiredWidth), desiredHeight);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var padding = Padding;

        var content = Content;
        if (content is not null)
        {
            var inner = new Rectangle(
                finalRect.X + 1 + padding.Left,
                finalRect.Y + 1 + padding.Top,
                Math.Max(0, finalRect.Width - 2 - padding.Horizontal),
                Math.Max(0, finalRect.Height - 2 - padding.Vertical));

            content.Arrange(inner);
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

        buffer.SetCell(left, top, glyphs.TopLeft, style);
        buffer.SetCell(right, top, glyphs.TopRight, style);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
        buffer.SetCell(right, bottom, glyphs.BottomRight, style);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, style);
            buffer.SetCell(x, bottom, glyphs.Horizontal, style);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, style);
            buffer.SetCell(right, y, glyphs.Vertical, style);
        }
    }
}
