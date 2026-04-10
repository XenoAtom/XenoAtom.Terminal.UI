// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

internal delegate void TextSegmentWriter(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn);

internal readonly record struct TextEditorOptions(
    bool SingleLine,
    bool AcceptsReturn,
    bool AcceptsTab,
    bool WordWrap,
    int TabSize,
    TextAlignment Alignment,
    bool ShowPlaceholderWhenUnfocusedOnly);

internal readonly record struct TextEditorRenderContext(
    CellBuffer Buffer,
    Rectangle ContentRect,
    Style TextStyle,
    Style SelectionStyle,
    Style PlaceholderStyle,
    string? Placeholder,
    bool IsFocused,
    TextSegmentWriter SegmentWriter);

internal interface ITextEditorHost
{
    TerminalApp? App { get; }
    bool IsFocused { get; }
    bool TryOpenSearchReplacePopup(SearchReplaceMode mode, string? initialSearchText);
}

internal sealed partial class TextEditorCore
{
    private readonly ITextEditorHost _host;
    private ITextDocument _document;
    private readonly ScrollModel _scroll;
    private readonly TextUndoRedoManager _undoRedo;

    private string _cachedText = string.Empty;
    private int _cachedVersion = -1;

    private int _caretIndex;
    private int _selectionAnchor = -1;
    private int _selectionEnd = -1;
    private int _preferredColumn = -1;
    private string? _killBuffer;

    private int _contentX;
    private int _contentY;
    private int _contentWidth;
    private int _contentHeight;

    private bool _draggingSelection;
    private bool _hasCachedVisualPosition;
    private CachedVisualPosition _cachedVisualPosition;
    private WrappedLineBoundaryMoveKind _wrappedLineBoundaryMove;

    private SearchQuery _searchQuery;
    private readonly List<TextMatch> _searchMatches = new(32);
    private int _activeSearchMatchIndex = -1;
    private string? _searchError;

    private readonly record struct CachedVisualPosition(
        int SnapshotVersion,
        int Index,
        bool WordWrap,
        int ContentWidth,
        int TabSize,
        int Row,
        int Column);

    private enum WrappedLineBoundaryMoveKind
    {
        None,
        Home,
        End,
    }

    public TextEditorCore(ITextEditorHost host, ITextDocument document, ScrollModel scroll, TextUndoRedoManager undoRedo)
    {
        _host = host;
        _document = document;
        _scroll = scroll;
        _undoRedo = undoRedo;
    }

    [Bindable]
    public partial int Version { get; private set; }

    private void IncrementVersion()
    {
        // `Version++` would read+write the bindable property in the same tracking context, which the binding system
        // forbids to prevent dependency loops. Use the generated backing field directly instead.
        Version = unchecked(_version + 1);
    }

    private static int NormalizeIndexToTextElementBoundary(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        if (index == 0 || index == text.Length)
        {
            return index;
        }

        var prev = GetPreviousTextElementIndexFast(text, index);
        if (prev == index)
        {
            return index;
        }

        var next = GetNextTextElementIndexFast(text, prev);
        if (index == next)
        {
            return index;
        }

        // Index points inside a grapheme cluster, snap to the end of that cluster.
        return next;
    }

    public void SetDocument(ITextDocument document)
    {
        _document = document;
        _cachedVersion = -1;
        ResetLayoutCache();
        InvalidateVisualPositionCache();
        OnDocumentChanged();
    }

    public int CaretIndex => _caretIndex;

    public void SetCaretIndex(int value, in TextEditorOptions options)
    {
        var text = GetText().AsSpan();
        var textLength = text.Length;
        _caretIndex = NormalizeIndexToTextElementBoundary(text, Math.Clamp(value, 0, textLength));
        ClearSelection();
        _preferredColumn = -1;
        ResetWrappedLineBoundaryMove();
        EnsureCaretVisible(options);
        IncrementVersion();
    }

    private string GetText()
    {
        var version = _document.Version;
        if (version != _cachedVersion)
        {
            _cachedText = TextDocumentUtility.GetText(_document);
            _cachedVersion = version;
        }

        return _cachedText;
    }

    private void InvalidateVisualPositionCache()
    {
        _hasCachedVisualPosition = false;
        _cachedVisualPosition = default;
    }

    private void ResetWrappedLineBoundaryMove()
    {
        _wrappedLineBoundaryMove = WrappedLineBoundaryMoveKind.None;
    }

    private bool TryGetCachedVisualPosition(int snapshotVersion, int index, in TextEditorOptions options, out (int Row, int Column) position)
    {
        if (!_hasCachedVisualPosition
            || _cachedVisualPosition.SnapshotVersion != snapshotVersion
            || _cachedVisualPosition.Index != index
            || _cachedVisualPosition.WordWrap != options.WordWrap
            || _cachedVisualPosition.ContentWidth != _contentWidth
            || _cachedVisualPosition.TabSize != options.TabSize)
        {
            position = default;
            return false;
        }

        position = (_cachedVisualPosition.Row, _cachedVisualPosition.Column);
        return true;
    }

    private void CacheVisualPosition(int snapshotVersion, int index, in TextEditorOptions options, int row, int column)
    {
        _cachedVisualPosition = new CachedVisualPosition(
            snapshotVersion,
            index,
            options.WordWrap,
            _contentWidth,
            options.TabSize,
            row,
            column);
        _hasCachedVisualPosition = true;
    }

    private bool HasSelection => _selectionAnchor >= 0 && _selectionEnd >= 0 && _selectionAnchor != _selectionEnd;

    internal bool HasSelectionForSelectionOwner => HasSelection;

    internal void ClearSelectionForSelectionOwner()
    {
        if (!HasSelection)
        {
            return;
        }

        ClearSelection();
        IncrementVersion();
    }

    internal bool TryGetSelectionText(out string text)
    {
        var span = GetSelectedTextSpan(GetText().AsSpan());
        if (span.IsEmpty)
        {
            text = string.Empty;
            return false;
        }

        text = span.ToString();
        return true;
    }

    public bool UpdateViewport(Rectangle contentRect)
    {
        var width = Math.Max(0, contentRect.Width);
        var height = Math.Max(0, contentRect.Height);
        var viewportChanged = width != _contentWidth || height != _contentHeight;

        _contentX = contentRect.X;
        _contentY = contentRect.Y;
        _contentWidth = width;
        _contentHeight = height;
        _scroll.SetViewport(_contentWidth, _contentHeight);
        return viewportChanged;
    }

    public void UpdateLayout(Rectangle contentRect, in TextEditorOptions options)
    {
        var viewportChanged = UpdateViewport(contentRect);

        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            _scroll.SetExtent(0, 0);
            return;
        }

        if (options.SingleLine)
        {
            var text = GetText();
            var totalCells = GetTextCells(text.AsSpan(), options.TabSize);
            _scroll.SetExtent(Math.Max(totalCells, _contentWidth), 1);
            if (totalCells <= _contentWidth)
            {
                _scroll.SetOffset(0, 0);
            }

            if (viewportChanged)
            {
                EnsureCaretVisible(options);
            }
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        var totalRows = ComputeExtent(snapshot, options, out var extentWidth);
        _scroll.SetExtent(extentWidth, totalRows);
        if (options.WordWrap)
        {
            _scroll.SetOffset(0, _scroll.OffsetY);
        }

        if (viewportChanged)
        {
            EnsureCaretVisible(options);
        }
    }

