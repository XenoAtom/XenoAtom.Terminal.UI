// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Provides data for text document change events.
/// </summary>
public sealed class TextDocumentChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous document version.
    /// </summary>
    public required int OldVersion { get; init; }

    /// <summary>
    /// Gets the new document version.
    /// </summary>
    public required int NewVersion { get; init; }

    /// <summary>
    /// Gets the start position of the change.
    /// </summary>
    public required int Position { get; init; }

    /// <summary>
    /// Gets the number of removed characters.
    /// </summary>
    public required int RemovedLength { get; init; }

    /// <summary>
    /// Gets the number of inserted characters.
    /// </summary>
    public required int InsertedLength { get; init; }

    /// <summary>
    /// Gets the line count before the change.
    /// </summary>
    public required int OldLineCount { get; init; }

    /// <summary>
    /// Gets the line count after the change.
    /// </summary>
    public required int NewLineCount { get; init; }

    /// <summary>
    /// Gets a hint of the inserted text when available.
    /// </summary>
    public string? InsertedTextHint { get; init; }
}
