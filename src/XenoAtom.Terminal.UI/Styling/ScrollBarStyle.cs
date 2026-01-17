// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.ScrollBar"/>.
/// </summary>
public sealed record ScrollBarStyle : IStyle<ScrollBarStyle>
{
    /// <summary>
    /// Gets the default scroll bar style.
    /// </summary>
    public static ScrollBarStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ScrollBarStyle"/>.
    /// </summary>
    public static StyleKey<ScrollBarStyle> Key { get; } = new("ScrollBarStyle", Default);

    /// <summary>
    /// Gets the scroll bar thickness in cells.
    /// </summary>
    public int Thickness { get; init; } = 1;

    /// <summary>
    /// Gets the minimum thumb length in cells.
    /// </summary>
    public int MinThumbLength { get; init; } = 1;

    /// <summary>
    /// Gets the optional style used for the track.
    /// </summary>
    public CellStyle? TrackStyle { get; init; }

    /// <summary>
    /// Gets the optional style used for the thumb.
    /// </summary>
    public CellStyle? ThumbStyle { get; init; }

    /// <summary>
    /// Resolves the track style for the provided <paramref name="theme"/>.
    /// </summary>
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
    /// Resolves the thumb style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="highlighted">Whether the thumb is highlighted (hovered/dragging/focused).</param>
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
