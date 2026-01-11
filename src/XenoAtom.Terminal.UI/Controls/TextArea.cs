// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TextArea : Visual, ICursorProvider
{
    private int _caretIndex;
    private int _selectionAnchor = -1;
    private int _selectionEnd = -1;

    private int _scrollLineOffset;
    private int _scrollColumnOffset;
    private int _preferredColumn = -1;

    private int _contentX;
    private int _contentY;
    private int _contentWidth;
    private int _contentHeight;

    private string? _cachedRawText;
    private string _cachedNormalizedText = string.Empty;
    private string? _cachedLineStartsText;
    private List<int>? _lineStarts;

    public TextArea()
    {
        Focusable = true;
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial string? Placeholder { get; set; }

    [Bindable]
    public partial bool AcceptTab { get; set; }

    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            var t = GetText();
            _caretIndex = Math.Clamp(value, 0, t.Length);
            _preferredColumn = -1;
            ClearSelection();
            EnsureCaretVisible();
            Invalidate();
        }
    }

    private bool HasSelection => _selectionAnchor >= 0 && _selectionEnd >= 0 && _selectionAnchor != _selectionEnd;

    public bool TryGetCursorCell(out int x, out int y)
    {
        x = 0;
        y = 0;

        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return false;
        }

        var t = GetText();
        var caret = Math.Clamp(_caretIndex, 0, t.Length);

        var (line, col) = GetLineColumnForIndex(caret);
        var visibleLine = line - _scrollLineOffset;
        var visibleCol = col - _scrollColumnOffset;
        if ((uint)visibleLine >= (uint)_contentHeight || (uint)visibleCol >= (uint)_contentWidth)
        {
            return false;
        }

        x = _contentX + visibleCol;
        y = _contentY + visibleLine;
        return true;
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;

        var width = 32;
        var height = 10;

        if (showBorder)
        {
            width = Math.Max(width, 3);
            height = Math.Max(height, 3);
        }

        return SizeHints.Fixed(constraints.Clamp(new Size(width, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;
        var padding = style.Padding;

        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = finalRect.Width;
        var innerHeight = finalRect.Height;
        if (showBorder && finalRect.Width >= 2 && finalRect.Height >= 2)
        {
            innerLeft++;
            innerTop++;
            innerWidth = Math.Max(0, innerWidth - 2);
            innerHeight = Math.Max(0, innerHeight - 2);
        }

        _contentX = innerLeft + padding.Left;
        _contentY = innerTop + padding.Top;
        _contentWidth = Math.Max(0, innerWidth - padding.Horizontal);
        _contentHeight = Math.Max(0, innerHeight - padding.Vertical);

        EnsureCaretVisible();
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var isFocused = ReferenceEquals(App?.FocusedElement, this);
        var theme = GetTheme();
        var style = Get<TextAreaStyle>();
        var showBorder = style.ShowBorder;
        var borderStyle = style.BorderStyle(theme, isFocused);
        var selectionStyle = style.SelectionStyle(theme);
        var backgroundStyle = style.BackgroundStyle(theme);
        var placeholderStyle = style.PlaceholderStyle(theme);
        var padding = style.Padding;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = rect.Width;
        var innerHeight = rect.Height;
        if (showBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            var glyphs = theme.Lines;
            var left = rect.X;
            var top = rect.Y;
            var right = rect.X + rect.Width - 1;
            var bottom = rect.Y + rect.Height - 1;

            buffer.SetCell(left, top, glyphs.TopLeft, borderStyle);
            buffer.SetCell(right, top, glyphs.TopRight, borderStyle);
            buffer.SetCell(left, bottom, glyphs.BottomLeft, borderStyle);
            buffer.SetCell(right, bottom, glyphs.BottomRight, borderStyle);

            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, borderStyle);
                buffer.SetCell(x, bottom, glyphs.Horizontal, borderStyle);
            }

            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, borderStyle);
                buffer.SetCell(right, y, glyphs.Vertical, borderStyle);
            }

            innerLeft = rect.X + 1;
            innerTop = rect.Y + 1;
            innerWidth = Math.Max(0, rect.Width - 2);
            innerHeight = Math.Max(0, rect.Height - 2);
        }

        var contentX = innerLeft + padding.Left;
        var contentY = innerTop + padding.Top;
        var contentWidth = Math.Max(0, innerWidth - padding.Horizontal);
        var contentHeight = Math.Max(0, innerHeight - padding.Vertical);

        // Fill background (text area only).
        for (var y = contentY; y < contentY + contentHeight; y++)
        {
            for (var x = contentX; x < contentX + contentWidth; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
            }
        }

        _contentX = contentX;
        _contentY = contentY;
        _contentWidth = contentWidth;
        _contentHeight = contentHeight;

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return;
        }

        var t = GetText();
        var lineStarts = GetLineStarts(t);
        var lineCount = Math.Max(1, lineStarts.Count);

        var selectionStart = 0;
        var selectionEnd = 0;
        if (HasSelection)
        {
            selectionStart = Math.Min(_selectionAnchor, _selectionEnd);
            selectionEnd = Math.Max(_selectionAnchor, _selectionEnd);
        }

        // Placeholder.
        if (t.Length == 0 && !string.IsNullOrEmpty(Placeholder))
        {
            var ph = Placeholder.AsSpan();
            var phLen = Math.Min(contentWidth, TerminalTextUtility.GetWidth(ph));
            if (phLen > 0)
            {
                buffer.WriteText(contentX, contentY, ph[..Math.Min(ph.Length, contentWidth)], placeholderStyle);
            }
            return;
        }

        for (var row = 0; row < contentHeight; row++)
        {
            var lineIndex = _scrollLineOffset + row;
            if ((uint)lineIndex >= (uint)lineCount)
            {
                continue;
            }

            var start = lineStarts[lineIndex];
            var end = (lineIndex + 1) < lineStarts.Count ? lineStarts[lineIndex + 1] - 1 : t.Length;
            if (end < start)
            {
                end = start;
            }

            var lineSpan = t.AsSpan(start, end - start);
            var skip = Math.Clamp(_scrollColumnOffset, 0, lineSpan.Length);
            var visibleSpan = lineSpan[skip..];
            if (visibleSpan.Length > contentWidth)
            {
                visibleSpan = visibleSpan[..contentWidth];
            }

            var y = contentY + row;

            if (!HasSelection)
            {
                buffer.WriteText(contentX, y, visibleSpan, backgroundStyle);
                continue;
            }

            var lineSelStart = Math.Clamp(selectionStart, start, end);
            var lineSelEnd = Math.Clamp(selectionEnd, start, end);

            // Selection doesn't intersect this line.
            if (lineSelEnd <= lineSelStart)
            {
                buffer.WriteText(contentX, y, visibleSpan, backgroundStyle);
                continue;
            }

            var localSelStart = lineSelStart - start;
            var localSelEnd = lineSelEnd - start;

            // Apply horizontal scroll window.
            var visSelStart = Math.Clamp(localSelStart - _scrollColumnOffset, 0, contentWidth);
            var visSelEnd = Math.Clamp(localSelEnd - _scrollColumnOffset, 0, contentWidth);

            var leftSpan = visibleSpan[..Math.Min(visibleSpan.Length, visSelStart)];
            var selSpan = visibleSpan.Slice(Math.Min(visibleSpan.Length, visSelStart), Math.Max(0, Math.Min(visibleSpan.Length, visSelEnd) - Math.Min(visibleSpan.Length, visSelStart)));
            var rightSpanStart = Math.Min(visibleSpan.Length, visSelEnd);
            var rightSpan = visibleSpan[rightSpanStart..];

            if (!leftSpan.IsEmpty)
            {
                buffer.WriteText(contentX, y, leftSpan, backgroundStyle);
            }

            if (!selSpan.IsEmpty)
            {
                buffer.WriteText(contentX + visSelStart, y, selSpan, selectionStyle);
            }

            if (!rightSpan.IsEmpty)
            {
                buffer.WriteText(contentX + rightSpanStart, y, rightSpan, backgroundStyle);
            }
        }

        if (isFocused && !HasSelection)
        {
            if (TryGetCursorCell(out var cx, out var cy))
            {
                buffer.SetCell(cx, cy, new Rune(' '), CellStyle.None | TextStyle.Invert);
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

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        var x = Math.Clamp(e.UiX - _contentX + _scrollColumnOffset, 0, LayoutConstants.Infinite);
        var y = Math.Clamp(e.UiY - _contentY + _scrollLineOffset, 0, LayoutConstants.Infinite);

        var t = GetText();
        var lineStarts = GetLineStarts(t);
        var line = Math.Clamp(y, 0, Math.Max(0, lineStarts.Count - 1));
        var start = lineStarts[line];
        var end = (line + 1) < lineStarts.Count ? lineStarts[line + 1] - 1 : t.Length;
        var col = Math.Clamp(x, 0, Math.Max(0, end - start));

        _caretIndex = start + col;
        _preferredColumn = -1;
        ClearSelection();
        EnsureCaretVisible();
        Invalidate();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var text = GetText();
        _caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        var ctrl = (e.Modifiers & TerminalModifiers.Ctrl) != 0;
        var shift = (e.Modifiers & TerminalModifiers.Shift) != 0;

        if (!shift && HasSelection && e.Key is TerminalKey.Left or TerminalKey.Right or TerminalKey.Up or TerminalKey.Down or TerminalKey.Home or TerminalKey.End)
        {
            ClearSelection();
        }

        if (ctrl)
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

            if (e.Key == TerminalKey.Home)
            {
                MoveCaretTo(0, shift);
                e.Handled = true;
                return;
            }

            if (e.Key == TerminalKey.End)
            {
                MoveCaretTo(text.Length, shift);
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                MoveCaretHorizontal(-1, shift);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                MoveCaretHorizontal(1, shift);
                e.Handled = true;
                return;
            case TerminalKey.Up:
                MoveCaretVertical(-1, shift);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                MoveCaretVertical(1, shift);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                MoveCaretToLineBoundary(start: true, shift);
                e.Handled = true;
                return;
            case TerminalKey.End:
                MoveCaretToLineBoundary(start: false, shift);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                MoveCaretVertical(-Math.Max(1, _contentHeight), shift);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                MoveCaretVertical(Math.Max(1, _contentHeight), shift);
                e.Handled = true;
                return;
            case TerminalKey.Backspace:
                Backspace();
                e.Handled = true;
                return;
            case TerminalKey.Delete:
                Delete();
                e.Handled = true;
                return;
            case TerminalKey.Enter:
                InsertText("\n");
                e.Handled = true;
                return;
            case TerminalKey.Tab:
                if (AcceptTab)
                {
                    InsertText("\t");
                    e.Handled = true;
                }
                return;
        }
    }

    private string GetText()
    {
        var raw = Text ?? string.Empty;
        if (ReferenceEquals(raw, _cachedRawText))
        {
            return _cachedNormalizedText;
        }

        _cachedRawText = raw;
        _cachedNormalizedText = raw.Contains('\r')
            ? raw.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal)
            : raw;

        _cachedLineStartsText = null;
        return _cachedNormalizedText;
    }

    private List<int> GetLineStarts(string text)
    {
        if (_lineStarts is not null && ReferenceEquals(text, _cachedLineStartsText))
        {
            return _lineStarts;
        }

        _cachedLineStartsText = text;
        _lineStarts = new List<int>(capacity: 32) { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                _lineStarts.Add(i + 1);
            }
        }

        if (_lineStarts.Count == 0)
        {
            _lineStarts.Add(0);
        }

        return _lineStarts;
    }

    private (int Line, int Column) GetLineColumnForIndex(int index)
    {
        var t = GetText();
        var starts = GetLineStarts(t);
        var i = Math.Clamp(index, 0, t.Length);

        var line = 0;
        for (var l = 0; l < starts.Count; l++)
        {
            if (starts[l] <= i)
            {
                line = l;
            }
            else
            {
                break;
            }
        }

        var start = starts[line];
        var end = (line + 1) < starts.Count ? starts[line + 1] - 1 : t.Length;
        var col = Math.Clamp(i - start, 0, Math.Max(0, end - start));
        return (line, col);
    }

    private int GetIndexForLineColumn(int line, int column)
    {
        var t = GetText();
        var starts = GetLineStarts(t);
        if (starts.Count == 0)
        {
            return 0;
        }

        line = Math.Clamp(line, 0, starts.Count - 1);
        var start = starts[line];
        var end = (line + 1) < starts.Count ? starts[line + 1] - 1 : t.Length;
        var col = Math.Clamp(column, 0, Math.Max(0, end - start));
        return start + col;
    }

    private void EnsureCaretVisible()
    {
        if (_contentWidth <= 0 || _contentHeight <= 0)
        {
            return;
        }

        var (line, col) = GetLineColumnForIndex(_caretIndex);

        if (line < _scrollLineOffset)
        {
            _scrollLineOffset = line;
        }
        else if (line >= _scrollLineOffset + _contentHeight)
        {
            _scrollLineOffset = Math.Max(0, line - _contentHeight + 1);
        }

        if (col < _scrollColumnOffset)
        {
            _scrollColumnOffset = col;
        }
        else if (col >= _scrollColumnOffset + _contentWidth)
        {
            _scrollColumnOffset = Math.Max(0, col - _contentWidth + 1);
        }
    }

    private void MoveCaretTo(int index, bool extendSelection)
    {
        index = Math.Clamp(index, 0, GetText().Length);
        if (extendSelection)
        {
            ExtendSelection(index);
        }
        else
        {
            ClearSelection();
        }
        _caretIndex = index;
        _preferredColumn = -1;
        EnsureCaretVisible();
        Invalidate();
    }

    private void MoveCaretHorizontal(int delta, bool extendSelection)
    {
        var next = Math.Clamp(_caretIndex + delta, 0, GetText().Length);
        MoveCaretTo(next, extendSelection);
    }

    private void MoveCaretVertical(int deltaLines, bool extendSelection)
    {
        var (line, col) = GetLineColumnForIndex(_caretIndex);
        if (_preferredColumn < 0)
        {
            _preferredColumn = col;
        }

        var newLine = line + deltaLines;
        var next = GetIndexForLineColumn(newLine, _preferredColumn);
        MoveCaretTo(next, extendSelection);
    }

    private void MoveCaretToLineBoundary(bool start, bool extendSelection)
    {
        var t = GetText();
        var (line, _) = GetLineColumnForIndex(_caretIndex);
        var starts = GetLineStarts(t);
        var lineStart = starts[line];
        var lineEnd = (line + 1) < starts.Count ? starts[line + 1] - 1 : t.Length;
        MoveCaretTo(start ? lineStart : lineEnd, extendSelection);
    }

    private void InsertText(string raw)
    {
        var text = GetText();
        var toInsert = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        if (toInsert.Length == 0)
        {
            return;
        }

        if (HasSelection)
        {
            DeleteSelection();
            text = GetText();
        }

        var idx = Math.Clamp(_caretIndex, 0, text.Length);
        Text = text.Insert(idx, toInsert);
        _caretIndex = idx + toInsert.Length;
        _preferredColumn = -1;
        EnsureCaretVisible();
        Invalidate();
    }

    private void Backspace()
    {
        var text = GetText();
        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_caretIndex <= 0 || text.Length == 0)
        {
            return;
        }

        var idx = Math.Clamp(_caretIndex, 0, text.Length);
        Text = text.Remove(idx - 1, 1);
        _caretIndex = idx - 1;
        _preferredColumn = -1;
        EnsureCaretVisible();
        Invalidate();
    }

    private void Delete()
    {
        var text = GetText();
        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        var idx = Math.Clamp(_caretIndex, 0, text.Length);
        if (idx >= text.Length)
        {
            return;
        }

        Text = text.Remove(idx, 1);
        _preferredColumn = -1;
        EnsureCaretVisible();
        Invalidate();
    }

    private void SelectAll()
    {
        var t = GetText();
        if (t.Length == 0)
        {
            return;
        }

        _selectionAnchor = 0;
        _selectionEnd = t.Length;
        _caretIndex = t.Length;
        Invalidate();
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionEnd = -1;
    }

    private void ExtendSelection(int caret)
    {
        if (_selectionAnchor < 0)
        {
            _selectionAnchor = _caretIndex;
        }

        _selectionEnd = caret;
    }

    private void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var text = GetText();
        var start = Math.Min(_selectionAnchor, _selectionEnd);
        var end = Math.Max(_selectionAnchor, _selectionEnd);
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end <= start)
        {
            ClearSelection();
            return;
        }

        Text = text.Remove(start, end - start);
        _caretIndex = start;
        ClearSelection();
        _preferredColumn = -1;
        EnsureCaretVisible();
        Invalidate();
    }

    private ReadOnlySpan<char> GetSelectedTextSpan(ReadOnlySpan<char> text)
    {
        if (!HasSelection || text.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var start = Math.Clamp(Math.Min(_selectionAnchor, _selectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(_selectionAnchor, _selectionEnd), 0, text.Length);
        if (end <= start)
        {
            return ReadOnlySpan<char>.Empty;
        }

        return text[start..end];
    }
}
