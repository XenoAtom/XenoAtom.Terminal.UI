// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling options for <c>TextBox</c>.
/// </summary>
public record TextBoxStyle : IStyle<TextBoxStyle>
{
    /// <summary>
    /// Gets the default text box style.
    /// </summary>
    public static TextBoxStyle Default { get; } = new();

    /// <summary>
    /// Gets a style variant that uses ellipsis glyphs for overflow indicators.
    /// </summary>
    public static TextBoxStyle Ellipsis { get; } = Default with
    {
        OverflowIndicatorLeft = new Rune('…'),
        OverflowIndicatorRight = new Rune('…'),
    };

    /// <summary>
    /// Gets a style variant that uses double-arrow glyphs for overflow indicators.
    /// </summary>
    public static TextBoxStyle DoubleArrows { get; } = Default with
    {
        OverflowIndicatorLeft = new Rune('⇦'),
        OverflowIndicatorRight = new Rune('⇨'),
    };

    /// <summary>
    /// Gets the environment key for <see cref="TextBoxStyle"/>.
    /// </summary>
    public static StyleKey<TextBoxStyle> Key { get; } = new("TextBoxStyle", Default);

    /// <summary>
    /// Gets the padding applied inside the text box.
    /// </summary>
    public Thickness Padding { get; init; } = new(1, 0, 1, 0);

    /// <summary>
    /// Gets the optional overflow indicator displayed on the left when content is horizontally scrolled.
    /// </summary>
    public Rune? OverflowIndicatorLeft { get; init; } = new('←');

    /// <summary>
    /// Gets the optional overflow indicator displayed on the right when content is horizontally scrolled.
    /// </summary>
    public Rune? OverflowIndicatorRight { get; init; } = new('→');

    /// <summary>Gets an optional border color override.</summary>
    public Color? Border { get; init; }
    /// <summary>Gets an optional focused border color override.</summary>
    public Color? FocusBorder { get; init; }
    /// <summary>Gets an optional selection background color override.</summary>
    public Color? Selection { get; init; }
    /// <summary>Gets an optional background color override for the text region.</summary>
    public Color? Background { get; init; }
    /// <summary>Gets an optional placeholder foreground color override.</summary>
    public Color? Placeholder { get; init; }

    /// <summary>
    /// Resolves the border style for the specified focus state.
    /// </summary>
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
    /// Resolves the selection style.
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
    /// Resolves the background style used for the text region.
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
    /// Resolves the placeholder style.
    /// </summary>
    public Style PlaceholderStyle(Theme theme)
    {
        var style = BackgroundStyle(theme);
        var fg = Placeholder ?? theme.Muted ?? theme.Foreground;
        if (fg is { } c) style = style.WithForeground(c);
        return style;
    }

    /// <summary>
    /// Resolves the style used for overflow indicators.
    /// </summary>
    public Style OverflowIndicatorStyle(Theme theme)
        => PlaceholderStyle(theme) | TextStyle.Bold;
}
