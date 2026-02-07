// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for <see cref="Controls.TextBlock"/>.
/// </summary>
public sealed record TextBlockStyle : IStyle<TextBlockStyle>
{
    /// <summary>
    /// Gets the default text block style.
    /// </summary>
    /// <remarks>
    /// The default style does not set any explicit colors, so it inherits from the current buffer/theme.
    /// </remarks>
    public static TextBlockStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key for <see cref="TextBlockStyle"/>.
    /// </summary>
    public static StyleKey<TextBlockStyle> Key { get; } = new("TextBlockStyle", Default);

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
    /// Gets a value indicating whether to fill the entire text block bounds with <see cref="Background"/>.
    /// </summary>
    /// <remarks>
    /// When enabled and <see cref="Background"/> is set, the control fills its bounds with spaces using the background
    /// color before drawing the text. When disabled, the background applies only to the text cells written.
    /// </remarks>
    public bool FillBackground { get; init; }

    /// <summary>
    /// Resolves the text style applied to characters.
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
    /// Resolves the style used to fill the background of the control.
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

