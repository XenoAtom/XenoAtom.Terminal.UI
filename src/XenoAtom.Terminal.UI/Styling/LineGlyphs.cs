// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines a set of glyphs used to draw line borders and separators.
/// </summary>
public readonly partial record struct LineGlyphs
{
    /// <summary>Gets the horizontal line glyph.</summary>
    public Rune Horizontal { get; init; }
    /// <summary>Gets the vertical line glyph.</summary>
    public Rune Vertical { get; init; }
    /// <summary>Gets the top-left corner glyph.</summary>
    public Rune TopLeft { get; init; }
    /// <summary>Gets the top-right corner glyph.</summary>
    public Rune TopRight { get; init; }
    /// <summary>Gets the bottom-left corner glyph.</summary>
    public Rune BottomLeft { get; init; }
    /// <summary>Gets the bottom-right corner glyph.</summary>
    public Rune BottomRight { get; init; }
    /// <summary>Gets the tee glyph connecting to the top.</summary>
    public Rune TeeTop { get; init; }
    /// <summary>Gets the tee glyph connecting to the bottom.</summary>
    public Rune TeeBottom { get; init; }
    /// <summary>Gets the tee glyph connecting to the left.</summary>
    public Rune TeeLeft { get; init; }
    /// <summary>Gets the tee glyph connecting to the right.</summary>
    public Rune TeeRight { get; init; }
    /// <summary>Gets the cross glyph.</summary>
    public Rune Cross { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LineGlyphs"/> struct.
    /// </summary>
    public LineGlyphs(
        Rune horizontal,
        Rune vertical,
        Rune topLeft,
        Rune topRight,
        Rune bottomLeft,
        Rune bottomRight,
        Rune teeTop,
        Rune teeBottom,
        Rune teeLeft,
        Rune teeRight,
        Rune cross)
    {
        Horizontal = horizontal;
        Vertical = vertical;
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
        TeeTop = teeTop;
        TeeBottom = teeBottom;
        TeeLeft = teeLeft;
        TeeRight = teeRight;
        Cross = cross;
    }

    /// <summary>
    /// Gets a single-line border glyph set.
    /// </summary>
    public static LineGlyphs Single { get; } = new(
        horizontal: new Rune(0x2500),   // ─
        vertical: new Rune(0x2502),     // │
        topLeft: new Rune(0x250C),      // ┌
        topRight: new Rune(0x2510),     // ┐
        bottomLeft: new Rune(0x2514),   // └
        bottomRight: new Rune(0x2518),  // ┘
        teeTop: new Rune(0x252C),       // ┬
        teeBottom: new Rune(0x2534),    // ┴
        teeLeft: new Rune(0x251C),      // ├
        teeRight: new Rune(0x2524),     // ┤
        cross: new Rune(0x253C));       // ┼

    /// <summary>
    /// Gets a single-line border glyph set with rounded corners.
    /// </summary>
    public static LineGlyphs Rounded { get; } = new(
        horizontal: new Rune(0x2500),   // ─
        vertical: new Rune(0x2502),     // │
        topLeft: new Rune(0x256D),      // ╭
        topRight: new Rune(0x256E),     // ╮
        bottomLeft: new Rune(0x2570),   // ╰
        bottomRight: new Rune(0x256F),  // ╯
        teeTop: new Rune(0x252C),       // ┬
        teeBottom: new Rune(0x2534),    // ┴
        teeLeft: new Rune(0x251C),      // ├
        teeRight: new Rune(0x2524),     // ┤
        cross: new Rune(0x253C));       // ┼

    /// <summary>
    /// Gets a double-line border glyph set.
    /// </summary>
    public static LineGlyphs Double { get; } = new(
        horizontal: new Rune(0x2550),   // ═
        vertical: new Rune(0x2551),     // ║
        topLeft: new Rune(0x2554),      // ╔
        topRight: new Rune(0x2557),     // ╗
        bottomLeft: new Rune(0x255A),   // ╚
        bottomRight: new Rune(0x255D),  // ╝
        teeTop: new Rune(0x2566),       // ╦
        teeBottom: new Rune(0x2569),    // ╩
        teeLeft: new Rune(0x2560),      // ╠
        teeRight: new Rune(0x2563),     // ╣
        cross: new Rune(0x256C));       // ╬

    /// <summary>
    /// Gets a heavy single-line border glyph set.
    /// </summary>
    public static LineGlyphs Heavy { get; } = new(
        horizontal: new Rune(0x2501),   // ━
        vertical: new Rune(0x2503),     // ┃
        topLeft: new Rune(0x250F),      // ┏
        topRight: new Rune(0x2513),     // ┓
        bottomLeft: new Rune(0x2517),   // ┗
        bottomRight: new Rune(0x251B),  // ┛
        teeTop: new Rune(0x2533),       // ┳
        teeBottom: new Rune(0x253B),    // ┻
        teeLeft: new Rune(0x2523),      // ┣
        teeRight: new Rune(0x252B),     // ┫
        cross: new Rune(0x254B));       // ╋
}
