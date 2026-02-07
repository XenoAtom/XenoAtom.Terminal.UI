// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Provides a simple in-memory text document implementation.
/// </summary>
public sealed class TextDocument : ITextDocument
{
    private readonly TextPieceTable _table;
    private int _version;
    private TextSnapshot _snapshot;

    private int _updateDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextDocument"/> class.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public TextDocument(string? text = null)
    {
        _table = new TextPieceTable(text ?? string.Empty);
        _snapshot = _table.CreateSnapshot(_version);
    }

    /// <inheritdoc />
    public ITextSnapshot CurrentSnapshot => _snapshot;

    /// <inheritdoc />
    public int Version => _version;

    /// <inheritdoc />
    public event EventHandler<TextDocumentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    /// <inheritdoc />
    public void Insert(int position, ReadOnlySpan<char> text)
    {
        Replace(position, 0, text);
    }

    /// <inheritdoc />
    public void Remove(int position, int length)
    {
        Replace(position, length, ReadOnlySpan<char>.Empty);
    }

    /// <inheritdoc />
    public void Replace(int position, int length, ReadOnlySpan<char> text)
    {
        if (position < 0 || position > _table.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (length < 0 || position + length > _table.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var inserted = text.ToString();
        if (length == 0 && inserted.Length == 0)
        {
            return;
        }

        var oldVersion = _version;
        var oldLineCount = _snapshot.LineCount;

        _table.Replace(position, length, text);
        _version++;
        _snapshot = _table.CreateSnapshot(_version);

        RaiseChanged(new TextDocumentChangedEventArgs
        {
            OldVersion = oldVersion,
            NewVersion = _version,
            Position = position,
            RemovedLength = length,
            InsertedLength = inserted.Length,
            OldLineCount = oldLineCount,
            NewLineCount = _snapshot.LineCount,
            InsertedTextHint = inserted.Length == 0 ? null : inserted,
        });
    }

    internal void SetText(string text)
    {
        Replace(0, _table.Length, text.AsSpan());
    }

    internal string GetText() => _table.GetText();

    private void RaiseChanged(TextDocumentChangedEventArgs args)
    {
        if (_updateDepth > 0)
        {
            // V1 keeps the change events simple; batching can be added later.
        }

        Changed?.Invoke(this, args);
    }

    private sealed class UpdateScope : IDisposable
    {
        private TextDocument? _owner;

        public UpdateScope(TextDocument owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            var owner = _owner;
            if (owner is null)
            {
                return;
            }

            owner._updateDepth = Math.Max(0, owner._updateDepth - 1);
            _owner = null;
        }
    }
}
