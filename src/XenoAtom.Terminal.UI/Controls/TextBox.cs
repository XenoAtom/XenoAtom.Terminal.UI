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
/// Specifies when a password text box should reveal its text instead of masking it.
/// </summary>
public enum PasswordRevealMode
{
    /// <summary>
    /// Never reveal the text (always masked).
    /// </summary>
    Never = 0,

    /// <summary>
    /// Reveal the text while the control is focused.
    /// </summary>
    WhileFocused = 1,

    /// <summary>
    /// Always reveal the text.
    /// </summary>
    Always = 2,
}

/// <summary>
/// Specifies clipboard behavior for a <see cref="TextBox"/>.
/// </summary>
public enum TextBoxClipboardMode
{
    /// <summary>
    /// Disable copy/cut operations via keyboard shortcuts (Ctrl+C / Ctrl+X).
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Allow copy/cut operations.
    /// </summary>
    CopyText = 1,
}

/// <summary>
/// Represents a single-line text editor with optional overflow indicators.
/// </summary>
public partial class TextBox : TextEditorBase
{
    private Rectangle _editorRect;
    private bool _showOverflowIndicatorLeft;
    private bool _showOverflowIndicatorRight;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBox"/> class.
    /// </summary>
    public TextBox()
    {
        this.HorizontalAlignment(Align.Stretch);
        this.IsPassword(false);
        this.PasswordRevealMode(PasswordRevealMode.Never);
        this.ClipboardMode(TextBoxClipboardMode.CopyText);
        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBox"/> class with initial text.
    /// </summary>
    /// <param name="text">The initial text.</param>
    public TextBox(string? text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the TextBox class and binds its text content to the specified binding.
    /// </summary>
    /// <remarks>Changes to the bound value are automatically reflected in the TextBox, and updates to the
    /// TextBox's text are propagated to the binding source if supported. The binding can be null to indicate no initial
    /// value.</remarks>
    /// <param name="text">A binding that provides the text value for the TextBox. The binding may be updated to reflect changes in the
    /// TextBox or its source.</param>
    public TextBox(Binding<string?> text) : this()
    {
        this.BindText(text);
    }

    /// <summary>
    /// Initializes a new instance of the TextBox class and binds its text content to the specified state object.
    /// </summary>
    /// <remarks>Use this constructor to create a TextBox that automatically synchronizes its displayed text
    /// with the provided state. This enables two-way data binding between the TextBox and the state object.</remarks>
    /// <param name="text">The state object representing the text value to bind to the TextBox. Changes to this state will update the
    /// TextBox content, and vice versa.</param>
    public TextBox(State<string?> text) : this()
    {
        this.BindText(text);
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the text should be masked (password mode).
    /// </summary>
    [Bindable]
    public partial bool IsPassword { get; set; }

    /// <summary>
    /// Gets or sets the reveal behavior used when <see cref="IsPassword"/> is enabled.
    /// </summary>
    [Bindable]
    public partial PasswordRevealMode PasswordRevealMode { get; set; }

    /// <summary>
    /// Gets or sets the clipboard behavior for this text box.
    /// </summary>
    [Bindable]
    public partial TextBoxClipboardMode ClipboardMode { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of the text within the editor.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    /// <inheritdoc />
    protected override bool IsSingleLine => true;

    /// <inheritdoc />
    protected override bool AcceptsReturn => false;

    /// <inheritdoc />
    protected override TextAlignment Alignment => TextAlignment;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => true;

    /// <summary>
    /// Gets the style used to render the text box.
    /// </summary>
    /// <returns>The current <see cref="TextBoxStyle"/>.</returns>
    protected virtual TextBoxStyle GetTextBoxStyle() => GetStyle<TextBoxStyle>();

    /// <inheritdoc />
    protected override void WriteTextSegment(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, Style style, bool isPlaceholder, int textIndexStart, int startColumn)
    {
        if (isPlaceholder || !IsPassword || ShouldRevealPassword())
        {
            base.WriteTextSegment(buffer, x, y, text, style, isPlaceholder, textIndexStart, startColumn);
            return;
        }

        var textBoxStyle = GetTextBoxStyle();
        var rune = textBoxStyle.PasswordMaskGlyph;
        var runeWidth = TerminalTextUtility.GetRuneWidth(rune);
        if (runeWidth != 1)
        {
            rune = new Rune('*');
        }

        var totalCells = TerminalTextUtility.GetWidth(text);
        for (var i = 0; i < totalCells; i++)
        {
            buffer.SetCell(x + i, y, rune, style);
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ClipboardMode != TextBoxClipboardMode.CopyText && (e.Modifiers & TerminalModifiers.Ctrl) != 0)
        {
            if (e.Char is TerminalChar.CtrlC or TerminalChar.CtrlX)
            {
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var width = Math.Max(10, Math.Min(availableSize.Width, 24));
        var padding = GetTextBoxStyle().Padding;
        var height = Math.Max(1, 1 + padding.Vertical);
        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, height)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var style = GetTextBoxStyle();
        var padding = style.Padding;

        var innerLeft = finalRect.X;
        var innerTop = finalRect.Y;
        var innerWidth = finalRect.Width;
        var innerHeight = finalRect.Height;

        var baseRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

        UpdateEditorLayoutForOverflowIndicators(baseRect, style);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var isFocused = HasFocus;
        var theme = GetTheme();
        var textBoxStyle = GetTextBoxStyle();
        var selectionStyle = textBoxStyle.SelectionStyle(theme);
        var backgroundStyle = textBoxStyle.BackgroundStyle(theme, isFocused);
        var placeholderStyle = textBoxStyle.PlaceholderStyle(theme, isFocused);
        var padding = textBoxStyle.Padding;

        var innerLeft = rect.X;
        var innerTop = rect.Y;
        var innerWidth = rect.Width;
        var innerHeight = rect.Height;

        var baseRect = new Rectangle(
            innerLeft + padding.Left,
            innerTop + padding.Top,
            Math.Max(0, innerWidth - padding.Horizontal),
            Math.Max(0, innerHeight - padding.Vertical));

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

        var editorRect = _editorRect.Width <= 0 || _editorRect.Height <= 0 ? baseRect : _editorRect;
        RenderEditor(buffer, editorRect, backgroundStyle, selectionStyle, placeholderStyle);

        if (editorRect.Width > 0 && editorRect.Height > 0)
        {
            var y = editorRect.Y;
            var indicatorStyle = textBoxStyle.OverflowIndicatorStyle(theme);

            if (_showOverflowIndicatorLeft && textBoxStyle.OverflowIndicatorLeft is { } left)
            {
                var x = editorRect.X - 1;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, left, indicatorStyle);
                }
            }

            if (_showOverflowIndicatorRight && textBoxStyle.OverflowIndicatorRight is { } right)
            {
                var x = editorRect.X + editorRect.Width;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, right, indicatorStyle);
                }
            }
        }
    }

    private bool ShouldRevealPassword()
    {
        var mode = PasswordRevealMode;
        return mode == PasswordRevealMode.Always
               || (mode == PasswordRevealMode.WhileFocused && HasFocus);
    }

    private void UpdateEditorLayoutForOverflowIndicators(Rectangle baseRect, TextBoxStyle style)
    {
        _editorRect = baseRect;
        _showOverflowIndicatorLeft = false;
        _showOverflowIndicatorRight = false;

        for (var pass = 0; pass < 3; pass++)
        {
            UpdateEditorLayout(_editorRect);

            var canShowLeft = style.OverflowIndicatorLeft is not null;
            var canShowRight = style.OverflowIndicatorRight is not null;

            var showLeft = canShowLeft && Scroll.OffsetX > 0;
            var showRight = canShowRight && Scroll.OffsetX + Scroll.ViewportWidth < Scroll.ExtentWidth;

            var nextRect = baseRect;
            if (showLeft)
            {
                nextRect = new Rectangle(nextRect.X + 1, nextRect.Y, Math.Max(0, nextRect.Width - 1), nextRect.Height);
            }

            if (showRight)
            {
                nextRect = new Rectangle(nextRect.X, nextRect.Y, Math.Max(0, nextRect.Width - 1), nextRect.Height);
            }

            if (nextRect == _editorRect && showLeft == _showOverflowIndicatorLeft && showRight == _showOverflowIndicatorRight)
            {
                return;
            }

            _editorRect = nextRect;
            _showOverflowIndicatorLeft = showLeft;
            _showOverflowIndicatorRight = showRight;
        }
    }
}
