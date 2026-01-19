// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.Popup"/>.
/// </summary>
public sealed record PopupStyle : IStyle<PopupStyle>
{
    /// <summary>
    /// Gets the default popup style.
    /// </summary>
    public static PopupStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for popups.
    /// </summary>
    public static StyleKey<PopupStyle> Key { get; } = new("PopupStyle", Default);

    /// <summary>
    /// Gets the padding applied inside the popup surface.
    /// </summary>
    public Thickness Padding { get; init; } = new(0);

    /// <summary>
    /// Gets the optional surface style.
    /// </summary>
    public Style? SurfaceStyle { get; init; }

    /// <summary>
    /// Gets the optional border style.
    /// </summary>
    public Style? BorderStyle { get; init; }

    /// <summary>
    /// Resolves the surface style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
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

        // Fallbacks (terminal theme / older themes).
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
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public Style ResolveBorderStyle(Theme theme)
    {
        if (BorderStyle is { } border)
        {
            return border;
        }

        var style = theme.BorderStyle(focused: false);

        // Match the popup surface fill so borders blend naturally when using RGBA strokes.
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
