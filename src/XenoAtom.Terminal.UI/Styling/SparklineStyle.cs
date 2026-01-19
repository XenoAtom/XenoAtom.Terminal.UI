// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for <see cref="Controls.Sparkline"/>.
/// </summary>
public sealed record SparklineStyle : IStyle<SparklineStyle>
{
    /// <summary>
    /// Gets the default sparkline style.
    /// </summary>
    public static SparklineStyle Default { get; } = new();

    /// <summary>
    /// Gets the style key for sparklines.
    /// </summary>
    public static StyleKey<SparklineStyle> Key { get; } = new("SparklineStyle", Default);

    /// <summary>
    /// Gets the glyph set used for levels.
    /// </summary>
    public SparklineGlyphs Glyphs { get; init; } = SparklineGlyphs.Blocks8;

    /// <summary>
    /// Gets the optional cell style.
    /// </summary>
    public Style? Style { get; init; }

    /// <summary>
    /// Resolves the sparkline style for the given theme.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved cell style.</returns>
    public Style Resolve(Theme theme)
    {
        if (Style is { } s)
        {
            return s;
        }

        var style = theme.ForegroundTextStyle();
        if (theme.Accent is { } accent)
        {
            style = style.WithForeground(accent);
        }
        return style;
    }
}
