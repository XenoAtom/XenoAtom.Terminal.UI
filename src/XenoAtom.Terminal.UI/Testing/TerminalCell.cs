// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Testing;

/// <summary>
/// A cell of a rendered frame, as read through <see cref="TerminalScreen"/>.
/// </summary>
/// <param name="Text">
/// The text displayed in the cell: one grapheme (which may span several code points, for example an emoji sequence),
/// a space for a blank cell, and a space for the second cell of a wide glyph (see <paramref name="IsWideContinuation"/>).
/// </param>
/// <param name="Style">The cell style (foreground, background and text decorations).</param>
/// <param name="IsWideContinuation">
/// <see langword="true"/> when the cell is the second half of a glyph two cells wide; the glyph itself is in the cell to the left.
/// </param>
public readonly record struct TerminalCell(string Text, Style Style, bool IsWideContinuation);
