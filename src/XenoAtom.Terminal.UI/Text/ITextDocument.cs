// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Represents a mutable text document that provides snapshots and change notifications.
/// </summary>
public interface ITextDocument
{
    /// <summary>
    /// Gets the current snapshot of the document.
    /// </summary>
    ITextSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Gets the current version number for the document.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Begins a batch update scope.
    /// </summary>
    /// <returns>A disposable scope that ends the update when disposed.</returns>
    IDisposable BeginUpdate();

    /// <summary>
    /// Inserts text at the specified position.
    /// </summary>
    /// <param name="position">The insertion index.</param>
    /// <param name="text">The text to insert.</param>
    void Insert(int position, ReadOnlySpan<char> text);

    /// <summary>
    /// Removes text from the document.
    /// </summary>
    /// <param name="position">The start index.</param>
    /// <param name="length">The number of characters to remove.</param>
    void Remove(int position, int length);

    /// <summary>
    /// Replaces text in the document.
    /// </summary>
    /// <param name="position">The start index.</param>
    /// <param name="length">The number of characters to replace.</param>
    /// <param name="text">The replacement text.</param>
    void Replace(int position, int length, ReadOnlySpan<char> text);

    /// <summary>
    /// Occurs when the document content changes.
    /// </summary>
    event EventHandler<TextDocumentChangedEventArgs> Changed;
}
