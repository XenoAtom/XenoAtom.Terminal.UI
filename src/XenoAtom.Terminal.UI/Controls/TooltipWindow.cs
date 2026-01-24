// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using System.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Internal non-interactive window used to display tooltip content in fullscreen apps.
/// </summary>
/// <remarks>
/// Tooltips are hosted through the window layer, but they must not steal focus or intercept pointer events.
/// </remarks>
internal sealed class TooltipWindow : ContentVisual
{
    private Rectangle _popupRect;

    public TooltipWindow()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
        IsHitTestVisible = false;
        IsEnabled = false;
    }

    public Visual? Anchor { get; set; }

    public Rectangle? AnchorRect { get; set; }

    public PopupPlacement Placement { get; set; } = PopupPlacement.Below;

    public int OffsetX { get; set; }

    public int OffsetY { get; set; } = 1;

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<TooltipStyle>();
        var padding = style.Padding;

        var maxWidth = constraints.MaxWidth;
        if (style.MaxWidth is int cap)
        {
            maxWidth = Math.Min(maxWidth, cap);
        }

        var innerWidth = Math.Max(0, maxWidth - padding.Horizontal - 2);
        var innerHeight = constraints.MaxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, constraints.MaxHeight - padding.Vertical - 2);

        Content?.Measure(new LayoutConstraints(0, innerWidth, 0, innerHeight));

        return SizeHints.Flex(
            min: Size.Zero,
            natural: Size.Zero,
            max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
            growX: 1,
            growY: 1,
            shrinkX: 0,
            shrinkY: 0);
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = GetStyle<TooltipStyle>();
        var padding = style.Padding;

        var content = Content;
        var desired = content?.DesiredSize ?? default;

        var desiredWidth = Math.Clamp(desired.Width + padding.Horizontal + 2, 1, finalRect.Width);
        var desiredHeight = Math.Clamp(desired.Height + padding.Vertical + 2, 1, finalRect.Height);

        var x = finalRect.X + Math.Max(0, (finalRect.Width - desiredWidth) / 2);
        var y = finalRect.Y + Math.Max(0, (finalRect.Height - desiredHeight) / 2);

        var anchorRect = AnchorRect;
        var anchor = Anchor;
        if (anchorRect is null && anchor is not null)
        {
            anchorRect = anchor.Bounds;
        }

        if (anchorRect is Rectangle anchorBounds)
        {
            var belowY = anchorBounds.Y + anchorBounds.Height;
            var aboveY = anchorBounds.Y - desiredHeight;
            var rightX = anchorBounds.X + anchorBounds.Width;
            var leftX = anchorBounds.X - desiredWidth;

            switch (Placement)
            {
                case PopupPlacement.Above:
                    x = anchorBounds.X;
                    y = aboveY;
                    if (y < finalRect.Y && belowY + desiredHeight <= finalRect.Bottom)
                    {
                        y = belowY;
                    }
                    break;

                case PopupPlacement.Right:
                    x = rightX;
                    y = anchorBounds.Y;
                    if (x + desiredWidth > finalRect.Right && leftX >= finalRect.X)
                    {
                        x = leftX;
                    }
                    break;

                case PopupPlacement.Left:
                    x = leftX;
                    y = anchorBounds.Y;
                    if (x < finalRect.X && rightX + desiredWidth <= finalRect.Right)
                    {
                        x = rightX;
                    }
                    break;

                case PopupPlacement.Below:
                default:
                    x = anchorBounds.X;
                    y = belowY;
                    if (y + desiredHeight > finalRect.Bottom && aboveY >= finalRect.Y)
                    {
                        y = aboveY;
                    }
                    break;
            }
        }

        x += OffsetX;
        y += OffsetY;

        x = Math.Clamp(x, finalRect.X, Math.Max(finalRect.X, finalRect.Right - desiredWidth));
        y = Math.Clamp(y, finalRect.Y, Math.Max(finalRect.Y, finalRect.Bottom - desiredHeight));

        _popupRect = new Rectangle(x, y, desiredWidth, desiredHeight);

        if (content is not null)
        {
            var inner = new Rectangle(
                _popupRect.X + 1 + padding.Left,
                _popupRect.Y + 1 + padding.Top,
                Math.Max(0, _popupRect.Width - 2 - padding.Horizontal),
                Math.Max(0, _popupRect.Height - 2 - padding.Vertical));

            content.Arrange(inner);
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = _popupRect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<TooltipStyle>();

        var surface = style.ResolveSurfaceStyle(theme);
        var border = style.ResolveBorderStyle(theme);
        var glyphs = style.Glyphs;

        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        if (rect.Width < 2 || rect.Height < 2)
        {
            return;
        }

        buffer.SetCell(rect.X, rect.Y, glyphs.TopLeft, border);
        buffer.SetCell(rect.Right - 1, rect.Y, glyphs.TopRight, border);
        buffer.SetCell(rect.X, rect.Bottom - 1, glyphs.BottomLeft, border);
        buffer.SetCell(rect.Right - 1, rect.Bottom - 1, glyphs.BottomRight, border);

        for (var x = rect.X + 1; x < rect.Right - 1; x++)
        {
            buffer.SetCell(x, rect.Y, glyphs.Horizontal, border);
            buffer.SetCell(x, rect.Bottom - 1, glyphs.Horizontal, border);
        }

        for (var y = rect.Y + 1; y < rect.Bottom - 1; y++)
        {
            buffer.SetCell(rect.X, y, glyphs.Vertical, border);
            buffer.SetCell(rect.Right - 1, y, glyphs.Vertical, border);
        }
    }
}
