// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class SplitterBase : Visual
{
    private Rectangle _barRect;
    private bool _dragging;
    private bool _barHovered;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartFirstSize;

    protected SplitterBase()
    {
        Focusable = true;
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.VerticalAlignment(VerticalAlignment.Stretch);
        _ratio = 0.5;
        _barSize = 1;
    }

    protected abstract Orientation SplitOrientation { get; }

    [Bindable]
    public partial Visual? First { get; set; }

    [Bindable]
    public partial Visual? Second { get; set; }

    [Bindable]
    public partial double Ratio { get; set; }

    [Bindable]
    public partial int BarSize { get; set; }

    [Bindable]
    public partial int MinFirst { get; set; }

    [Bindable]
    public partial int MinSecond { get; set; }

    partial void OnRatioChanging(ref double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 0.5;
        }

        value = Math.Clamp(value, 0.0, 1.0);
    }

    partial void OnBarSizeChanging(ref int value)
    {
        if (value < 1)
        {
            value = 1;
        }
    }

    protected override int ChildrenCount => (_first is null ? 0 : 1) + (_second is null ? 0 : 1);

    protected override Visual GetChild(int index)
    {
        if (_first is not null)
        {
            if (index == 0) return _first;
            index--;
        }

        if (_second is not null)
        {
            if (index == 0) return _second;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var first = First;
        var second = Second;
        if (first is null && second is null)
        {
            return default;
        }

        var bar = Math.Max(1, BarSize);
        if (SplitOrientation == Orientation.Horizontal)
        {
            var w = Math.Max(0, availableSize.Width - bar);
            var w1 = w / 2;
            var w2 = w - w1;
            first?.Measure(new Size(w1, availableSize.Height));
            second?.Measure(new Size(w2, availableSize.Height));
            var desiredW = (first?.DesiredSize.Width ?? 0) + (second?.DesiredSize.Width ?? 0) + bar;
            var desiredH = Math.Max(first?.DesiredSize.Height ?? 0, second?.DesiredSize.Height ?? 0);
            return new Size(Math.Min(availableSize.Width, desiredW), Math.Min(availableSize.Height, desiredH));
        }
        else
        {
            var h = Math.Max(0, availableSize.Height - bar);
            var h1 = h / 2;
            var h2 = h - h1;
            first?.Measure(new Size(availableSize.Width, h1));
            second?.Measure(new Size(availableSize.Width, h2));
            var desiredW = Math.Max(first?.DesiredSize.Width ?? 0, second?.DesiredSize.Width ?? 0);
            var desiredH = (first?.DesiredSize.Height ?? 0) + (second?.DesiredSize.Height ?? 0) + bar;
            return new Size(Math.Min(availableSize.Width, desiredW), Math.Min(availableSize.Height, desiredH));
        }
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var first = First;
        var second = Second;
        if (finalRect.Width <= 0 || finalRect.Height <= 0 || (first is null && second is null))
        {
            _barRect = default;
            return;
        }

        var bar = Math.Max(1, BarSize);

        if (SplitOrientation == Orientation.Horizontal)
        {
            var available = Math.Max(0, finalRect.Width - bar);
            var minFirst = Math.Clamp(MinFirst, 0, available);
            var minSecond = Math.Clamp(MinSecond, 0, available);

            var firstSize = (int)Math.Round(available * Ratio);
            firstSize = Math.Clamp(firstSize, minFirst, Math.Max(minFirst, available - minSecond));
            var secondSize = Math.Max(0, available - firstSize);

            var x = finalRect.X;
            first?.Arrange(new Rectangle(x, finalRect.Y, firstSize, finalRect.Height));
            x += firstSize;

            _barRect = new Rectangle(x, finalRect.Y, bar, finalRect.Height);
            x += bar;

            second?.Arrange(new Rectangle(x, finalRect.Y, secondSize, finalRect.Height));

            var denom = Math.Max(1, available);
            _ratio = Math.Clamp(firstSize / (double)denom, 0.0, 1.0);
            return;
        }

        {
            var available = Math.Max(0, finalRect.Height - bar);
            var minFirst = Math.Clamp(MinFirst, 0, available);
            var minSecond = Math.Clamp(MinSecond, 0, available);

            var firstSize = (int)Math.Round(available * Ratio);
            firstSize = Math.Clamp(firstSize, minFirst, Math.Max(minFirst, available - minSecond));
            var secondSize = Math.Max(0, available - firstSize);

            var y = finalRect.Y;
            first?.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, firstSize));
            y += firstSize;

            _barRect = new Rectangle(finalRect.X, y, finalRect.Width, bar);
            y += bar;

            second?.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, secondSize));

            var denom = Math.Max(1, available);
            _ratio = Math.Clamp(firstSize / (double)denom, 0.0, 1.0);
        }
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        if (_barRect.Width <= 0 || _barRect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<SplitterStyle>();
        var focused = ReferenceEquals(App?.FocusedElement, this);

        var barStyle = style.Resolve(theme, IsEnabled, focused, _barHovered, _dragging);
        var glyph = SplitOrientation == Orientation.Horizontal ? style.VerticalGlyph : style.HorizontalGlyph;

        for (var y = _barRect.Y; y < _barRect.Y + _barRect.Height; y++)
        {
            for (var x = _barRect.X; x < _barRect.X + _barRect.Width; x++)
            {
                buffer.SetCell(x, y, glyph, barStyle);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var localX = e.UiX - Bounds.X;
        var localY = e.UiY - Bounds.Y;
        var isOverBar = _barRect.Contains(Bounds.X + localX, Bounds.Y + localY);

        if (!_dragging && _barHovered != isOverBar)
        {
            _barHovered = isOverBar;
            Invalidate();
        }

        if (!_dragging)
        {
            return;
        }

        UpdateFromDrag(e.UiX, e.UiY);
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left || !IsEnabled)
        {
            return;
        }

        if (!_barRect.Contains(e.UiX, e.UiY))
        {
            return;
        }

        _dragging = true;
        _dragStartUiX = e.UiX;
        _dragStartUiY = e.UiY;
        _dragStartFirstSize = GetCurrentFirstSize();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_dragging)
        {
            _dragging = false;
            Invalidate();
            e.Handled = true;
        }
    }

    private int GetCurrentFirstSize()
    {
        if (SplitOrientation == Orientation.Horizontal)
        {
            return Math.Max(0, _barRect.X - Bounds.X);
        }

        return Math.Max(0, _barRect.Y - Bounds.Y);
    }

    private void UpdateFromDrag(int uiX, int uiY)
    {
        var bar = Math.Max(1, BarSize);
        if (SplitOrientation == Orientation.Horizontal)
        {
            var delta = uiX - _dragStartUiX;
            var available = Math.Max(0, Bounds.Width - bar);
            var newSize = _dragStartFirstSize + delta;
            var minFirst = Math.Clamp(MinFirst, 0, available);
            var minSecond = Math.Clamp(MinSecond, 0, available);
            newSize = Math.Clamp(newSize, minFirst, Math.Max(minFirst, available - minSecond));
            Ratio = available <= 0 ? 0.5 : newSize / (double)available;
            return;
        }

        {
            var delta = uiY - _dragStartUiY;
            var available = Math.Max(0, Bounds.Height - bar);
            var newSize = _dragStartFirstSize + delta;
            var minFirst = Math.Clamp(MinFirst, 0, available);
            var minSecond = Math.Clamp(MinSecond, 0, available);
            newSize = Math.Clamp(newSize, minFirst, Math.Max(minFirst, available - minSecond));
            Ratio = available <= 0 ? 0.5 : newSize / (double)available;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var bar = Math.Max(1, BarSize);
        var smallStep = 1;
        if ((e.RawEvent.Modifiers & TerminalModifiers.Shift) != 0)
        {
            smallStep = 5;
        }
        else if ((e.RawEvent.Modifiers & TerminalModifiers.Ctrl) != 0)
        {
            smallStep = 10;
        }

        if (SplitOrientation == Orientation.Horizontal)
        {
            var available = Math.Max(1, Bounds.Width - bar);
            if (e.Key == TerminalKey.Left)
            {
                Ratio -= smallStep / (double)available;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.Right)
            {
                Ratio += smallStep / (double)available;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.Home)
            {
                Ratio = 0;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.End)
            {
                Ratio = 1;
                e.Handled = true;
            }
            return;
        }

        {
            var available = Math.Max(1, Bounds.Height - bar);
            if (e.Key == TerminalKey.Up)
            {
                Ratio -= smallStep / (double)available;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.Down)
            {
                Ratio += smallStep / (double)available;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.Home)
            {
                Ratio = 0;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.End)
            {
                Ratio = 1;
                e.Handled = true;
            }
        }
    }
}
