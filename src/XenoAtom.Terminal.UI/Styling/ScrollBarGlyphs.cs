// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public readonly record struct ScrollBarGlyphs(char Track, char Thumb)
{
    public static ScrollBarGlyphs Default { get; } = new(
        Track: '░',
        Thumb: '█');
}

