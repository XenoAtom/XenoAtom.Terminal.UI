// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines glyphs for horizontal and vertical rules.
/// </summary>
/// <param name="Horizontal">The horizontal glyph.</param>
/// <param name="Vertical">The vertical glyph.</param>
public readonly record struct RuleGlyphs(Rune Horizontal, Rune Vertical)
{
    /// <summary>
    /// Gets the ASCII rule glyphs.
    /// </summary>
    public static RuleGlyphs Ascii { get; } = new(new Rune('-'), new Rune('|'));

    /// <summary>
    /// Gets the single-line rule glyphs.
    /// </summary>
    public static RuleGlyphs Single { get; } = new(new Rune(0x2500), new Rune(0x2502)); // ─ │

    /// <summary>
    /// Gets the double-line rule glyphs.
    /// </summary>
    public static RuleGlyphs Double { get; } = new(new Rune(0x2550), new Rune(0x2551)); // ═ ║

    /// <summary>
    /// Gets the heavy rule glyphs.
    /// </summary>
    public static RuleGlyphs Heavy { get; } = new(new Rune(0x2501), new Rune(0x2503)); // ━ ┃

    /// <summary>
    /// Gets the dashed rule glyphs.
    /// </summary>
    public static RuleGlyphs Dashed { get; } = new(new Rune(0x2504), new Rune(0x2506)); // ┄ ┆

    /// <summary>
    /// Gets the dotted rule glyphs.
    /// </summary>
    public static RuleGlyphs Dotted { get; } = new(new Rune(0x2508), new Rune(0x250A)); // ┈ ┊

    /// <summary>
    /// Gets the block rule glyphs.
    /// </summary>
    public static RuleGlyphs Block { get; } = new(new Rune(0x2584), new Rune(0x258C)); // ▄ ▌
}
