// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public sealed record ScrollViewerStyle
{
    public static ScrollViewerStyle Default { get; } = new();

    public static EnvironmentKey<ScrollViewerStyle> Key { get; } = new("ScrollViewerStyle", Default);

    public int ScrollBarThickness { get; init; } = 2;

    public CellStyle? TrackStyle { get; init; }
    public CellStyle? ThumbStyle { get; init; }

    public CellStyle ResolveTrackStyle(Theme theme)
    {
        if (TrackStyle is { } track)
        {
            return track;
        }

        var style = CellStyle.None;

        // Prefer a subtle background for the track so the thumb stands out even when using space glyphs.
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        else if (theme.Surface is { } bg2)
        {
            style = style.WithBackground(bg2);
        }

        if (theme.Muted is { } fg)
        {
            style = style.WithForeground(fg);
            style |= TextStyle.Dim;
        }

        return style;
    }

    public CellStyle ResolveThumbStyle(Theme theme, bool highlighted)
    {
        if (ThumbStyle is { } thumb)
        {
            return thumb;
        }

        var style = CellStyle.None | TextStyle.Bold;

        // When using space glyphs for the thumb, the background is the primary differentiator.
        var thumbBg = highlighted
            ? (theme.Selection ?? theme.Accent ?? theme.FocusBorder ?? theme.Border ?? theme.SurfaceAlt ?? theme.Surface)
            : (theme.Border ?? theme.SurfaceAlt ?? theme.Surface);

        if (thumbBg is { } bg)
        {
            style = style.WithBackground(bg);
        }

        // Keep a sensible foreground in case a theme uses non-space glyphs.
        var thumbFg = highlighted
            ? (theme.Background ?? theme.Foreground ?? theme.Muted)
            : (theme.Foreground ?? theme.Muted);

        if (thumbFg is { } fg)
        {
            style = style.WithForeground(fg);
        }

        return style;
    }
}
