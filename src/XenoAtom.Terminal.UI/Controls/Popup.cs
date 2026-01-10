// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public enum PopupPlacement
{
    Below = 0,
    Above = 1,
    Right = 2,
    Left = 3,
}

public sealed partial class Popup : ContentVisual, IModalVisual
{
    private Rectangle _layoutSlot;
    private Rectangle _popupRect;
    private bool _isOpen;

    public Popup()
    {
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
        this.MatchAnchorWidth(true);
        this.Placement(PopupPlacement.Below);
    }

    /// <summary>
    /// Gets or sets the anchor visual used for positioning the popup.
    /// </summary>
    public Visual? Anchor { get; set; }

    public bool IsModal => true;

    [Bindable]
    public partial bool MatchAnchorWidth { get; set; }

    [Bindable]
    public partial PopupPlacement Placement { get; set; }

    public void Show()
    {
        VerifyAccess();

        if (_isOpen)
        {
            return;
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            throw new InvalidOperationException("Popup.Show is only supported while a TerminalApp is running.");
        }

        _isOpen = true;
        app.ShowWindow(this);
    }

    public void Close()
    {
        VerifyAccess();

        if (!_isOpen)
        {
            return;
        }

        var app = App ?? Dispatcher.AttachedApp;
        if (app is null)
        {
            return;
        }

        _isOpen = false;
        app.CloseWindow(this);
        RaiseEvent(ClosedEvent, new PopupClosedEventArgs());
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Fill the available space so the popup can detect outside clicks.
        // The inner content is measured by the base implementation.
        _ = base.MeasureOverride(availableSize);
        return availableSize;
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        _layoutSlot = finalRect;
        Bounds = finalRect;

        var style = Get<PopupStyle>();
        var showBorder = style.ShowBorder;
        var padding = style.Padding;

        var content = Content;
        var contentDesired = content?.DesiredSize ?? default;

        var border = showBorder ? 1 : 0;
        var desiredWidth = Math.Max(1, border * 2 + padding.Horizontal + contentDesired.Width);
        var desiredHeight = Math.Max(1, border * 2 + padding.Vertical + contentDesired.Height);

        var anchor = Anchor;
        var width = desiredWidth;
        if (MatchAnchorWidth && anchor is not null)
        {
            width = Math.Max(width, anchor.Bounds.Width);
        }

        width = Math.Clamp(width, 1, finalRect.Width);
        desiredHeight = Math.Clamp(desiredHeight, 1, finalRect.Height);

        var x = finalRect.X;
        var y = finalRect.Y;

        if (anchor is not null)
        {
            var belowY = anchor.Bounds.Y + anchor.Bounds.Height;
            var aboveY = anchor.Bounds.Y - desiredHeight;
            var rightX = anchor.Bounds.X + anchor.Bounds.Width;
            var leftX = anchor.Bounds.X - width;

            switch (Placement)
            {
                case PopupPlacement.Above:
                    x = anchor.Bounds.X;
                    y = aboveY;
                    if (y < finalRect.Y && belowY + desiredHeight <= finalRect.Bottom)
                    {
                        y = belowY;
                    }
                    break;

                case PopupPlacement.Right:
                    x = rightX;
                    y = anchor.Bounds.Y;
                    if (x + width > finalRect.Right && leftX >= finalRect.X)
                    {
                        x = leftX;
                    }
                    break;

                case PopupPlacement.Left:
                    x = leftX;
                    y = anchor.Bounds.Y;
                    if (x < finalRect.X && rightX + width <= finalRect.Right)
                    {
                        x = rightX;
                    }
                    break;

                case PopupPlacement.Below:
                default:
                    x = anchor.Bounds.X;
                    y = belowY;
                    if (y + desiredHeight > finalRect.Bottom && aboveY >= finalRect.Y)
                    {
                        y = aboveY;
                    }
                    break;
            }
        }
        else
        {
            x = finalRect.X + Math.Max(0, (finalRect.Width - width) / 2);
            y = finalRect.Y + Math.Max(0, (finalRect.Height - desiredHeight) / 2);
        }

        x = Math.Clamp(x, finalRect.X, Math.Max(finalRect.X, finalRect.Right - width));
        y = Math.Clamp(y, finalRect.Y, Math.Max(finalRect.Y, finalRect.Bottom - desiredHeight));

        _popupRect = new Rectangle(x, y, width, desiredHeight);

        if (content is not null)
        {
            var inner = new Rectangle(
                _popupRect.X + border + padding.Left,
                _popupRect.Y + border + padding.Top,
                Math.Max(0, _popupRect.Width - border * 2 - padding.Horizontal),
                Math.Max(0, _popupRect.Height - border * 2 - padding.Vertical));

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
        var style = Get<PopupStyle>();
        var showBorder = style.ShowBorder && rect.Width >= 2 && rect.Height >= 2;
        var surface = style.ResolveSurfaceStyle(theme);
        var border = style.ResolveBorderStyle(theme);

        // Fill popup surface.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), surface);
            }
        }

        if (!showBorder)
        {
            return;
        }

        var glyphs = theme.Lines;

        var left = rect.X;
        var top = rect.Y;
        var right = rect.Right - 1;
        var bottom = rect.Bottom - 1;

        buffer.SetCell(left, top, glyphs.TopLeft, border);
        buffer.SetCell(right, top, glyphs.TopRight, border);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, border);
        buffer.SetCell(right, bottom, glyphs.BottomRight, border);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, border);
            buffer.SetCell(x, bottom, glyphs.Horizontal, border);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, border);
            buffer.SetCell(right, y, glyphs.Vertical, border);
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        // Close on clicks outside the popup content area.
        if (!_popupRect.Contains(e.UiX, e.UiY))
        {
            Close();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == TerminalKey.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    [RoutedEvent(RoutingStrategy.Direct)]
    private void OnClosed(PopupClosedEventArgs e) { }
}