    private void UpdateExtent(in TextEditorOptions options)
    {
        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        if (options.SingleLine)
        {
            var text = GetText();
            var totalCells = GetTextCells(text.AsSpan(), options.TabSize);
            _scroll.SetExtent(Math.Max(totalCells, _contentWidth), 1);
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        var totalRows = ComputeExtent(snapshot, options, out var extentWidth);
        _scroll.SetExtent(extentWidth, totalRows);
        if (options.WordWrap)
        {
            _scroll.SetOffset(0, _scroll.OffsetY);
        }
    }

    private void UpdateAfterDocumentChange(in TextEditorOptions options)
    {
        UpdateExtent(options);
        EnsureCaretVisible(options);
        IncrementVersion();
    }

    public int SelectionStart
        => HasSelection ? Math.Min(_selectionAnchor, _selectionEnd) : _caretIndex;

    public int SelectionLength
        => HasSelection ? Math.Abs(_selectionEnd - _selectionAnchor) : 0;

    internal TextEditorLineLayoutDiagnostics GetLineLayoutDiagnostics(int lineIndex, in TextEditorOptions options)
    {
        if (!options.SingleLine && _contentWidth > 0)
        {
            EnsureMultiLineLayoutCache(options);
        }

        return _layoutCache.GetLineDiagnostics(lineIndex);
    }

    public void OnDocumentChanged()
    {
        var textLength = GetText().Length;
        if (_caretIndex > textLength)
        {
            _caretIndex = textLength;
            _preferredColumn = -1;
        }

        if (_selectionAnchor > textLength) _selectionAnchor = textLength;
        if (_selectionEnd > textLength) _selectionEnd = textLength;

        InvalidateVisualPositionCache();
        ResetWrappedLineBoundaryMove();
        Version = _version + 1;
    }

    public void OnDocumentChanged(TextDocumentChangedEventArgs e)
    {
        NoteLayoutChange(e);
        OnDocumentChanged();
    }

    public void Render(in TextEditorRenderContext context, in TextEditorOptions options)
    {
        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        if (options.SingleLine)
        {
            RenderSingleLine(context, options);
        }
        else
        {
            RenderMultiLine(context, options);
        }
    }

    public bool TryGetCursorCell(in TextEditorOptions options, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (!options.SingleLine && _contentHeight <= 0)
        {
            return false;
        }

        if (_contentWidth <= 0)
        {
            return false;
        }

        var text = GetText();
        var caret = Math.Clamp(_caretIndex, 0, text.Length);

        if (options.SingleLine)
        {
            var caretCells = GetCellOffsetAtIndex(text.AsSpan(), caret, options.TabSize);
            var xCell = caretCells - _scroll.OffsetX;
            xCell = Math.Clamp(xCell, 0, _contentWidth);

            var alignedOffset = 0;
            if (_scroll.OffsetX == 0)
            {
                var totalCells = GetTextCells(text.AsSpan(), options.TabSize);
                if (totalCells <= _contentWidth && options.Alignment is TextAlignment.Center or TextAlignment.Right)
                {
                    alignedOffset = options.Alignment == TextAlignment.Center
                        ? (_contentWidth - totalCells) / 2
                        : (_contentWidth - totalCells);
                }
            }

            x = _contentX + alignedOffset + xCell;
            y = _contentY;
            return true;
        }

        var (row, col) = GetVisualPosition(text.AsSpan(), caret, options);
        var visibleRow = row - _scroll.OffsetY;
        var visibleCol = col - (options.WordWrap ? 0 : _scroll.OffsetX);
        if ((uint)visibleRow >= (uint)_contentHeight || (uint)visibleCol >= (uint)_contentWidth)
        {
            return false;
        }

        x = _contentX + visibleCol;
        y = _contentY + visibleRow;
        return true;
    }

    public void OnTextInput(TextInputEventArgs e, in TextEditorOptions options)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        ResetWrappedLineBoundaryMove();
        InsertText(e.Text, TextUndoRedoManager.TextUndoKind.Typing, allowCoalesce: true, options);
        e.Handled = true;
    }

