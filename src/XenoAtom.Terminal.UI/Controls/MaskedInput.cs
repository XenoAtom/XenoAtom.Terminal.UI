// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// An input control that restricts user input according to a template mask.
/// </summary>
/// <remarks>
/// The template defines a fixed set of editable positions and literal separators (e.g. <c>9999-9999-9999-9999</c>).
/// Non-editable characters in the template are always displayed. Unfilled editable positions display a placeholder
/// character (customizable via the template suffix <c>;c</c> or <see cref="MaskedInputStyle.DefaultPlaceholderChar"/>).
/// </remarks>
public sealed partial class MaskedInput : Visual, ICursorProvider
{
    private enum CaseMode
    {
        None,
        Upper,
        Lower,
    }

    private enum TokenKind
    {
        Literal,
        Alpha,
        AlphaNum,
        NonSpace,
        Digit,
        DigitNonZero,
        DigitSign,
        Hex,
        Bit,
    }

    private readonly record struct TemplateToken(TokenKind Kind, bool Required, CaseMode Case, char Literal)
    {
        public bool IsEditable => Kind != TokenKind.Literal;
    }

    private Rectangle _contentRect;
    private int _caretSlotIndex;
    private int _scrollX;

    private string _cachedTemplate = string.Empty;
    private TemplateToken[] _tokens = Array.Empty<TemplateToken>();
    private int[] _slotTokenIndexes = Array.Empty<int>();

