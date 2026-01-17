// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Represents a position within text.
/// </summary>
/// <param name="Index">The character index.</param>
public readonly record struct TextPosition(int Index);

/// <summary>
/// Represents a contiguous text range.
/// </summary>
/// <param name="Start">The start index.</param>
/// <param name="Length">The length of the range.</param>
public readonly record struct TextRange(int Start, int Length)
{
    /// <summary>
    /// Gets the end index of the range.
    /// </summary>
    public int End => Start + Length;
}
