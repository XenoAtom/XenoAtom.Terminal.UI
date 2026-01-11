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

public sealed partial class ScrollBar : Visual
{
    private bool _dragging;
    private int _dragStartUiX;
    private int _dragStartUiY;
    private int _dragStartValue;
    private int _oldValueForEvent;

    public ScrollBar(bool focusable = true)
    {
        Focusable = focusable;
        this.Minimum(0);
        this.Maximum(0);
        this.Value(0);
        this.ViewportSize(0);
        this.SmallChange(1);
        this.LargeChange(0);
    }

    [Bindable]
    public partial Orientation Orientation { get; set; }

    [Bindable]
    public partial int Minimum { get; set; }

    [Bindable]
    public partial int Maximum { get; set; }

    [Bindable]
    public partial int Value { get; set; }

    [Bindable]
    public partial int ViewportSize { get; set; }

    [Bindable]
    public partial int SmallChange { get; set; }

    [Bindable]
    public partial int LargeChange { get; set; }

    partial void OnMinimumChanged(int value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Maximum = Minimum;
        }
        Value = Clamp(Value);
    }

    partial void OnMaximumChanged(int value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Minimum = Maximum;
        }
        Value = Clamp(Value);
    }

    partial void OnViewportSizeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnSmallChangeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnLargeChangeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnValueChanging(ref int value)
    {
        _oldValueForEvent = _value;
        value = Clamp(value);
    }

    partial void OnValueChanged(int value)
    {
        if (_oldValueForEvent != value)
        {
            RaiseEvent(ValueChangedEvent, new ScrollValueChangedEventArgs { OldValue = _oldValueForEvent, NewValue = value });
        }
    }

    private int Clamp(int value)
    {
        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return Math.Clamp(value, min, max);
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var thickness = Math.Max(1, Get<ScrollBarStyle>().Thickness);
        thickness = LayoutConstants.ClampFinite(thickness);

        if (Orientation == Orientation.Vertical)
        {
            // Fixed thickness, flexible length.
            return SizeHints.FlexY(
                min: new Size(thickness, 1),
                natural: new Size(thickness, 1),
                growY: 1,
                shrinkY: 1);
        }

        // Fixed thickness, flexible length.
        return SizeHints.FlexX(
            min: new Size(1, thickness),
            natural: new Size(1, thickness),
            growX: 1,
            shrinkX: 1);
    }

    protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<ScrollBarStyle>();
        var glyphs = theme.ScrollBars;

        var highlighted = IsHovered || _dragging || ReferenceEquals(App?.FocusedElement, this);
        var trackStyle = style.ResolveTrackStyle(theme);
        var thumbStyle = style.ResolveThumbStyle(theme, highlighted);

        if (Orientation == Orientation.Vertical)
        {
            RenderVertical(buffer, rect, glyphs, trackStyle, thumbStyle);
        }
        else
        {
            RenderHorizontal(buffer, rect, glyphs, trackStyle, thumbStyle);
        }
    }

    private void RenderVertical(CellBuffer buffer, Rectangle rect, ScrollBarGlyphs glyphs, CellStyle trackStyle, CellStyle thumbStyle)
    {
        var trackLength = rect.Height;
        if (trackLength <= 0)
        {
            return;
        }

        var (thumbStart, thumbLength) = GetThumbMetrics(trackLength);

        for (var y = 0; y < trackLength; y++)
        {
            var isThumb = y >= thumbStart && y < thumbStart + thumbLength;
            var rune = isThumb ? glyphs.Thumb : glyphs.Track;
            var st = isThumb ? thumbStyle : trackStyle;
            for (var x = 0; x < rect.Width; x++)
            {
                buffer.SetCell(rect.X + x, rect.Y + y, rune, st);
            }
        }
    }

    private void RenderHorizontal(CellBuffer buffer, Rectangle rect, ScrollBarGlyphs glyphs, CellStyle trackStyle, CellStyle thumbStyle)
    {
        var trackLength = rect.Width;
        if (trackLength <= 0)
        {
            return;
        }

        var (thumbStart, thumbLength) = GetThumbMetrics(trackLength);

        for (var x = 0; x < trackLength; x++)
        {
            var isThumb = x >= thumbStart && x < thumbStart + thumbLength;
            var rune = isThumb ? glyphs.Thumb : glyphs.Track;
            var st = isThumb ? thumbStyle : trackStyle;
            for (var y = 0; y < rect.Height; y++)
            {
                buffer.SetCell(rect.X + x, rect.Y + y, rune, st);
            }
        }
    }

    private (int ThumbStart, int ThumbLength) GetThumbMetrics(int trackLength)
    {
        if (trackLength <= 1)
        {
            return (0, 1);
        }

        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        var range = max - min;
        if (range <= 0)
        {
            return (0, trackLength);
        }

        var viewport = Math.Max(0, ViewportSize);
        var contentSize = range + viewport;
        var minThumb = Math.Max(1, Get<ScrollBarStyle>().MinThumbLength);

        var thumbLength = viewport <= 0 || contentSize <= 0
            ? minThumb
            : (int)Math.Round((double)trackLength * viewport / contentSize);

        thumbLength = Math.Clamp(thumbLength, minThumb, trackLength);

        var trackAvail = Math.Max(1, trackLength - thumbLength);
        var offset = Math.Clamp(Value - min, 0, range);
        var thumbStart = (int)Math.Round((double)offset * trackAvail / range);
        thumbStart = Math.Clamp(thumbStart, 0, trackLength - thumbLength);

        return (thumbStart, thumbLength);
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var rect = Bounds;
        var trackLength = Orientation == Orientation.Vertical ? rect.Height : rect.Width;
        if (trackLength <= 0)
        {
            return;
        }

        var (thumbStart, thumbLength) = GetThumbMetrics(trackLength);
        var local = Orientation == Orientation.Vertical ? e.UiY - rect.Y : e.UiX - rect.X;

        if (local >= thumbStart && local < thumbStart + thumbLength)
        {
            _dragging = true;
            _dragStartUiX = e.UiX;
            _dragStartUiY = e.UiY;
            _dragStartValue = Value;
            e.Handled = true;
            return;
        }

        // Page.
        var page = LargeChange;
        if (page <= 0)
        {
            page = Math.Max(1, ViewportSize);
        }

        Value = local < thumbStart ? Value - page : Value + page;
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var rect = Bounds;
        var trackLength = Orientation == Orientation.Vertical ? rect.Height : rect.Width;
        if (trackLength <= 0)
        {
            return;
        }

        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        var range = max - min;
        if (range <= 0)
        {
            return;
        }

        var (_, thumbLength) = GetThumbMetrics(trackLength);
        var trackAvail = Math.Max(1, trackLength - thumbLength);

        var delta = Orientation == Orientation.Vertical ? (e.UiY - _dragStartUiY) : (e.UiX - _dragStartUiX);
        var deltaValue = (int)Math.Round((double)delta * range / trackAvail);
        Value = Math.Clamp(_dragStartValue + deltaValue, min, max);
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
            e.Handled = true;
        }
    }

    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        var step = Math.Max(1, SmallChange);
        Value = e.WheelDelta > 0 ? Value - step : Value + step;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step = Math.Max(1, SmallChange);
        var page = LargeChange > 0 ? LargeChange : Math.Max(1, ViewportSize);

        if (Orientation == Orientation.Vertical)
        {
            switch (e.Key)
            {
                case TerminalKey.Up:
                    Value -= step;
                    e.Handled = true;
                    return;
                case TerminalKey.Down:
                    Value += step;
                    e.Handled = true;
                    return;
                case TerminalKey.PageUp:
                    Value -= page;
                    e.Handled = true;
                    return;
                case TerminalKey.PageDown:
                    Value += page;
                    e.Handled = true;
                    return;
                case TerminalKey.Home:
                    Value = Minimum;
                    e.Handled = true;
                    return;
                case TerminalKey.End:
                    Value = Maximum;
                    e.Handled = true;
                    return;
            }
        }
        else
        {
            switch (e.Key)
            {
                case TerminalKey.Left:
                    Value -= step;
                    e.Handled = true;
                    return;
                case TerminalKey.Right:
                    Value += step;
                    e.Handled = true;
                    return;
                case TerminalKey.PageUp:
                    Value -= page;
                    e.Handled = true;
                    return;
                case TerminalKey.PageDown:
                    Value += page;
                    e.Handled = true;
                    return;
                case TerminalKey.Home:
                    Value = Minimum;
                    e.Handled = true;
                    return;
                case TerminalKey.End:
                    Value = Maximum;
                    e.Handled = true;
                    return;
            }
        }
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnValueChanged(ScrollValueChangedEventArgs e) { }
}
