// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SwitchStyle : IStyle<SwitchStyle>
{
    public static SwitchStyle Default { get; } = new();

    public static StyleKey<SwitchStyle> Key { get; } = new("SwitchStyle", Default);

    public int SpaceBetweenGlyphAndText { get; init; } = 2;

    public Rune TrackLeft { get; init; } = new('▕');

    public Rune TrackRight { get; init; } = new('▏');

    public Rune ThumbGlyph { get; init; } = new('●');

    public CellStyle? TrackOn { get; init; }
    public CellStyle? TrackOff { get; init; }
    public CellStyle? TrackHovered { get; init; }
    public CellStyle? TrackPressed { get; init; }
    public CellStyle? TrackFocused { get; init; }
    public CellStyle? TrackDisabled { get; init; }

    public CellStyle? ThumbOn { get; init; }
    public CellStyle? ThumbOff { get; init; }
    public CellStyle? ThumbDisabled { get; init; }

    public CellStyle ResolveTrack(Theme theme, bool enabled, bool focused, bool hovered, bool pressed, bool isOn)
    {
        ArgumentNullException.ThrowIfNull(theme);

        if (!enabled)
        {
            var disabled = TrackDisabled ?? ResolveDefaultTrackDisabled(theme);
            return disabled | TextStyle.Dim;
        }

        var style = isOn ? (TrackOn ?? ResolveDefaultTrackOn(theme)) : (TrackOff ?? ResolveDefaultTrackOff(theme));

        if (pressed)
        {
            return TrackPressed ?? ResolveDefaultTrackPressed(theme, style);
        }

        if (hovered)
        {
            style = TrackHovered ?? ResolveDefaultTrackHovered(theme, style);
        }

        if (focused)
        {
            style = TrackFocused ?? ResolveDefaultTrackFocused(theme, style);
        }

        return style;
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
