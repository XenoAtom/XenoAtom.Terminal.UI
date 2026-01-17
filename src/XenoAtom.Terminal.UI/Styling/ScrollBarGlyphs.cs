// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Defines glyphs used for scroll bars.
/// </summary>
/// <param name="Track">The track glyph.</param>
/// <param name="Thumb">The thumb glyph.</param>
public readonly record struct ScrollBarGlyphs(Rune Track, Rune Thumb)
{
    /// <summary>
    /// Gets the default scroll bar glyphs.
    /// </summary>
    public static ScrollBarGlyphs Default { get; } = new(
        Track: new Rune(0x2591), // ░
        Thumb: new Rune(0x2588)); // █
}
