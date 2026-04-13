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

/// <summary>
/// Displays a scroll bar for a scrollable extent and viewport.
/// </summary>
public abstract partial class ScrollBar : Visual
{
    private bool _dragging;
    private int _dragPointerOffsetInThumb;
    private int _dragCurrentUiX;
    private int _dragCurrentUiY;
    private int _oldValueForEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollBar"/> control.
    /// </summary>
    /// <param name="focusable">Whether the scroll bar can receive focus.</param>
    protected ScrollBar(bool focusable = true)
    {
        Focusable = focusable;
        if (Orientation == Orientation.Vertical)
        {
            VerticalAlignment = Align.Stretch;
        }
        else
        {
            HorizontalAlignment = Align.Stretch;
        }

        this.SmallChange = 1;
    }

    /// <summary>
    /// Gets or sets the scroll bar orientation.
    /// </summary>
    public abstract Orientation Orientation { get; }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    [Bindable]
    public partial int Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    [Bindable]
    public partial int Maximum { get; set; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Bindable]
    public partial int Value { get; set; }

    /// <summary>
    /// Gets or sets the viewport size, used to compute the thumb length.
    /// </summary>
    [Bindable]
    public partial int ViewportSize { get; set; }

    /// <summary>
    /// Gets or sets the small change step (e.g. mouse wheel).
    /// </summary>
    [Bindable]
    public partial int SmallChange { get; set; }

    /// <summary>
    /// Gets or sets the large change step (e.g. page up/down).
    /// </summary>
    [Bindable]
    public partial int LargeChange { get; set; }

    internal bool IsDragging => _dragging;

    partial void OnViewportSizeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnSmallChangeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnLargeChangeChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnMinimumChanged(int value)
    {
        _ = value;
        UpdateDraggedValueFromPointer();
    }

    partial void OnMaximumChanged(int value)
    {
        _ = value;
        UpdateDraggedValueFromPointer();
    }

    partial void OnViewportSizeChanged(int value)
    {
        _ = value;
        UpdateDraggedValueFromPointer();
    }

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

    /// <inheritdoc/>
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var thickness = Math.Max(1, GetStyle<ScrollBarStyle>().Thickness);
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

    /// <inheritdoc/>
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<ScrollBarStyle>();
        var glyphs = theme.ScrollBars;

        var highlighted = IsHovered || _dragging || HasFocus;
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

    private void RenderVertical(CellBuffer buffer, Rectangle rect, ScrollBarGlyphs glyphs, Style trackStyle, Style thumbStyle)
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

    private void RenderHorizontal(CellBuffer buffer, Rectangle rect, ScrollBarGlyphs glyphs, Style trackStyle, Style thumbStyle)
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
        => GetThumbMetrics(trackLength, Value);

    private (int ThumbStart, int ThumbLength) GetThumbMetrics(int trackLength, int value)
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
        var minThumb = Math.Max(1, GetStyle<ScrollBarStyle>().MinThumbLength);

        var thumbLength = viewport <= 0 || contentSize <= 0
            ? minThumb
            : (int)Math.Round((double)trackLength * viewport / contentSize);

        thumbLength = Math.Clamp(thumbLength, minThumb, trackLength);

        var trackAvail = Math.Max(1, trackLength - thumbLength);
        var offset = Math.Clamp(value - min, 0, range);
        var thumbStart = (int)Math.Round((double)offset * trackAvail / range);
        thumbStart = Math.Clamp(thumbStart, 0, trackLength - thumbLength);

