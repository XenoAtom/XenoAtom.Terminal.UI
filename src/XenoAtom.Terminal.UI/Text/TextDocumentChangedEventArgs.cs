// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

public sealed class TextDocumentChangedEventArgs : EventArgs
{
    public required int OldVersion { get; init; }
    public required int NewVersion { get; init; }
    public required int Position { get; init; }
    public required int RemovedLength { get; init; }
    public required int InsertedLength { get; init; }
    public required int OldLineCount { get; init; }
    public required int NewLineCount { get; init; }

    public string? InsertedTextHint { get; init; }
}
