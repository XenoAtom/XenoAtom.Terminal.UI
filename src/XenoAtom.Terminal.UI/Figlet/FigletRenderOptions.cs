// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Figlet;

/// <summary>
/// Options that control how FIGlet text is rendered.
/// </summary>
/// <param name="LetterSpacing">The number of spaces inserted between characters.</param>
/// <param name="TrimTrailingSpaces">Whether trailing spaces are trimmed on each output line.</param>
/// <param name="MissingGlyph">The character used when the font does not define a glyph for the input character.</param>
public readonly record struct FigletRenderOptions(
    int LetterSpacing = 1,
    bool TrimTrailingSpaces = true,
    char MissingGlyph = '?');