    private char[] _slots = Array.Empty<char>();
    private int _slotCount;
    private bool _hasTemplatePlaceholderChar;
    private char _templatePlaceholderChar;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedInput"/> class.
    /// </summary>
    public MaskedInput()
    {
        Focusable = true;
        this.HorizontalAlignment(HorizontalAlignment.Stretch);
        this.Template(string.Empty);
        this.Value(string.Empty);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedInput"/> class with a template.
    /// </summary>
    /// <param name="template">The template mask.</param>
    public MaskedInput(string template) : this()
    {
        this.Template(template);
    }

    /// <summary>
    /// Gets or sets the template mask string.
    /// </summary>
    /// <remarks>
    /// See the control documentation for the supported template tokens. Any other character is treated as a literal
    /// separator. Use <c>\</c> to escape token characters. The template may end with <c>;c</c> to specify a placeholder
    /// character for empty slots.
    /// </remarks>
    [Bindable]
    public partial string? Template { get; set; }

    /// <summary>
    /// Gets or sets the current input value.
    /// </summary>
    /// <remarks>
    /// The value represents the slot characters (separators are not included). The string is positional: index 0
    /// corresponds to the first editable slot, index 1 to the second, etc. Empty slots are represented by a space
    /// character. Trailing empty slots are trimmed.
    /// </remarks>
    [Bindable]
    public partial string? Value { get; set; }

    /// <summary>
    /// Gets the current value without any empty-slot markers.
    /// </summary>
    /// <remarks>
    /// This property is convenient when the consumer wants only the typed characters (e.g. credit-card digits).
    /// </remarks>
    public string CompactValue
    {
        get
        {
            EnsureTemplateParsed();
            ApplyValueToSlots(Value ?? string.Empty);
            return BuildCompactValue();
        }
    }

    /// <summary>
    /// Gets a value indicating whether all required slots are filled and the value matches the template constraints.
    /// </summary>
    public bool IsValid
    {
        get
        {
            EnsureTemplateParsed();
            ApplyValueToSlots(Value ?? string.Empty);

            for (var i = 0; i < _tokens.Length; i++)
            {
                var token = _tokens[i];
                if (!token.IsEditable)
                {
                    continue;
                }

                var slotIndex = token.Literal; // stored as slot index for editable tokens
                var ch = _slots[slotIndex];
                if (ch == '\0')
                {
                    if (token.Required)
                    {
                        return false;
                    }
                    continue;
                }

                if (!IsCharAllowed(token, ch))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureTemplateParsed();

        var style = Get<MaskedInputStyle>();
        var padding = style.Padding;
        var padW = padding.Horizontal;
        var padH = padding.Vertical;

        var availableW = constraints.MaxWidth;
        var availableH = constraints.MaxHeight;

        var display = BuildDisplayText(GetPlaceholderChar(style));
        var contentWidth = TerminalTextUtility.GetWidth(display.AsSpan());
        var width = Math.Max(0, Math.Min(availableW, contentWidth + padW));
        var height = Math.Max(1, Math.Min(availableH, 1 + padH));

        return SizeHints.Fixed(new Size(width, height));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var style = Get<MaskedInputStyle>();
        var padding = style.Padding;

        _contentRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        EnsureCaretVisible();
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        EnsureTemplateParsed();
        ApplyValueToSlots(Value ?? string.Empty);

        var theme = GetTheme();
        var focused = ReferenceEquals(App?.FocusedElement, this);
        var style = Get<MaskedInputStyle>();
        var backgroundStyle = style.BackgroundStyle(theme, focused);
        var placeholderChar = GetPlaceholderChar(style);

        if (_contentRect.Width > 0 && _contentRect.Height > 0)
        {
            for (var y = _contentRect.Y; y < _contentRect.Y + _contentRect.Height; y++)
            {
                for (var x = _contentRect.X; x < _contentRect.X + _contentRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        if (_contentRect.Width <= 0 || _contentRect.Height <= 0)
        {
            return;
        }

        var valueStyle = style.ValueStyle(theme, focused);
        var placeholderStyle = style.PlaceholderCellStyle(theme, focused);
        var separatorStyle = style.SeparatorCellStyle(theme, focused);

        var displayText = BuildDisplayText(placeholderChar);
        RenderTemplateText(buffer, _contentRect.X, _contentRect.Y, _contentRect.Width, displayText, placeholderChar, valueStyle, placeholderStyle, separatorStyle);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        EnsureTemplateParsed();
        ApplyValueToSlots(Value ?? string.Empty);

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0)
        {
            if (e.Char is TerminalChar.CtrlC)
            {
                var raw = CompactValue;
                if (!string.IsNullOrEmpty(raw))
                {
                    App?.Terminal.Clipboard.TrySetText(raw);
                }
                e.Handled = true;
                return;
            }

            if (e.Char is TerminalChar.CtrlV)
            {
                var clip = App?.Terminal.Clipboard.Text;
                if (!string.IsNullOrEmpty(clip))
                {
                    InsertText(clip.AsSpan());
                }
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                MoveCaret(-1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                MoveCaret(1);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                _caretSlotIndex = 0;
                EnsureCaretVisible();
                e.Handled = true;
                return;
            case TerminalKey.End:
                _caretSlotIndex = Math.Max(0, _slotCount - 1);
                EnsureCaretVisible();
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

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        EnsureTemplateParsed();
        ApplyValueToSlots(Value ?? string.Empty);

        InsertText(e.Text.AsSpan());
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        EnsureTemplateParsed();

        if (_contentRect.Width <= 0 || _contentRect.Height <= 0 || !_contentRect.Contains(e.X, e.Y))
        {
            return;
        }

        ApplyValueToSlots(Value ?? string.Empty);

        var cellX = Math.Max(0, (e.X - _contentRect.X) + _scrollX);
        var slot = SlotIndexFromCellX(cellX);
        _caretSlotIndex = Math.Clamp(slot, 0, Math.Max(0, _slotCount - 1));
        EnsureCaretVisible();

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        // Masked input uses horizontal scrolling only; wheel is ignored for now.
        _ = e;
    }

    /// <inheritdoc />
    public bool TryGetCursorCell(out int x, out int y)
    {
        x = 0;
        y = 0;

        if (!ReferenceEquals(App?.FocusedElement, this))
        {
            return false;
        }

        if (_contentRect.Width <= 0 || _contentRect.Height <= 0)
        {
            return false;
        }

        EnsureTemplateParsed();

        var caretCell = GetSlotCellX(Math.Clamp(_caretSlotIndex, 0, Math.Max(0, _slotCount - 1)));
        x = _contentRect.X + Math.Clamp(caretCell - _scrollX, 0, Math.Max(0, _contentRect.Width - 1));
        y = _contentRect.Y;
        return true;
    }

    private void MoveCaret(int delta)
    {
        if (_slotCount <= 0)
        {
            return;
        }

        _caretSlotIndex = Math.Clamp(_caretSlotIndex + delta, 0, _slotCount - 1);
        EnsureCaretVisible();
    }

    private void Backspace()
    {
        if (_slotCount <= 0)
        {
            return;
        }

        var slot = Math.Clamp(_caretSlotIndex - 1, 0, _slotCount - 1);
        _slots[slot] = '\0';
        _caretSlotIndex = slot;
        CommitSlotsToValue();
    }

    private void Delete()
    {
        if (_slotCount <= 0)
        {
            return;
        }

        var slot = Math.Clamp(_caretSlotIndex, 0, _slotCount - 1);
        _slots[slot] = '\0';
        CommitSlotsToValue();
    }

    private void InsertText(ReadOnlySpan<char> text)
    {
        if (_slotCount <= 0)
        {
            return;
        }

        var slot = Math.Clamp(_caretSlotIndex, 0, _slotCount - 1);
        var index = 0;
        while (index < text.Length && slot < _slotCount)
        {
            var elementLength = StringInfo.GetNextTextElementLength(text[index..]);
            if (elementLength <= 0)
            {
                elementLength = 1;
            }

            var elementSpan = text.Slice(index, Math.Min(elementLength, text.Length - index));
            index += elementLength;

            if (elementSpan.Length != 1)
            {
                continue;
            }

            var ch = elementSpan[0];
            var tokenIndex = _slotTokenIndexes[slot];
            var token = _tokens[tokenIndex];

            if (!IsCharAllowed(token, ch))
            {
                continue;
            }

            ch = ApplyCase(token, ch);
            _slots[slot] = ch;
            slot++;
        }

        _caretSlotIndex = Math.Clamp(slot, 0, Math.Max(0, _slotCount - 1));
        CommitSlotsToValue();
    }

    private void CommitSlotsToValue()
    {
        var newValue = BuildValueString();
        Value = newValue;
        EnsureCaretVisible();
    }

    private string BuildCompactValue()
    {
        if (_slotCount == 0)
        {
            return string.Empty;
        }

        var buffer = new char[_slotCount];
        var count = 0;
        for (var i = 0; i < _slotCount; i++)
        {
            var ch = _slots[i];
            if (ch == '\0')
            {
                continue;
            }

            buffer[count++] = ch;
        }

        return count == 0 ? string.Empty : new string(buffer, 0, count);
    }

    private string BuildValueString()
    {
        if (_slotCount == 0)
        {
            return string.Empty;
        }

        var last = -1;
        for (var i = _slotCount - 1; i >= 0; i--)
        {
            if (_slots[i] != '\0')
            {
                last = i;
                break;
            }
        }

        if (last < 0)
        {
            return string.Empty;
        }

        var chars = new char[last + 1];
        for (var i = 0; i <= last; i++)
        {
            chars[i] = _slots[i] == '\0' ? ' ' : _slots[i];
        }

        return new string(chars);
    }

    private void ApplyValueToSlots(string value)
    {
        if (_slotCount == 0)
        {
            return;
        }

        if (_slots.Length != _slotCount)
        {
            _slots = new char[_slotCount];
        }

        Array.Fill(_slots, '\0');

        for (var slot = 0; slot < _slotCount; slot++)
        {
            if (slot >= value.Length)
            {
                break;
            }

            var ch = value[slot];
            if (ch == ' ')
            {
                continue;
            }

            var tokenIndex = _slotTokenIndexes[slot];
            var token = _tokens[tokenIndex];
            if (!IsCharAllowed(token, ch))
            {
                continue;
            }

            ch = ApplyCase(token, ch);
            _slots[slot] = ch;
        }
    }

    private string BuildDisplayText()
        => BuildDisplayText(GetPlaceholderChar(Get<MaskedInputStyle>()));

    private string BuildDisplayText(char placeholderChar)
    {
        if (_tokens.Length == 0)
        {
            return string.Empty;
        }

        var chars = new char[_tokens.Length];
        for (var i = 0; i < _tokens.Length; i++)
        {
            var token = _tokens[i];
            if (!token.IsEditable)
            {
                chars[i] = token.Literal;
                continue;
            }

            var slotIndex = token.Literal;
            var ch = slotIndex >= 0 && slotIndex < _slots.Length ? _slots[slotIndex] : '\0';
            chars[i] = ch == '\0' ? placeholderChar : ch;
        }

        return new string(chars);
    }

    private int SlotIndexFromCellX(int cellX)
    {
        if (_slotCount == 0)
        {
            return 0;
        }

        var display = BuildDisplayText().AsSpan();
        for (var slot = 0; slot < _slotCount; slot++)
        {
            var tokenIndex = _slotTokenIndexes[slot];
            var tokenCharIndex = tokenIndex;
            var cellAtToken = TerminalTextUtility.GetWidth(display[..Math.Clamp(tokenCharIndex, 0, display.Length)]);
            if (cellX <= cellAtToken)
            {
                return slot;
            }
        }

        return _slotCount - 1;
    }

    private int GetSlotCellX(int slotIndex)
    {
        if (_slotCount == 0)
        {
            return 0;
        }

        slotIndex = Math.Clamp(slotIndex, 0, _slotCount - 1);
        var tokenIndex = _slotTokenIndexes[slotIndex];
        var display = BuildDisplayText().AsSpan();
        return TerminalTextUtility.GetWidth(display[..Math.Clamp(tokenIndex, 0, display.Length)]);
    }

    private void EnsureCaretVisible()
    {
        if (_contentRect.Width <= 0)
        {
            _scrollX = 0;
            return;
        }

        if (_slotCount <= 0)
        {
            _scrollX = 0;
            return;
        }

        var caretCell = GetSlotCellX(Math.Clamp(_caretSlotIndex, 0, _slotCount - 1));
        if (caretCell < _scrollX)
        {
            _scrollX = caretCell;
        }
        else if (caretCell >= _scrollX + _contentRect.Width)
        {
            _scrollX = Math.Max(0, caretCell - Math.Max(0, _contentRect.Width - 1));
        }
    }

    private void EnsureTemplateParsed()
    {
        var template = Template ?? string.Empty;
        if (string.Equals(_cachedTemplate, template, StringComparison.Ordinal))
        {
            return;
        }

        _cachedTemplate = template;
        ParseTemplate(template, out _tokens, out _slotTokenIndexes, out _slotCount, out _hasTemplatePlaceholderChar, out _templatePlaceholderChar);
        _caretSlotIndex = 0;
        _scrollX = 0;
        _slots = _slotCount == 0 ? Array.Empty<char>() : new char[_slotCount];
    }

    private static void ParseTemplate(string template, out TemplateToken[] tokens, out int[] slotTokenIndexes, out int slotCount, out bool hasPlaceholderChar, out char placeholderChar)
    {
        hasPlaceholderChar = false;
        placeholderChar = '\0';

        if (string.IsNullOrEmpty(template))
        {
            tokens = Array.Empty<TemplateToken>();
            slotTokenIndexes = Array.Empty<int>();
            slotCount = 0;
            return;
        }

        // Template terminator: ;c
        if (template.Length >= 2 && template[^2] == ';')
        {
            hasPlaceholderChar = true;
            placeholderChar = template[^1];
            template = template[..^2];
        }

        var list = new List<TemplateToken>(template.Length);
        var slots = new List<int>(template.Length);

        var caseMode = CaseMode.None;
        var escape = false;
        var currentSlot = 0;

        for (var i = 0; i < template.Length; i++)
        {
            var ch = template[i];
            if (escape)
            {
                list.Add(new TemplateToken(TokenKind.Literal, Required: false, Case: CaseMode.None, Literal: ch));
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '>')
            {
                caseMode = CaseMode.Upper;
                continue;
            }

            if (ch == '<')
            {
                caseMode = CaseMode.Lower;
                continue;
            }

            if (ch == '!')
            {
                caseMode = CaseMode.None;
                continue;
            }

            if (TryGetToken(ch, caseMode, currentSlot, out var token))
            {
                list.Add(token);
                slots.Add(list.Count - 1);
                currentSlot++;
            }
            else
            {
                list.Add(new TemplateToken(TokenKind.Literal, Required: false, Case: CaseMode.None, Literal: ch));
            }
        }

        tokens = list.ToArray();
        slotCount = currentSlot;
        slotTokenIndexes = new int[slotCount];
        for (var i = 0; i < slotCount; i++)
        {
            slotTokenIndexes[i] = slots[i];
        }
    }

    private static bool TryGetToken(char ch, CaseMode caseMode, int slotIndex, out TemplateToken token)
    {
        // Textual template token set:
        // A/a: alpha, N/n: alnum, X/x: non-space, 9/0: digit, D/d: digit 1-9, #: digit/sign, H/h: hex, B/b: bit
        token = default;
        TokenKind kind;
        bool required;

        switch (ch)
        {
            case 'A': kind = TokenKind.Alpha; required = true; break;
            case 'a': kind = TokenKind.Alpha; required = false; break;
            case 'N': kind = TokenKind.AlphaNum; required = true; break;
            case 'n': kind = TokenKind.AlphaNum; required = false; break;
            case 'X': kind = TokenKind.NonSpace; required = true; break;
            case 'x': kind = TokenKind.NonSpace; required = false; break;
            case '9': kind = TokenKind.Digit; required = true; break;
            case '0': kind = TokenKind.Digit; required = false; break;
            case 'D': kind = TokenKind.DigitNonZero; required = true; break;
            case 'd': kind = TokenKind.DigitNonZero; required = false; break;
            case '#': kind = TokenKind.DigitSign; required = false; break;
            case 'H': kind = TokenKind.Hex; required = true; break;
            case 'h': kind = TokenKind.Hex; required = false; break;
            case 'B': kind = TokenKind.Bit; required = true; break;
            case 'b': kind = TokenKind.Bit; required = false; break;
            default:
                return false;
        }

        token = new TemplateToken(kind, required, caseMode, Literal: (char)slotIndex);
        return true;
    }

    private static bool IsCharAllowed(TemplateToken token, char ch)
    {
        return token.Kind switch
        {
            TokenKind.Literal => false,
            TokenKind.Alpha => char.IsLetter(ch),
            TokenKind.AlphaNum => char.IsLetterOrDigit(ch),
            TokenKind.NonSpace => ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n',
            TokenKind.Digit => ch >= '0' && ch <= '9',
            TokenKind.DigitNonZero => ch >= '1' && ch <= '9',
            TokenKind.DigitSign => (ch >= '0' && ch <= '9') || ch == '+' || ch == '-',
            TokenKind.Hex => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'),
            TokenKind.Bit => ch == '0' || ch == '1',
            _ => false,
        };
    }

    private static char ApplyCase(TemplateToken token, char ch)
    {
        if (token.Kind is TokenKind.Alpha or TokenKind.AlphaNum)
        {
            return token.Case switch
            {
                CaseMode.Upper => char.ToUpperInvariant(ch),
                CaseMode.Lower => char.ToLowerInvariant(ch),
                _ => ch,
            };
        }

        return ch;
    }

    private void RenderTemplateText(CellBuffer buffer, int x, int y, int width, string displayText, char placeholderChar, Style valueStyle, Style placeholderStyle, Style separatorStyle)
    {
        if (width <= 0)
        {
            return;
        }

        var cell = 0;
        var span = displayText.AsSpan();

        for (var i = 0; i < span.Length && cell < _scrollX + width; i++)
        {
            var token = _tokens[i];
            var ch = span[i];

            var w = TerminalTextUtility.GetWidth(span.Slice(i, 1));
            if (w <= 0)
            {
                continue;
            }

            var startCell = cell;
            var endCell = cell + w;
            cell = endCell;

            if (endCell <= _scrollX)
            {
                continue;
            }

            var drawX = x + (startCell - _scrollX);
            if (drawX < x || drawX >= x + width)
            {
                continue;
            }

            var style = token.Kind == TokenKind.Literal
                ? separatorStyle
                : (ch == placeholderChar ? placeholderStyle : valueStyle);

            buffer.WriteText(drawX, y, span.Slice(i, 1), style);
        }
    }

    private char GetPlaceholderChar(MaskedInputStyle style)
    {
        if (_hasTemplatePlaceholderChar && _templatePlaceholderChar != '\0')
        {
            return _templatePlaceholderChar;
        }

        return style.DefaultPlaceholderChar;
    }
}