        return (thumbStart, thumbLength);
    }

    /// <inheritdoc/>
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

        // Pointer interactions can mutate Value in the same tracking context (e.g. while ScrollViewer updates the
        // bar during Arrange). Read the current value from the backing field to avoid a tracked Value read before write.
        var (thumbStart, thumbLength) = GetThumbMetrics(trackLength, _value);
        var local = Orientation == Orientation.Vertical ? e.UiY - rect.Y : e.UiX - rect.X;

        if (local >= thumbStart && local < thumbStart + thumbLength)
        {
            _dragCurrentUiX = e.UiX;
            _dragCurrentUiY = e.UiY;
            BeginDragging(local - thumbStart);
            e.Handled = true;
            return;
        }

        Value = GetValueFromTrackPosition(local, trackLength, thumbLength);
        var (updatedThumbStart, updatedThumbLength) = GetThumbMetrics(trackLength, _value);
        var dragOffset = Math.Clamp(local - updatedThumbStart, 0, Math.Max(0, updatedThumbLength - 1));
        _dragCurrentUiX = e.UiX;
        _dragCurrentUiY = e.UiY;
        BeginDragging(dragOffset);
        e.Handled = true;
    }

    /// <inheritdoc/>
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

        var (_, thumbLength) = GetThumbMetrics(trackLength, _value);
        _dragCurrentUiX = e.UiX;
        _dragCurrentUiY = e.UiY;
        var local = Orientation == Orientation.Vertical ? e.UiY - rect.Y : e.UiX - rect.X;
        Value = GetValueFromThumbStart(local - _dragPointerOffsetInThumb, trackLength, thumbLength);
        e.Handled = true;
    }

    private void BeginDragging(int pointerOffsetInThumb)
    {
        _dragging = true;
        _dragPointerOffsetInThumb = Math.Max(0, pointerOffsetInThumb);
    }

    private int GetValueFromTrackPosition(int local, int trackLength, int thumbLength)
        => GetValueFromThumbStart(local - (thumbLength / 2), trackLength, thumbLength);

    private int GetValueFromThumbStart(int desiredThumbStart, int trackLength, int thumbLength)
    {
        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        var range = max - min;
        if (range <= 0)
        {
            return min;
        }

        var trackAvail = Math.Max(1, trackLength - thumbLength);
        desiredThumbStart = Math.Clamp(desiredThumbStart, 0, trackAvail);
        var offset = (int)Math.Round((double)desiredThumbStart * range / trackAvail);
        return Math.Clamp(min + offset, min, max);
    }

    private void UpdateDraggedValueFromPointer()
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

        var (_, thumbLength) = GetThumbMetrics(trackLength, _value);
        var local = Orientation == Orientation.Vertical ? _dragCurrentUiY - rect.Y : _dragCurrentUiX - rect.X;
        Value = GetValueFromThumbStart(local - _dragPointerOffsetInThumb, trackLength, thumbLength);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        var step = Math.Max(1, SmallChange);
        Value = e.WheelDelta > 0 ? _value - step : _value + step;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step = Math.Max(1, SmallChange);
        var page = LargeChange > 0 ? LargeChange : Math.Max(1, ViewportSize);

        if (Orientation == Orientation.Vertical)
        {
            switch (e.Key)
            {
                case TerminalKey.Up:
                    Value = _value - step;
                    e.Handled = true;
                    return;
                case TerminalKey.Down:
                    Value = _value + step;
                    e.Handled = true;
                    return;
                case TerminalKey.PageUp:
                    Value = _value - page;
                    e.Handled = true;
                    return;
                case TerminalKey.PageDown:
                    Value = _value + page;
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
                    Value = _value - step;
                    e.Handled = true;
                    return;
                case TerminalKey.Right:
                    Value = _value + step;
                    e.Handled = true;
                    return;
                case TerminalKey.PageUp:
                    Value = _value - page;
                    e.Handled = true;
                    return;
                case TerminalKey.PageDown:
                    Value = _value + page;
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

/// <summary>
/// A vertical scroll bar.
/// </summary>
public sealed class VScrollBar : ScrollBar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VScrollBar"/> control.
    /// </summary>
    /// <param name="focusable">Whether the scroll bar can receive focus.</param>
    public VScrollBar(bool focusable = true) : base(focusable)
    {
    }

    /// <inheritdoc/>
    public override Orientation Orientation => Orientation.Vertical;
}

/// <summary>
/// A horizontal scroll bar.
/// </summary>
public sealed class HScrollBar : ScrollBar
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HScrollBar"/> control.
    /// </summary>
    /// <param name="focusable">Whether the scroll bar can receive focus.</param>
    public HScrollBar(bool focusable = true) : base(focusable)
    {
    }

    /// <inheritdoc/>
    public override Orientation Orientation => Orientation.Horizontal;
}
