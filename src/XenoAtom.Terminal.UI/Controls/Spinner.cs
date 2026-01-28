// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents an animated spinner with optional label.
/// </summary>
public sealed partial class Spinner : Visual, IAnimatedVisual
{
    private SpinnerStyle? _cachedStyle;
    private long _intervalTicks;
    private int _frameIndex;
    private long _nextTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class.
    /// </summary>
    public Spinner()
    {
        IsActive = true;
        Tone = ControlTone.Primary;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Spinner"/> class with a label.
    /// </summary>
    /// <param name="label">The label text.</param>
    public Spinner(string label) : this()
    {
        Label = label;
    }

    /// <summary>
    /// Gets or sets the label visual.
    /// </summary>
    [Bindable]
    public partial Visual? Label { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the spinner is active.
    /// </summary>
    [Bindable]
    public partial bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the control tone used for styling.
    /// </summary>
    [Bindable]
    public partial ControlTone Tone { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _label is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 && _label is not null ? _label : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    long IAnimatedVisual.NextAnimationTick => _nextTick;

    /// <inheritdoc />
    bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

    private bool AdvanceAnimation(long timestamp)
    {
        if (App is null || !IsActive || !IsVisible)
        {
            _nextTick = long.MaxValue;
            return false;
        }

        var style = GetStyle<SpinnerStyle>();
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

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<SpinnerStyle>();
        var frameWidth = Math.Max(1, style.FrameWidth);

        var label = Label;
        if (label is null)
        {
            return SizeHints.Fixed(constraints.Clamp(new Size(frameWidth, 1)));
        }

        label.Measure(new LayoutConstraints(0, LayoutConstants.Infinite, 0, 1));
        var width = frameWidth + 1 + label.DesiredSize.Width;
        return SizeHints.Fixed(constraints.Clamp(new Size(width, 1)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var label = Label;
        if (label is null)
        {
            return;
        }

        var style = GetStyle<SpinnerStyle>();
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

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var style = GetStyle<SpinnerStyle>();

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
