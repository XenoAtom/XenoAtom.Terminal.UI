// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// A 16-color ANSI scheme (8 normal + 8 bright) with a foreground/background baseline.
/// </summary>
public sealed partial record ColorScheme
{
    /// <summary>
    /// Gets the human-readable scheme name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the background color, or <c>null</c> to use the terminal default background.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the foreground color, or <c>null</c> to use the terminal default foreground.
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Gets the black color (ANSI 0).
    /// </summary>
    public required Color Black { get; init; }

    /// <summary>
    /// Gets the blue color (ANSI 4).
    /// </summary>
    public required Color Blue { get; init; }

    /// <summary>
    /// Gets the cyan color (ANSI 6).
    /// </summary>
    public required Color Cyan { get; init; }

    /// <summary>
    /// Gets the green color (ANSI 2).
    /// </summary>
    public required Color Green { get; init; }

    /// <summary>
    /// Gets the purple/magenta color (ANSI 5).
    /// </summary>
    public required Color Purple { get; init; }

    /// <summary>
    /// Gets the red color (ANSI 1).
    /// </summary>
    public required Color Red { get; init; }

    /// <summary>
    /// Gets the white color (ANSI 7).
    /// </summary>
    public required Color White { get; init; }

    /// <summary>
    /// Gets the yellow color (ANSI 3).
    /// </summary>
    public required Color Yellow { get; init; }

    /// <summary>
    /// Gets the bright black color (ANSI 8).
    /// </summary>
    public required Color BrightBlack { get; init; }

    /// <summary>
    /// Gets the bright blue color (ANSI 12).
    /// </summary>
    public required Color BrightBlue { get; init; }

    /// <summary>
    /// Gets the bright cyan color (ANSI 14).
    /// </summary>
    public required Color BrightCyan { get; init; }

    /// <summary>
    /// Gets the bright green color (ANSI 10).
    /// </summary>
    public required Color BrightGreen { get; init; }

    /// <summary>
    /// Gets the bright purple/magenta color (ANSI 13).
    /// </summary>
    public required Color BrightPurple { get; init; }

    /// <summary>
    /// Gets the bright red color (ANSI 9).
    /// </summary>
    public required Color BrightRed { get; init; }

    /// <summary>
    /// Gets the bright white color (ANSI 15).
    /// </summary>
    public required Color BrightWhite { get; init; }

    /// <summary>
    /// Gets the bright yellow color (ANSI 11).
    /// </summary>
    public required Color BrightYellow { get; init; }

    /// <inheritdoc/>
    public override string? ToString() => Name;

    /// <summary>
    /// Gets a scheme that maps to the terminal indexed 16-color palette and uses terminal default background/foreground.
    /// </summary>
    public static ColorScheme Terminal { get; } = new ColorScheme
    {
        Name = "Terminal",
        Background = null, // terminal default
        Foreground = null, // terminal default
        Black = Color.Basic16(0),
        Red = Color.Basic16(1),
        Green = Color.Basic16(2),
        Yellow = Color.Basic16(3),
        Blue = Color.Basic16(4),
        Purple = Color.Basic16(5),
        Cyan = Color.Basic16(6),
        White = Color.Basic16(7),
        BrightBlack = Color.Basic16(8),
        BrightRed = Color.Basic16(9),
        BrightGreen = Color.Basic16(10),
        BrightYellow = Color.Basic16(11),
        BrightBlue = Color.Basic16(12),
        BrightPurple = Color.Basic16(13),
        BrightCyan = Color.Basic16(14),
        BrightWhite = Color.Basic16(15),
    };

    // https://rootloops.sh/?sugar=7&colors=9&sogginess=4&flavor=1&milk=0.94&fruit=8
    /// <summary>
    /// Gets the default dark scheme used by <see cref="Theme.Default"/>.
    /// </summary>
    public static ColorScheme RootLoopsDark { get; } = new ColorScheme
    {
        Name = "Root Loops (Dark)",
        Background = Color.Rgb(0x0F, 0x1D, 0x27),
        Foreground = Color.Rgb(0xCD, 0xE0, 0xEE),
        Black = Color.Rgb(0x1F, 0x35, 0x44),
        Blue = Color.Rgb(0x50, 0x9A, 0xF6),
        Cyan = Color.Rgb(0x1F, 0xAE, 0xAE),
        Green = Color.Rgb(0x67, 0xAF, 0x34),
        Purple = Color.Rgb(0xCA, 0x64, 0xF3),
        Red = Color.Rgb(0xF7, 0x5B, 0x72),
        White = Color.Rgb(0x7D, 0xA9, 0xC9),
        Yellow = Color.Rgb(0xC9, 0x8B, 0x1A),
        BrightBlack = Color.Rgb(0x3B, 0x5E, 0x76),
        BrightBlue = Color.Rgb(0x77, 0xB1, 0xFB),
        BrightCyan = Color.Rgb(0x24, 0xC6, 0xC6),
        BrightGreen = Color.Rgb(0x75, 0xC7, 0x3B),
        BrightPurple = Color.Rgb(0xD6, 0x8A, 0xF7),
        BrightRed = Color.Rgb(0xFB, 0x85, 0x90),
        BrightWhite = Color.Rgb(0xBA, 0xD4, 0xE8),
        BrightYellow = Color.Rgb(0xE4, 0x9F, 0x27),
    };

    // https://rootloops.sh/?sugar=7&colors=9&sogginess=4&flavor=1&milk=3&fruit=8
    /// <summary>
    /// Gets a light variant of the default Root Loops scheme.
    /// </summary>
    public static ColorScheme RootLoopsLight { get; } = new ColorScheme
    {
        Name = "Root Loops (Light)",
        Background = Color.Rgb(0xEE, 0xF5, 0xF9),
        Foreground = Color.Rgb(0x02, 0x06, 0x0B),
        Black = Color.Rgb(0xD5, 0xE5, 0xF1),
        Blue = Color.Rgb(0x50, 0x9A, 0xF6),
        Cyan = Color.Rgb(0x1F, 0xAE, 0xAE),
        Green = Color.Rgb(0x67, 0xAF, 0x34),
        Purple = Color.Rgb(0xCA, 0x64, 0xF3),
        Red = Color.Rgb(0xF7, 0x5B, 0x72),
        White = Color.Rgb(0x36, 0x56, 0x6C),
        Yellow = Color.Rgb(0xC9, 0x8B, 0x1A),
        BrightBlack = Color.Rgb(0x87, 0xB1, 0xD0),
        BrightBlue = Color.Rgb(0x77, 0xB1, 0xFB),
        BrightCyan = Color.Rgb(0x24, 0xC6, 0xC6),
        BrightGreen = Color.Rgb(0x75, 0xC7, 0x3B),
        BrightPurple = Color.Rgb(0xD6, 0x8A, 0xF7),
        BrightRed = Color.Rgb(0xFB, 0x85, 0x90),
        BrightWhite = Color.Rgb(0x14, 0x25, 0x30),
        BrightYellow = Color.Rgb(0xE4, 0x9F, 0x27),
    };
}
