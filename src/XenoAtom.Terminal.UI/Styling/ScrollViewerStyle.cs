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

        var style = CellStyle.None | TextStyle.Dim;
        if (theme.Muted is { } fg)
        {
            style = style.WithForeground(fg);
        }
        else if (theme.Border is { } border)
        {
            style = style.WithForeground(border);
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
        var fg = highlighted
            ? (theme.FocusBorder ?? theme.Selection ?? theme.Accent ?? theme.Border ?? theme.Foreground)
            : (theme.Border ?? theme.Muted ?? theme.Foreground);
        if (fg is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }
}
