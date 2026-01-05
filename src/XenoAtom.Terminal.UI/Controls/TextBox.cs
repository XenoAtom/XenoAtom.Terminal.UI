// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed partial class TextBox : Visual
{
    private int _caretIndex;
    private int _scrollCellOffset;

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
            App?.RequestRender();
        }
    }

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
        var borderStyle = isFocused ? CellStyle.Invert : CellStyle.Dim;

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

        buffer.WriteText(rect.X + 1, rect.Y, text.AsSpan(startIndex, endIndex - startIndex), CellStyle.None);

        if (isFocused)
        {
            var caretX = caretCells - _scrollCellOffset;
            if (caretX >= 0 && caretX < innerWidth)
            {
                buffer.SetCell(rect.X + 1 + caretX, rect.Y, new Rune(' '), CellStyle.Invert);
            }
            else if (caretX == innerWidth)
            {
                buffer.SetCell(rect.X + rect.Width - 1, rect.Y, new Rune(']'), CellStyle.Invert);
            }
        }
    }

    protected override void OnTextInput(KeyEventArgs e)
    {
        var ch = e.Char;
        if (ch is null || ch < ' ')
        {
            return;
        }

        InsertText(ch.Value.ToString());
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var text = Text ?? string.Empty;
        _caretIndex = Math.Clamp(_caretIndex, 0, text.Length);

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0)
        {
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
                App?.Terminal.Clipboard.TrySetText(text.AsSpan());
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                _caretIndex = TerminalTextUtility.GetPreviousRuneIndex(text.AsSpan(), _caretIndex);
                App?.RequestRender();
                e.Handled = true;
                return;
            case TerminalKey.Right:
                _caretIndex = TerminalTextUtility.GetNextRuneIndex(text.AsSpan(), _caretIndex);
                App?.RequestRender();
                e.Handled = true;
                return;
            case TerminalKey.Home:
                _caretIndex = 0;
                App?.RequestRender();
                e.Handled = true;
                return;
            case TerminalKey.End:
                _caretIndex = text.Length;
                App?.RequestRender();
                e.Handled = true;
                return;
            case TerminalKey.Backspace:
                if (_caretIndex > 0)
                {
                    var prev = TerminalTextUtility.GetPreviousRuneIndex(text.AsSpan(), _caretIndex);
                    Text = string.Concat(text.AsSpan(0, prev), text.AsSpan(_caretIndex));
                    _caretIndex = prev;
                }
                e.Handled = true;
                return;
            case TerminalKey.Delete:
                if (_caretIndex < text.Length)
                {
                    var next = TerminalTextUtility.GetNextRuneIndex(text.AsSpan(), _caretIndex);
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

        var newText = string.Concat(text.AsSpan(0, _caretIndex), insert.AsSpan(), text.AsSpan(_caretIndex));
        Text = newText;
        _caretIndex += insert.Length;
    }
}
