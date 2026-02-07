// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for <see cref="Controls.Placeholder"/>.
/// </summary>
public sealed record PlaceholderStyle : IStyle<PlaceholderStyle>
{
    /// <summary>
    /// Gets the default placeholder style.
    /// </summary>
    public static PlaceholderStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="PlaceholderStyle"/>.
    /// </summary>
    public static StyleKey<PlaceholderStyle> Key { get; } = new("PlaceholderStyle", Default);

    /// <summary>
    /// Gets an optional foreground color override.
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Gets an optional background color override.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets an optional foreground brush override.
    /// </summary>
    public Brush? ForegroundBrush { get; init; }

    /// <summary>
    /// Gets an optional background brush override.
    /// </summary>
    public Brush? BackgroundBrush { get; init; }

    /// <summary>
    /// Gets optional text decorations applied to the rendered text.
    /// </summary>
    public TextStyle TextStyle { get; init; }

    /// <summary>
    /// Gets a value indicating whether to fill the whole placeholder bounds with background.
    /// </summary>
    public bool FillBackground { get; init; } = true;

    /// <summary>
    /// Gets the padding applied inside the placeholder.
    /// </summary>
    public Thickness Padding { get; init; }

    /// <summary>
    /// Resolves the text style applied to rendered text.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The resolved style.</returns>
    public Style ResolveTextStyle(Theme theme)
    {
        _ = theme;
        var style = Style.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }

        if (Background is { } bg)
        {
            style = style.WithBackground(bg);
        }

        if (TextStyle != default)
        {
            style |= TextStyle;
        }

        return style;
    }

    /// <summary>
    /// Resolves the style used to fill the placeholder background.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <returns>The fill style.</returns>
    public Style ResolveFillStyle(Theme theme)
    {
        _ = theme;
        var style = Style.None;
        if (Background is { } bg)
        {
            style = style.WithBackground(bg);
        }

        return style;
    }
}