    public void OnPaste(PasteEventArgs e, in TextEditorOptions options)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        ResetWrappedLineBoundaryMove();
        InsertText(e.Text, TextUndoRedoManager.TextUndoKind.Paste, allowCoalesce: false, options);
        e.Handled = true;
    }
    public void OnPointerPressed(PointerEventArgs e, in TextEditorOptions options)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        var index = GetIndexFromPointer(e.UiX, e.UiY, options);

        if (e.ClickCount >= 2)
        {
            SelectWordAt(index);
            _caretIndex = _selectionEnd >= 0 ? _selectionEnd : index;
            _preferredColumn = -1;
            EnsureCaretVisible(options);
            IncrementVersion();
            e.Handled = true;
            return;
        }

        _draggingSelection = true;
        if ((e.Modifiers & TerminalModifiers.Shift) != 0)
        {
            ExtendSelection(index);
        }
        else
        {
            ClearSelection();
        }

        _caretIndex = index;
        _preferredColumn = -1;
        ResetWrappedLineBoundaryMove();
        EnsureCaretVisible(options);
        IncrementVersion();
        e.Handled = true;
    }

    public void OnPointerMoved(PointerEventArgs e, in TextEditorOptions options)
    {
        if (!_draggingSelection)
        {
            return;
        }

        var index = GetIndexFromPointer(e.UiX, e.UiY, options);
        ExtendSelection(index);
        _caretIndex = index;
        _preferredColumn = -1;
        ResetWrappedLineBoundaryMove();
        EnsureCaretVisible(options);
        IncrementVersion();
        e.Handled = true;
    }

    public void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_draggingSelection)
        {
            _draggingSelection = false;
            ResetWrappedLineBoundaryMove();
            e.Handled = true;
        }
    }

    public void OnKeyDown(KeyEventArgs e, in TextEditorOptions options)
    {
        var text = GetText();
        _caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        var ctrl = (e.Modifiers & TerminalModifiers.Ctrl) != 0;
        var shift = (e.Modifiers & TerminalModifiers.Shift) != 0;

        if (!(options.WordWrap && !options.SingleLine && !ctrl && e.Key is TerminalKey.Home or TerminalKey.End))
        {
            ResetWrappedLineBoundaryMove();
        }

        if (ctrl)
        {
            if (e.Char is TerminalChar.CtrlZ)
            {
                Undo(options);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlR)
            {
                Redo(options);
                e.Handled = true;
                return;
            }
        }

        if (ctrl && !options.SingleLine)
        {
            if (e.Char is TerminalChar.CtrlF)
            {
                var selection = GetSelectedTextSpan(text.AsSpan());
                var initial = selection.IsEmpty ? null : selection.ToString();
                _host.TryOpenSearchReplacePopup(SearchReplaceMode.Find, initial);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlH)
            {
                var selection = GetSelectedTextSpan(text.AsSpan());
                var initial = selection.IsEmpty ? null : selection.ToString();
                _host.TryOpenSearchReplacePopup(SearchReplaceMode.Replace, initial);
                e.Handled = true;
                return;
            }
        }

        if (!shift && HasSelection && e.Key is TerminalKey.Left or TerminalKey.Right or TerminalKey.Home or TerminalKey.End
            or TerminalKey.Up or TerminalKey.Down)
        {
            ClearSelection();
        }

        if (ctrl)
        {
            if (e.Char is TerminalChar.CtrlA)
            {
                SelectAll();
                EnsureCaretVisible(options);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlV)
            {
                var clip = _host.App?.Terminal.Clipboard.Text;
                if (!string.IsNullOrEmpty(clip))
                {
                    InsertText(clip, TextUndoRedoManager.TextUndoKind.Paste, allowCoalesce: false, options);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlC)
            {
                var span = GetSelectedTextSpan(text.AsSpan());
                if (!span.IsEmpty)
                {
                    _host.App?.Terminal.Clipboard.TrySetText(span);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlX)
            {
                if (HasSelection)
                {
                    var span = GetSelectedTextSpan(text.AsSpan());
                    if (!span.IsEmpty)
                    {
                        _host.App?.Terminal.Clipboard.TrySetText(span);
                    }
                    DeleteSelection(TextUndoRedoManager.TextUndoKind.Delete, options);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlK)
            {
                KillToEnd(text.AsSpan(), options);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlU)
            {
                KillToStart(text.AsSpan(), options);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlW)
            {
                KillPreviousWord(text.AsSpan(), options);
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlY)
            {
                if (!string.IsNullOrEmpty(_killBuffer))
                {
                    InsertText(_killBuffer, TextUndoRedoManager.TextUndoKind.Paste, allowCoalesce: false, options);
                }
                e.Handled = true;
                return;
            }
        }

        if (options.SingleLine)
        {
            HandleSingleLineKeyDown(e, options, text.AsSpan());
        }
        else
        {
            HandleMultiLineKeyDown(e, options, text.AsSpan());
        }
    }

    private void HandleSingleLineKeyDown(KeyEventArgs e, in TextEditorOptions options, ReadOnlySpan<char> text)
    {
        switch (e.Key)
        {
            case TerminalKey.Left:
                var oldCaretLeft = _caretIndex;
                _caretIndex = (e.Modifiers & TerminalModifiers.Ctrl) != 0
                    ? GetPreviousWordIndex(text, _caretIndex)
                    : GetPreviousTextElementIndexFast(text, _caretIndex);
                UpdateSelectionAfterCaretMove((e.Modifiers & TerminalModifiers.Shift) != 0, oldCaretLeft);
                EnsureCaretVisible(options);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                var oldCaretRight = _caretIndex;
                _caretIndex = (e.Modifiers & TerminalModifiers.Ctrl) != 0
                    ? GetNextWordIndex(text, _caretIndex)
                    : GetNextTextElementIndexFast(text, _caretIndex);
                UpdateSelectionAfterCaretMove((e.Modifiers & TerminalModifiers.Shift) != 0, oldCaretRight);
                EnsureCaretVisible(options);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                var oldCaretHome = _caretIndex;
                _caretIndex = 0;
                UpdateSelectionAfterCaretMove((e.Modifiers & TerminalModifiers.Shift) != 0, oldCaretHome);
                EnsureCaretVisible(options);
                e.Handled = true;
                return;
            case TerminalKey.End:
                var oldCaretEnd = _caretIndex;
                _caretIndex = text.Length;
                UpdateSelectionAfterCaretMove((e.Modifiers & TerminalModifiers.Shift) != 0, oldCaretEnd);
                EnsureCaretVisible(options);
                e.Handled = true;
                return;
            case TerminalKey.Backspace:
                if (HasSelection)
                {
                    DeleteSelection(TextUndoRedoManager.TextUndoKind.Delete, options);
                }
                else if (_caretIndex > 0)
                {
                    var prev = (e.Modifiers & TerminalModifiers.Ctrl) != 0
                        ? GetPreviousWordIndex(text, _caretIndex)
                        : GetPreviousTextElementIndexFast(text, _caretIndex);
                    ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Delete, prev, _caretIndex - prev, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
                    {
                        _caretIndex = prev;
                        _preferredColumn = -1;
                    });
                    e.Handled = true;
                    return;
                }
                e.Handled = true;
                return;
            case TerminalKey.Delete:
                if (HasSelection)
                {
                    DeleteSelection(TextUndoRedoManager.TextUndoKind.Delete, options);
                }
                else if (_caretIndex < text.Length)
                {
                    var next = (e.Modifiers & TerminalModifiers.Ctrl) != 0
                        ? GetNextWordIndex(text, _caretIndex)
                        : GetNextTextElementIndexFast(text, _caretIndex);
                    ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Delete, _caretIndex, next - _caretIndex, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
                    {
                        _preferredColumn = -1;
                    });
                    e.Handled = true;
                    return;
                }
                e.Handled = true;
                return;
        }
    }
    private void HandleMultiLineKeyDown(KeyEventArgs e, in TextEditorOptions options, ReadOnlySpan<char> text)
    {
        var ctrl = (e.Modifiers & TerminalModifiers.Ctrl) != 0;

        switch (e.Key)
        {
            case TerminalKey.Left:
                if (ctrl)
                {
                    MoveCaretTo(GetPreviousWordIndex(text, _caretIndex), (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                else
                {
                    MoveCaretHorizontal(-1, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                e.Handled = true;
                return;
            case TerminalKey.Right:
                if (ctrl)
                {
                    MoveCaretTo(GetNextWordIndex(text, _caretIndex), (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                else
                {
                    MoveCaretHorizontal(1, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                e.Handled = true;
                return;
            case TerminalKey.Up:
                MoveCaretVertical(-1, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                MoveCaretVertical(1, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                if (ctrl)
                {
                    MoveCaretTo(0, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                else
                {
                    MoveCaretToLineBoundary(start: true, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                e.Handled = true;
                return;
            case TerminalKey.End:
                if (ctrl)
                {
                    MoveCaretTo(text.Length, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                else
                {
                    MoveCaretToLineBoundary(start: false, (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                }
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                MoveCaretVertical(-Math.Max(1, _contentHeight), (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                MoveCaretVertical(Math.Max(1, _contentHeight), (e.Modifiers & TerminalModifiers.Shift) != 0, options);
                e.Handled = true;
                return;
            case TerminalKey.Backspace:
                Backspace(options);
                e.Handled = true;
                return;
            case TerminalKey.Delete:
                Delete(options);
                e.Handled = true;
                return;
            case TerminalKey.Enter:
                if (options.AcceptsReturn)
                {
                    InsertText("\n", TextUndoRedoManager.TextUndoKind.Typing, allowCoalesce: false, options);
                    e.Handled = true;
                }
                return;
            case TerminalKey.Tab:
                if (options.AcceptsTab)
                {
                    InsertText("\t", TextUndoRedoManager.TextUndoKind.Typing, allowCoalesce: true, options);
                    e.Handled = true;
                }
                return;
        }
    }

    private void RenderSingleLine(in TextEditorRenderContext context, in TextEditorOptions options)
    {
        var text = GetText();
        var contentWidth = _contentWidth;
        if (contentWidth <= 0)
        {
            return;
        }

        var totalTextCells = GetTextCells(text.AsSpan(), options.TabSize);
        var scrollX = totalTextCells <= contentWidth ? 0 : _scroll.OffsetX;

        var startIndex = GetIndexAtCell(text.AsSpan(), scrollX, options.TabSize);
        var endIndex = GetIndexAtCell(text.AsSpan(), scrollX + contentWidth, options.TabSize);
        var startColumn = GetCellOffsetAtIndex(text.AsSpan(), startIndex, options.TabSize);

        var contentXAligned = _contentX;
        if (scrollX == 0 && totalTextCells <= contentWidth && options.Alignment is TextAlignment.Center or TextAlignment.Right)
        {
            var shift = options.Alignment == TextAlignment.Center ? (contentWidth - totalTextCells) / 2 : (contentWidth - totalTextCells);
            contentXAligned += Math.Max(0, shift);
        }

        if (!HasSelection)
        {
            if (text.Length == 0 && !string.IsNullOrEmpty(context.Placeholder)
                && (!options.ShowPlaceholderWhenUnfocusedOnly || !context.IsFocused))
            {
                var placeholder = context.Placeholder.AsSpan();
                if (options.Alignment is TextAlignment.Center or TextAlignment.Right)
                {
                    var placeholderCells = GetTextCells(placeholder, options.TabSize);
                    if (placeholderCells < contentWidth)
                    {
                        var shift = options.Alignment == TextAlignment.Center ? (contentWidth - placeholderCells) / 2 : (contentWidth - placeholderCells);
                        contentXAligned = _contentX + Math.Max(0, shift);
                    }
                    else
                    {
                        contentXAligned = _contentX;
                    }
                }

                context.SegmentWriter(context.Buffer, contentXAligned, _contentY, placeholder, context.PlaceholderStyle, isPlaceholder: true, textIndexStart: -1, startColumn: 0);
            }
            else if (endIndex > startIndex)
            {
                context.SegmentWriter(context.Buffer, contentXAligned, _contentY, text.AsSpan(startIndex, endIndex - startIndex), context.TextStyle, isPlaceholder: false, textIndexStart: startIndex, startColumn: startColumn);
            }
        }
        else
        {
            var (selStart, selEnd) = GetOrderedSelection();
            var visSelStart = Math.Clamp(selStart, startIndex, endIndex);
            var visSelEnd = Math.Clamp(selEnd, startIndex, endIndex);

            if (visSelStart > startIndex)
            {
                context.SegmentWriter(context.Buffer, contentXAligned, _contentY, text.AsSpan(startIndex, visSelStart - startIndex), context.TextStyle, isPlaceholder: false, textIndexStart: startIndex, startColumn: startColumn);
            }

            if (visSelEnd > visSelStart)
            {
                var selStartCells = GetTextCells(text.AsSpan(startIndex, visSelStart - startIndex), options.TabSize);
                context.SegmentWriter(context.Buffer, contentXAligned + selStartCells, _contentY, text.AsSpan(visSelStart, visSelEnd - visSelStart), context.SelectionStyle, isPlaceholder: false, textIndexStart: visSelStart, startColumn: startColumn + selStartCells);
            }

            if (endIndex > visSelEnd)
            {
                var selEndCells = GetTextCells(text.AsSpan(startIndex, visSelEnd - startIndex), options.TabSize);
                context.SegmentWriter(context.Buffer, contentXAligned + selEndCells, _contentY, text.AsSpan(visSelEnd, endIndex - visSelEnd), context.TextStyle, isPlaceholder: false, textIndexStart: visSelEnd, startColumn: startColumn + selEndCells);
            }
        }
    }
    private void RenderMultiLine(in TextEditorRenderContext context, in TextEditorOptions options)
    {
        var text = GetText();
        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        if (text.Length == 0 && !string.IsNullOrEmpty(context.Placeholder))
        {
            context.SegmentWriter(context.Buffer, _contentX, _contentY, context.Placeholder.AsSpan(), context.PlaceholderStyle, isPlaceholder: true, textIndexStart: -1, startColumn: 0);
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        EnsureMultiLineLayoutCache(options);

        var startRow = _scroll.OffsetY;
        var endRow = startRow + _contentHeight;

        var selectionStart = 0;
        var selectionEnd = 0;
        if (HasSelection)
        {
            selectionStart = Math.Min(_selectionAnchor, _selectionEnd);
            selectionEnd = Math.Max(_selectionAnchor, _selectionEnd);
        }

        if (snapshot.LineCount == 0)
        {
            return;
        }

        var startInfo = _layoutCache.GetLineFromRow(startRow);
        var lineIndex = startInfo.LineIndex;
        var row = _layoutCache.GetLine(lineIndex).RowOffset + startInfo.RowInLine;

        while (lineIndex < snapshot.LineCount && row < endRow)
        {
            var line = snapshot.GetLine(lineIndex);
            var lineSpan = text.AsSpan(line.Start, line.Length);

            if (!options.WordWrap)
            {
                RenderSingleLineSegment(
                    context,
                    options,
                    lineSpan,
                    line.Start,
                    row - startRow,
                    selectionStart,
                    selectionEnd);
                row++;
                lineIndex++;
                continue;
            }

            var rowInLine = lineIndex == startInfo.LineIndex ? startInfo.RowInLine : 0;
            var rowCount = Math.Max(1, _layoutCache.GetLine(lineIndex).RowCount);
            var currentRowInLine = rowInLine;
            while (currentRowInLine < rowCount && row < endRow)
            {
                var blockStarts = _layoutCache.GetWrapRowBlock(lineIndex, text, _contentWidth, options.TabSize, currentRowInLine, out var blockStartRow, out var blockRowCount);
                var localRow = currentRowInLine - blockStartRow;
                var availableRows = Math.Min(rowCount - currentRowInLine, blockRowCount - localRow);

                for (var i = 0; i < availableRows && row < endRow; i++)
                {
                    var segmentStart = blockStarts[localRow + i];
                    var segmentLength = blockStarts[localRow + i + 1] - segmentStart;

                    RenderWrappedSegment(
                        context,
                        options,
                        lineSpan,
                        line.Start,
                        segmentStart,
                        segmentLength,
                        row - startRow,
                        selectionStart,
                        selectionEnd);

                    row++;
                    currentRowInLine++;
                }
            }

            lineIndex++;
        }
    }

    private void RenderSingleLineSegment(
        in TextEditorRenderContext context,
        in TextEditorOptions options,
        ReadOnlySpan<char> lineSpan,
        int lineStartIndex,
        int visualRow,
        int selectionStart,
        int selectionEnd)
    {
        var scrollX = options.WordWrap ? 0 : _scroll.OffsetX;
        var startIndex = GetIndexAtCell(lineSpan, scrollX, options.TabSize);
        var endIndex = GetIndexAtCell(lineSpan, scrollX + _contentWidth, options.TabSize);
        var startColumn = GetCellOffsetAtIndex(lineSpan, startIndex, options.TabSize);

        var y = _contentY + visualRow;
        if (!HasSelection)
        {
            if (endIndex > startIndex)
            {
                context.SegmentWriter(context.Buffer, _contentX, y, lineSpan.Slice(startIndex, endIndex - startIndex), context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + startIndex, startColumn: startColumn);
            }

            return;
        }

        var selStart = Math.Clamp(selectionStart, lineStartIndex, lineStartIndex + lineSpan.Length);
        var selEnd = Math.Clamp(selectionEnd, lineStartIndex, lineStartIndex + lineSpan.Length);

        if (selEnd <= selStart)
        {
            if (endIndex > startIndex)
            {
                context.SegmentWriter(context.Buffer, _contentX, y, lineSpan.Slice(startIndex, endIndex - startIndex), context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + startIndex, startColumn: startColumn);
            }
            return;
        }

        var localSelStart = selStart - lineStartIndex;
        var localSelEnd = selEnd - lineStartIndex;

        var visSelStart = Math.Clamp(localSelStart - startIndex, 0, endIndex - startIndex);
        var visSelEnd = Math.Clamp(localSelEnd - startIndex, 0, endIndex - startIndex);

        var left = lineSpan.Slice(startIndex, visSelStart);
        var sel = lineSpan.Slice(startIndex + visSelStart, Math.Max(0, visSelEnd - visSelStart));
        var right = lineSpan.Slice(startIndex + visSelEnd, Math.Max(0, endIndex - (startIndex + visSelEnd)));

        if (!left.IsEmpty)
        {
            context.SegmentWriter(context.Buffer, _contentX, y, left, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + startIndex, startColumn: startColumn);
        }

        if (!sel.IsEmpty)
        {
            var selStartCells = GetTextCells(left, options.TabSize);
            context.SegmentWriter(context.Buffer, _contentX + selStartCells, y, sel, context.SelectionStyle, isPlaceholder: false, textIndexStart: lineStartIndex + startIndex + visSelStart, startColumn: startColumn + selStartCells);
        }

        if (!right.IsEmpty)
        {
            var leftCells = GetTextCells(left, options.TabSize);
            var selCells = GetTextCells(sel, options.TabSize);
            context.SegmentWriter(context.Buffer, _contentX + leftCells + selCells, y, right, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + startIndex + visSelEnd, startColumn: startColumn + leftCells + selCells);
        }
    }

    private void RenderWrappedSegment(
        in TextEditorRenderContext context,
        in TextEditorOptions options,
        ReadOnlySpan<char> lineSpan,
        int lineStartIndex,
        int segmentStart,
        int segmentLength,
        int visualRow,
        int selectionStart,
        int selectionEnd)
    {
        var segment = lineSpan.Slice(segmentStart, segmentLength);
        var y = _contentY + visualRow;

        if (!HasSelection)
        {
            context.SegmentWriter(context.Buffer, _contentX, y, segment, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + segmentStart, startColumn: 0);
            return;
        }

        var segStartIndex = lineStartIndex + segmentStart;
        var segEndIndex = segStartIndex + segmentLength;

        var selStart = Math.Clamp(selectionStart, segStartIndex, segEndIndex);
        var selEnd = Math.Clamp(selectionEnd, segStartIndex, segEndIndex);

        if (selEnd <= selStart)
        {
            context.SegmentWriter(context.Buffer, _contentX, y, segment, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + segmentStart, startColumn: 0);
            return;
        }

        var localSelStart = selStart - segStartIndex;
        var localSelEnd = selEnd - segStartIndex;

        var left = segment[..localSelStart];
        var sel = segment.Slice(localSelStart, localSelEnd - localSelStart);
        var right = segment[localSelEnd..];

        if (!left.IsEmpty)
        {
            context.SegmentWriter(context.Buffer, _contentX, y, left, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + segmentStart, startColumn: 0);
        }

        if (!sel.IsEmpty)
        {
            var selStartCells = GetTextCells(left, options.TabSize);
            context.SegmentWriter(context.Buffer, _contentX + selStartCells, y, sel, context.SelectionStyle, isPlaceholder: false, textIndexStart: lineStartIndex + segmentStart + localSelStart, startColumn: selStartCells);
        }

        if (!right.IsEmpty)
        {
            var leftCells = GetTextCells(left, options.TabSize);
            var selCells = GetTextCells(sel, options.TabSize);
            context.SegmentWriter(context.Buffer, _contentX + leftCells + selCells, y, right, context.TextStyle, isPlaceholder: false, textIndexStart: lineStartIndex + segmentStart + localSelEnd, startColumn: leftCells + selCells);
        }
    }
    private int ComputeExtent(ITextSnapshot snapshot, in TextEditorOptions options, out int extentWidth)
    {
        _ = snapshot;
        EnsureMultiLineLayoutCache(options);
        extentWidth = options.WordWrap ? Math.Max(0, _contentWidth) : Math.Max(0, _layoutCache.MaxWidth);
        return _layoutCache.TotalRows;
    }

    private TextUndoRedoManager.TextEditorStateSnapshot CaptureStateSnapshot()
        => new(
            CaretIndex: _caretIndex,
            SelectionAnchor: _selectionAnchor,
            SelectionEnd: _selectionEnd,
            ScrollX: _scroll.OffsetX,
            ScrollY: _scroll.OffsetY,
            PreferredColumn: _preferredColumn);

    private void RestoreStateSnapshot(in TextUndoRedoManager.TextEditorStateSnapshot snapshot, in TextEditorOptions options)
    {
        var text = GetText().AsSpan();
        var length = text.Length;

        _caretIndex = NormalizeIndexToTextElementBoundary(text, Math.Clamp(snapshot.CaretIndex, 0, length));
        _preferredColumn = snapshot.PreferredColumn;

        if (snapshot.SelectionAnchor < 0 || snapshot.SelectionEnd < 0 || snapshot.SelectionAnchor == snapshot.SelectionEnd)
        {
            ClearSelection();
        }
        else
        {
            _selectionAnchor = NormalizeIndexToTextElementBoundary(text, Math.Clamp(snapshot.SelectionAnchor, 0, length));
            _selectionEnd = NormalizeIndexToTextElementBoundary(text, Math.Clamp(snapshot.SelectionEnd, 0, length));
        }

        _scroll.SetOffset(Math.Max(0, snapshot.ScrollX), Math.Max(0, snapshot.ScrollY));
        _ = options;
    }

    private void ApplyReplaceWithUndo(
        TextUndoRedoManager.TextUndoKind kind,
        int position,
        int length,
        ReadOnlySpan<char> inserted,
        bool allowCoalesce,
        in TextEditorOptions options,
        Action afterDocumentChange)
    {
        _undoRedo.EnsureSynchronized();

        var before = CaptureStateSnapshot();

        string removedText;
        string insertedText;
        if (_undoRedo.Enabled)
        {
            var text = GetText();
            removedText = length == 0 ? string.Empty : text.AsSpan(position, length).ToString();
            insertedText = inserted.IsEmpty ? string.Empty : inserted.ToString();
        }
        else
        {
            removedText = string.Empty;
            insertedText = string.Empty;
        }

        using var _ = _undoRedo.BeginRecording();
        _document.Replace(position, length, inserted);

        afterDocumentChange();

        var after = CaptureStateSnapshot();
        if (_undoRedo.Enabled)
        {
            _undoRedo.RecordSingle(kind, new(position, removedText, insertedText), before, after, allowCoalesce);
        }

        UpdateAfterDocumentChange(options);
    }

    private void InsertText(string text, TextUndoRedoManager.TextUndoKind kind, bool allowCoalesce, in TextEditorOptions options)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (!options.AcceptsReturn)
        {
            text = text.Replace("\r", string.Empty, StringComparison.Ordinal);
        }

        if (HasSelection)
        {
            var snapshotText = GetText();
            var (start, end) = GetOrderedSelection();
            start = Math.Clamp(start, 0, snapshotText.Length);
            end = Math.Clamp(end, 0, snapshotText.Length);
            if (end <= start)
            {
                ClearSelection();
            }
            else
            {
                var insertedLength = text.Length;
                ApplyReplaceWithUndo(kind, start, end - start, text.AsSpan(), allowCoalesce: false, options, () =>
                {
                    _caretIndex = start + insertedLength;
                    ClearSelection();
                    _preferredColumn = -1;
                });
                return;
            }
        }

        var insertPos = Math.Clamp(_caretIndex, 0, GetText().Length);
        var insertedLengthNoSelection = text.Length;
        ApplyReplaceWithUndo(kind, insertPos, length: 0, text.AsSpan(), allowCoalesce, options, () =>
        {
            _caretIndex = insertPos + insertedLengthNoSelection;
            _preferredColumn = -1;
        });
    }

    private void Backspace(in TextEditorOptions options)
    {
        if (HasSelection)
        {
            DeleteSelection(TextUndoRedoManager.TextUndoKind.Delete, options);
            return;
        }

        var text = GetText();
        if (_caretIndex <= 0 || text.Length == 0)
        {
            return;
        }

        var prev = GetPreviousTextElementIndexFast(text.AsSpan(), _caretIndex);
        ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Delete, prev, _caretIndex - prev, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
        {
            _caretIndex = prev;
            _preferredColumn = -1;
        });
    }

    private void Delete(in TextEditorOptions options)
    {
        if (HasSelection)
        {
            DeleteSelection(TextUndoRedoManager.TextUndoKind.Delete, options);
            return;
        }

        var text = GetText();
        if (_caretIndex >= text.Length)
        {
            return;
        }

        var next = GetNextTextElementIndexFast(text.AsSpan(), _caretIndex);
        ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Delete, _caretIndex, next - _caretIndex, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
        {
            _preferredColumn = -1;
        });
    }

    private void MoveCaretTo(int index, bool extendSelection, in TextEditorOptions options)
        => MoveCaretTo(index, extendSelection, options, row: null, column: null, preserveWrappedBoundaryMove: false);

    private void MoveCaretTo(int index, bool extendSelection, in TextEditorOptions options, int? row, int? column, bool preserveWrappedBoundaryMove = false)
    {
        index = NormalizeIndexToTextElementBoundary(GetText().AsSpan(), Math.Clamp(index, 0, GetText().Length));
        if (extendSelection)
        {
            ExtendSelection(index);
        }
        else
        {
            ClearSelection();
        }

        _caretIndex = index;
        if (row.HasValue && column.HasValue)
        {
            CacheVisualPosition(_document.CurrentSnapshot.Version, index, options, row.Value, column.Value);
        }

        if (!preserveWrappedBoundaryMove)
        {
            ResetWrappedLineBoundaryMove();
        }

        _preferredColumn = -1;
        EnsureCaretVisible(options);
        IncrementVersion();
    }

    private void MoveCaretHorizontal(int delta, bool extendSelection, in TextEditorOptions options)
    {
        var text = GetText().AsSpan();
        var next = _caretIndex;
        if (delta < 0)
        {
            next = GetPreviousTextElementIndexFast(text, _caretIndex);
        }
        else if (delta > 0)
        {
            next = GetNextTextElementIndexFast(text, _caretIndex);
        }

        MoveCaretTo(next, extendSelection, options);
    }

    private void MoveCaretVertical(int deltaLines, bool extendSelection, in TextEditorOptions options)
    {
        var text = GetText();
        if (options.WordWrap)
        {
            var (row, visualCol) = GetVisualPosition(text.AsSpan(), _caretIndex, options);
            if (_preferredColumn < 0)
            {
                _preferredColumn = visualCol;
            }

            var targetRow = Math.Max(0, row + deltaLines);
            var index = GetIndexFromVisualPosition(text.AsSpan(), targetRow, _preferredColumn, options, out var actualCol);
            MoveCaretTo(index, extendSelection, options, targetRow, actualCol);
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        var (line, lineCol) = GetLineColumnForIndex(snapshot, _caretIndex);
        if (_preferredColumn < 0)
        {
            _preferredColumn = lineCol;
        }

        var newLine = line + deltaLines;
        var next = GetIndexForLineColumn(snapshot, newLine, _preferredColumn);
        MoveCaretTo(next, extendSelection, options);
    }

    private void MoveCaretToLineBoundary(bool start, bool extendSelection, in TextEditorOptions options)
    {
        var text = GetText().AsSpan();
        if (options.WordWrap)
        {
            var (row, _) = GetVisualPosition(text, _caretIndex, options);
            var lineInfo = GetLineFromVisualRow(text, row, options, out var lineStart, out var lineEnd, out var rowInLine);
            if (!lineInfo)
            {
                return;
            }

            EnsureMultiLineLayoutCache(options);
            var lineIndex = _layoutCache.GetLineIndexFromPosition(lineStart);
            var segment = _layoutCache.GetWrapSegmentAtRow(lineIndex, GetText(), _contentWidth, options.TabSize, rowInLine);
            var segmentStart = segment.Start;
            var segmentLength = segment.Length;
            var visualBoundaryIndex = start ? lineStart + segmentStart : lineStart + segmentStart + segmentLength;
            var visualBoundaryColumn = start ? 0 : GetTextCells(text.Slice(lineStart + segmentStart, segmentLength), options.TabSize);
            var moveKind = start ? WrappedLineBoundaryMoveKind.Home : WrappedLineBoundaryMoveKind.End;
            var moveToLogicalBoundary = _wrappedLineBoundaryMove == moveKind;

            if (moveToLogicalBoundary)
            {
                var logicalBoundaryIndex = start ? lineStart : lineEnd;
                MoveCaretTo(logicalBoundaryIndex, extendSelection, options, row: null, column: null, preserveWrappedBoundaryMove: true);
            }
            else
            {
                var targetRow = row;
                var targetColumn = visualBoundaryColumn;
                if (!start && visualBoundaryIndex < lineEnd)
                {
                    targetRow = row + 1;
                    targetColumn = 0;
                }

                MoveCaretTo(visualBoundaryIndex, extendSelection, options, targetRow, targetColumn, preserveWrappedBoundaryMove: true);
            }

            _wrappedLineBoundaryMove = moveKind;
            return;
        }

        var snapshot = _document.CurrentSnapshot;
        var (line, _) = GetLineColumnForIndex(snapshot, _caretIndex);
        var currentLine = snapshot.GetLine(line);
        var lineStartIndex = currentLine.Start;
        var lineEndIndex = currentLine.End;
        MoveCaretTo(start ? lineStartIndex : lineEndIndex, extendSelection, options);
    }

    private void EnsureCaretVisible(in TextEditorOptions options)
    {
        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        var text = GetText().AsSpan();
        if (options.SingleLine)
        {
            var caretCells = GetCellOffsetAtIndex(text, _caretIndex, options.TabSize);
            var targetX = _scroll.OffsetX;

            if (caretCells < targetX)
            {
                targetX = caretCells;
            }
            else if (caretCells >= targetX + _contentWidth)
            {
                targetX = Math.Max(0, caretCells - _contentWidth + 1);
            }

            _scroll.SetOffset(targetX, 0);
            return;
        }

        EnsureMultiLineLayoutCache(options);
        var (row, col) = GetVisualPosition(text, _caretIndex, options);
        var offsetX = options.WordWrap ? 0 : _scroll.OffsetX;
        var offsetY = _scroll.OffsetY;

        if (row < offsetY)
        {
            offsetY = row;
        }
        else if (row >= offsetY + _contentHeight)
        {
            offsetY = Math.Max(0, row - _contentHeight + 1);
        }

        if (!options.WordWrap)
        {
            if (col < offsetX)
            {
                offsetX = col;
            }
            else if (col >= offsetX + _contentWidth)
            {
                offsetX = Math.Max(0, col - _contentWidth + 1);
            }
        }

        _scroll.SetOffset(offsetX, offsetY);
    }
    private int GetIndexFromPointer(int uiX, int uiY, in TextEditorOptions options)
    {
        var text = GetText().AsSpan();
        var localX = Math.Clamp(uiX - _contentX, 0, _contentWidth);
        var localY = Math.Clamp(uiY - _contentY, 0, _contentHeight);

        if (options.SingleLine)
        {
            var cell = localX + _scroll.OffsetX;
            return GetIndexAtCell(text, cell, options.TabSize);
        }

        var row = localY + _scroll.OffsetY;
        if (options.WordWrap)
        {
            return GetIndexFromVisualPosition(text, row, localX, options);
        }

        var snapshot = _document.CurrentSnapshot;
        var lineIndex = Math.Clamp(row, 0, Math.Max(0, snapshot.LineCount - 1));
        var line = snapshot.GetLine(lineIndex);
        var lineSpan = text.Slice(line.Start, line.Length);
        var col = localX + _scroll.OffsetX;
        var indexInLine = GetIndexAtCell(lineSpan, col, options.TabSize);
        return line.Start + indexInLine;
    }

    private void KillToEnd(ReadOnlySpan<char> text, in TextEditorOptions options)
    {
        if (HasSelection)
        {
            _killBuffer = GetSelectedTextSpan(text).ToString();
            DeleteSelection(TextUndoRedoManager.TextUndoKind.Kill, options);
            return;
        }
        else if (_caretIndex < text.Length)
        {
            _killBuffer = text[_caretIndex..].ToString();
            ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Kill, _caretIndex, text.Length - _caretIndex, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () => { });
            return;
        }
    }

    private void KillToStart(ReadOnlySpan<char> text, in TextEditorOptions options)
    {
        if (HasSelection)
        {
            _killBuffer = GetSelectedTextSpan(text).ToString();
            DeleteSelection(TextUndoRedoManager.TextUndoKind.Kill, options);
            return;
        }
        else if (_caretIndex > 0)
        {
            _killBuffer = text[.._caretIndex].ToString();
            var start = _caretIndex;
            ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Kill, 0, start, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
            {
                _caretIndex = 0;
            });
            return;
        }
    }

    private void KillPreviousWord(ReadOnlySpan<char> text, in TextEditorOptions options)
    {
        if (HasSelection)
        {
            _killBuffer = GetSelectedTextSpan(text).ToString();
            DeleteSelection(TextUndoRedoManager.TextUndoKind.Kill, options);
            return;
        }

        if (_caretIndex <= 0)
        {
            return;
        }

        var prev = GetPreviousWordIndex(text, _caretIndex);
        _killBuffer = text[prev.._caretIndex].ToString();
        ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Kill, prev, _caretIndex - prev, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
        {
            _caretIndex = prev;
        });
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionEnd = -1;
    }

    private void SelectWordAt(int index)
    {
        var text = GetText().AsSpan();
        index = Math.Clamp(index, 0, text.Length);

        var start = TerminalTextUtility.GetWordStart(text, index);
        var end = TerminalTextUtility.GetWordEnd(text, index);

        if (start == end)
        {
            ClearSelection();
            return;
        }

        _selectionAnchor = NormalizeIndexToTextElementBoundary(text, start);
        _selectionEnd = NormalizeIndexToTextElementBoundary(text, end);
    }

    private void ExtendSelection(int caret)
    {
        if (_selectionAnchor < 0)
        {
            _selectionAnchor = _caretIndex;
        }

        _selectionEnd = caret;
    }

    private void DeleteSelection(TextUndoRedoManager.TextUndoKind kind, in TextEditorOptions options)
    {
        if (!HasSelection)
        {
            return;
        }

        var text = GetText();
        var (start, end) = GetOrderedSelection();
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end <= start)
        {
            ClearSelection();
            return;
        }

        ApplyReplaceWithUndo(kind, start, end - start, ReadOnlySpan<char>.Empty, allowCoalesce: false, options, () =>
        {
            _caretIndex = start;
            ClearSelection();
            _preferredColumn = -1;
        });
    }

    internal void Undo(in TextEditorOptions options)
    {
        _undoRedo.EnsureSynchronized();
        if (!_undoRedo.Enabled || !_undoRedo.CanUndo)
        {
            return;
        }

        var entry = _undoRedo.Undo();
        using var _ = _undoRedo.BeginApplying();
        using var __ = _document.BeginUpdate();

        for (var i = entry.Changes.Length - 1; i >= 0; i--)
        {
            var change = entry.Changes[i];
            _document.Replace(change.Position, change.InsertedText.Length, change.RemovedText.AsSpan());
        }

        RestoreStateSnapshot(entry.Before, options);
        UpdateAfterDocumentChange(options);

        if (!string.IsNullOrEmpty(_searchQuery.Text))
        {
            RebuildSearchMatches();
        }

        IncrementVersion();
    }

    internal void Redo(in TextEditorOptions options)
    {
        _undoRedo.EnsureSynchronized();
        if (!_undoRedo.Enabled || !_undoRedo.CanRedo)
        {
            return;
        }

        var entry = _undoRedo.Redo();
        using var _ = _undoRedo.BeginApplying();
        using var __ = _document.BeginUpdate();

        for (var i = 0; i < entry.Changes.Length; i++)
        {
            var change = entry.Changes[i];
            _document.Replace(change.Position, change.RemovedText.Length, change.InsertedText.AsSpan());
        }

        RestoreStateSnapshot(entry.After, options);
        UpdateAfterDocumentChange(options);

        if (!string.IsNullOrEmpty(_searchQuery.Text))
        {
            RebuildSearchMatches();
        }

        IncrementVersion();
    }

    private void SelectAll()
    {
        var text = GetText();
        if (text.Length == 0)
        {
            return;
        }

        _selectionAnchor = 0;
        _selectionEnd = text.Length;
        _caretIndex = text.Length;
        IncrementVersion();
    }

    private void UpdateSelectionAfterCaretMove(bool shift, int oldCaretIndex)
    {
        if (!shift)
        {
            ClearSelection();
            IncrementVersion();
            return;
        }

        if (_selectionAnchor < 0)
        {
            _selectionAnchor = oldCaretIndex;
        }

        _selectionEnd = _caretIndex;
        IncrementVersion();
    }

    private (int Start, int End) GetOrderedSelection()
    {
        var start = _selectionAnchor;
        var end = _selectionEnd;
        if (start > end)
        {
            (start, end) = (end, start);
        }
        return (start, end);
    }

    private ReadOnlySpan<char> GetSelectedTextSpan(ReadOnlySpan<char> text)
    {
        if (!HasSelection || text.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var (start, end) = GetOrderedSelection();
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        return end > start ? text.Slice(start, end - start) : ReadOnlySpan<char>.Empty;
    }

    private static (int Line, int Column) GetLineColumnForIndex(ITextSnapshot snapshot, int index)
    {
        index = Math.Clamp(index, 0, snapshot.Length);
        var lineIndex = snapshot.GetLineIndexFromPosition(index);
        var line = snapshot.GetLine(lineIndex);
        return (lineIndex, index - line.Start);
    }

    private static int GetIndexForLineColumn(ITextSnapshot snapshot, int line, int column)
    {
        if (snapshot.LineCount == 0)
        {
            return 0;
        }

        line = Math.Clamp(line, 0, snapshot.LineCount - 1);
        var currentLine = snapshot.GetLine(line);
        column = Math.Clamp(column, 0, currentLine.Length);
        return currentLine.Start + column;
    }

    private (int Row, int Column) GetVisualPosition(ReadOnlySpan<char> text, int index, in TextEditorOptions options)
    {
        index = Math.Clamp(index, 0, text.Length);
        var snapshot = _document.CurrentSnapshot;

        if (snapshot.LineCount == 0)
        {
            return (0, 0);
        }

        if (TryGetCachedVisualPosition(snapshot.Version, index, options, out var cachedPosition))
        {
            return cachedPosition;
        }

        if (options.WordWrap && _contentWidth > 0)
        {
            EnsureMultiLineLayoutCache(options);
        }

        var lineIndex = snapshot.GetLineIndexFromPosition(index);
        var line = snapshot.GetLine(lineIndex);
        var lineSpan = text.Slice(line.Start, line.Length);
        var indexInLine = Math.Clamp(index - line.Start, 0, line.Length);

        if (!options.WordWrap || _contentWidth <= 0 || _layoutCache.LineCount == 0)
        {
            var position = (Row: lineIndex, Column: GetCellOffsetAtIndex(lineSpan, indexInLine, options.TabSize));
            CacheVisualPosition(snapshot.Version, index, options, position.Row, position.Column);
            return position;
        }

        EnsureMultiLineLayoutCache(options);
        var layout = _layoutCache.GetLine(lineIndex);
        var segment = _layoutCache.FindWrapSegmentForIndex(lineIndex, GetText(), _contentWidth, options.TabSize, indexInLine);
        var rowInLine = segment.RowInLine;
        var segmentStart = segment.Start;
        var col = GetCellOffsetAtIndex(lineSpan.Slice(segmentStart, indexInLine - segmentStart), indexInLine - segmentStart, options.TabSize);
        var row = layout.RowOffset + rowInLine;
        CacheVisualPosition(snapshot.Version, index, options, row, col);
        return (row, col);
    }

    private int GetIndexFromVisualPosition(ReadOnlySpan<char> text, int targetRow, int targetCol, in TextEditorOptions options)
        => GetIndexFromVisualPosition(text, targetRow, targetCol, options, out _);

    private int GetIndexFromVisualPosition(ReadOnlySpan<char> text, int targetRow, int targetCol, in TextEditorOptions options, out int actualCol)
    {
        var snapshot = _document.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            actualCol = 0;
            return 0;
        }

        if (!options.WordWrap || _contentWidth <= 0 || _layoutCache.LineCount == 0)
        {
            var lineIndex = Math.Clamp(targetRow, 0, snapshot.LineCount - 1);
            var line = snapshot.GetLine(lineIndex);
            var lineSpan = text.Slice(line.Start, line.Length);
            var indexInLine = GetIndexAtCell(lineSpan, targetCol, options.TabSize);
            actualCol = GetCellOffsetAtIndex(lineSpan, indexInLine, options.TabSize);
            return line.Start + indexInLine;
        }

        EnsureMultiLineLayoutCache(options);
        var lineRowInfo = _layoutCache.GetLineFromRow(targetRow);
        var currentLine = snapshot.GetLine(lineRowInfo.LineIndex);
        var lineSpanCurrent = text.Slice(currentLine.Start, currentLine.Length);
        var wrapSegment = _layoutCache.GetWrapSegmentAtRow(lineRowInfo.LineIndex, GetText(), _contentWidth, options.TabSize, lineRowInfo.RowInLine);
        var segmentStart = wrapSegment.Start;
        var segmentLength = wrapSegment.Length;
        var segmentSpan = lineSpanCurrent.Slice(segmentStart, segmentLength);
        var indexInSegment = GetIndexAtCell(segmentSpan, targetCol, options.TabSize);
        actualCol = GetCellOffsetAtIndex(segmentSpan, indexInSegment, options.TabSize);
        return currentLine.Start + segmentStart + indexInSegment;
    }

    private bool GetLineFromVisualRow(ReadOnlySpan<char> text, int targetRow, in TextEditorOptions options, out int lineStart, out int lineEnd, out int rowInLine)
    {
        _ = text;
        var snapshot = _document.CurrentSnapshot;
        if (snapshot.LineCount == 0)
        {
            lineStart = 0;
            lineEnd = 0;
            rowInLine = 0;
            return false;
        }

        if (!options.WordWrap || _contentWidth <= 0 || _layoutCache.LineCount == 0)
        {
            var lineIndex = Math.Clamp(targetRow, 0, snapshot.LineCount - 1);
            var line = snapshot.GetLine(lineIndex);
            lineStart = line.Start;
            lineEnd = line.End;
            rowInLine = 0;
            return true;
        }

        EnsureMultiLineLayoutCache(options);
        var lineRowInfo = _layoutCache.GetLineFromRow(targetRow);
        var currentLine = snapshot.GetLine(lineRowInfo.LineIndex);
        lineStart = currentLine.Start;
        lineEnd = currentLine.End;
        rowInLine = lineRowInfo.RowInLine;
        return true;
    }

    private static int GetWrapSegmentLength(ReadOnlySpan<char> lineSpan, int startIndex, int wrapWidth, int tabSize)
    {
        if (wrapWidth <= 0)
        {
            return lineSpan.Length - startIndex;
        }

        if (startIndex >= lineSpan.Length)
        {
            return 0;
        }

        var col = 0;
        var i = startIndex;
        var last = i;
        while (i < lineSpan.Length)
        {
            var next = GetNextTextElementIndexFast(lineSpan, i);
            if (next <= i)
            {
                break;
            }

            var width = GetTextElementCellWidth(lineSpan.Slice(i, next - i), col, tabSize);

            if (col + width > wrapWidth && col > 0)
            {
                break;
            }

            col += width;
            last = next;

            if (col >= wrapWidth)
            {
                break;
            }

            i = next;
        }

        return Math.Max(0, last - startIndex);
    }

    private static int GetTextCells(ReadOnlySpan<char> text, int tabSize)
    {
        var col = 0;
        var i = 0;
        while (i < text.Length)
        {
            var next = GetNextTextElementIndexFast(text, i);
            if (next <= i)
            {
                break;
            }

            col += GetTextElementCellWidth(text.Slice(i, next - i), col, tabSize);
            i = next;
        }

        return col;
    }

    private static int GetCellOffsetAtIndex(ReadOnlySpan<char> text, int index, int tabSize)
    {
        index = Math.Clamp(index, 0, text.Length);
        var col = 0;
        var i = 0;
        while (i < index)
        {
            var next = GetNextTextElementIndexFast(text, i);
            if (next <= i)
            {
                break;
            }

            col += GetTextElementCellWidth(text.Slice(i, next - i), col, tabSize);
            i = next;
        }

        return col;
    }

    private static int GetIndexAtCell(ReadOnlySpan<char> text, int targetCell, int tabSize)
    {
        if (targetCell <= 0)
        {
            return 0;
        }

        var col = 0;
        var i = 0;
        while (i < text.Length)
        {
            var next = GetNextTextElementIndexFast(text, i);
            if (next <= i)
            {
                break;
            }

            var width = GetTextElementCellWidth(text.Slice(i, next - i), col, tabSize);
            if (col + width > targetCell)
            {
                return i;
            }

            col += width;
            i = next;
        }

        return text.Length;
    }

    private static int GetTextElementCellWidth(ReadOnlySpan<char> element, int column, int tabSize)
    {
        if (element.Length == 1 && element[0] == '\t')
        {
            var size = Math.Max(1, tabSize);
            return size - (column % size);
        }

        if (element.Length == 1 && element[0] <= '\u007F')
        {
            return 1;
        }

        if (element.Length == 2 && element[0] == '\r' && element[1] == '\n')
        {
            return 1;
        }

        return Math.Max(1, TerminalTextUtility.GetWidth(element));
    }

    private static int GetPreviousWordIndex(ReadOnlySpan<char> text, int caretIndex)
    {
        caretIndex = Math.Clamp(caretIndex, 0, text.Length);
        if (caretIndex == 0)
        {
            return 0;
        }

        var i = caretIndex;
        while (i > 0)
        {
            var prev = GetPreviousTextElementIndexFast(text, i);
            if (!IsWhitespaceAt(text, prev))
            {
                i = prev;
                break;
            }
            i = prev;
        }

        if (i == 0)
        {
            return 0;
        }

        var category = GetCategoryAt(text, i);
        while (i > 0)
        {
            var prev = GetPreviousTextElementIndexFast(text, i);
            if (GetCategoryAt(text, prev) != category)
            {
                break;
            }
            i = prev;
        }

        return i;
    }

    private static int GetNextWordIndex(ReadOnlySpan<char> text, int caretIndex)
    {
        caretIndex = Math.Clamp(caretIndex, 0, text.Length);
        if (caretIndex >= text.Length)
        {
            return text.Length;
        }

        var i = caretIndex;
        while (i < text.Length)
        {
            if (!IsWhitespaceAt(text, i))
            {
                break;
            }
            i = GetNextTextElementIndexFast(text, i);
        }

        if (i >= text.Length)
        {
            return text.Length;
        }

        var category = GetCategoryAt(text, i);
        while (i < text.Length)
        {
            var next = GetNextTextElementIndexFast(text, i);
            if (next >= text.Length)
            {
                return text.Length;
            }

            if (GetCategoryAt(text, next) != category)
            {
                return next;
            }

            i = next;
        }

        return text.Length;
    }

    private enum RuneCategory
    {
        Whitespace,
        Word,
        Other,
    }

    private static RuneCategory GetCategory(Rune rune)
    {
        if (IsWhitespace(rune))
        {
            return RuneCategory.Whitespace;
        }

        if (IsWord(rune))
        {
            return RuneCategory.Word;
        }

        return RuneCategory.Other;
    }

    private static bool IsWhitespace(Rune rune) => Rune.IsWhiteSpace(rune);

    private static bool IsWhitespaceAt(ReadOnlySpan<char> text, int index)
    {
        if (TryReadAsciiTextElementAt(text, index, out var ch))
        {
            return char.IsWhiteSpace(ch);
        }

        return IsWhitespace(ReadTextElementAt(text, index));
    }

    private static bool IsWord(Rune rune)
    {
        if (rune.Value is < 128)
        {
            var ch = (char)rune.Value;
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        return Rune.IsLetterOrDigit(rune) || rune.Value == '_';
    }

    private static RuneCategory GetCategoryAt(ReadOnlySpan<char> text, int index)
    {
        if (TryReadAsciiTextElementAt(text, index, out var ch))
        {
            if (char.IsWhiteSpace(ch))
            {
                return RuneCategory.Whitespace;
            }

            return char.IsLetterOrDigit(ch) || ch == '_'
                ? RuneCategory.Word
                : RuneCategory.Other;
        }

        return GetCategory(ReadTextElementAt(text, index));
    }

    private static Rune ReadTextElementAt(ReadOnlySpan<char> text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return Rune.ReplacementChar;
        }

        if (TryReadAsciiTextElementAt(text, index, out var ch))
        {
            return new Rune(ch);
        }

        var next = GetNextTextElementIndexFast(text, index);
        if (next <= index)
        {
            return Rune.ReplacementChar;
        }

        if (Rune.DecodeFromUtf16(text.Slice(index, next - index), out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
        {
            return Rune.ReplacementChar;
        }

        return rune;
    }

    private static int GetNextTextElementIndexFast(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        if (index >= text.Length)
        {
            return text.Length;
        }

        var ch = text[index];
        if (ch <= '\u007F')
        {
            return ch == '\r' && index + 1 < text.Length && text[index + 1] == '\n'
                ? index + 2
                : index + 1;
        }

        return TerminalTextUtility.GetNextTextElementIndex(text, index);
    }

    private static int GetPreviousTextElementIndexFast(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        if (index == 0)
        {
            return 0;
        }

        var ch = text[index - 1];
        if (ch <= '\u007F')
        {
            return ch == '\n' && index >= 2 && text[index - 2] == '\r'
                ? index - 2
                : index - 1;
        }

        return TerminalTextUtility.GetPreviousTextElementIndex(text, index);
    }

    private static bool TryReadAsciiTextElementAt(ReadOnlySpan<char> text, int index, out char ch)
    {
        ch = default;
        if ((uint)index >= (uint)text.Length)
        {
            return false;
        }

        var current = text[index];
        if (current > '\u007F')
        {
            return false;
        }

        ch = current == '\r' && index + 1 < text.Length && text[index + 1] == '\n'
            ? '\n'
            : current;
        return true;
    }

    internal void SetSearchQuery(in SearchQuery query, in TextEditorOptions options)
    {
        _searchQuery = query;
        RebuildSearchMatches();

        if (_searchMatches.Count == 0)
        {
            _activeSearchMatchIndex = -1;
            IncrementVersion();
            return;
        }

        // Prefer the first match at/after the caret when applying a query.
        var caret = Math.Clamp(_caretIndex, 0, GetText().Length);
        var active = 0;
        for (var i = 0; i < _searchMatches.Count; i++)
        {
            if (_searchMatches[i].Start >= caret)
            {
                active = i;
                break;
            }
        }

        _activeSearchMatchIndex = active;
        SelectActiveSearchMatch(options);
        IncrementVersion();
    }

    internal void GoToNextSearchMatch(in TextEditorOptions options)
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _activeSearchMatchIndex++;
        if (_activeSearchMatchIndex >= _searchMatches.Count)
        {
            _activeSearchMatchIndex = 0;
        }

        SelectActiveSearchMatch(options);
        IncrementVersion();
    }

    internal void GoToPreviousSearchMatch(in TextEditorOptions options)
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _activeSearchMatchIndex--;
        if (_activeSearchMatchIndex < 0)
        {
            _activeSearchMatchIndex = _searchMatches.Count - 1;
        }

        SelectActiveSearchMatch(options);
        IncrementVersion();
    }

    internal int ReplaceCurrentSearchMatch(string replacement, in TextEditorOptions options)
    {
        if (_searchMatches.Count == 0 || (uint)_activeSearchMatchIndex >= (uint)_searchMatches.Count)
        {
            return 0;
        }

        var match = _searchMatches[_activeSearchMatchIndex];
        ApplyReplaceWithUndo(TextUndoRedoManager.TextUndoKind.Replace, match.Start, match.Length, replacement.AsSpan(), allowCoalesce: false, options, () =>
        {
            _caretIndex = match.Start + replacement.Length;
            _preferredColumn = -1;
            ClearSelection();
        });
        RebuildSearchMatches();
        IncrementVersion();
        return 1;
    }

    internal int ReplaceAllSearchMatches(string replacement, in TextEditorOptions options)
    {
        if (_searchMatches.Count == 0)
        {
            return 0;
        }

        _undoRedo.EnsureSynchronized();

        var before = CaptureStateSnapshot();
        _undoRedo.BeginGroup(TextUndoRedoManager.TextUndoKind.ReplaceAll, before);

        var replaced = 0;
        var textBefore = GetText();

        try
        {
            using var __ = _undoRedo.BeginRecording();
            using var _ = _document.BeginUpdate();

            for (var i = _searchMatches.Count - 1; i >= 0; i--)
            {
                var match = _searchMatches[i];
                var removedText = match.Length == 0 ? string.Empty : textBefore.Substring(match.Start, match.Length);
                _document.Replace(match.Start, match.Length, replacement.AsSpan());
                _undoRedo.AddGroupChange(new TextUndoRedoManager.TextChange(match.Start, removedText, replacement));
                replaced++;
            }

            _preferredColumn = -1;
            UpdateAfterDocumentChange(options);
            RebuildSearchMatches();
            IncrementVersion();

            var after = CaptureStateSnapshot();
            _undoRedo.CommitGroup(after);
        }
        finally
        {
            // Ensure the group is not left open if an exception is thrown while applying changes.
            if (_undoRedo.HasOpenGroup)
            {
                _undoRedo.AbortGroup();
            }
        }

        return replaced;
    }

    internal string GetSearchStatusText()
    {
        _ = Version;
        if (string.IsNullOrEmpty(_searchQuery.Text))
        {
            return "No search";
        }

        if (_searchMatches.Count == 0)
        {
            return "0 matches";
        }

        var active = _activeSearchMatchIndex < 0 ? 0 : _activeSearchMatchIndex + 1;
        return $"{active}/{_searchMatches.Count}";
    }

    internal string? GetSearchErrorText()
    {
        _ = Version;
        return _searchError;
    }

    private void SelectActiveSearchMatch(in TextEditorOptions options)
    {
        if ((uint)_activeSearchMatchIndex >= (uint)_searchMatches.Count)
        {
            return;
        }

        var match = _searchMatches[_activeSearchMatchIndex];
        _selectionAnchor = match.Start;
        _selectionEnd = match.Start + match.Length;
        _caretIndex = _selectionEnd;
        _preferredColumn = -1;
        EnsureCaretVisible(options);
    }

    private void RebuildSearchMatches()
    {
        _searchError = null;
        _searchMatches.Clear();

        var queryText = _searchQuery.Text ?? string.Empty;
        if (string.IsNullOrEmpty(queryText))
        {
            _activeSearchMatchIndex = -1;
            return;
        }

        var text = GetText();
        if (text.Length == 0)
        {
            _activeSearchMatchIndex = -1;
            return;
        }

        try
        {
            BuildSearchMatches(text, queryText, _searchQuery);
        }
        catch (ArgumentException ex) when (_searchQuery.UseRegex)
        {
            _searchError = ex.Message;
            _searchMatches.Clear();
            _activeSearchMatchIndex = -1;
        }

        if (_searchMatches.Count == 0)
        {
            _activeSearchMatchIndex = -1;
        }
        else if (_activeSearchMatchIndex < 0)
        {
            _activeSearchMatchIndex = 0;
        }
    }

    private void BuildSearchMatches(string text, string queryText, SearchQuery query)
    {
        if (query.UseRegex)
        {
            var pattern = query.WholeWord ? $"\\b(?:{queryText})\\b" : queryText;
            var options = System.Text.RegularExpressions.RegexOptions.CultureInvariant;
            if (!query.CaseSensitive)
            {
                options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            }

            var regex = new System.Text.RegularExpressions.Regex(pattern, options);
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(text))
            {
                if (!match.Success || match.Length <= 0)
                {
                    continue;
                }

                _searchMatches.Add(new TextMatch(match.Index, match.Length));
            }

            return;
        }

        var comparison = query.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var start = 0;
        while (start < text.Length)
        {
            var found = text.IndexOf(queryText, start, comparison);
            if (found < 0)
            {
                break;
            }

            var ok = true;
            if (query.WholeWord)
            {
                ok = IsWordBoundary(text, found, queryText.Length);
            }

            if (ok)
            {
                _searchMatches.Add(new TextMatch(found, queryText.Length));
            }

            start = found + Math.Max(1, queryText.Length);
        }
    }

    private static bool IsWordBoundary(string text, int start, int length)
        => WordBoundaryUtility.IsWordBoundary(text, start, length);

    private readonly record struct TextMatch(int Start, int Length);
}
