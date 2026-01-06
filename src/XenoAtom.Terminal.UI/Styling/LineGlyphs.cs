// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct LineGlyphs(
    Rune Horizontal,
    Rune Vertical,
    Rune TopLeft,
    Rune TopRight,
    Rune BottomLeft,
    Rune BottomRight,
    Rune TeeTop,
    Rune TeeBottom,
    Rune TeeLeft,
    Rune TeeRight,
    Rune Cross)
{
    public static LineGlyphs Single { get; } = new(
        Horizontal: new Rune(0x2500),   // ─
        Vertical: new Rune(0x2502),     // │
        TopLeft: new Rune(0x250C),      // ┌
        TopRight: new Rune(0x2510),     // ┐
        BottomLeft: new Rune(0x2514),   // └
        BottomRight: new Rune(0x2518),  // ┘
        TeeTop: new Rune(0x252C),       // ┬
        TeeBottom: new Rune(0x2534),    // ┴
        TeeLeft: new Rune(0x251C),      // ├
        TeeRight: new Rune(0x2524),     // ┤
        Cross: new Rune(0x253C));       // ┼
}
