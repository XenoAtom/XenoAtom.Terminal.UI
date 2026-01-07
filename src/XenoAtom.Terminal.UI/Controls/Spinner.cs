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
    public partial string? Label { get; set; }

    [Bindable]
    public partial bool IsActive { get; set; }

    [Bindable]
    public partial ControlTone Tone { get; set; }

    long IAnimatedVisual.NextAnimationTick => _nextTick;

    bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

    private bool AdvanceAnimation(long timestamp)
    {
        if (App is null || !IsActive || !IsVisible)
        {
            _nextTick = long.MaxValue;
            return false;
        }

        var style = GetEnvironmentValue(SpinnerStyle.Key);
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
        var style = GetEnvironmentValue(SpinnerStyle.Key);
        var frameWidth = 1;
        var frames = style.Frames;
        if (frames.Length > 0)
        {
            for (var i = 0; i < frames.Length; i++)
            {
                frameWidth = Math.Max(frameWidth, TerminalTextUtility.GetRuneWidth(frames[i]));
            }
        }

        var label = Label;
        if (string.IsNullOrEmpty(label))
        {
            return new Size(Math.Min(availableSize.Width, frameWidth), 1);
        }

        var labelCells = TerminalTextUtility.GetWidth(label.AsSpan());
        var width = frameWidth + 1 + labelCells;
        return new Size(Math.Min(availableSize.Width, width), 1);
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
        var style = GetEnvironmentValue(SpinnerStyle.Key);

        var spinnerStyle = style.Resolve(theme, IsEnabled, Tone);
        var labelStyle = theme.ForegroundTextStyle();
        if (!IsEnabled)
        {
            labelStyle |= TextStyle.Dim;
        }

        var frame = IsActive ? style.GetFrame(_frameIndex) : style.GetFrame(0);
        buffer.SetCell(rect.X, rect.Y, frame, spinnerStyle);

        var label = Label;
        if (string.IsNullOrEmpty(label) || rect.Width <= 1)
        {
            return;
        }

        var frameWidth = Math.Max(1, TerminalTextUtility.GetRuneWidth(frame));
        var labelX = rect.X + frameWidth + 1;
        if (labelX >= rect.X + rect.Width)
        {
            return;
        }

        buffer.SetCell(rect.X + frameWidth, rect.Y, new Rune(' '), labelStyle);

        var span = label.AsSpan();
        var maxCells = rect.Right - labelX;
        if (TerminalTextUtility.TryGetIndexAtCell(span, maxCells, out var endIndex))
        {
            span = span[..endIndex];
        }

        buffer.WriteText(labelX, rect.Y, span, labelStyle);
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
