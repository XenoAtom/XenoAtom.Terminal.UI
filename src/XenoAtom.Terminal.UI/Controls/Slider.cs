// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Slider : Visual
{
    private bool _dragging;
    private double _oldValueForEvent;

    public Slider()
    {
        Focusable = true;
        this.Minimum(0.0);
        this.Maximum(1.0);
        this.Step(0.1);
        this.LargeStep(0.25);
        this.SnapToStep(true);
    }

    [Bindable]
    public partial Orientation Orientation { get; set; }

    [Bindable]
    public partial double Minimum { get; set; }

    [Bindable]
    public partial double Maximum { get; set; }

    [Bindable]
    public partial double Value { get; set; }

    [Bindable]
    public partial double Step { get; set; }

    [Bindable]
    public partial double LargeStep { get; set; }

    [Bindable]
    public partial bool SnapToStep { get; set; }

    [Bindable]
    public partial bool ShowValueLabel { get; set; }

    [Bindable]
    public partial Func<double, string>? ValueFormatter { get; set; }

    partial void OnMinimumChanging(ref double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 0.0;
        }
    }

    partial void OnMaximumChanging(ref double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = 1.0;
        }
    }

    partial void OnMinimumChanged(double value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Maximum = Minimum;
        }
        Value = ClampAndSnap(Value);
    }

    partial void OnMaximumChanged(double value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Minimum = Maximum;
        }
        Value = ClampAndSnap(Value);
    }

    partial void OnStepChanging(ref double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            value = 0.0;
        }
    }

    partial void OnLargeStepChanging(ref double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            value = 0.0;
        }
    }

    partial void OnValueChanging(ref double value)
    {
        _oldValueForEvent = _value;
        value = ClampAndSnap(value);
    }

    partial void OnValueChanged(double value)
    {
        if (!Equals(_oldValueForEvent, value))
        {
            RaiseEvent(ValueChangedEvent, new ValueChangedEventArgs { OldValue = _oldValueForEvent, NewValue = value });
        }
    }

    private double ClampAndSnap(double value)
    {
        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        value = Math.Clamp(value, min, max);

        if (SnapToStep)
        {
            var step = Step;
            if (step > 0 && max > min)
            {
                var t = (value - min) / step;
                var snapped = Math.Round(t) * step + min;
                value = Math.Clamp(snapped, min, max);
            }
        }

        return value;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        const int MinTrackLength = 6;

        if (Orientation == Orientation.Vertical)
        {
            return new Size(
                Math.Max(0, Math.Min(availableSize.Width, 1)),
                Math.Max(0, Math.Min(availableSize.Height, MinTrackLength)));
        }

        var desiredWidth = Math.Min(availableSize.Width, MinTrackLength);
        return new Size(
            Math.Max(0, desiredWidth),
            Math.Max(0, Math.Min(availableSize.Height, 1)));
    }

    protected override void ArrangeOverride(Rectangle finalRect) => Bounds = finalRect;

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<SliderStyle>();
        var focused = ReferenceEquals(App?.FocusedElement, this);
        var hovered = IsHovered;
        var pressed = _dragging;
        var thumbStyle = style.ResolveThumbStyle(theme, IsEnabled, focused, hovered, pressed);
        var trackStyle = style.ResolveTrackStyle(theme);
        var activeTrackStyle = style.ResolveActiveTrackStyle(theme);

        if (Orientation == Orientation.Vertical)
        {
            RenderVertical(buffer, rect, style, trackStyle, activeTrackStyle, thumbStyle);
            return;
        }

        RenderHorizontal(buffer, rect, style, trackStyle, activeTrackStyle, thumbStyle);
    }

    private void RenderHorizontal(CellBuffer buffer, Rectangle rect, SliderStyle style, CellStyle trackStyle, CellStyle activeStyle, CellStyle thumbStyle)
    {
        var label = ShowValueLabel ? FormatValue(Value) : null;
        var labelCells = label is null ? 0 : TerminalTextUtility.GetWidth(label.AsSpan());
        var trackWidth = rect.Width;
        if (labelCells > 0)
        {
            trackWidth = Math.Max(0, rect.Width - labelCells - 1);
        }

        if (trackWidth <= 0)
        {
            return;
        }

        var thumbIndex = GetThumbIndex(trackWidth);
        var y = rect.Y;

        for (var i = 0; i < trackWidth; i++)
        {
            var x = rect.X + i;
            if (i == thumbIndex)
            {
                buffer.SetCell(x, y, style.Thumb, thumbStyle);
            }
            else if (i < thumbIndex)
            {
                buffer.SetCell(x, y, style.ActiveTrack, activeStyle);
            }
            else
            {
                buffer.SetCell(x, y, style.Track, trackStyle);
            }
        }

        if (labelCells > 0 && label is not null)
        {
            buffer.WriteText(rect.X + trackWidth + 1, y, label.AsSpan(), CellStyle.None | TextStyle.Dim);
        }
    }

    private void RenderVertical(CellBuffer buffer, Rectangle rect, SliderStyle style, CellStyle trackStyle, CellStyle activeStyle, CellStyle thumbStyle)
    {
        var trackHeight = rect.Height;
        var thumbOffset = GetThumbIndex(trackHeight);
        var thumbY = rect.Y + (trackHeight - 1 - thumbOffset);
        var x = rect.X;

        for (var i = 0; i < trackHeight; i++)
        {
            var y = rect.Y + i;
            if (y == thumbY)
            {
                buffer.SetCell(x, y, style.Thumb, thumbStyle);
                continue;
            }

            var belowThumb = y > thumbY;
            buffer.SetCell(x, y, belowThumb ? style.VerticalActiveTrack : style.VerticalTrack, belowThumb ? activeStyle : trackStyle);
        }
    }

    private int GetThumbIndex(int trackLength)
    {
        if (trackLength <= 1)
        {
            return 0;
        }

        var range = Maximum - Minimum;
        if (range <= 0)
        {
            return 0;
        }

        var t = (Value - Minimum) / range;
        t = Math.Clamp(t, 0.0, 1.0);
        return (int)Math.Round(t * (trackLength - 1));
    }

    private double GetValueAtCell(int cell, int trackLength)
    {
        if (trackLength <= 1)
        {
            return Minimum;
        }

        var t = Math.Clamp(cell / (double)(trackLength - 1), 0.0, 1.0);
        return Minimum + (t * (Maximum - Minimum));
    }

    private string FormatValue(double value)
    {
        var formatter = ValueFormatter;
        if (formatter is not null)
        {
            return formatter(value);
        }

        var percent = Maximum > Minimum ? (value - Minimum) / (Maximum - Minimum) : 0.0;
        return $"{percent * 100:000}%";
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var step = Step;
        var large = LargeStep > 0 ? LargeStep : step * 5;

        switch (e.Key)
        {
            case TerminalKey.Home:
                Value = Minimum;
                e.Handled = true;
                return;
            case TerminalKey.End:
                Value = Maximum;
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                Value -= large;
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                Value += large;
                e.Handled = true;
                return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            if (e.Key == TerminalKey.Left)
            {
                Value -= step;
                e.Handled = true;
            }
            else if (e.Key == TerminalKey.Right)
            {
                Value += step;
                e.Handled = true;
            }
            return;
        }

        if (e.Key == TerminalKey.Up)
        {
            Value += step;
            e.Handled = true;
        }
        else if (e.Key == TerminalKey.Down)
        {
            Value -= step;
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left || !IsEnabled)
        {
            return;
        }

        _dragging = true;
        UpdateValueFromPointer(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        UpdateValueFromPointer(e);
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
        if (!IsEnabled || e.WheelDelta == 0)
        {
            return;
        }

        var delta = Step;
        if (delta <= 0)
        {
            delta = 0.05;
        }

        Value = e.WheelDelta > 0 ? Value - delta : Value + delta;
        e.Handled = true;
    }

    private void UpdateValueFromPointer(PointerEventArgs e)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (Orientation == Orientation.Vertical)
        {
            var trackHeight = rect.Height;
            var y = Math.Clamp(e.LocalY, 0, Math.Max(0, trackHeight - 1));
            var cellFromBottom = (trackHeight - 1) - y;
            Value = GetValueAtCell(cellFromBottom, trackHeight);
            return;
        }

        var trackWidth = rect.Width;
        if (ShowValueLabel)
        {
            var label = FormatValue(Value);
            var labelCells = TerminalTextUtility.GetWidth(label.AsSpan());
            if (labelCells > 0)
            {
                trackWidth = Math.Max(0, trackWidth - labelCells - 1);
            }
        }

        if (trackWidth <= 0)
        {
            return;
        }

        var x = Math.Clamp(e.LocalX, 0, Math.Max(0, trackWidth - 1));
        Value = GetValueAtCell(x, trackWidth);
    }

    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnValueChanged(ValueChangedEventArgs e) { }
}
