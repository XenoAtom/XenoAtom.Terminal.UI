// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines a set of glyphs used to draw line borders and separators.
/// </summary>
public readonly record struct LineGlyphs
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
        horizontal: new Rune(0x2500),   // Ä
        vertical: new Rune(0x2502),     // ³
        topLeft: new Rune(0x250C),      // Ú
        topRight: new Rune(0x2510),     // ¿
        bottomLeft: new Rune(0x2514),   // À
        bottomRight: new Rune(0x2518),  // Ù
        teeTop: new Rune(0x252C),       // Â
        teeBottom: new Rune(0x2534),    // Á
        teeLeft: new Rune(0x251C),      // Ã
        teeRight: new Rune(0x2524),     // ´
        cross: new Rune(0x253C));       // Å

    /// <summary>
    /// Gets a single-line border glyph set with rounded corners.
    /// </summary>
    public static LineGlyphs Rounded { get; } = new(
        horizontal: new Rune(0x2500),   // Ä
        vertical: new Rune(0x2502),     // ³
        topLeft: new Rune(0x256D),      // ╭
        topRight: new Rune(0x256E),     // ╮
        bottomLeft: new Rune(0x2570),   // ╰
        bottomRight: new Rune(0x256F),  // ╯
        teeTop: new Rune(0x252C),       // Â
        teeBottom: new Rune(0x2534),    // Á
        teeLeft: new Rune(0x251C),      // Ã
        teeRight: new Rune(0x2524),     // ´
        cross: new Rune(0x253C));       // Å

    /// <summary>
    /// Gets a double-line border glyph set.
    /// </summary>
    public static LineGlyphs Double { get; } = new(
        horizontal: new Rune(0x2550),   // Í
        vertical: new Rune(0x2551),     // º
        topLeft: new Rune(0x2554),      // É
        topRight: new Rune(0x2557),     // »
        bottomLeft: new Rune(0x255A),   // È
        bottomRight: new Rune(0x255D),  // ¼
        teeTop: new Rune(0x2566),       // Ë
        teeBottom: new Rune(0x2569),    // Ê
        teeLeft: new Rune(0x2560),      // Ì
        teeRight: new Rune(0x2563),     // ¹
        cross: new Rune(0x256C));       // Î

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
