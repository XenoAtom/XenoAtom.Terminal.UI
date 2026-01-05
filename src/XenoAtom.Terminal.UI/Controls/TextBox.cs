// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using System.Buffers;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class TextBox : Visual
{
    private int _caretIndex;
    private int _scrollCellOffset;
    private int _selectionAnchor = -1;
    private int _selectionEnd = -1;

    public TextBox()
    {
        Focusable = true;
    }

    [Bindable]
    public partial string? Text { get; set; }

    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            var t = Text ?? string.Empty;
            _caretIndex = Math.Clamp(value, 0, t.Length);
            ClearSelection();
            App?.RequestRender();
        }
    }

    private bool HasSelection => _selectionAnchor >= 0 && _selectionEnd >= 0 && _selectionAnchor != _selectionEnd;

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var width = Math.Max(3, Math.Min(availableSize.Width, 12));
        return new CellSize(width, 1);
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0)
        {
            return;
        }

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var borderStyle = theme.BorderStyle(isFocused);

        var text = Text ?? string.Empty;
        var innerWidth = Math.Max(0, rect.Width - 2);

        if (innerWidth == 0)
        {
            buffer.SetCell(rect.X, rect.Y, new Rune('['), borderStyle);
            buffer.SetCell(rect.X + rect.Width - 1, rect.Y, new Rune(']'), borderStyle);
            return;
        }

        var caretCells = TerminalTextUtility.GetWidth(text.AsSpan(0, Math.Clamp(_caretIndex, 0, text.Length)));

        if (caretCells < _scrollCellOffset)
        {
            _scrollCellOffset = caretCells;
        }
        else if (caretCells >= _scrollCellOffset + innerWidth)
        {
            _scrollCellOffset = Math.Max(0, caretCells - innerWidth + 1);
        }

        buffer.SetCell(rect.X, rect.Y, new Rune('['), borderStyle);
        buffer.SetCell(rect.X + rect.Width - 1, rect.Y, new Rune(']'), borderStyle);

        for (var i = 0; i < innerWidth; i++)
        {
            buffer.SetCell(rect.X + 1 + i, rect.Y, new Rune(' '), CellStyle.None);
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), _scrollCellOffset, out var startIndex))
        {
            startIndex = 0;
        }

        TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), _scrollCellOffset + innerWidth, out var endIndex);
        endIndex = Math.Clamp(endIndex, startIndex, text.Length);

        if (!HasSelection)
        {
            buffer.WriteText(rect.X + 1, rect.Y, text.AsSpan(startIndex, endIndex - startIndex), CellStyle.None);
        }
        else
        {
            var (selStart, selEnd) = GetOrderedSelection();
            var visSelStart = Math.Clamp(selStart, startIndex, endIndex);
            var visSelEnd = Math.Clamp(selEnd, startIndex, endIndex);

            if (visSelStart > startIndex)
            {
                buffer.WriteText(rect.X + 1, rect.Y, text.AsSpan(startIndex, visSelStart - startIndex), CellStyle.None);
            }

            if (visSelEnd > visSelStart)
            {
                var selStartCell = TerminalTextUtility.GetWidth(text.AsSpan(startIndex, visSelStart - startIndex));
                buffer.WriteText(rect.X + 1 + selStartCell, rect.Y, text.AsSpan(visSelStart, visSelEnd - visSelStart), theme.SelectionStyle());
            }

            if (endIndex > visSelEnd)
            {
                var selEndCell = TerminalTextUtility.GetWidth(text.AsSpan(startIndex, visSelEnd - startIndex));
                buffer.WriteText(rect.X + 1 + selEndCell, rect.Y, text.AsSpan(visSelEnd, endIndex - visSelEnd), CellStyle.None);
            }
        }

        if (isFocused)
        {
            var caretX = caretCells - _scrollCellOffset;
            if (caretX >= 0 && caretX < innerWidth)
            {
                if (!HasSelection)
                {
                    buffer.SetCell(rect.X + 1 + caretX, rect.Y, new Rune(' '), CellStyle.Invert);
                }
            }
            else if (caretX == innerWidth)
            {
                buffer.SetCell(rect.X + rect.Width - 1, rect.Y, new Rune(']'), CellStyle.Invert);
            }
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        InsertText(e.Text);
        e.Handled = true;
    }

    protected override void OnPaste(PasteEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        InsertText(e.Text);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var text = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        var ctrl = (e.Modifiers & TerminalModifiers.Ctrl) != 0;
        var shift = (e.Modifiers & TerminalModifiers.Shift) != 0;

        if (!shift && HasSelection && e.Key is TerminalKey.Left or TerminalKey.Right or TerminalKey.Home or TerminalKey.End)
        {
            ClearSelection();
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0)
        {
            if (e.Char is 'a' or 'A')
            {
                SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Char is 'v' or 'V')
            {
                var clip = App?.Terminal.Clipboard.Text;
                if (!string.IsNullOrEmpty(clip))
                {
                    InsertText(clip);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'c' or 'C')
            {
                var span = GetSelectedTextSpan(text.AsSpan());
                if (!span.IsEmpty)
                {
                    App?.Terminal.Clipboard.TrySetText(span);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'x' or 'X')
            {
                if (HasSelection)
                {
                    var span = GetSelectedTextSpan(text.AsSpan());
                    if (!span.IsEmpty)
                    {
                        App?.Terminal.Clipboard.TrySetText(span);
                    }
                    DeleteSelection();
                }
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                var oldCaretLeft = _caretIndex;
                _caretIndex = ctrl ? GetPreviousWordIndex(text.AsSpan(), _caretIndex) : TerminalTextUtility.GetPreviousRuneIndex(text.AsSpan(), _caretIndex);
                UpdateSelectionAfterCaretMove(shift, oldCaretLeft);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                var oldCaretRight = _caretIndex;
                _caretIndex = ctrl ? GetNextWordIndex(text.AsSpan(), _caretIndex) : TerminalTextUtility.GetNextRuneIndex(text.AsSpan(), _caretIndex);
                UpdateSelectionAfterCaretMove(shift, oldCaretRight);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                var oldCaretHome = _caretIndex;
                _caretIndex = 0;
                UpdateSelectionAfterCaretMove(shift, oldCaretHome);
                e.Handled = true;
                return;
            case TerminalKey.End:
                var oldCaretEnd = _caretIndex;
                _caretIndex = text.Length;
                UpdateSelectionAfterCaretMove(shift, oldCaretEnd);
                e.Handled = true;
                return;
            case TerminalKey.Backspace:
                if (HasSelection)
                {
                    DeleteSelection();
                }
                else if (_caretIndex > 0)
                {
                    var prev = ctrl ? GetPreviousWordIndex(text.AsSpan(), _caretIndex) : TerminalTextUtility.GetPreviousRuneIndex(text.AsSpan(), _caretIndex);
                    Text = string.Concat(text.AsSpan(0, prev), text.AsSpan(_caretIndex));
                    _caretIndex = prev;
                }
                e.Handled = true;
                return;
            case TerminalKey.Delete:
                if (HasSelection)
                {
                    DeleteSelection();
                }
                else if (_caretIndex < text.Length)
                {
                    var next = ctrl ? GetNextWordIndex(text.AsSpan(), _caretIndex) : TerminalTextUtility.GetNextRuneIndex(text.AsSpan(), _caretIndex);
                    Text = string.Concat(text.AsSpan(0, _caretIndex), text.AsSpan(next));
                }
                e.Handled = true;
                return;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var rect = Bounds;
        var innerWidth = Math.Max(0, rect.Width - 2);
        if (innerWidth <= 0)
        {
            return;
        }

        var cell = Math.Clamp(e.LocalX - 1, 0, innerWidth) + _scrollCellOffset;
        var text = Text ?? string.Empty;

        if (!TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), cell, out var index))
        {
            index = text.Length;
        }

        CaretIndex = index;
        e.Handled = true;
    }

    private void InsertText(string insert)
    {
        if (string.IsNullOrEmpty(insert))
        {
            return;
        }

        var text = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        if (HasSelection)
        {
            DeleteSelection();
            text = Text ?? string.Empty;
        }

        var newText = string.Concat(text.AsSpan(0, _caretIndex), insert.AsSpan(), text.AsSpan(_caretIndex));
        Text = newText;
        _caretIndex += insert.Length;
        ClearSelection();
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionEnd = -1;
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
        if (!HasSelection)
        {
            return text;
        }

        var (start, end) = GetOrderedSelection();
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        return end > start ? text.Slice(start, end - start) : ReadOnlySpan<char>.Empty;
    }

    private void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var (start, end) = GetOrderedSelection();
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        Text = string.Concat(text.AsSpan(0, start), text.AsSpan(end));
        _caretIndex = start;
        ClearSelection();
    }

    private void SelectAll()
    {
        var text = Text ?? string.Empty;
        _selectionAnchor = 0;
        _selectionEnd = text.Length;
        _caretIndex = text.Length;
        App?.RequestRender();
    }

    private void UpdateSelectionAfterCaretMove(bool shift, int oldCaretIndex)
    {
        if (!shift)
        {
            ClearSelection();
            App?.RequestRender();
            return;
        }

        if (_selectionAnchor < 0)
        {
            _selectionAnchor = oldCaretIndex;
        }

        _selectionEnd = _caretIndex;
        App?.RequestRender();
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
            var prev = TerminalTextUtility.GetPreviousRuneIndex(text, i);
            if (!IsWhitespace(ReadRuneAt(text, prev)))
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

        var category = GetCategory(ReadRuneAt(text, i));
        while (i > 0)
        {
            var prev = TerminalTextUtility.GetPreviousRuneIndex(text, i);
            if (GetCategory(ReadRuneAt(text, prev)) != category)
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
            var rune = ReadRuneAt(text, i);
            if (!IsWhitespace(rune))
            {
                break;
            }
            i = TerminalTextUtility.GetNextRuneIndex(text, i);
        }

        if (i >= text.Length)
        {
            return text.Length;
        }

        var category = GetCategory(ReadRuneAt(text, i));
        while (i < text.Length)
        {
            var next = TerminalTextUtility.GetNextRuneIndex(text, i);
            if (next >= text.Length)
            {
                return text.Length;
            }

            if (GetCategory(ReadRuneAt(text, next)) != category)
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

    private static bool IsWord(Rune rune)
    {
        if (rune.Value is < 128)
        {
            var ch = (char)rune.Value;
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        return Rune.IsLetterOrDigit(rune) || rune.Value == '_';
    }

    private static Rune ReadRuneAt(ReadOnlySpan<char> text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return Rune.ReplacementChar;
        }

        if (Rune.DecodeFromUtf16(text[index..], out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
        {
            return Rune.ReplacementChar;
        }

        return rune;
    }
}
