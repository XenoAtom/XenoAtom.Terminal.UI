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
    private string _sourceText = string.Empty;
    private TextPieceTable _table = new(string.Empty);
    private int _version;
    private TextSnapshot _snapshot;

    private int _updateDepth;

    public DynamicTextDocument(Func<string> getter, Action<string> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        _getter = getter;
        _setter = setter;
        _snapshot = _table.CreateSnapshot(version: 0);
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
        var updated = _table.GetText();
        _sourceText = updated;

        _setter(updated);

        // Read back through the getter to support bindings/state-based setters.
        var received = _getter();
        if (string.Equals(received, updated, StringComparison.Ordinal))
        {
            _sourceText = received;
            _version++;
            _snapshot = _table.CreateSnapshot(_version);
        }
        else
        {
            SetTextInternal(received, incrementVersion: true);
        }

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

    private void EnsureFresh()
    {
        var text = _getter();
        if (!_initialized)
        {
            SetTextInternal(text, incrementVersion: false);
            _initialized = true;
            return;
        }

        if (string.Equals(text, _sourceText, StringComparison.Ordinal))
        {
            return;
        }

        SetTextInternal(text, incrementVersion: true);
    }

    private void SetTextInternal(string text, bool incrementVersion)
    {
        _sourceText = text;
        _table = new TextPieceTable(text);
        if (incrementVersion)
        {
            _version++;
        }

        _snapshot = _table.CreateSnapshot(_version);
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
