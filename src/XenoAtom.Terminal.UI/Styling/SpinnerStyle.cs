// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SpinnerStyle
{
    public static SpinnerStyle Default { get; } = SpinnerStyles.Dots;

    public static EnvironmentKey<SpinnerStyle> Key { get; } = new("SpinnerStyle", Default);

    public string Name { get; init; } = "Custom";

    public TimeSpan Interval { get; init; } = TimeSpan.FromMilliseconds(80);

    public Rune[] Frames { get; init; } = [new Rune('|'), new Rune('/'), new Rune('-'), new Rune('\\')];

    public int FrameCount => Frames.Length;

    public TextStyle TextStyle { get; init; } = TextStyle.Bold;

    public AnsiColor? Foreground { get; init; }

    public Rune GetFrame(int frameIndex)
    {
        var frames = Frames;
        if (frames.Length == 0)
        {
            return new Rune(' ');
        }

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

