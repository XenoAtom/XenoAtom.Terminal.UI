// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SpinnerStyle : IStyle<SpinnerStyle>
{
    public static SpinnerStyle Default { get; } = SpinnerStyles.Dots;

    public static StyleKey<SpinnerStyle> Key { get; } = new("SpinnerStyle", Default);

    private readonly string[] _frames;

    public SpinnerStyle(string name, TimeSpan interval, params string[] frames)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (interval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        if (frames is null || frames.Length == 0)
        {
            throw new ArgumentException("Spinner frames cannot be null or empty.", nameof(frames));
        }

        _frames = frames;

        Name = name;
        Interval = interval;

        var width = TerminalTextUtility.GetWidth(frames[0].AsSpan());
        if (width <= 0)
        {
            throw new ArgumentException("Spinner frames must have a width > 0.", nameof(frames));
        }

        for (var i = 1; i < frames.Length; i++)
        {
            if (TerminalTextUtility.GetWidth(frames[i].AsSpan()) != width)
            {
                throw new ArgumentException("All spinner frames must have the same cell width.", nameof(frames));
            }
        }

        FrameWidth = width;
    }

    public string Name { get; }

    public TimeSpan Interval { get; }

    public int FrameWidth { get; }

    public ReadOnlySpan<string> Frames => _frames;

    public int FrameCount => _frames.Length;

    public TextStyle TextStyle { get; init; } = TextStyle.Bold;

    public AnsiColor? Foreground { get; init; }

    public string GetFrame(int frameIndex)
    {
        var frames = _frames;

        var idx = frameIndex % frames.Length;
        if (idx < 0)
        {
            idx += frames.Length;
        }

        return frames[idx];
    }

    public CellStyle Resolve(Theme theme, bool enabled, ControlTone tone)
    {
        var fg = Foreground ?? tone switch
        {
            ControlTone.Primary => theme.Primary ?? theme.Accent ?? theme.FocusBorder ?? theme.Foreground,
            ControlTone.Success => theme.Success ?? theme.Foreground,
            ControlTone.Warning => theme.Warning ?? theme.Foreground,
            ControlTone.Error => theme.Error ?? theme.Foreground,
            _ => theme.Muted ?? theme.Foreground,
        };

        var style = CellStyle.None;
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }

        if (!enabled)
        {
            style |= TextStyle.Dim;
        }

        style |= TextStyle;
        return style;
    }
}
