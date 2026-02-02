// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for the <c>Switch</c> control.
/// </summary>
public sealed record SwitchStyle : IStyle<SwitchStyle>
{
    /// <summary>
    /// Gets the default switch style.
    /// </summary>
    public static SwitchStyle Default { get; } = new();

    /// <summary>
    /// Gets a rounded switch style variant.
    /// </summary>
    public static SwitchStyle Round { get; } = new()
    {
        ThumbGlyphOn = new('⬤'),
        ThumbGlyphOff = RadioButtonStyle.Default.UncheckedGlyph,
    };

    /// <summary>
    /// Gets the environment key for <see cref="SwitchStyle"/>.
    /// </summary>
    public static StyleKey<SwitchStyle> Key { get; } = new("SwitchStyle", Default);

    /// <summary>
    /// Gets the number of spaces inserted between the switch glyph area and the label text.
    /// </summary>
    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    /// <summary>
    /// Gets the glyph used for the left track segment.
    /// </summary>
    public Rune TrackLeft { get; init; } = new(' ');

    /// <summary>
    /// Gets the glyph used for the right track segment.
    /// </summary>
    public Rune TrackRight { get; init; } = new(' ');

    /// <summary>
    /// Gets the glyph used for the thumb.
    /// </summary>
    public Rune ThumbGlyphOn { get; init; } = RadioButtonStyle.Default.CheckedGlyph;

    /// <summary>
    /// Gets the glyph used for the thumb when the switch is off.
    /// </summary>
    public Rune ThumbGlyphOff { get; init; } = RadioButtonStyle.Default.UncheckedGlyph;

    /// <summary>Gets the base style for the track when the switch is on.</summary>
    public Style? TrackOn { get; init; }
    /// <summary>Gets the base style for the track when the switch is off.</summary>
    public Style? TrackOff { get; init; }
    /// <summary>Gets the style for the active track segment when the switch is on.</summary>
    public Style? TrackOnActive { get; init; }
    /// <summary>Gets the style for the inactive track segment when the switch is on.</summary>
    public Style? TrackOnInactive { get; init; }
    /// <summary>Gets the style for the active track segment when the switch is off.</summary>
    public Style? TrackOffActive { get; init; }
    /// <summary>Gets the style for the inactive track segment when the switch is off.</summary>
    public Style? TrackOffInactive { get; init; }
    /// <summary>Gets the style applied when the switch is hovered.</summary>
    public Style? TrackHovered { get; init; }
    /// <summary>Gets the style applied when the switch is pressed.</summary>
    public Style? TrackPressed { get; init; }
    /// <summary>Gets the style applied when the switch is focused.</summary>
    public Style? TrackFocused { get; init; }
    /// <summary>Gets the style applied when the switch is disabled.</summary>
    public Style? TrackDisabled { get; init; }

    /// <summary>Gets the thumb style when the switch is on.</summary>
    public Style? ThumbOn { get; init; }
    /// <summary>Gets the thumb style when the switch is off.</summary>
    public Style? ThumbOff { get; init; }
    /// <summary>Gets the thumb style when the switch is disabled.</summary>
    public Style? ThumbDisabled { get; init; }

    /// <summary>
    /// Resolves the final track style for the given state.
    /// </summary>
    public Style ResolveTrack(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn)
        => ResolveTrackPart(theme, enabled, focused, hovered, pressed, isOn, activePart: true);

    /// <summary>
    /// Resolves the final track style for a specific track segment.
    /// </summary>
    public Style ResolveTrackPart(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn, bool activePart)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (!enabled)
        {
            var disabled = TrackDisabled ?? ResolveDefaultTrackDisabled(theme);
            return disabled | TextStyle.Dim;
        }

        var style = ResolveBaseTrack(theme, isOn, activePart);

        if (pressed)
        {
            return TrackPressed ?? ResolveDefaultTrackPressed(theme, style);
        }

        if (hovered && !activePart)
        {
            style = TrackHovered ?? ResolveDefaultTrackHovered(theme, style);
        }

        if (focused)
        {
            style = TrackFocused ?? ResolveDefaultTrackFocused(theme, style);
        }

        return style;
    }

    private Style ResolveBaseTrack(Theme theme, bool isOn, bool activePart)
    {
        if (isOn)
        {
            if (activePart)
            {
                return TrackOnActive ?? TrackOn ?? ResolveDefaultTrackOnActive(theme);
            }

            return TrackOnInactive ?? TrackOn ?? ResolveDefaultTrackOnInactive(theme);
        }

        if (activePart)
        {
            return TrackOffActive ?? TrackOff ?? ResolveDefaultTrackOffActive(theme);
        }

        return TrackOffInactive ?? TrackOff ?? ResolveDefaultTrackOffInactive(theme);
    }

    /// <summary>
    /// Resolves the thumb style for the given state.
    /// </summary>
    public Style ResolveThumb(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (!enabled)
        {
            var disabled = ThumbDisabled ?? ResolveDefaultThumbDisabled(theme);
            return disabled | TextStyle.Dim;
        }

        var style = isOn ? (ThumbOn ?? ResolveDefaultThumbOn(theme)) : (ThumbOff ?? ResolveDefaultThumbOff(theme));

        if (pressed)
        {
            style |= TextStyle.Bold;
        }
        else if (hovered)
        {
            style |= TextStyle.Bold;
        }

        return style;
    }

    private static Style ResolveDefaultTrackOn(Theme theme)
    {
        var style = Style.None;
        if (theme.Background is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.Primary is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    private static Style ResolveDefaultTrackOnActive(Theme theme)
        => ResolveDefaultTrackOn(theme);

    private static Style ResolveDefaultTrackOnInactive(Theme theme)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    private static Style ResolveDefaultTrackOff(Theme theme)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    private static Style ResolveDefaultTrackOffActive(Theme theme)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.Surface is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    private static Style ResolveDefaultTrackOffInactive(Theme theme)
        => ResolveDefaultTrackOff(theme);

    private static Style ResolveDefaultTrackHovered(Theme theme, Style baseStyle)
    {
        if (theme.Surface is { } hoverBg)
        {
            return baseStyle.WithBackground(hoverBg);
        }

        return baseStyle | TextStyle.Bold;
    }

    private static Style ResolveDefaultTrackPressed(Theme theme, Style baseStyle)
    {
        if (theme.Selection is { } pressedBg)
        {
            return baseStyle.WithBackground(pressedBg) | TextStyle.Bold;
        }

        return baseStyle | TextStyle.Bold;
    }

    private static Style ResolveDefaultTrackFocused(Theme theme, Style baseStyle)
    {
        if (theme.FocusBorder is { } focusFg)
        {
            baseStyle = baseStyle.WithForeground(focusFg);
        }

        return baseStyle;
    }

    private static Style ResolveDefaultTrackDisabled(Theme theme)
    {
        var style = Style.None;
        if (theme.Disabled is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    private static Style ResolveDefaultThumbOn(Theme theme)
    {
        var style = Style.None;
        if (theme.Background is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style | TextStyle.Bold;
    }

    private static Style ResolveDefaultThumbOff(Theme theme)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style | TextStyle.Bold;
    }

    private static Style ResolveDefaultThumbDisabled(Theme theme)
    {
        var style = Style.None;
        if (theme.Disabled is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style;
    }
}
