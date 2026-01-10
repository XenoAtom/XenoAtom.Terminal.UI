// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Spinner : Visual, IAnimatedVisual
{
    private SpinnerStyle? _cachedStyle;
    private long _intervalTicks;
    private int _frameIndex;
    private long _nextTick;

    public Spinner()
    {
        IsActive = true;
        Tone = ControlTone.Primary;
    }

    public Spinner(string label) : this()
    {
        Label = label;
    }

    [Bindable]
    public partial Visual? Label { get; set; }

    [Bindable]
    public partial bool IsActive { get; set; }

    [Bindable]
    public partial ControlTone Tone { get; set; }

    protected override int ChildrenCount => _label is null ? 0 : 1;

    protected override Visual GetChild(int index)
        => index == 0 && _label is not null ? _label : throw new ArgumentOutOfRangeException(nameof(index));

    long IAnimatedVisual.NextAnimationTick => _nextTick;

    bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

    private bool AdvanceAnimation(long timestamp)
    {
        if (App is null || !IsActive || !IsVisible)
        {
            _nextTick = long.MaxValue;
            return false;
        }

        var style = Get<SpinnerStyle>();
        if (!ReferenceEquals(_cachedStyle, style))
        {
            _cachedStyle = style;
            _frameIndex = 0;
            _intervalTicks = ToStopwatchTicks(style.Interval);
            _nextTick = timestamp + _intervalTicks;
            return true;
        }

        var interval = _intervalTicks;
        if (interval <= 0)
        {
            interval = 1;
        }

        if (_nextTick == long.MaxValue)
        {
            _nextTick = timestamp + interval;
            return true;
        }

        if (timestamp < _nextTick)
        {
            return false;
        }

        var steps = 1 + (timestamp - _nextTick) / interval;
        _frameIndex = unchecked(_frameIndex + (int)steps);
        _nextTick = _nextTick + (steps * interval);
        return true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var style = Get<SpinnerStyle>();
        var frameWidth = Math.Max(1, style.FrameWidth);

        var label = Label;
        if (label is null)
        {
            return new Size(Math.Min(availableSize.Width, frameWidth), 1);
        }

        label.Measure(new Size(LayoutConstants.Infinite, 1));
        var width = frameWidth + 1 + label.DesiredSize.Width;
        return new Size(Math.Min(availableSize.Width, width), 1);
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var label = Label;
        if (label is null)
        {
            return;
        }

        var style = Get<SpinnerStyle>();
        var frameWidth = Math.Max(1, style.FrameWidth);
        var labelX = finalRect.X + frameWidth + 1;
        if (labelX >= finalRect.Right)
        {
            return;
        }

        var available = Math.Max(0, finalRect.Right - labelX);
        var w = Math.Min(available, label.DesiredSize.Width);
        label.Arrange(new Rectangle(labelX, finalRect.Y, w, 1));
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = Get<SpinnerStyle>();

        var spinnerStyle = style.Resolve(theme, IsEnabled, Tone);
        var labelStyle = theme.ForegroundTextStyle();
        if (!IsEnabled)
        {
            labelStyle |= TextStyle.Dim;
        }

        var frameWidth = Math.Max(1, style.FrameWidth);
        var frameText = IsActive ? style.GetFrame(_frameIndex) : style.GetFrame(0);
        var span = frameText.AsSpan();
        if (TerminalTextUtility.TryGetIndexAtCell(span, Math.Min(frameWidth, rect.Width), out var frameEndIndex))
        {
            span = span[..frameEndIndex];
        }

        buffer.WriteText(rect.X, rect.Y, span, spinnerStyle);

        var label = Label;
        if (label is null || rect.Width <= frameWidth)
        {
            return;
        }

        var labelX = rect.X + frameWidth + 1;
        if (labelX >= rect.X + rect.Width)
        {
            return;
        }

        buffer.SetCell(rect.X + frameWidth, rect.Y, new Rune(' '), labelStyle);

        // Fill label area style so that the label visual inherits the proper styling.
        if (label.Bounds.Width > 0)
        {
            for (var x = label.Bounds.X; x < label.Bounds.X + label.Bounds.Width && x < rect.Right; x++)
            {
                buffer.SetCell(x, rect.Y, new Rune(' '), labelStyle);
            }
        }
    }

    private static long ToStopwatchTicks(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return 1;
        }

        var seconds = interval.TotalSeconds;
        var ticks = seconds * Stopwatch.Frequency;
        if (ticks < 1)
        {
            return 1;
        }

        return (long)ticks;
    }
}
