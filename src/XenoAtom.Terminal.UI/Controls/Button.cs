// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public partial class Button : Visual
{
    private bool _pressedInside;
    public Button()
    {
        Focusable = true;
        Tone = ControlTone.Default;
    }

    public Button(string text) : this()
    {
        Text = text;
    }

    [Bindable]
    public partial Visual? Text { get; set; }

    [Bindable]
    public partial ControlTone Tone { get; set; }

    [Bindable]
    public partial bool IsPressed { get; set; }

    protected override int ChildrenCount => _text is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _text is not null ? _text : throw new ArgumentOutOfRangeException(nameof(index));

    protected override Size MeasureOverride(Size availableSize)
    {
        var textVisual = Text;
        var innerWidth = 0;
        if (textVisual is not null)
        {
            textVisual.Measure(new Size(int.MaxValue / 4, 1));
            innerWidth = textVisual.DesiredSize.Width;
        }
        var style = Get<ButtonStyle>();
        var padding = style.Padding;

        var borderPad = style.ShowBorder ? 1 : 0;
        var width = Math.Min(availableSize.Width, innerWidth + padding.Horizontal + (borderPad * 2));
        var height = Math.Min(availableSize.Height, 1 + padding.Vertical + (borderPad * 2));
        if (borderPad != 0)
        {
            height = Math.Min(availableSize.Height, Math.Max(3, height));
        }
        return new Size(width, height);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var textVisual = Text;
        if (textVisual is null)
        {
            return;
        }

        var buttonStyle = Get<ButtonStyle>();
        var borderPad = buttonStyle.ShowBorder ? 1 : 0;

        var padding = buttonStyle.Padding;
        var contentX = finalRect.X + borderPad + padding.Left;
        var contentWidth = Math.Max(0, finalRect.Width - (borderPad * 2) - padding.Horizontal);
        var contentHeight = Math.Max(1, finalRect.Height - (borderPad * 2) - padding.Vertical);
        var contentY = finalRect.Y + borderPad + padding.Top + (contentHeight / 2);

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        var desiredWidth = Math.Min(contentWidth, textVisual.DesiredSize.Width);
        var x = contentX + Math.Max(0, (contentWidth - desiredWidth) / 2);
        textVisual.Arrange(new Rectangle(x, contentY, desiredWidth, 1));
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
        var contentHeight = Math.Max(1, rect.Height - (borderPad * 2) - padding.Vertical);
        var contentY = rect.Y + borderPad + padding.Top + (contentHeight / 2);

        var textVisual = Text;
        if (textVisual is not null && contentWidth > 0 && contentHeight > 0)
        {
            for (var x = contentX; x < contentX + contentWidth; x++)
            {
                buffer.SetCell(x, contentY, new Rune(' '), style | TextStyle.Bold);
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
