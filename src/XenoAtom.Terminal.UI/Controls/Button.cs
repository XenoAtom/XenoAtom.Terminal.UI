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
    private bool _isPressed;

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
    public partial string? Text { get; set; }

    [Bindable]
    public partial ControlTone Tone { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text ?? string.Empty;
        var innerWidth = TerminalTextUtility.GetWidth(text.AsSpan());
        var style = GetEnvironmentValue(ButtonStyle.Key);
        var padding = style.Padding;

        var width = Math.Min(availableSize.Width, innerWidth + padding.Horizontal + 2);
        var height = Math.Min(availableSize.Height, Math.Max(3, 1 + padding.Vertical + 2));
        return new Size(width, height);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var buttonStyle = GetEnvironmentValue(ButtonStyle.Key);
        var style = buttonStyle.Resolve(theme, IsEnabled, isFocused, hovered: IsHovered, pressed: _isPressed, Tone);

        var rect = Bounds;
        var text = Text ?? string.Empty;

        var glyphs = theme.Lines;

        // Background fill.
        for (var y = rect.Y; y < rect.Y + rect.Height; y++)
        {
            for (var x = rect.X; x < rect.X + rect.Width; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }

        var border = theme.BorderStyle(isFocused);

        if (rect.Width >= 2 && rect.Height >= 2)
        {
            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            buffer.SetCell(left, top, new Rune(glyphs.TopLeft), border);
            buffer.SetCell(right, top, new Rune(glyphs.TopRight), border);
            buffer.SetCell(left, bottom, new Rune(glyphs.BottomLeft), border);
            buffer.SetCell(right, bottom, new Rune(glyphs.BottomRight), border);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, new Rune(glyphs.Horizontal), border);
                buffer.SetCell(x, bottom, new Rune(glyphs.Horizontal), border);
            }
        }

        var padding = buttonStyle.Padding;
        var contentX = rect.X + 1 + padding.Left;
        var contentWidth = Math.Max(0, rect.Width - 2 - padding.Horizontal);
        var contentY = rect.Y + (rect.Height / 2);
        if (contentWidth > 0 && rect.Height > 0)
        {
            var span = text.AsSpan();
            if (TerminalTextUtility.TryGetIndexAtCell(span, contentWidth, out var endIndex))
            {
                span = span[..endIndex];
            }

            var textCells = TerminalTextUtility.GetWidth(span);
            var textX = contentX + Math.Max(0, (contentWidth - textCells) / 2);
            buffer.WriteText(textX, contentY, span, style | TextStyle.Bold);
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

        _isPressed = true;
        App?.RequestRender();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_isPressed)
        {
            _isPressed = false;
            App?.RequestRender();
            if (e.LocalX >= 0 && e.LocalX < Bounds.Width && e.LocalY >= 0 && e.LocalY < Bounds.Height)
            {
                RaiseEvent(Button.ClickEvent, new ClickEventArgs());
            }
            e.Handled = true;
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnClick(ClickEventArgs e) { }
}
