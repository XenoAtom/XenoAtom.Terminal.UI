// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines rendering and theming options for a <see cref="Controls.TextArea"/> and related text editors.
/// </summary>
public sealed record TextAreaStyle : IStyle<TextAreaStyle>
{
    /// <summary>
    /// Gets the default text area style.
    /// </summary>
    public static TextAreaStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="TextAreaStyle"/>.
    /// </summary>
    public static StyleKey<TextAreaStyle> Key { get; } = new("TextAreaStyle", Default);

    /// <summary>
    /// Gets the padding between the border and the text content.
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional border color.
    /// </summary>
    public Color? Border { get; init; }

    /// <summary>
    /// Gets the optional border color when focused.
    /// </summary>
    public Color? FocusBorder { get; init; }

    /// <summary>
    /// Gets the optional selection background color.
    /// </summary>
    public Color? Selection { get; init; }

    /// <summary>
    /// Gets the optional background color for the text surface.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the optional placeholder foreground color.
    /// </summary>
    public Color? Placeholder { get; init; }

    /// <summary>
    /// Resolves the border style for the provided <paramref name="theme"/>.
    /// </summary>
    /// <param name="theme">The current theme.</param>
    /// <param name="focused">Whether the control is focused.</param>
    public Style BorderStyle(Theme theme, bool focused)
    {
        var color = focused ? (FocusBorder ?? theme.FocusBorder) : (Border ?? theme.Border);
        var style = Style.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    /// <summary>
    /// Resolves the selection style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style SelectionStyle(Theme theme)
    {
        var style = Style.None;
        var color = Selection ?? theme.Selection;
        if (color is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= TextStyle.Bold;
        return style;
    }

    /// <summary>
    /// Resolves the background style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style BackgroundStyle(Theme theme)
    {
        var style = Style.None;
        if (theme.Foreground is { } fg) style = style.WithForeground(fg);
        var bg = Background ?? theme.InputFill ?? theme.SurfaceAlt ?? theme.Surface ?? theme.Background;
        if (bg is { } b) style = style.WithBackground(b);
        return style;
    }

    /// <summary>
    /// Resolves the placeholder style for the provided <paramref name="theme"/>.
    /// </summary>
    public Style PlaceholderStyle(Theme theme)
    {
        var style = BackgroundStyle(theme);
        var fg = Placeholder ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }
}
