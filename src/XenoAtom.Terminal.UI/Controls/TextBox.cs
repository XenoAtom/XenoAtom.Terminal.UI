// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TextBox : Visual, ICursorProvider
{
    private int _caretIndex;
    private int _scrollCellOffset;
    private int _selectionAnchor = -1;
    private int _selectionEnd = -1;
    private string? _killBuffer;

    public TextBox()
    {
        Focusable = true;
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial string? Placeholder { get; set; }

    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            var t = Text ?? string.Empty;
            _caretIndex = Math.Clamp(value, 0, t.Length);
            ClearSelection();
            Invalidate();
        }
    }

    private bool HasSelection => _selectionAnchor >= 0 && _selectionEnd >= 0 && _selectionAnchor != _selectionEnd;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Max(10, Math.Min(availableSize.Width, 24));
        var height = Get<TextBoxStyle>().ShowBorder ? 3 : 1;
        return new Size(width, Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
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
        var textBoxStyle = Get<TextBoxStyle>();
        var borderStyle = textBoxStyle.BorderStyle(theme, isFocused);
        var selectionStyle = textBoxStyle.SelectionStyle(theme);
        var backgroundStyle = textBoxStyle.BackgroundStyle(theme);
        var placeholderStyle = textBoxStyle.PlaceholderStyle(theme);
        var padding = textBoxStyle.Padding;
        var showBorder = textBoxStyle.ShowBorder;

        var textRowY = rect.Y;
        var innerLeft = rect.X;
        var innerWidth = rect.Width;
        var innerTop = rect.Y;
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

            textRowY = rect.Y + 1;
            innerLeft = rect.X + 1;
            innerWidth = Math.Max(0, rect.Width - 2);
            innerTop = rect.Y + 1;
            innerHeight = Math.Max(0, rect.Height - 2);
        }

        // Fill background (text area only).
        if (innerWidth > 0 && innerHeight > 0)
        {
            for (var y = innerTop; y < innerTop + innerHeight; y++)
            {
                for (var x = innerLeft; x < innerLeft + innerWidth; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        var contentX = innerLeft + padding.Left;
        var contentWidth = Math.Max(0, innerWidth - padding.Horizontal);

        var text = Text ?? string.Empty;
        var alignment = TextAlignment;

        if (contentWidth == 0)
        {
            return;
        }

        var totalTextCells = TerminalTextUtility.GetWidth(text.AsSpan());
        var fits = totalTextCells <= contentWidth;

        var caretCells = TerminalTextUtility.GetWidth(text.AsSpan(0, Math.Clamp(_caretIndex, 0, text.Length)));
        if (fits)
        {
            _scrollCellOffset = 0;
        }

        if (caretCells < _scrollCellOffset)
        {
            _scrollCellOffset = caretCells;
        }
        else if (caretCells >= _scrollCellOffset + contentWidth)
        {
            _scrollCellOffset = Math.Max(0, caretCells - contentWidth + 1);
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), _scrollCellOffset, out var startIndex))
        {
            startIndex = 0;
        }

        TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), _scrollCellOffset + contentWidth, out var endIndex);
        endIndex = Math.Clamp(endIndex, startIndex, text.Length);

        var contentXAligned = contentX;
        if (fits && alignment != TextAlignment.Left && alignment != TextAlignment.Justify)
        {
            var shift = alignment == TextAlignment.Center ? (contentWidth - totalTextCells) / 2 : (contentWidth - totalTextCells);
            contentXAligned = contentX + Math.Max(0, shift);
        }

        if (!HasSelection)
        {
            if (text.Length == 0 && !isFocused && !string.IsNullOrEmpty(Placeholder))
            {
                var placeholder = Placeholder.AsSpan();
                if (alignment != TextAlignment.Left && alignment != TextAlignment.Justify)
                {
                    var placeholderCells = TerminalTextUtility.GetWidth(placeholder);
                    if (placeholderCells < contentWidth)
                    {
                        var shift = alignment == TextAlignment.Center ? (contentWidth - placeholderCells) / 2 : (contentWidth - placeholderCells);
                        contentXAligned = contentX + Math.Max(0, shift);
                    }
                    else
                    {
                        contentXAligned = contentX;
                    }
                }

                buffer.WriteText(contentXAligned, textRowY, placeholder, placeholderStyle);
            }
            else
            {
                buffer.WriteText(contentXAligned, textRowY, text.AsSpan(startIndex, endIndex - startIndex), backgroundStyle);
            }
        }
        else
        {
            var (selStart, selEnd) = GetOrderedSelection();
            var visSelStart = Math.Clamp(selStart, startIndex, endIndex);
            var visSelEnd = Math.Clamp(selEnd, startIndex, endIndex);

            if (visSelStart > startIndex)
            {
                buffer.WriteText(contentXAligned, textRowY, text.AsSpan(startIndex, visSelStart - startIndex), backgroundStyle);
            }

            if (visSelEnd > visSelStart)
            {
                var selStartCell = TerminalTextUtility.GetWidth(text.AsSpan(startIndex, visSelStart - startIndex));
                buffer.WriteText(contentXAligned + selStartCell, textRowY, text.AsSpan(visSelStart, visSelEnd - visSelStart), selectionStyle);
            }

            if (endIndex > visSelEnd)
            {
                var selEndCell = TerminalTextUtility.GetWidth(text.AsSpan(startIndex, visSelEnd - startIndex));
                buffer.WriteText(contentXAligned + selEndCell, textRowY, text.AsSpan(visSelEnd, endIndex - visSelEnd), backgroundStyle);
            }
        }

        if (isFocused)
        {
            var caretX = caretCells - _scrollCellOffset;
            if (caretX >= 0 && caretX < contentWidth)
            {
                if (!HasSelection)
                {
                    buffer.SetCell(contentXAligned + caretX, textRowY, new Rune(' '), CellStyle.None | TextStyle.Invert);
                }
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

            if (e.Char is 'k' or 'K')
            {
                if (HasSelection)
                {
                    _killBuffer = GetSelectedTextSpan(text.AsSpan()).ToString();
                    DeleteSelection();
                }
                else if (_caretIndex < text.Length)
                {
                    _killBuffer = text[_caretIndex..];
                    Text = text[.._caretIndex];
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'u' or 'U')
            {
                if (HasSelection)
                {
                    _killBuffer = GetSelectedTextSpan(text.AsSpan()).ToString();
                    DeleteSelection();
                }
                else if (_caretIndex > 0)
                {
                    _killBuffer = text[.._caretIndex];
                    Text = text[_caretIndex..];
                    _caretIndex = 0;
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'w' or 'W')
            {
                if (HasSelection)
                {
                    _killBuffer = GetSelectedTextSpan(text.AsSpan()).ToString();
                    DeleteSelection();
                }
                else if (_caretIndex > 0)
                {
                    var prev = GetPreviousWordIndex(text.AsSpan(), _caretIndex);
                    _killBuffer = text[prev.._caretIndex];
                    Text = string.Concat(text.AsSpan(0, prev), text.AsSpan(_caretIndex));
                    _caretIndex = prev;
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'y' or 'Y')
            {
                if (!string.IsNullOrEmpty(_killBuffer))
                {
                    InsertText(_killBuffer);
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
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var style = Get<TextBoxStyle>();
        var padding = style.Padding;

        var showBorder = style.ShowBorder;
        var textRowY = showBorder ? 1 : 0;
        if (e.LocalY != textRowY)
        {
            // Clicking border rows doesn't reposition the caret.
            e.Handled = true;
            return;
        }

        var innerLeft = showBorder ? 1 : 0;
        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var contentX = innerLeft + padding.Left;
        var contentWidth = Math.Max(0, innerWidth - padding.Horizontal);
        if (contentWidth <= 0)
        {
            return;
        }

        var localCell = Math.Clamp(e.LocalX - contentX, 0, contentWidth) + _scrollCellOffset;
        var text = Text ?? string.Empty;

        if (!TerminalTextUtility.TryGetIndexAtCell(text.AsSpan(), localCell, out var index))
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
        Invalidate();
    }

    private void UpdateSelectionAfterCaretMove(bool shift, int oldCaretIndex)
    {
        if (!shift)
        {
            ClearSelection();
            Invalidate();
            return;
        }

        if (_selectionAnchor < 0)
        {
            _selectionAnchor = oldCaretIndex;
        }

        _selectionEnd = _caretIndex;
        Invalidate();
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

    public bool TryGetCursorCell(out int x, out int y)
    {
        x = 0;
        y = 0;

        if (!ReferenceEquals(App?.FocusedElement, this) || !IsVisible || !IsEnabled)
        {
            return false;
        }

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return false;
        }

        var style = Get<TextBoxStyle>();
        var padding = style.Padding;
        var showBorder = style.ShowBorder;

        var innerWidth = Math.Max(0, rect.Width - (showBorder ? 2 : 0));
        var contentWidth = Math.Max(0, innerWidth - padding.Horizontal);
        if (contentWidth == 0)
        {
            return false;
        }

        var text = Text ?? string.Empty;
        var caretIndex = Math.Clamp(_caretIndex, 0, text.Length);
        var caretCells = TerminalTextUtility.GetWidth(text.AsSpan(0, caretIndex));

        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var caretX = caretCells - _scrollCellOffset;
        if (caretX < 0)
        {
            caretX = 0;
        }

        caretX = Math.Min(contentWidth, caretX);

        var alignedOffset = 0;
        var alignment = TextAlignment;
        var totalTextCells = TerminalTextUtility.GetWidth(text.AsSpan());
        if (_scrollCellOffset == 0 && totalTextCells <= contentWidth && alignment != TextAlignment.Left && alignment != TextAlignment.Justify)
        {
            alignedOffset = alignment == TextAlignment.Center ? (contentWidth - totalTextCells) / 2 : (contentWidth - totalTextCells);
            alignedOffset = Math.Max(0, alignedOffset);
        }

        x = innerLeft + padding.Left + alignedOffset + caretX;
        y = rect.Y + (showBorder ? 1 : 0);
        return true;
    }
}
