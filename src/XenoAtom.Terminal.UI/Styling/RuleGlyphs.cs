// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct RuleGlyphs(Rune Horizontal, Rune Vertical)
{
    public static RuleGlyphs Ascii { get; } = new(new Rune('-'), new Rune('|'));

    public static RuleGlyphs Single { get; } = new(new Rune(0x2500), new Rune(0x2502)); // ─ │

    public static RuleGlyphs Double { get; } = new(new Rune(0x2550), new Rune(0x2551)); // ═ ║

    public static RuleGlyphs Heavy { get; } = new(new Rune(0x2501), new Rune(0x2503)); // ━ ┃

    public static RuleGlyphs Dashed { get; } = new(new Rune(0x2504), new Rune(0x2506)); // ┄ ┆

    public static RuleGlyphs Dotted { get; } = new(new Rune(0x2508), new Rune(0x250A)); // ┈ ┊

    public static RuleGlyphs Block { get; } = new(new Rune(0x2584), new Rune(0x258C)); // ▄ ▌
}

