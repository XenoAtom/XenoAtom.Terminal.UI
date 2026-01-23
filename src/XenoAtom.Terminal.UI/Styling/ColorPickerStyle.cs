// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines styling for a <see cref="Controls.ColorPicker"/> control.
/// </summary>
public sealed record ColorPickerStyle : IStyle<ColorPickerStyle>
{
    /// <summary>
    /// Gets the default color picker style.
    /// </summary>
    public static ColorPickerStyle Default { get; } = new();

    /// <summary>
    /// Gets the environment key used to resolve a <see cref="ColorPickerStyle"/>.
    /// </summary>
    public static StyleKey<ColorPickerStyle> Key { get; } = new("ColorPickerStyle", Default);

    /// <summary>
    /// Gets the padding around the control content.
    /// </summary>
    public Thickness Padding { get; init; } = default;

    /// <summary>
    /// Gets the swatch width in cells.
    /// </summary>
    public int SwatchWidth { get; init; } = 14;

    /// <summary>
    /// Gets the swatch height in cells.
    /// </summary>
    public int SwatchHeight { get; init; } = 7;

    /// <summary>
    /// Gets the glyphs used to draw the swatch border.
    /// </summary>
    public LineGlyphs SwatchGlyphs { get; init; } = LineGlyphs.Rounded;

    /// <summary>
    /// Gets the optional swatch border style.
    /// </summary>
    public Style? SwatchBorderStyle { get; init; }

    /// <summary>
    /// Gets the optional light checkerboard color used when previewing alpha.
    /// </summary>
    public Color? CheckerLight { get; init; }

    /// <summary>
    /// Gets the optional dark checkerboard color used when previewing alpha.
    /// </summary>
    public Color? CheckerDark { get; init; }

    /// <summary>
    /// Gets the number of palette columns.
    /// </summary>
    public int PaletteColumns { get; init; } = 8;

    /// <summary>
    /// Gets the palette swatch width in cells.
    /// </summary>
    public int PaletteSwatchWidth { get; init; } = 4;

    /// <summary>
    /// Gets the palette swatch height in cells.
    /// </summary>
    public int PaletteSwatchHeight { get; init; } = 2;

    /// <summary>
    /// Gets the spacing in cells between palette swatches.
    /// </summary>
    public int PaletteGap { get; init; } = 1;

    /// <summary>
    /// Gets the glyphs used to draw a selection border in the palette.
    /// </summary>
    public LineGlyphs PaletteSelectionGlyphs { get; init; } = LineGlyphs.Single;

    /// <summary>
    /// Gets the optional palette selection border style.
    /// </summary>
    public Style? PaletteSelectionStyle { get; init; }

    /// <summary>
    /// Gets a value indicating whether hex formatting uses uppercase letters.
    /// </summary>
    public bool UppercaseHex { get; init; } = true;

    /// <summary>
    /// Resolves the swatch border style for the given theme.
    /// </summary>
    public Style ResolveSwatchBorderStyle(Theme theme)
    {
        if (SwatchBorderStyle is { } border)
        {
            return border;
        }

        return theme.BorderStyle(focused: false);
    }

    /// <summary>
    /// Resolves the palette selection style for the given theme.
    /// </summary>
    public Style ResolvePaletteSelectionStyle(Theme theme)
    {
        if (PaletteSelectionStyle is { } style)
        {
            return style;
        }

        var border = theme.BorderStyle(focused: true);
        if (theme.FocusBorder is { } focus)
        {
            border = border.WithForeground(focus);
        }
        return border;
    }

    internal (Color Light, Color Dark) ResolveCheckerboardColors(Theme theme)
    {
        if (CheckerLight is { } a && CheckerDark is { } b)
        {
            return (a, b);
        }

        var background = (theme.Surface ?? theme.Background ?? Color.Default).ToRgb();
        if (background.Kind == ColorKind.Default)
        {
            // Inline/terminal theme: use a neutral grayscale.
            return (Color.Rgb(0x55, 0x55, 0x55), Color.Rgb(0x33, 0x33, 0x33));
        }

        // Subtle checkerboard derived from the background.
        var light = background.Mix(Color.Rgb(0xFF, 0xFF, 0xFF), 0.08f, ColorMixSpace.Oklab);
        var dark = background.Mix(Color.Rgb(0x00, 0x00, 0x00), 0.12f, ColorMixSpace.Oklab);
        return (CheckerLight ?? light, CheckerDark ?? dark);
    }
}

