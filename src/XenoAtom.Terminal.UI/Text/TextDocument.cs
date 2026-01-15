// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Text;

public sealed class TextDocument : ITextDocument
{
    private readonly List<int> _lineStarts = new(capacity: 32);
    private string _text;
    private int _version;
    private TextSnapshot _snapshot;

    private int _updateDepth;

    public TextDocument(string? text = null)
    {
        _text = Normalize(text ?? string.Empty);
        RebuildLineStarts();
        _snapshot = new TextSnapshot(_version, _text, new List<int>(_lineStarts));
    }

    public ITextSnapshot CurrentSnapshot => _snapshot;

    public int Version => _version;

    public event EventHandler<TextDocumentChangedEventArgs>? Changed;

    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    public void Insert(int position, ReadOnlySpan<char> text)
    {
        Replace(position, 0, text);
    }

    public void Remove(int position, int length)
    {
        Replace(position, length, ReadOnlySpan<char>.Empty);
    }

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

        var inserted = Normalize(text);
        if (length == 0 && inserted.Length == 0)
        {
            return;
        }

        var oldVersion = _version;
        var oldLineCount = _lineStarts.Count;

        _text = string.Concat(_text.AsSpan(0, position), inserted.AsSpan(), _text.AsSpan(position + length));
        _version++;
        RebuildLineStarts();
        _snapshot = new TextSnapshot(_version, _text, new List<int>(_lineStarts));

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

    private static string Normalize(ReadOnlySpan<char> text)
    {
        if (!text.Contains('\r'))
        {
            return text.ToString();
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                builder.Append('\n');
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private void RebuildLineStarts()
    {
        _lineStarts.Clear();
        _lineStarts.Add(0);

        for (var i = 0; i < _text.Length; i++)
        {
            if (_text[i] == '\n')
            {
                _lineStarts.Add(i + 1);
            }
        }

        if (_lineStarts.Count == 0)
        {
            _lineStarts.Add(0);
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
