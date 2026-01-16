// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines the frames and styling for a <see cref="Controls.Spinner"/>.
/// </summary>
public sealed record SpinnerStyle : IStyle<SpinnerStyle>
{
    /// <summary>
    /// Gets the default spinner style.
    /// </summary>
    public static SpinnerStyle Default { get; } = SpinnerStyles.Dots;

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SpinnerStyle"/>.
    /// </summary>
    public static StyleKey<SpinnerStyle> Key { get; } = new("SpinnerStyle", Default);

    private readonly string[] _frames;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpinnerStyle"/> class.
    /// </summary>
    /// <param name="name">The style name.</param>
    /// <param name="interval">The frame interval.</param>
    /// <param name="frames">The frames. All frames must have the same cell width.</param>
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

    /// <summary>
    /// Gets the style name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the time between frames.
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    /// Gets the cell width of each frame.
    /// </summary>
    public int FrameWidth { get; }

    /// <summary>
    /// Gets the frames for this style.
    /// </summary>
    public ReadOnlySpan<string> Frames => _frames;

    /// <summary>
    /// Gets the number of frames.
    /// </summary>
    public int FrameCount => _frames.Length;

    /// <summary>
    /// Gets the decorations applied to the spinner text.
    /// </summary>
    public TextStyle TextStyle { get; init; } = TextStyle.Bold;

    /// <summary>
    /// Gets the optional foreground color used to render the spinner.
    /// </summary>
    public AnsiColor? Foreground { get; init; }

    /// <summary>
    /// Gets a frame string for the given frame index.
    /// </summary>
    /// <param name="frameIndex">The frame index.</param>
    /// <returns>The resolved frame.</returns>
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

    /// <summary>
    /// Resolves the <see cref="CellStyle"/> to use for rendering a spinner.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the spinner is enabled.</param>
    /// <param name="tone">The semantic tone used for color selection.</param>
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
