// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// Provides a simple in-memory text document implementation.
/// </summary>
public sealed class TextDocument : ITextDocument
{
    private readonly List<int> _lineStarts = new(capacity: 32);
    private readonly List<byte> _lineBreakLengths = new(capacity: 32);
    private string _text;
    private int _version;
    private TextSnapshot _snapshot;

    private int _updateDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextDocument"/> class.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public TextDocument(string? text = null)
    {
        _text = text ?? string.Empty;
        RebuildLineStarts();
        _snapshot = new TextSnapshot(_version, _text, _lineStarts, _lineBreakLengths);
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
        if (position < 0 || position > _text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (length < 0 || position + length > _text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var inserted = text.ToString();
        if (length == 0 && inserted.Length == 0)
        {
            return;
        }

        var oldVersion = _version;
        var oldLineCount = _lineStarts.Count;

        _text = string.Concat(_text.AsSpan(0, position), inserted.AsSpan(), _text.AsSpan(position + length));
        _version++;
        RebuildLineStarts();
        _snapshot = new TextSnapshot(_version, _text, _lineStarts, _lineBreakLengths);

        RaiseChanged(new TextDocumentChangedEventArgs
        {
            OldVersion = oldVersion,
            NewVersion = _version,
            Position = position,
            RemovedLength = length,
            InsertedLength = inserted.Length,
            OldLineCount = oldLineCount,
            NewLineCount = _lineStarts.Count,
            InsertedTextHint = inserted.Length == 0 ? null : inserted,
        });
    }

    internal void SetText(string text)
    {
        Replace(0, _text.Length, text.AsSpan());
    }

    internal string GetText() => _text;

    private void RaiseChanged(TextDocumentChangedEventArgs args)
    {
        if (_updateDepth > 0)
        {
            // V1 keeps the change events simple; batching can be added later.
        }

        Changed?.Invoke(this, args);
    }

    private void RebuildLineStarts()
    {
        _lineStarts.Clear();
        _lineStarts.Add(0);
        _lineBreakLengths.Clear();
        _lineBreakLengths.Add(0);

        for (var i = 0; i < _text.Length; i++)
        {
            var ch = _text[i];
            if (ch == '\r')
            {
                _lineBreakLengths[^1] = (byte)(i + 1 < _text.Length && _text[i + 1] == '\n' ? 2 : 1);
                if (i + 1 < _text.Length && _text[i + 1] == '\n')
                {
                    i++;
                }

                _lineStarts.Add(i + 1);
                _lineBreakLengths.Add(0);
            }
            else if (ch == '\n')
            {
                _lineBreakLengths[^1] = 1;
                _lineStarts.Add(i + 1);
                _lineBreakLengths.Add(0);
            }
        }

        if (_lineStarts.Count == 0)
        {
            _lineStarts.Add(0);
            _lineBreakLengths.Add(0);
        }
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
