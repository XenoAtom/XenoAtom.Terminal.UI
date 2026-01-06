// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Styling;

public sealed class ScrollViewerStyle
{
    public static ScrollViewerStyle Default { get; } = new();

    public static EnvironmentKey<ScrollViewerStyle> Key { get; } = new("ScrollViewerStyle", Default);

    public int ScrollBarThickness { get; init; } = 2;

    public Cell? TrackStyle { get; init; }
    public Cell? ThumbStyle { get; init; }

    public Cell ResolveTrackStyle(Theme theme)
    {
        if (TrackStyle is { } track)
        {
            return track;
        }

        var style = Cell.None | TextStyle.Dim;
        if (theme.Muted is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (theme.Surface is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    public Cell ResolveThumbStyle(Theme theme, bool focused)
    {
        if (ThumbStyle is { } thumb)
        {
            return thumb;
        }

        var style = Cell.None | TextStyle.Bold;
        var fg = focused ? (theme.Accent ?? theme.Selection) : (theme.Border ?? theme.Muted);
        if (fg is { } fgc) style = style.WithForeground(fgc);
        if (theme.SurfaceAlt is { } bgc) style = style.WithBackground(bgc);
        return style;
    }
}
