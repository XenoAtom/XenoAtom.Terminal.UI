// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Represents an immutable snapshot of a text document at a point in time.
/// </summary>
public interface ITextSnapshot
{
    /// <summary>
    /// Gets the snapshot version.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the total length of the text.
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Gets the number of lines in the snapshot.
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Gets the character at the specified index.
    /// </summary>
    /// <param name="index">The character index.</param>
    char this[int index] { get; }

    /// <summary>
    /// Gets a line by index.
    /// </summary>
    /// <param name="lineIndex">The line index.</param>
    TextLine GetLine(int lineIndex);

    /// <summary>
    /// Gets the line index that contains the specified position.
    /// </summary>
    /// <param name="position">The character position.</param>
    int GetLineIndexFromPosition(int position);

    /// <summary>
    /// Copies a range of characters into the destination span.
    /// </summary>
    /// <param name="start">The start index.</param>
    /// <param name="destination">The destination span.</param>
    void CopyTo(int start, Span<char> destination);
}
