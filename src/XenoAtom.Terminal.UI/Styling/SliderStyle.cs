// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.Slider{T}"/>.
/// </summary>
public sealed record SliderStyle : IStyle<SliderStyle>
{
    /// <summary>
    /// Gets the default slider style.
    /// </summary>
    public static SliderStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="SliderStyle"/>.
    /// </summary>
    public static StyleKey<SliderStyle> Key { get; } = new("SliderStyle", Default);

    /// <summary>
    /// Gets the track glyph for horizontal sliders.
    /// </summary>
    public Rune Track { get; init; } = new(0x2500); // ─

    /// <summary>
    /// Gets the track glyph for the active (filled) portion of horizontal sliders.
    /// </summary>
    public Rune ActiveTrack { get; init; } = new(0x2501); // ━

    /// <summary>
    /// Gets the track glyph for vertical sliders.
    /// </summary>
    public Rune VerticalTrack { get; init; } = new(0x2502); // │

    /// <summary>
    /// Gets the track glyph for the active (filled) portion of vertical sliders.
    /// </summary>
    public Rune VerticalActiveTrack { get; init; } = new(0x2503); // ┃

    /// <summary>
    /// Gets the thumb glyph.
    /// </summary>
    public Rune Thumb { get; init; } = new('●');

    /// <summary>
    /// Gets the optional style for the inactive track.
    /// </summary>
    public Style? TrackStyle { get; init; }

    /// <summary>
    /// Gets the optional style for the active track.
    /// </summary>
    public Style? ActiveTrackStyle { get; init; }

    /// <summary>
    /// Gets the optional style for the thumb.
    /// </summary>
    public Style? ThumbStyle { get; init; }

    /// <summary>
    /// Gets the optional style for the thumb when hovered.
    /// </summary>
    public Style? ThumbHoveredStyle { get; init; }

    /// <summary>
    /// Gets the optional style for the thumb when pressed.
    /// </summary>
    public Style? ThumbPressedStyle { get; init; }

    /// <summary>
    /// Gets the optional style for the thumb when focused.
    /// </summary>
    public Style? ThumbFocusedStyle { get; init; }

    /// <summary>
    /// Gets the optional style when the slider is disabled.
    /// </summary>
    public Style? DisabledStyle { get; init; }

    /// <summary>
    /// Resolves the inactive track style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveTrackStyle(Theme theme)
        => TrackStyle ?? theme.BorderStyle(focused: false);

    /// <summary>
    /// Resolves the active track style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style ResolveActiveTrackStyle(Theme theme)
    {
        var style = ActiveTrackStyle;
        if (style is not null)
        {
            return style.Value;
        }

        var fg = theme.Accent ?? theme.FocusBorder ?? theme.Foreground;
        var resolved = Style.None | TextStyle.Bold;
        if (fg is { } c)
        {
            resolved = resolved.WithForeground(c);
        }
        return resolved;
    }

    /// <summary>
    /// Resolves the thumb style for the provided state.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="enabled">Whether the slider is enabled.</param>
    /// <param name="focused">Whether the slider is focused.</param>
    /// <param name="hovered">Whether the thumb is hovered.</param>
    /// <param name="pressed">Whether the thumb is pressed.</param>
    public Style ResolveThumbStyle(Theme theme, bool enabled, bool focused, bool hovered, bool pressed)
    {
        if (!enabled)
        {
            if (DisabledStyle is { } d)
            {
                return d;
            }

            var dim = theme.BorderStyle(focused: false) | TextStyle.Dim;
            if (theme.Disabled is { } c)
            {
                dim = dim.WithForeground(c);
            }
            return dim;
        }

        if (pressed)
        {
            if (ThumbPressedStyle is { } p)
            {
                return p;
            }

            var pressedStyle = ResolveActiveTrackStyle(theme);
            if (theme.Selection is { } bg)
            {
                pressedStyle = pressedStyle.WithForeground(bg);
            }
            return pressedStyle | TextStyle.Bold;
        }

        if (hovered)
        {
            if (ThumbHoveredStyle is { } h)
            {
                return h;
            }

            return ResolveActiveTrackStyle(theme) | TextStyle.Bold;
        }

        if (focused)
        {
            if (ThumbFocusedStyle is { } f)
            {
                return f;
            }

            var focusStyle = ResolveActiveTrackStyle(theme) | TextStyle.Underline;
            if (theme.FocusBorder is { } c)
            {
                focusStyle = focusStyle.WithForeground(c);
            }
            return focusStyle;
        }

        if (ThumbStyle is { } t)
        {
            return t;
        }

        return ResolveActiveTrackStyle(theme);
    }
}
