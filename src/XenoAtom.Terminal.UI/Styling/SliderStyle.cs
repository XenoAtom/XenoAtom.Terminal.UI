// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record SliderStyle : IStyle<SliderStyle>
{
    public static SliderStyle Default { get; } = new();

    public static StyleKey<SliderStyle> Key { get; } = new("SliderStyle", Default);

    public Rune Track { get; init; } = new(0x2500); // ─

    public Rune ActiveTrack { get; init; } = new(0x2501); // ━

    public Rune VerticalTrack { get; init; } = new(0x2502); // │

    public Rune VerticalActiveTrack { get; init; } = new(0x2503); // ┃

    public Rune Thumb { get; init; } = new('●');

    public CellStyle? TrackStyle { get; init; }
    public CellStyle? ActiveTrackStyle { get; init; }

    public CellStyle? ThumbStyle { get; init; }
    public CellStyle? ThumbHoveredStyle { get; init; }
    public CellStyle? ThumbPressedStyle { get; init; }
    public CellStyle? ThumbFocusedStyle { get; init; }
    public CellStyle? DisabledStyle { get; init; }

    public CellStyle ResolveTrackStyle(Theme theme)
        => TrackStyle ?? theme.BorderStyle(focused: false);

    public CellStyle ResolveActiveTrackStyle(Theme theme)
    {
        var style = ActiveTrackStyle;
        if (style is not null)
        {
            return style.Value;
        }

        var fg = theme.Accent ?? theme.FocusBorder ?? theme.Foreground;
        var resolved = CellStyle.None | TextStyle.Bold;
        if (fg is { } c)
        {
            resolved = resolved.WithForeground(c);
        }
        return resolved;
    }

    public CellStyle ResolveThumbStyle(Theme theme, bool enabled, bool focused, bool hovered, bool pressed)
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

