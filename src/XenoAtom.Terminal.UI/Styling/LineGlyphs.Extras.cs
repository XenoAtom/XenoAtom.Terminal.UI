// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly partial record struct LineGlyphs
{
    /// <summary>
    /// Gets an ASCII border glyph set (<c>+ - |</c>).
    /// </summary>
    public static LineGlyphs Ascii { get; } = new(
        horizontal: new Rune('-'),      // -
        vertical: new Rune('|'),        // |
        topLeft: new Rune('+'),         // +
        topRight: new Rune('+'),        // +
        bottomLeft: new Rune('+'),      // +
        bottomRight: new Rune('+'),     // +
        teeTop: new Rune('+'),          // +
        teeBottom: new Rune('+'),       // +
        teeLeft: new Rune('+'),         // +
        teeRight: new Rune('+'),        // +
        cross: new Rune('+'));          // +

    /// <summary>
    /// Gets an ASCII border glyph set using <c>=</c> for the horizontal line.
    /// </summary>
    public static LineGlyphs AsciiHeavy { get; } = new(
        horizontal: new Rune('='),      // =
        vertical: new Rune('|'),        // |
        topLeft: new Rune('+'),         // +
        topRight: new Rune('+'),        // +
        bottomLeft: new Rune('+'),      // +
        bottomRight: new Rune('+'),     // +
        teeTop: new Rune('+'),          // +
        teeBottom: new Rune('+'),       // +
        teeLeft: new Rune('+'),         // +
        teeRight: new Rune('+'),        // +
        cross: new Rune('+'));          // +

    /// <summary>
    /// Gets a dashed single-line border glyph set (┄ ┆) using standard corners/connectors.
    /// </summary>
    public static LineGlyphs Dashed { get; } = new(
        horizontal: new Rune(0x2504),   // ┄
        vertical: new Rune(0x2506),     // ┆
        topLeft: new Rune(0x250C),      // ┌
        topRight: new Rune(0x2510),     // ┐
        bottomLeft: new Rune(0x2514),   // └
        bottomRight: new Rune(0x2518),  // ┘
        teeTop: new Rune(0x252C),       // ┬
        teeBottom: new Rune(0x2534),    // ┴
        teeLeft: new Rune(0x251C),      // ├
        teeRight: new Rune(0x2524),     // ┤
        cross: new Rune(0x253C));       // ┼
}