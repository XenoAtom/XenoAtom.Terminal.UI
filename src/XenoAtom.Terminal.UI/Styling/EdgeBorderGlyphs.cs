// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct EdgeBorderGlyphs(
    Rune Top,
    Rune Bottom,
    Rune Left,
    Rune Right,
    Rune TopLeft,
    Rune TopRight,
    Rune BottomLeft,
    Rune BottomRight)
{
    public static EdgeBorderGlyphs LegacyComputing { get; } = new(
        Top: new Rune('▔'), // U+2594
        Bottom: new Rune('▁'), // U+2581
        Left: new Rune('▏'), // U+258F
        Right: new Rune('▕'), // U+2595
        TopLeft: new Rune(0x1FB7D),
        TopRight: new Rune(0x1FB7E),
        BottomLeft: new Rune(0x1FB7C),
        BottomRight: new Rune(0x1FB7F));
}

