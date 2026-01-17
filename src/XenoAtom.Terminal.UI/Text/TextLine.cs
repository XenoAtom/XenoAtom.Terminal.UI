// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Represents a line of text within a snapshot.
/// </summary>
public readonly struct TextLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextLine"/> struct.
    /// </summary>
    /// <param name="index">The line index.</param>
    /// <param name="start">The start position in the document.</param>
    /// <param name="length">The line length excluding line break.</param>
    /// <param name="lineBreakLength">The length of the line break sequence.</param>
    public TextLine(int index, int start, int length, int lineBreakLength)
    {
        Index = index;
        Start = start;
        Length = length;
        LineBreakLength = lineBreakLength;
    }

    /// <summary>
    /// Gets the line index.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the start position of the line.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the length of the line excluding the line break.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the length of the line break sequence.
    /// </summary>
    public int LineBreakLength { get; }

    /// <summary>
    /// Gets the end position of the line excluding the line break.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Gets the end position of the line including the line break.
    /// </summary>
    public int EndIncludingBreak => End + LineBreakLength;
}
