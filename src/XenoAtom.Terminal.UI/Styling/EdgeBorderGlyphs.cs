// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct EdgeBorderGlyphs(
    int Top,
    int Bottom,
    int Left,
    int Right,
    int TopLeft,
    int TopRight,
    int BottomLeft,
    int BottomRight)
{
    public static EdgeBorderGlyphs LegacyComputing { get; } = new(
        Top: '▔', // U+2594
        Bottom: '▁', // U+2581
        Left: '▏', // U+258F
        Right: '▕', // U+2595
        TopLeft: 0x1FB7C,
        TopRight: 0x1FB7D,
        BottomLeft: 0x1FB7E,
        BottomRight: 0x1FB7F);
}

