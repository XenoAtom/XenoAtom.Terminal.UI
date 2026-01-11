// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public enum MaskedInputRevealMode
{
    Never = 0,
    WhileFocused = 1,
    Always = 2,
}

public enum MaskedInputClipboardMode
{
    Disabled = 0,
    CopyText = 1,
}

public sealed partial class MaskedInput : Visual, ICursorProvider
{
    private int _caretIndex;
    private int _scrollCellOffset;
    private int _selectionAnchor = -1;
    private int _selectionEnd = -1;
    private string? _killBuffer;

    public MaskedInput()
    {
        Focusable = true;
        this.MaskGlyph(new Rune('•'));
        this.RevealMode(MaskedInputRevealMode.Never);
        this.ClipboardMode(MaskedInputClipboardMode.Disabled);
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial string? Placeholder { get; set; }

    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    [Bindable]
    public partial Rune MaskGlyph { get; set; }

    [Bindable]
    public partial MaskedInputRevealMode RevealMode { get; set; }

    [Bindable]
    public partial MaskedInputClipboardMode ClipboardMode { get; set; }

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

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var width = Math.Max(10, Math.Min(availableSize.Width, 24));
        var height = Get<MaskedInputStyle>().ShowBorder ? 3 : 1;
        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, height)));
    }

    protected override void ArrangeCore(in Rectangle finalRect)
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
        var style = Get<MaskedInputStyle>();
        var borderStyle = style.BorderStyle(theme, isFocused);
        var selectionStyle = style.SelectionStyle(theme);
        var backgroundStyle = style.BackgroundStyle(theme);
        var placeholderStyle = style.PlaceholderStyle(theme);
        var padding = style.Padding;
        var showBorder = style.ShowBorder;

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
        if (contentWidth == 0)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        var reveal = RevealMode == MaskedInputRevealMode.Always || (RevealMode == MaskedInputRevealMode.WhileFocused && isFocused);
        if (reveal)
        {
            RenderRevealed(buffer, textRowY, contentX, contentWidth, text, caretIndex, isFocused, backgroundStyle, selectionStyle, placeholderStyle);
        }
        else
        {
            RenderMasked(buffer, textRowY, contentX, contentWidth, text, caretIndex, isFocused, backgroundStyle, selectionStyle, placeholderStyle);
        }
    }

    private void RenderRevealed(
        CellBuffer buffer,
        int textRowY,
        int contentX,
        int contentWidth,
        string text,
        int caretIndex,
        bool isFocused,
        CellStyle backgroundStyle,
        CellStyle selectionStyle,
        CellStyle placeholderStyle)
    {
        var alignment = TextAlignment;
        var totalTextCells = TerminalTextUtility.GetWidth(text.AsSpan());
        var fits = totalTextCells <= contentWidth;

        var caretCells = TerminalTextUtility.GetWidth(text.AsSpan(0, caretIndex));
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
            var selStart = Math.Min(_selectionAnchor, _selectionEnd);
            var selEnd = Math.Max(_selectionAnchor, _selectionEnd);

            var localStart = Math.Clamp(selStart, startIndex, endIndex);
            var localEnd = Math.Clamp(selEnd, startIndex, endIndex);

            if (localStart == localEnd)
            {
                buffer.WriteText(contentXAligned, textRowY, text.AsSpan(startIndex, endIndex - startIndex), backgroundStyle);
            }
            else
            {
                var leftSpan = text.AsSpan(startIndex, localStart - startIndex);
                var midSpan = text.AsSpan(localStart, localEnd - localStart);
                var rightSpan = text.AsSpan(localEnd, endIndex - localEnd);

                var x = contentXAligned;
                if (!leftSpan.IsEmpty)
                {
                    buffer.WriteText(x, textRowY, leftSpan, backgroundStyle);
                    x += TerminalTextUtility.GetWidth(leftSpan);
                }

                if (!midSpan.IsEmpty)
                {
                    buffer.WriteText(x, textRowY, midSpan, selectionStyle);
                    x += TerminalTextUtility.GetWidth(midSpan);
                }

                if (!rightSpan.IsEmpty)
                {
                    buffer.WriteText(x, textRowY, rightSpan, backgroundStyle);
                }
            }
        }
    }

    private void RenderMasked(
        CellBuffer buffer,
        int textRowY,
        int contentX,
        int contentWidth,
        string text,
        int caretIndex,
        bool isFocused,
        CellStyle backgroundStyle,
        CellStyle selectionStyle,
        CellStyle placeholderStyle)
    {
        var totalCells = text.Length;
        var fits = totalCells <= contentWidth;

        var caretCells = caretIndex;
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

        var startIndex = Math.Clamp(_scrollCellOffset, 0, text.Length);
        var endIndex = Math.Clamp(_scrollCellOffset + contentWidth, startIndex, text.Length);

        var alignedOffset = 0;
        var alignment = TextAlignment;
        if (_scrollCellOffset == 0 && fits && alignment != TextAlignment.Left && alignment != TextAlignment.Justify)
        {
            alignedOffset = alignment == TextAlignment.Center ? (contentWidth - totalCells) / 2 : (contentWidth - totalCells);
            alignedOffset = Math.Max(0, alignedOffset);
        }

        var xBase = contentX + alignedOffset;

        if (text.Length == 0 && !isFocused && !string.IsNullOrEmpty(Placeholder))
        {
            buffer.WriteText(xBase, textRowY, Placeholder.AsSpan(), placeholderStyle);
            return;
        }

        var selStart = HasSelection ? Math.Min(_selectionAnchor, _selectionEnd) : -1;
        var selEnd = HasSelection ? Math.Max(_selectionAnchor, _selectionEnd) : -1;

        var glyph = MaskGlyph;
        for (var i = startIndex; i < endIndex; i++)
        {
            var x = xBase + (i - startIndex);
            if (x < contentX || x >= contentX + contentWidth)
            {
                continue;
            }

            var cellStyle = backgroundStyle;
            if (HasSelection && i >= selStart && i < selEnd)
            {
                cellStyle = selectionStyle;
            }

            buffer.SetCell(x, textRowY, glyph, cellStyle);
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
                if (ClipboardMode == MaskedInputClipboardMode.CopyText)
                {
                    var span = GetSelectedTextSpan(text.AsSpan());
                    if (!span.IsEmpty)
                    {
                        App?.Terminal.Clipboard.TrySetText(span);
                    }
                }
                e.Handled = true;
                return;
            }

            if (e.Char is 'x' or 'X')
            {
                if (ClipboardMode == MaskedInputClipboardMode.CopyText && HasSelection)
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
                    Text = text[..prev] + text[_caretIndex..];
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

            if (e.Key == TerminalKey.Left)
            {
                _caretIndex = Math.Max(0, GetPreviousWordIndex(text.AsSpan(), _caretIndex));
                ClearSelection();
                e.Handled = true;
                Invalidate();
                return;
            }

            if (e.Key == TerminalKey.Right)
            {
                _caretIndex = Math.Min(text.Length, GetNextWordIndex(text.AsSpan(), _caretIndex));
                ClearSelection();
                e.Handled = true;
                Invalidate();
                return;
            }
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                MoveCaretLeft(text.AsSpan(), shift);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                MoveCaretRight(text.AsSpan(), shift);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                MoveCaretHome(text.AsSpan(), shift);
                e.Handled = true;
                return;
            case TerminalKey.End:
                MoveCaretEnd(text.AsSpan(), shift);
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
        }
    }

    private void InsertText(string input)
    {
        var t = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, t.Length);

        if (HasSelection)
        {
            DeleteSelection();
            t = Text ?? string.Empty;
        }

        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        Text = t[.._caretIndex] + input + t[_caretIndex..];
        _caretIndex += input.Length;
        ClearSelection();
        Invalidate();
    }

    private void Backspace()
    {
        var t = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, t.Length);

        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_caretIndex <= 0)
        {
            return;
        }

        var removeIndex = GetPreviousTextElementIndex(t.AsSpan(), _caretIndex);
        Text = t[..removeIndex] + t[_caretIndex..];
        _caretIndex = removeIndex;
        Invalidate();
    }

    private void Delete()
    {
        var t = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, t.Length);

        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_caretIndex >= t.Length)
        {
            return;
        }

        var next = GetNextTextElementIndex(t.AsSpan(), _caretIndex);
        Text = t[.._caretIndex] + t[next..];
        Invalidate();
    }

    private void DeleteSelection()
    {
        var t = Text ?? string.Empty;
        if (!HasSelection)
        {
            return;
        }

        var start = Math.Clamp(Math.Min(_selectionAnchor, _selectionEnd), 0, t.Length);
        var end = Math.Clamp(Math.Max(_selectionAnchor, _selectionEnd), start, t.Length);
        Text = t[..start] + t[end..];
        _caretIndex = start;
        ClearSelection();
        Invalidate();
    }

    private void MoveCaretLeft(ReadOnlySpan<char> text, bool extendSelection)
    {
        if (_caretIndex <= 0)
        {
            return;
        }

        var next = GetPreviousTextElementIndex(text, _caretIndex);
        UpdateSelectionForMove(extendSelection, next);
        _caretIndex = next;
        Invalidate();
    }

    private void MoveCaretRight(ReadOnlySpan<char> text, bool extendSelection)
    {
        if (_caretIndex >= text.Length)
        {
            return;
        }

        var next = GetNextTextElementIndex(text, _caretIndex);
        UpdateSelectionForMove(extendSelection, next);
        _caretIndex = next;
        Invalidate();
    }

    private void MoveCaretHome(ReadOnlySpan<char> text, bool extendSelection)
    {
        UpdateSelectionForMove(extendSelection, 0);
        _caretIndex = 0;
        Invalidate();
    }

    private void MoveCaretEnd(ReadOnlySpan<char> text, bool extendSelection)
    {
        UpdateSelectionForMove(extendSelection, text.Length);
        _caretIndex = text.Length;
        Invalidate();
    }

    private void UpdateSelectionForMove(bool extendSelection, int newIndex)
    {
        if (!extendSelection)
        {
            ClearSelection();
            return;
        }

        if (_selectionAnchor < 0)
        {
            _selectionAnchor = _caretIndex;
        }

        _selectionEnd = newIndex;
    }

    private void ClearSelection()
    {
        _selectionAnchor = -1;
        _selectionEnd = -1;
    }

    private void SelectAll()
    {
        var t = Text ?? string.Empty;
        _selectionAnchor = 0;
        _selectionEnd = t.Length;
        _caretIndex = t.Length;
        Invalidate();
    }

    private ReadOnlySpan<char> GetSelectedTextSpan(ReadOnlySpan<char> text)
    {
        if (!HasSelection)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var start = Math.Clamp(Math.Min(_selectionAnchor, _selectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(_selectionAnchor, _selectionEnd), start, text.Length);
        return text.Slice(start, end - start);
    }

    private static int GetPreviousTextElementIndex(ReadOnlySpan<char> text, int index)
    {
        if (index <= 0)
        {
            return 0;
        }

        var i = index;
        i--;
        if (i > 0 && char.IsLowSurrogate(text[i]) && char.IsHighSurrogate(text[i - 1]))
        {
            i--;
        }
        return i;
    }

    private static int GetNextTextElementIndex(ReadOnlySpan<char> text, int index)
    {
        if (index >= text.Length)
        {
            return text.Length;
        }

        var i = index;
        if (i + 1 < text.Length && char.IsHighSurrogate(text[i]) && char.IsLowSurrogate(text[i + 1]))
        {
            i += 2;
        }
        else
        {
            i++;
        }
        return i;
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

        var style = Get<MaskedInputStyle>();
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

        var reveal = RevealMode == MaskedInputRevealMode.Always || (RevealMode == MaskedInputRevealMode.WhileFocused);
        var caretCells = reveal ? TerminalTextUtility.GetWidth(text.AsSpan(0, caretIndex)) : caretIndex;

        var innerLeft = rect.X + (showBorder ? 1 : 0);
        var caretX = caretCells - _scrollCellOffset;
        if (caretX < 0)
        {
            caretX = 0;
        }

        caretX = Math.Min(contentWidth, caretX);

        var alignedOffset = 0;
        var alignment = TextAlignment;
        var totalTextCells = reveal ? TerminalTextUtility.GetWidth(text.AsSpan()) : text.Length;
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
