// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public partial class Button : ContentVisual
{
    private bool _pressedInside;
    public Button()
    {
        Focusable = true;
        Tone = ControlTone.Default;
    }

    public Button(string text) : this()
    {
        this.Content(text);
    }

    [Bindable]
    public partial ControlTone Tone { get; set; }

    [Bindable]
    public partial bool IsPressed { get; set; }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var style = Get<ButtonStyle>();
        var padding = style.Padding;

        var borderPad = style.ShowBorder ? 1 : 0;

        var innerAvailable = new Size(
            Math.Max(0, availableSize.Width - padding.Horizontal - (borderPad * 2)),
            Math.Max(0, availableSize.Height - padding.Vertical - (borderPad * 2)));

        var content = Content;
        content?.Measure(innerAvailable);

        var width = Math.Min(availableSize.Width, (content?.DesiredSize.Width ?? 0) + padding.Horizontal + (borderPad * 2));
        var height = Math.Min(availableSize.Height, (content?.DesiredSize.Height ?? 0) + padding.Vertical + (borderPad * 2));
        if (borderPad != 0)
        {
            width = Math.Min(availableSize.Width, Math.Max(3, width));
            height = Math.Min(availableSize.Height, Math.Max(3, height));
        }
        return SizeHints.Fixed(new Size(width, height));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var content = Content;
        if (content is null)
        {
            return;
        }

        var buttonStyle = Get<ButtonStyle>();
        var borderPad = buttonStyle.ShowBorder ? 1 : 0;

        var padding = buttonStyle.Padding;
        var contentX = finalRect.X + borderPad + padding.Left;
        var contentY = finalRect.Y + borderPad + padding.Top;
        var contentWidth = Math.Max(0, finalRect.Width - (borderPad * 2) - padding.Horizontal);
        var contentHeight = Math.Max(0, finalRect.Height - (borderPad * 2) - padding.Vertical);

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        var w = Math.Min(contentWidth, content.DesiredSize.Width);
        var h = Math.Min(contentHeight, content.DesiredSize.Height);
        var x = contentX + Math.Max(0, (contentWidth - w) / 2);
        var y = contentY + Math.Max(0, (contentHeight - h) / 2);
        content.Arrange(new Rectangle(x, y, w, h));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var buttonStyle = Get<ButtonStyle>();
        var pressed = IsPressed && _pressedInside;
        var hovered = IsPressed ? _pressedInside : IsHovered;
        var style = buttonStyle.Resolve(theme, IsEnabled, isFocused, hovered: hovered, pressed: pressed, Tone);

        var rect = Bounds;

        // Background fill.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }

        var borderPad = buttonStyle.ShowBorder ? 1 : 0;
        if (borderPad != 0 && rect.Width >= 2 && rect.Height >= 2)
        {
            var glyphs = buttonStyle.BorderGlyphs;
            var border = theme.BorderStyle(isFocused);

            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            buffer.SetCell(left, top, glyphs.TopLeft, border);
            buffer.SetCell(right, top, glyphs.TopRight, border);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, border);
            buffer.SetCell(right, bottom, glyphs.BottomRight, border);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Top, border);
                buffer.SetCell(x, bottom, glyphs.Bottom, border);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Left, border);
                buffer.SetCell(right, y, glyphs.Right, border);
            }
        }

        var padding = buttonStyle.Padding;
        var contentX = rect.X + borderPad + padding.Left;
        var contentWidth = Math.Max(0, rect.Width - (borderPad * 2) - padding.Horizontal);
        var contentY = rect.Y + borderPad + padding.Top;
        var contentHeight = Math.Max(0, rect.Height - (borderPad * 2) - padding.Vertical);

        var content = Content;
        if (content is not null && contentWidth > 0 && contentHeight > 0)
        {
            for (var y = contentY; y < contentY + contentHeight; y++)
            {
                for (var x = contentX; x < contentX + contentWidth; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), style | TextStyle.Bold);
                }
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is TerminalKey.Enter or TerminalKey.Space)
        {
            RaiseEvent(Button.ClickEvent, new ClickEventArgs());
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _pressedInside = Bounds.Contains(e.UiX, e.UiY);
        IsPressed = true;
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!IsPressed)
        {
            return;
        }

        var inside = Bounds.Contains(e.UiX, e.UiY);
        if (_pressedInside != inside)
        {
            _pressedInside = inside;
            Invalidate();
        }
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (IsPressed)
        {
            _pressedInside = Bounds.Contains(e.UiX, e.UiY);
            IsPressed = false;
            if (_pressedInside)
            {
                RaiseEvent(Button.ClickEvent, new ClickEventArgs());
            }
            else
            {
                IsHovered = false;
            }
            e.Handled = true;
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnClick(ClickEventArgs e) { }
}
