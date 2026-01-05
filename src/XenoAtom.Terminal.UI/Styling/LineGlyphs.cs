// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

public readonly record struct LineGlyphs(
    char Horizontal,
    char Vertical,
    char TopLeft,
    char TopRight,
    char BottomLeft,
    char BottomRight,
    char TeeTop,
    char TeeBottom,
    char TeeLeft,
    char TeeRight,
    char Cross)
{
    public static LineGlyphs Single { get; } = new(
        Horizontal: '─',
        Vertical: '│',
        TopLeft: '┌',
        TopRight: '┐',
        BottomLeft: '└',
        BottomRight: '┘',
        TeeTop: '┬',
        TeeBottom: '┴',
        TeeLeft: '├',
        TeeRight: '┤',
        Cross: '┼');
}

