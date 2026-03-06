// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
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
    /// <param name="frames">The frames.</param>
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

        FrameWidth = GetMaxFrameWidth(frames, TerminalWideRuneResolvers.Default);
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
    /// Gets the maximum cell width of the frames under the default wide-rune resolver.
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
    public Color? Foreground { get; init; }

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
    /// Gets the maximum cell width of the frames for the specified wide-rune resolver.
    /// </summary>
    /// <param name="wideRuneResolver">The predicate used to widen additional runes.</param>
    public int GetFrameWidth(Func<Rune, bool>? wideRuneResolver = null)
        => GetMaxFrameWidth(_frames, wideRuneResolver ?? TerminalWideRuneResolvers.Default);

    /// <summary>
    /// Resolves the <see cref="Style"/> to use for rendering a spinner.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the spinner is enabled.</param>
    /// <param name="tone">The semantic tone used for color selection.</param>
    public Style Resolve(Theme theme, bool enabled, ControlTone tone)
    {
        var fg = Foreground ?? tone switch
        {
            ControlTone.Primary => theme.Primary ?? theme.Accent ?? theme.FocusBorder ?? theme.Foreground,
            ControlTone.Success => theme.Success ?? theme.Foreground,
            ControlTone.Warning => theme.Warning ?? theme.Foreground,
            ControlTone.Error => theme.Error ?? theme.Foreground,
            _ => theme.Muted ?? theme.Foreground,
        };

        var style = Style.None;
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

    private static int GetMaxFrameWidth(IReadOnlyList<string> frames, Func<Rune, bool> wideRuneResolver)
    {
        var width = 0;
        for (var i = 0; i < frames.Count; i++)
        {
            width = Math.Max(width, global::XenoAtom.Terminal.TerminalTextUtility.GetWidth(frames[i].AsSpan(), wideRuneResolver));
        }

        if (width <= 0)
        {
            throw new ArgumentException("Spinner frames must have a width > 0.", nameof(frames));
        }

        return width;
    }
}
