// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Numerics;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a slider control that allows selecting a numeric value within a range.
/// </summary>
public sealed partial class Slider<T> : Visual where T: struct, INumber<T>
{
    private bool _dragging;
    private T _oldValueForEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider{T}"/> class.
    /// </summary>
    public Slider()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        this.Minimum(T.Zero);
        // 10 (TODO: figure out a better way to express 10 in generic way)
        this.Maximum(T.One + T.One + T.One + T.One + T.One + T.One + T.One + T.One + T.One + T.One);
        this.Step(T.One);
        this.LargeStep(T.One + T.One);
        this.SnapToStep(true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider{T}"/> class with an initial value.
    /// </summary>
    /// <param name="value">The initial value.</param>
    public Slider(T value) : this()
    {
        this.Value(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider{T}"/> class with range and initial value.
    /// </summary>
    /// <param name="minimum">The minimum value.</param>
    /// <param name="maximum">The maximum value.</param>
    /// <param name="value">The initial value.</param>
    public Slider(T minimum, T maximum, T value) : this(value)
    {
        this.Minimum(minimum);
        this.Maximum(maximum);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slider{T}"/> class bound to a value binding.
    /// </summary>
    /// <param name="value">A binding that supplies the current value.</param>
    public Slider(Binding<T> value) : this()
    {
        this.BindValue(value);
    }

    /// <summary>
    /// Gets or sets the orientation of the slider.
    /// </summary>
    [Bindable]
    public partial Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    [Bindable]
    public partial T Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    [Bindable]
    public partial T Maximum { get; set; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Bindable]
    public partial T Value { get; set; }

    /// <summary>
    /// Gets or sets the small step increment.
    /// </summary>
    [Bindable]
    public partial T Step { get; set; }

    /// <summary>
    /// Gets or sets the large step increment.
    /// </summary>
    [Bindable]
    public partial T LargeStep { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the slider snaps to the nearest step.
    /// </summary>
    [Bindable]
    public partial bool SnapToStep { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display a formatted value label.
    /// </summary>
    [Bindable]
    public partial bool ShowValueLabel { get; set; }

    /// <summary>
    /// Gets or sets a formatter for the value label.
    /// </summary>
    [Bindable]
    public partial Delegator<Func<T, string>> ValueFormatter { get; set; }

    partial void OnMinimumChanging(ref T value)
    {

        if (!T.IsFinite(value))
        {
            value = T.Zero;
        }
    }

    partial void OnMaximumChanging(ref T value)
    {
        if (!T.IsFinite(value))
        {
            value = T.One;
        }
    }

    partial void OnMinimumChanged(T value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Maximum = Minimum;
        }
        Value = ClampAndSnap(Value);
    }

    partial void OnMaximumChanged(T value)
    {
        _ = value;
        if (Maximum < Minimum)
        {
            Minimum = Maximum;
        }
        Value = ClampAndSnap(Value);
    }

    partial void OnStepChanging(ref T value)
    {
        if (!T.IsFinite(value) || value < T.Zero)
        {
            value = T.Zero;
        }
    }

    partial void OnLargeStepChanging(ref T value)
    {
        if (!T.IsFinite(value) || value < T.Zero)
        {
            value = T.Zero;
        }
    }

    partial void OnValueChanging(ref T value)
    {
        _oldValueForEvent = _value;
        value = ClampAndSnap(value);
    }

    partial void OnValueChanged(T value)
    {
        if (!Equals(_oldValueForEvent, value))
        {
            RaiseEvent(ValueChangedEvent, new ValueChangedEventArgs<T>(_oldValueForEvent, value));
        }
    }

    private static double ToDouble(T value) => double.CreateChecked(value);

    private T ClampAndSnap(T value)
    {
        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        value = T.Clamp(value, min, max);

        if (SnapToStep)
        {
            var step = Step;
            if (step > T.Zero && max > min)
            {
                var t = (ToDouble(value) - ToDouble(min)) / ToDouble(step);
                var snapped = T.CreateChecked(Math.Round(t) * ToDouble(step)) + min;
                value = T.Clamp(snapped, min, max);
            }
        }

        return value;
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        const int MinTrackLength = 6;

        if (Orientation == Orientation.Vertical)
        {
            return SizeHints.Fixed(new Size(
                Math.Max(0, Math.Min(availableSize.Width, 1)),
                Math.Max(0, Math.Min(availableSize.Height, MinTrackLength))));
        }

        var desiredWidth = Math.Min(availableSize.Width, MinTrackLength);
        return SizeHints.Fixed(new Size(
            Math.Max(0, desiredWidth),
            Math.Max(0, Math.Min(availableSize.Height, 1))));
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<SliderStyle>();
        var focused = HasFocus;
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

    private void RenderHorizontal(CellBuffer buffer, Rectangle rect, SliderStyle style, Style trackStyle, Style activeStyle, Style thumbStyle)
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
            buffer.WriteText(rect.X + trackWidth + 1, y, label.AsSpan(), Style.None | TextStyle.Dim);
        }
    }

    private void RenderVertical(CellBuffer buffer, Rectangle rect, SliderStyle style, Style trackStyle, Style activeStyle, Style thumbStyle)
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
        if (range <= T.Zero)
        {
            return 0;
        }

        var t = (ToDouble(Value) - ToDouble(Minimum)) / ToDouble(range);
        t = Math.Clamp(t, 0.0, 1.0);
        return (int)Math.Round(t * (trackLength - 1));
    }

    private T GetValueAtCell(int cell, int trackLength)
    {
        if (trackLength <= 1)
        {
            return Minimum;
        }

        var t = Math.Clamp(cell / (double)(trackLength - 1), 0.0, 1.0);
        return T.CreateChecked(ToDouble(Minimum) + (t * (ToDouble(Maximum) - ToDouble(Minimum))));
    }

    private string FormatValue(T value)
    {
        var formatter = ValueFormatter.Invoke;
        if (formatter is not null)
        {
            return formatter(value);
        }

        //var percent = Maximum > Minimum ? (value - Minimum) / (Maximum - Minimum) : T.Zero;
        return ToStringValue(value);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var step = Step;
        var large = LargeStep > T.Zero ? LargeStep : step * (T.One + T.One);

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

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        UpdateValueFromPointer(e);
        e.Handled = true;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (!IsEnabled || e.WheelDelta == 0)
        {
            return;
        }

        var delta = Step;
        if (delta <= T.Zero)
        {
            delta = T.One;
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
    private void OnValueChanged(ValueChangedEventArgs<T> e) { }
}
