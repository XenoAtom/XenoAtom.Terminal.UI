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

    public static LineGlyphs Rounded { get; } = new(
        Horizontal: new Rune(0x2500),   // ─
        Vertical: new Rune(0x2502),     // │
        TopLeft: new Rune(0x256D),      // ╭
        TopRight: new Rune(0x256E),     // ╮
        BottomLeft: new Rune(0x2570),   // ╰
        BottomRight: new Rune(0x256F),  // ╯
        TeeTop: new Rune(0x252C),       // ┬
        TeeBottom: new Rune(0x2534),    // ┴
        TeeLeft: new Rune(0x251C),      // ├
        TeeRight: new Rune(0x2524),     // ┤
        Cross: new Rune(0x253C));       // ┼

    public static LineGlyphs Double { get; } = new(
        Horizontal: new Rune(0x2550),   // ═
        Vertical: new Rune(0x2551),     // ║
        TopLeft: new Rune(0x2554),      // ╔
        TopRight: new Rune(0x2557),     // ╗
        BottomLeft: new Rune(0x255A),   // ╚
        BottomRight: new Rune(0x255D),  // ╝
        TeeTop: new Rune(0x2566),       // ╦
        TeeBottom: new Rune(0x2569),    // ╩
        TeeLeft: new Rune(0x2560),      // ╠
        TeeRight: new Rune(0x2563),     // ╣
        Cross: new Rune(0x256C));       // ╬

    public static LineGlyphs Heavy { get; } = new(
        Horizontal: new Rune(0x2501),   // ━
        Vertical: new Rune(0x2503),     // ┃
        TopLeft: new Rune(0x250F),      // ┏
        TopRight: new Rune(0x2513),     // ┓
        BottomLeft: new Rune(0x2517),   // ┗
        BottomRight: new Rune(0x251B),  // ┛
        TeeTop: new Rune(0x2533),       // ┳
        TeeBottom: new Rune(0x253B),    // ┻
        TeeLeft: new Rune(0x2523),      // ┣
        TeeRight: new Rune(0x252B),     // ┫
        Cross: new Rune(0x254B));       // ╋
}
