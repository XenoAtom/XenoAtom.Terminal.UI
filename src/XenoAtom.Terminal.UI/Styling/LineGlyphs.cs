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
        Horizontal: new Rune(0x00C4),
        Vertical: new Rune(0x00B3),
        TopLeft: new Rune(0x00DA),
        TopRight: new Rune(0x00BF),
        BottomLeft: new Rune(0x00C0),
        BottomRight: new Rune(0x00D9),
        TeeTop: new Rune(0x00C2),
        TeeBottom: new Rune(0x00C1),
        TeeLeft: new Rune(0x00C3),
        TeeRight: new Rune(0x00B4),
        Cross: new Rune(0x00C5));
}

