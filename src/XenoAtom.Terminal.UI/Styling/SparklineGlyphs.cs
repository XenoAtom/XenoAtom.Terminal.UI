// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines the glyphs used by <see cref="Controls.Sparkline"/> to represent increasing levels.
/// </summary>
public readonly record struct SparklineGlyphs(
    Rune Level0,
    Rune Level1,
    Rune Level2,
    Rune Level3,
    Rune Level4,
    Rune Level5,
    Rune Level6,
    Rune Level7)
{
    /// <summary>
    /// Gets an 8-level sparkline glyph set based on block elements.
    /// </summary>
    public static SparklineGlyphs Blocks8 { get; } = new(
        new Rune(0x2581), // U+2581
        new Rune(0x2582), // U+2582
        new Rune(0x2583), // U+2583
        new Rune(0x2584), // U+2584
        new Rune(0x2585), // U+2585
        new Rune(0x2586), // U+2586
        new Rune(0x2587), // U+2587
        new Rune(0x2588)); // U+2588

    /// <summary>
    /// Gets an 8-level ASCII sparkline glyph set.
    /// </summary>
    public static SparklineGlyphs Ascii8 { get; } = new(
        new Rune(' '),
        new Rune('.'),
        new Rune(':'),
        new Rune('-'),
        new Rune('='),
        new Rune('+'),
        new Rune('*'),
        new Rune('#'));

    /// <summary>
    /// Gets a glyph for the specified level.
    /// </summary>
    /// <param name="level">The level (typically 0..7).</param>
    /// <returns>The glyph corresponding to the level.</returns>
    public Rune GetLevel(int level)
        => level switch
        {
            <= 0 => Level0,
            1 => Level1,
            2 => Level2,
            3 => Level3,
            4 => Level4,
            5 => Level5,
            6 => Level6,
            _ => Level7,
        };
}
