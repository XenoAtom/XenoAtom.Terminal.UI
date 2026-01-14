// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SwitchStyle : IStyle<SwitchStyle>
{
    public static SwitchStyle Default { get; } = new();

    public static SwitchStyle Round { get; } = new()
    {
        ThumbGlyph = new('⬤'),
    };

    public static StyleKey<SwitchStyle> Key { get; } = new("SwitchStyle", Default);

    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    public Rune TrackLeft { get; init; } = new(' '); 

    public Rune TrackRight { get; init; } = new(' ');

    public Rune ThumbGlyph { get; init; } = new('⬛');

    public CellStyle? TrackOn { get; init; }
    public CellStyle? TrackOff { get; init; }
    public CellStyle? TrackOnActive { get; init; }
    public CellStyle? TrackOnInactive { get; init; }
    public CellStyle? TrackOffActive { get; init; }
    public CellStyle? TrackOffInactive { get; init; }
    public CellStyle? TrackHovered { get; init; }
    public CellStyle? TrackPressed { get; init; }
    public CellStyle? TrackFocused { get; init; }
    public CellStyle? TrackDisabled { get; init; }

    public CellStyle? ThumbOn { get; init; }
    public CellStyle? ThumbOff { get; init; }
    public CellStyle? ThumbDisabled { get; init; }

    public CellStyle ResolveTrack(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn)
        => ResolveTrackPart(theme, enabled, focused, hovered, pressed, isOn, activePart: true);

    public CellStyle ResolveTrackPart(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn, bool activePart)
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

    private CellStyle ResolveBaseTrack(Theme theme, bool isOn, bool activePart)
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

    public CellStyle ResolveThumb(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn)
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

    private static CellStyle ResolveDefaultTrackOn(Theme theme)
    {
        var style = CellStyle.None;
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

    private static CellStyle ResolveDefaultTrackOnActive(Theme theme)
        => ResolveDefaultTrackOn(theme);

    private static CellStyle ResolveDefaultTrackOnInactive(Theme theme)
    {
        var style = CellStyle.None;
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

    private static CellStyle ResolveDefaultTrackOff(Theme theme)
    {
        var style = CellStyle.None;
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

    private static CellStyle ResolveDefaultTrackOffActive(Theme theme)
    {
        var style = CellStyle.None;
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

    private static CellStyle ResolveDefaultTrackOffInactive(Theme theme)
        => ResolveDefaultTrackOff(theme);

    private static CellStyle ResolveDefaultTrackHovered(Theme theme, CellStyle baseStyle)
    {
        if (theme.Surface is { } hoverBg)
        {
            return baseStyle.WithBackground(hoverBg);
        }

        return baseStyle | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultTrackPressed(Theme theme, CellStyle baseStyle)
    {
        if (theme.Selection is { } pressedBg)
        {
            return baseStyle.WithBackground(pressedBg) | TextStyle.Bold;
        }

        return baseStyle | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultTrackFocused(Theme theme, CellStyle baseStyle)
    {
        if (theme.FocusBorder is { } focusFg)
        {
            baseStyle = baseStyle.WithForeground(focusFg);
        }

        return baseStyle;
    }

    private static CellStyle ResolveDefaultTrackDisabled(Theme theme)
    {
        var style = CellStyle.None;
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

    private static CellStyle ResolveDefaultThumbOn(Theme theme)
    {
        var style = CellStyle.None;
        if (theme.Background is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultThumbOff(Theme theme)
    {
        var style = CellStyle.None;
        if (theme.Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style | TextStyle.Bold;
    }

    private static CellStyle ResolveDefaultThumbDisabled(Theme theme)
    {
        var style = CellStyle.None;
        if (theme.Disabled is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style;
    }
}
