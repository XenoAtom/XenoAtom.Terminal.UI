// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.ScrollViewer"/>.
/// </summary>
public sealed record ScrollViewerStyle : IStyle<ScrollViewerStyle>
{
    /// <summary>
    /// Gets the default scroll viewer style.
    /// </summary>
    public static ScrollViewerStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for scroll viewers.
    /// </summary>
    public static StyleKey<ScrollViewerStyle> Key { get; } = new("ScrollViewerStyle", Default);

    /// <summary>
    /// Gets the thickness of scroll bars.
    /// </summary>
    public int ScrollBarThickness { get; init; } = 1;

    /// <summary>
    /// Gets the optional track style.
    /// </summary>
    public CellStyle? TrackStyle { get; init; }

    /// <summary>
    /// Gets the optional thumb style.
    /// </summary>
    public CellStyle? ThumbStyle { get; init; }

    /// <summary>
    /// Resolves the scroll track style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
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

    /// <summary>
    /// Resolves the scroll thumb style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="highlighted">Whether the thumb is highlighted.</param>
    /// <returns>The resolved cell style.</returns>
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
