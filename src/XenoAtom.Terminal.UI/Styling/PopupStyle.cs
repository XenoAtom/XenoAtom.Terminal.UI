// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

public sealed record PopupStyle : IStyle<PopupStyle>
{
    public static PopupStyle Default { get; } = new();

    public static StyleKey<PopupStyle> Key { get; } = new("PopupStyle", Default);

    public Thickness Padding { get; init; } = new(0);

    public CellStyle? SurfaceStyle { get; init; }

    public CellStyle? BorderStyle { get; init; }

    public CellStyle ResolveSurfaceStyle(Theme theme)
    {
        if (SurfaceStyle is { } surface)
        {
            return surface;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        else if (theme.Surface is { } bg2)
        {
            style = style.WithBackground(bg2);
        }

        return style;
    }

    public CellStyle ResolveBorderStyle(Theme theme)
    {
        if (BorderStyle is { } border)
        {
            return border;
        }

        var style = theme.BorderStyle(focused: false);
        if (theme.SurfaceAlt is { } bg)
        {
            style = style.WithBackground(bg);
        }
        else if (theme.Surface is { } bg2)
        {
            style = style.WithBackground(bg2);
        }

        return style;
    }
}
