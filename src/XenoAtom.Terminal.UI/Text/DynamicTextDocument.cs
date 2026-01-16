// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Text;

/// <summary>
/// A simple <see cref="ITextDocument"/> implementation backed by user-provided delegates.
/// It is designed to bridge a bindable <c>Text</c> property with the text editor infrastructure.
/// </summary>
internal sealed class DynamicTextDocument : ITextDocument
{
    private readonly Func<string> _getter;
    private readonly Action<string> _setter;

    private bool _initialized;
    private string _text = string.Empty;
    private int _version;
    private TextSnapshot _snapshot = new(version: 0, text: string.Empty, lineStarts: [0], lineBreakLengths: [0]);
    private readonly List<int> _lineStarts = new(capacity: 32);
    private readonly List<byte> _lineBreakLengths = new(capacity: 32);

    private int _updateDepth;

    public DynamicTextDocument(Func<string> getter, Action<string> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        _getter = getter;
        _setter = setter;
    }

    public ITextSnapshot CurrentSnapshot
    {
        get
        {
            EnsureFresh();
            return _snapshot;
        }
    }

    public int Version
    {
        get
        {
            EnsureFresh();
            return _version;
        }
    }

    public event EventHandler<TextDocumentChangedEventArgs>? Changed;

    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    public void Insert(int position, ReadOnlySpan<char> text)
        => Replace(position, length: 0, text);

    public void Remove(int position, int length)
        => Replace(position, length, ReadOnlySpan<char>.Empty);

    public void Replace(int position, int length, ReadOnlySpan<char> text)
    {
        EnsureFresh();

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

        var oldText = _text;
        var oldVersion = _version;
        var oldLineCount = _lineStarts.Count;

        var updated = string.Concat(oldText.AsSpan(0, position), inserted.AsSpan(), oldText.AsSpan(position + length));
        _setter(updated);

        // Read back through the getter to support bindings/state-based setters.
        updated = _getter();
        SetTextInternal(updated, incrementVersion: true);

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

    private void EnsureFresh()
    {
        var text = _getter();
        if (!_initialized)
        {
            SetTextInternal(text, incrementVersion: false);
            _initialized = true;
            return;
        }

        if (string.Equals(text, _text, StringComparison.Ordinal))
        {
            return;
        }

        SetTextInternal(text, incrementVersion: true);
    }

    private void SetTextInternal(string text, bool incrementVersion)
    {
        _text = text;
        if (incrementVersion)
        {
            _version++;
        }

        RebuildLineStarts();
        _snapshot = new TextSnapshot(_version, _text, new List<int>(_lineStarts), new List<byte>(_lineBreakLengths));
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
        private DynamicTextDocument? _owner;

        public UpdateScope(DynamicTextDocument owner)
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
