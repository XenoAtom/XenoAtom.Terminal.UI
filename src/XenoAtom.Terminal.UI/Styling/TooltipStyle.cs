// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for tooltips.
/// </summary>
public sealed record TooltipStyle : IStyle<TooltipStyle>
{
    /// <summary>
    /// Gets the default tooltip style.
    /// </summary>
    public static TooltipStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for tooltips.
    /// </summary>
    public static StyleKey<TooltipStyle> Key { get; } = new("TooltipStyle", Default);

    /// <summary>
    /// Gets the padding applied inside the tooltip border.
    /// </summary>
    public Thickness Padding { get; init; } = new(1);

    /// <summary>
    /// Gets the optional maximum tooltip width (in cells) used when measuring content.
    /// </summary>
    /// <remarks>
    /// Tooltips typically wrap to remain readable; this cap prevents extremely wide tooltips when the content measures
    /// unconstrained.
    /// </remarks>
    public int? MaxWidth { get; init; } = 60;

    /// <summary>
    /// Gets the optional tooltip surface style.
    /// </summary>
    public Style? SurfaceStyle { get; init; }

    /// <summary>
    /// Gets the optional tooltip border style.
    /// </summary>
    public Style? BorderStyle { get; init; }

    /// <summary>
    /// Gets the optional line glyph set used to draw the tooltip border.
    /// </summary>
    public LineGlyphs Glyphs { get; init; } = LineGlyphs.Rounded;

    /// <summary>
    /// Resolves the surface style for the given theme.
    /// </summary>
    public Style ResolveSurfaceStyle(Theme theme)
    {
        if (SurfaceStyle is { } surface)
        {
            return surface;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.PopupSurface is { } bg)
        {
            return style.WithBackground(bg);
        }

        if (theme.SurfaceAlt is { } bg2)
        {
            return style.WithBackground(bg2);
        }

        if (theme.Surface is { } bg3)
        {
            return style.WithBackground(bg3);
        }

        return style;
    }

    /// <summary>
    /// Resolves the border style for the given theme.
    /// </summary>
    public Style ResolveBorderStyle(Theme theme)
    {
        if (BorderStyle is { } border)
        {
            return border;
        }

        var style = theme.BorderStyle(focused: false);
        if (theme.PopupSurface is { } bg)
        {
            return style.WithBackground(bg);
        }

        if (theme.SurfaceAlt is { } bg2)
        {
            return style.WithBackground(bg2);
        }

        if (theme.Surface is { } bg3)
        {
            return style.WithBackground(bg3);
        }

        return style;
    }
}

