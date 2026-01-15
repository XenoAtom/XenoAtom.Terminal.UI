// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

public interface ITextDocument
{
    ITextSnapshot CurrentSnapshot { get; }
    int Version { get; }

    IDisposable BeginUpdate();

    void Insert(int position, ReadOnlySpan<char> text);
    void Remove(int position, int length);
    void Replace(int position, int length, ReadOnlySpan<char> text);

    event EventHandler<TextDocumentChangedEventArgs> Changed;
}
