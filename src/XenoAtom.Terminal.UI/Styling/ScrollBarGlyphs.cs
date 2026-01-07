// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Styling;

public readonly record struct ScrollBarGlyphs(Rune Track, Rune Thumb)
{
    public static ScrollBarGlyphs Default { get; } = new(
        Track: new Rune(0x2591), // ░
        Thumb: new Rune(0x2588)); // █
}
