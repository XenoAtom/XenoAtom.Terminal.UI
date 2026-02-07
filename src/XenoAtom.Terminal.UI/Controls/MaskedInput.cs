// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// An input control that restricts user input according to a template mask.
/// </summary>
/// <remarks>
/// The template defines a fixed set of editable positions and literal separators (for example <c>9999-9999-9999-9999</c>).
/// Non-editable characters in the template are always displayed. Empty editable positions render a placeholder glyph.
/// <para />
/// This control reuses the text editor infrastructure (<see cref="TextEditorCore"/>) so it supports selection,
/// copy/cut/paste, mouse selection, and standard navigation.
/// </remarks>
public sealed partial class MaskedInput : TextEditorBase
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

    private readonly record struct TemplateToken(TokenKind Kind, bool Required, CaseMode Case, char Literal, int SlotIndex)
    {
        public bool IsEditable => Kind != TokenKind.Literal;
    }

    private readonly MaskedInputDocument _document;

    private string _cachedTemplate = string.Empty;
    private TemplateToken[] _tokens = Array.Empty<TemplateToken>();
    private int[] _slotTokenIndexes = Array.Empty<int>();
    private int _slotCount;
    private bool _hasTemplatePlaceholderChar;
    private char _templatePlaceholderChar;

    private bool _updatingDocumentFromValue;

    private Rectangle _contentRect;

    private Color? _separatorForeground;
    private Color? _placeholderForeground;
    private bool _placeholderDim;
    private Color? _mutedForeground;
    private bool _renderStateValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedInput"/> class.
    /// </summary>
    public MaskedInput()
    {
        _document = new MaskedInputDocument();
        TextDocument = _document;
        _document.Changed += OnMaskedDocumentChanged;

        Focusable = true;
        this.HorizontalAlignment(Align.Stretch);
        this.WordWrap(false);
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
    /// Initializes a new instance of the <see cref="MaskedInput"/> class with a dynamic template.
    /// </summary>
    /// <param name="template">A delegate that supplies the template mask.</param>
    public MaskedInput(Func<string?> template) : this()
    {
        this.Template(template);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaskedInput"/> class with a bound template.
    /// </summary>
    /// <param name="template">A binding that supplies the template mask.</param>
    public MaskedInput(Binding<string?> template) : this()
    {
        this.Template(template);
    }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        base.PrepareChildren();

        // Keep the internal masked document synchronized with the Template/Value bindable properties.
        // This must happen in PrepareChildren so dependency tracking can invalidate measure/arrange/render automatically.
        EnsureTemplateParsed();
        SyncDocumentFromValue();
    }

    /// <summary>
    /// Gets or sets the template mask string.
    /// </summary>
    /// <remarks>
    /// Supported tokens:
    /// <list type="bullet">
    /// <item><description><c>A</c>/<c>a</c>: alphabetic character (required/optional)</description></item>
    /// <item><description><c>N</c>/<c>n</c>: alphanumeric character (required/optional)</description></item>
    /// <item><description><c>X</c>/<c>x</c>: non-space character (required/optional)</description></item>
    /// <item><description><c>9</c>/<c>0</c>: digit (required/optional)</description></item>
    /// <item><description><c>D</c>/<c>d</c>: digit 1-9 (required/optional)</description></item>
    /// <item><description><c>#</c>: digit or sign (+/-)</description></item>
    /// <item><description><c>H</c>/<c>h</c>: hexadecimal digit (required/optional)</description></item>
    /// <item><description><c>B</c>/<c>b</c>: binary digit (required/optional)</description></item>
    /// </list>
    /// Any other character is treated as a literal separator. Use <c>\</c> to escape token characters. The template may end with
    /// <c>;c</c> to specify a placeholder glyph for all empty slots.
    /// <para />
    /// Case conversion directives:
    /// <list type="bullet">
    /// <item><description><c>&gt;</c>: uppercase subsequent alphabetic tokens</description></item>
    /// <item><description><c>&lt;</c>: lowercase subsequent alphabetic tokens</description></item>
    /// <item><description><c>!</c>: stop applying case conversion</description></item>
    /// </list>
    /// </remarks>
    [Bindable]
    public partial string? Template { get; set; }

    /// <summary>
    /// Gets or sets the current input value.
    /// </summary>
    /// <remarks>
    /// The value represents the slot characters (separators are not included). The string is positional: index 0 corresponds to the
    /// first editable slot, index 1 to the second, etc. Empty slots are represented by a space character. Trailing empty slots are trimmed.
    /// </remarks>
    [Bindable]
    public partial string? Value { get; set; }

    /// <summary>
    /// Gets the current value without any empty-slot markers.
    /// </summary>
    /// <remarks>
    /// This property is convenient when the consumer wants only the typed characters (for example credit-card digits).
    /// </remarks>
    public string CompactValue
    {
        get
        {
            EnsureTemplateParsed();
            var value = Value ?? string.Empty;
            if (value.Length == 0)
            {
                return string.Empty;
            }

            var chars = new char[value.Length];
            var count = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == ' ')
                {
                    continue;
                }
                chars[count++] = ch;
            }

            return count == 0 ? string.Empty : new string(chars, 0, count);
        }
    }

    /// <summary>
    /// Gets a value indicating whether all required slots are filled and the current value matches the template constraints.
    /// </summary>
    public bool IsValid
    {
        get
        {
            EnsureTemplateParsed();
            var text = _document.Text;
            if (text.Length == 0 || _tokens.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < _tokens.Length && i < text.Length; i++)
            {
                var token = _tokens[i];
                if (!token.IsEditable)
                {
                    continue;
                }

                var ch = text[i];
                if (ch == ' ')
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
    protected override bool IsSingleLine => true;

    /// <inheritdoc />
    protected override bool AcceptsReturn => false;

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        EnsureTemplateParsed();

        var style = GetStyle<MaskedInputStyle>();
        var padding = style.Padding;
        var width = Math.Max(0, Math.Min(constraints.MaxWidth, _tokens.Length + padding.Horizontal));
        var height = Math.Max(1, Math.Min(constraints.MaxHeight, 1 + padding.Vertical));
        return SizeHints.Fixed(new Size(width, height));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetStyle<MaskedInputStyle>();
        var padding = style.Padding;

        _contentRect = new Rectangle(
            finalRect.X + padding.Left,
            finalRect.Y + padding.Top,
            Math.Max(0, finalRect.Width - padding.Horizontal),
            Math.Max(0, finalRect.Height - padding.Vertical));

        UpdateEditorLayout(_contentRect);
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
        _ = Template;
        _ = Value;

        var isFocused = HasFocus;
        var theme = GetTheme();
        var style = GetStyle<MaskedInputStyle>();

        var selectionStyle = style.SelectionStyle(theme);
        var backgroundStyle = style.BackgroundStyle(theme, isFocused);
        var placeholderStyle = style.PlaceholderCellStyle(theme, isFocused);

        var padding = style.Padding;
        var baseRect = new Rectangle(
            rect.X + padding.Left,
            rect.Y + padding.Top,
            Math.Max(0, rect.Width - padding.Horizontal),
            Math.Max(0, rect.Height - padding.Vertical));

        if (baseRect.Width > 0 && baseRect.Height > 0)
        {
            for (var y = baseRect.Y; y < baseRect.Y + baseRect.Height; y++)
            {
                for (var x = baseRect.X; x < baseRect.X + baseRect.Width; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), backgroundStyle);
                }
            }
        }

        _separatorForeground = style.SeparatorForeground ?? theme.Muted ?? theme.Foreground;
        _placeholderForeground = style.Placeholder ?? theme.Muted ?? theme.Foreground;
        _placeholderDim = true;
        _mutedForeground = theme.Muted;
        _renderStateValid = true;

        RenderEditor(buffer, baseRect, backgroundStyle, selectionStyle, placeholderStyle);

        _renderStateValid = false;
    }

    /// <inheritdoc />
    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn)
    {
        if (isPlaceholder || !_renderStateValid || _tokens.Length == 0 || textIndexStart < 0)
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var theme = GetTheme();
        var focused = HasFocus;
        var maskedStyle = GetStyle<MaskedInputStyle>();

        var cellX = x;
        for (var i = 0; i < text.Length; i++)
        {
            var index = textIndexStart + i;
            if (index < 0 || index >= _tokens.Length)
            {
                buffer.SetCell(cellX++, y, new Rune(text[i]), style);
                continue;
            }

            var token = _tokens[index];
            if (token.Kind == TokenKind.Literal)
            {
                var s = style;
                if (_separatorForeground is { } c)
                {
                    s = s.WithForeground(c);
                }
                buffer.SetCell(cellX++, y, new Rune(text[i]), s);
                continue;
            }

            var ch = text[i];
            if (ch == ' ')
            {
                var placeholderChar = GetPlaceholderChar(maskedStyle, token);
                var s = style;
                if (_placeholderForeground is { } c)
                {
                    s = s.WithForeground(c);
                }
                if (_placeholderDim)
                {
                    s |= TextStyle.Dim;
                }
                buffer.SetCell(cellX++, y, new Rune(placeholderChar), s);
                continue;
            }

            buffer.SetCell(cellX++, y, new Rune(ch), style);
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!string.IsNullOrEmpty(e.Text) && e.Handled)
        {
            SnapCaretToNextEmptySlot(moveToEndWhenFull: e.Text.Length > 1);
        }
    }

    /// <inheritdoc />
    protected override void OnPaste(PasteEventArgs e)
    {
        base.OnPaste(e);
        if (!string.IsNullOrEmpty(e.Text) && e.Handled)
        {
            SnapCaretToNextEmptySlot(moveToEndWhenFull: e.Text.Length > 1);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled)
        {
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlV or TerminalChar.CtrlX)
        {
            SnapCaretToNextEmptySlot();
        }
    }

    private void OnMaskedDocumentChanged(object? sender, TextDocumentChangedEventArgs e)
    {
        EnsureTemplateParsed();

        if (_updatingDocumentFromValue)
        {
            return;
        }

        var newValue = ExtractValueFromDocument();
        if (string.Equals(newValue, Value ?? string.Empty, StringComparison.Ordinal))
        {
            return;
        }

        Value = newValue;
    }

    private void EnsureTemplateParsed(bool force = false)
    {
        var template = Template ?? string.Empty;
        if (!force && string.Equals(_cachedTemplate, template, StringComparison.Ordinal))
        {
            return;
        }

        _cachedTemplate = template;
        ParseTemplate(template, out _tokens, out _slotTokenIndexes, out _slotCount, out _hasTemplatePlaceholderChar, out _templatePlaceholderChar);
        _document.SetTemplate(_tokens);
    }

    private void SyncDocumentFromValue()
    {
        EnsureTemplateParsed();

        var templateText = BuildTemplateText();
        var text = ApplyValueToTemplateText(templateText, Value ?? string.Empty);

        _updatingDocumentFromValue = true;
        try
        {
            _document.SetText(text);
        }
        finally
        {
            _updatingDocumentFromValue = false;
        }
    }

    private string BuildTemplateText()
    {
        if (_tokens.Length == 0)
        {
            return string.Empty;
        }

        var chars = new char[_tokens.Length];
        for (var i = 0; i < _tokens.Length; i++)
        {
            var token = _tokens[i];
            chars[i] = token.Kind == TokenKind.Literal ? token.Literal : ' ';
        }

        return new string(chars);
    }

    private string ApplyValueToTemplateText(string templateText, string value)
    {
        if (_slotCount == 0 || templateText.Length == 0)
        {
            return templateText;
        }

        var chars = templateText.ToCharArray();
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
            if (tokenIndex < 0 || tokenIndex >= _tokens.Length)
            {
                continue;
            }

            var token = _tokens[tokenIndex];
            if (!IsCharAllowed(token, ch))
            {
                continue;
            }

            chars[tokenIndex] = ApplyCase(token, ch);
        }

        return new string(chars);
    }

    private string ExtractValueFromDocument()
    {
        if (_slotCount == 0)
        {
            return string.Empty;
        }

        var text = _document.Text;
        if (text.Length == 0)
        {
            return string.Empty;
        }

        var last = -1;
        var chars = new char[_slotCount];
        for (var slot = 0; slot < _slotCount; slot++)
        {
            var tokenIndex = _slotTokenIndexes[slot];
            if (tokenIndex < 0 || tokenIndex >= text.Length)
            {
                chars[slot] = ' ';
                continue;
            }

            var ch = text[tokenIndex];
            if (ch == ' ')
            {
                chars[slot] = ' ';
                continue;
            }

            chars[slot] = ch;
            last = slot;
        }

        return last < 0 ? string.Empty : new string(chars, 0, last + 1);
    }

    private void SnapCaretToNextEmptySlot(bool moveToEndWhenFull = false)
    {
        EnsureTemplateParsed();

        if (_tokens.Length == 0)
        {
            return;
        }

        var text = _document.Text;
        var start = Math.Clamp(CaretIndex, 0, _tokens.Length);

        for (var i = start; i < _tokens.Length; i++)
        {
            var token = _tokens[i];
            if (!token.IsEditable)
            {
                continue;
            }

            if (i >= text.Length || text[i] == ' ')
            {
                CaretIndex = i;
                return;
            }
        }

        if (moveToEndWhenFull)
        {
            CaretIndex = _tokens.Length;
        }
        // Otherwise keep the current caret position so users can continue overwriting
        // characters sequentially in a full mask instead of jumping to the end.
    }

    private char GetPlaceholderChar(MaskedInputStyle style, TemplateToken token)
    {
        if (_hasTemplatePlaceholderChar && _templatePlaceholderChar != '\0')
        {
            return _templatePlaceholderChar;
        }

        return token.Kind switch
        {
            TokenKind.Digit or TokenKind.DigitNonZero or TokenKind.DigitSign or TokenKind.Hex or TokenKind.Bit => style.DigitPlaceholderChar,
            TokenKind.Alpha or TokenKind.AlphaNum => style.AlphaPlaceholderChar,
            _ => style.DefaultPlaceholderChar,
        };
    }

    private static void ParseTemplate(
        string template,
        out TemplateToken[] tokens,
        out int[] slotTokenIndexes,
        out int slotCount,
        out bool hasPlaceholderChar,
        out char placeholderChar)
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
        var effectiveLength = template.Length;
        if (template.Length >= 2 && template[^2] == ';')
        {
            hasPlaceholderChar = true;
            placeholderChar = template[^1];
            effectiveLength -= 2;
        }

        var tokenList = new List<TemplateToken>(effectiveLength);
        var slotTokenIndexList = new List<int>(capacity: effectiveLength);
        var slotIndex = 0;
        var escape = false;
        var caseMode = CaseMode.None;

        for (var i = 0; i < effectiveLength; i++)
        {
            var ch = template[i];

            if (escape)
            {
                tokenList.Add(new TemplateToken(TokenKind.Literal, Required: false, CaseMode.None, Literal: ch, SlotIndex: -1));
                escape = false;
                continue;
            }

            if (ch == '\\')
            {
                escape = true;
                continue;
            }

            if (ch == '>' || ch == '<' || ch == '!')
            {
                caseMode = ch switch
                {
                    '>' => CaseMode.Upper,
                    '<' => CaseMode.Lower,
                    _ => CaseMode.None,
                };
                continue;
            }

            if (TryParseToken(ch, slotIndex, caseMode, out var token))
            {
                slotTokenIndexList.Add(tokenList.Count);
                tokenList.Add(token);
                slotIndex++;
                continue;
            }

            tokenList.Add(new TemplateToken(TokenKind.Literal, Required: false, CaseMode.None, Literal: ch, SlotIndex: -1));
        }

        tokens = tokenList.ToArray();
        slotTokenIndexes = slotTokenIndexList.ToArray();
        slotCount = slotIndex;
    }

    private static bool TryParseToken(char ch, int slotIndex, CaseMode caseMode, out TemplateToken token)
    {
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

        token = new TemplateToken(kind, required, caseMode, Literal: '\0', SlotIndex: slotIndex);
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

    private sealed class MaskedInputDocument : ITextDocument
    {
        private TemplateToken[] _tokens = Array.Empty<TemplateToken>();
        private string _text = string.Empty;
        private int _version;
        private TextSnapshot _snapshot = new(version: 0, text: string.Empty, lineStarts: [0], lineBreakLengths: [0]);

        private int _updateDepth;

        public string Text => _text;

        public ITextSnapshot CurrentSnapshot => _snapshot;

        public int Version => _version;

        public event EventHandler<TextDocumentChangedEventArgs>? Changed;

        public void SetTemplate(TemplateToken[] tokens)
        {
            _tokens = tokens ?? Array.Empty<TemplateToken>();
        }

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
            if (position < 0 || position > _text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            if (length < 0 || position + length > _text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0 && text.IsEmpty)
            {
                return;
            }

            if (_tokens.Length == 0)
            {
                SetText(string.Empty);
                return;
            }

            var chars = _text.Length == 0 ? new char[_tokens.Length] : _text.ToCharArray();
            if (chars.Length != _tokens.Length)
            {
                chars = new char[_tokens.Length];
                for (var i = 0; i < _tokens.Length; i++)
                {
                    chars[i] = _tokens[i].Kind == TokenKind.Literal ? _tokens[i].Literal : ' ';
                }
            }

            var changed = false;

            var clearEnd = Math.Min(position + length, _tokens.Length);
            for (var i = position; i < clearEnd; i++)
            {
                if (_tokens[i].IsEditable)
                {
                    if (chars[i] != ' ')
                    {
                        chars[i] = ' ';
                        changed = true;
                    }
                }
            }

            if (!text.IsEmpty)
            {
                var insertIndex = position;
                var i = 0;
                while (i < text.Length)
                {
                    insertIndex = FindNextEditableIndex(insertIndex);
                    if (insertIndex < 0)
                    {
                        break;
                    }

                    var next = TerminalTextUtility.GetNextTextElementIndex(text, i);
                    if (next <= i)
                    {
                        next = i + 1;
                    }

                    var element = text.Slice(i, next - i);
                    i = next;

                    if (element.Length != 1)
                    {
                        continue;
                    }

                    var ch = element[0];
                    if (ch == '\r' || ch == '\n')
                    {
                        continue;
                    }

                    var token = _tokens[insertIndex];
                    if (!IsCharAllowed(token, ch))
                    {
                        continue;
                    }

                    ch = ApplyCase(token, ch);
                    if (chars[insertIndex] != ch)
                    {
                        chars[insertIndex] = ch;
                        changed = true;
                    }

                    insertIndex++;
                }
            }

            if (!changed)
            {
                return;
            }

            var oldVersion = _version;
            _version++;
            _text = new string(chars);
            _snapshot = new TextSnapshot(_version, _text, [0], [0]);

            RaiseChanged(new TextDocumentChangedEventArgs
            {
                OldVersion = oldVersion,
                NewVersion = _version,
                Position = position,
                RemovedLength = length,
                InsertedLength = text.Length,
                OldLineCount = 1,
                NewLineCount = 1,
                InsertedTextHint = text.IsEmpty ? null : text.ToString(),
            });
        }

        public void SetText(string text)
        {
            if (string.Equals(text, _text, StringComparison.Ordinal))
            {
                return;
            }

            var oldVersion = _version;
            _version++;
            _text = text ?? string.Empty;
            _snapshot = new TextSnapshot(_version, _text, [0], [0]);

            RaiseChanged(new TextDocumentChangedEventArgs
            {
                OldVersion = oldVersion,
                NewVersion = _version,
                Position = 0,
                RemovedLength = 0,
                InsertedLength = 0,
                OldLineCount = 1,
                NewLineCount = 1,
                InsertedTextHint = null,
            });
        }

        private int FindNextEditableIndex(int startIndex)
        {
            for (var i = Math.Clamp(startIndex, 0, _tokens.Length); i < _tokens.Length; i++)
            {
                if (_tokens[i].IsEditable)
                {
                    return i;
                }
            }

            return -1;
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
            private MaskedInputDocument? _owner;

            public UpdateScope(MaskedInputDocument owner)
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
}
