// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Internal undo/redo manager for <see cref="TextEditorCore"/> based editors.
/// </summary>
/// <remarks>
/// This is a v1 implementation that focuses on correctness and low complexity.
/// It records document replacements (insert/remove/replace) as undo entries and can coalesce consecutive typing.
/// </remarks>
internal sealed class TextUndoRedoManager
{
    private const int TypingMergeWindowMilliseconds = 500;

    private readonly List<UndoEntry> _undo = new(capacity: 64);
    private readonly List<UndoEntry> _redo = new(capacity: 16);

    private ITextDocument? _document;
    private int _knownDocumentVersion;

    private bool _isApplying;
    private bool _isRecording;

    private int _maxEntries = 200;

    private int _groupDepth;
    private TextUndoKind _groupKind;
    private TextEditorStateSnapshot _groupBefore;
    private readonly List<TextChange> _groupChanges = new(capacity: 32);

    private IUndoClock _clock = EnvironmentUndoClock.Instance;

    public event Action? StateChanged;

    public bool Enabled { get; set; } = true;

    public int MaxEntries
    {
        get => _maxEntries;
        set
        {
            _maxEntries = Math.Max(0, value);
            TrimUndoToMax();
        }
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    internal void SetClockForTests(IUndoClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    public void Attach(ITextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (ReferenceEquals(_document, document))
        {
            return;
        }

        if (_document is not null)
        {
            _document.Changed -= OnDocumentChanged;
        }

        _document = document;
        _knownDocumentVersion = document.Version;
        _document.Changed += OnDocumentChanged;
        Clear();
    }

    public void Detach()
    {
        if (_document is not null)
        {
            _document.Changed -= OnDocumentChanged;
        }

        _document = null;
        _knownDocumentVersion = 0;
        Clear();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _groupDepth = 0;
        _groupChanges.Clear();
        OnStateChanged();
    }

    public void EnsureSynchronized()
    {
        var document = _document;
        if (document is null)
        {
            return;
        }

        var version = document.Version;
        if (version == _knownDocumentVersion)
        {
            return;
        }

        // The document changed without going through the undo manager.
        _knownDocumentVersion = version;
        Clear();
    }

    public RecordingScope BeginRecording()
    {
        _isRecording = true;
        return new RecordingScope(this);
    }

    public ApplyingScope BeginApplying()
    {
        _isApplying = true;
        return new ApplyingScope(this);
    }

    public UndoEntry Undo()
    {
        if (_undo.Count == 0)
        {
            throw new InvalidOperationException("No undo entries available.");
        }

        var entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(entry);
        OnStateChanged();
        return entry;
    }

    public UndoEntry Redo()
    {
        if (_redo.Count == 0)
        {
            throw new InvalidOperationException("No redo entries available.");
        }

        var entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(entry);
        OnStateChanged();
        return entry;
    }

    public void BeginGroup(TextUndoKind kind, TextEditorStateSnapshot before)
    {
        if (!Enabled)
        {
            return;
        }

        if (_groupDepth == 0)
        {
            _groupKind = kind;
            _groupBefore = before;
            _groupChanges.Clear();
        }

        _groupDepth++;
    }

    public bool HasOpenGroup => _groupDepth > 0;

    public void AbortGroup()
    {
        _groupDepth = 0;
        _groupChanges.Clear();
    }

    public void AddGroupChange(TextChange change)
    {
        if (!Enabled)
        {
            return;
        }

        if (_groupDepth <= 0)
        {
            throw new InvalidOperationException("No undo group active.");
        }

        _groupChanges.Add(change);
    }

    public void CommitGroup(TextEditorStateSnapshot after)
    {
        if (!Enabled)
        {
            return;
        }

        if (_groupDepth <= 0)
        {
            throw new InvalidOperationException("No undo group active.");
        }

        _groupDepth--;
        if (_groupDepth > 0)
        {
            return;
        }

        if (_groupChanges.Count == 0)
        {
            return;
        }

        CommitEntry(new UndoEntry(_groupKind, _groupBefore, after, _clock.NowMilliseconds, [.. _groupChanges]));
        _groupChanges.Clear();
    }

    public void RecordSingle(TextUndoKind kind, TextChange change, TextEditorStateSnapshot before, TextEditorStateSnapshot after, bool allowCoalesce)
    {
        if (!Enabled)
        {
            return;
        }

        if (_groupDepth > 0)
        {
            AddGroupChange(change);
            return;
        }

        if (allowCoalesce && TryCoalesce(kind, change, after))
        {
            return;
        }

        CommitEntry(new UndoEntry(kind, before, after, _clock.NowMilliseconds, [change]));
    }

    private void CommitEntry(UndoEntry entry)
    {
        _redo.Clear();
        _undo.Add(entry);

        TrimUndoToMax();

        OnStateChanged();
    }

    private void TrimUndoToMax()
    {
        if (_maxEntries <= 0)
        {
            _undo.Clear();
            _redo.Clear();
            return;
        }

        while (_undo.Count > _maxEntries)
        {
            _undo.RemoveAt(0);
        }
    }

    private bool TryCoalesce(TextUndoKind kind, TextChange change, TextEditorStateSnapshot after)
    {
        if (kind != TextUndoKind.Typing || _undo.Count == 0)
        {
            return false;
        }

        var last = _undo[^1];
        if (last.Kind != TextUndoKind.Typing || last.Changes.Length != 1)
        {
            return false;
        }

        if (change.RemovedText.Length != 0)
        {
            return false;
        }

        var delta = _clock.NowMilliseconds - last.TimestampMilliseconds;
        if (delta < 0 || delta > TypingMergeWindowMilliseconds)
        {
            return false;
        }

        var previousChange = last.Changes[0];
        if (previousChange.RemovedText.Length != 0)
        {
            return false;
        }

        if (change.Position != previousChange.Position + previousChange.InsertedText.Length)
        {
            return false;
        }

        var merged = previousChange with { InsertedText = previousChange.InsertedText + change.InsertedText };
        _undo[^1] = last with
        {
            TimestampMilliseconds = _clock.NowMilliseconds,
            After = after,
            Changes = [merged],
        };
        OnStateChanged();
        return true;
    }

    private void OnDocumentChanged(object? sender, TextDocumentChangedEventArgs args)
    {
        if (_isApplying || _isRecording)
        {
            _knownDocumentVersion = args.NewVersion;
            return;
        }

        _knownDocumentVersion = args.NewVersion;
        Clear();
    }

    private void OnStateChanged() => StateChanged?.Invoke();

    public readonly record struct UndoEntry(
        TextUndoKind Kind,
        TextEditorStateSnapshot Before,
        TextEditorStateSnapshot After,
        int TimestampMilliseconds,
        TextChange[] Changes);

    public readonly record struct TextChange(int Position, string RemovedText, string InsertedText);

    public readonly record struct TextEditorStateSnapshot(
        int CaretIndex,
        int SelectionAnchor,
        int SelectionEnd,
        int ScrollX,
        int ScrollY,
        int PreferredColumn);

    public enum TextUndoKind
    {
        Typing,
        Paste,
        Delete,
        Replace,
        ReplaceAll,
        Kill,
    }

    internal interface IUndoClock
    {
        int NowMilliseconds { get; }
    }

    private sealed class EnvironmentUndoClock : IUndoClock
    {
        public static EnvironmentUndoClock Instance { get; } = new();

        public int NowMilliseconds => Environment.TickCount;
    }

    public readonly struct RecordingScope : IDisposable
    {
        private readonly TextUndoRedoManager? _owner;

        public RecordingScope(TextUndoRedoManager owner) => _owner = owner;

        public void Dispose()
        {
            if (_owner is not null)
            {
                _owner._isRecording = false;
            }
        }
    }

    public readonly struct ApplyingScope : IDisposable
    {
        private readonly TextUndoRedoManager? _owner;

        public ApplyingScope(TextUndoRedoManager owner) => _owner = owner;

        public void Dispose()
        {
            if (_owner is not null)
            {
                _owner._isApplying = false;
            }
        }
    }
}
