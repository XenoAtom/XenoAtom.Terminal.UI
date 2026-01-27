// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a single-line text editor for editing numeric values.
/// </summary>
/// <typeparam name="T">The numeric type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="NumberBox{T}"/> uses the <see cref="TextEditorCore"/> infrastructure (via <see cref="TextEditorBase"/>)
/// to provide a full-featured editing experience (caret, selection, clipboard, etc.).
/// </para>
/// <para>
/// The current numeric value is exposed as a bindable <see cref="Value"/> property. Text input is validated on each
/// change; when the text parses successfully (and optional <see cref="ValueValidator"/> returns <c>null</c>), the
/// <see cref="Value"/> is updated. When validation fails, the numeric value is not updated and a validation message
/// is displayed below the editor.
/// </para>
/// </remarks>
public partial class NumberBox<T> : TextEditorBase where T : struct, INumber<T>
{
    private Rectangle _editorRect;
    private Rectangle _editorOuterRect;
    private bool _showOverflowIndicatorLeft;
    private bool _showOverflowIndicatorRight;

    private bool _updatingTextFromValue;
    private bool _updatingValueFromText;
    private bool _hasUserEditedText;

    private string? _validationMessage;
    private readonly ValidationMessageHost _validationHost;
    private readonly TextBlock _validationText;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberBox{T}"/> class.
    /// </summary>
    public NumberBox()
    {
        this.HorizontalAlignment(Align.Stretch);
        _validationHost = new ValidationMessageHost();
        _validationText = new TextBlock();
        AttachChild(_validationHost);

        ShowValidationMessage = true;
        InvalidNumberMessage = "Invalid number";
        ParseStyles = NumberStyles.Number;

        TextDocument = new DynamicTextDocument(
            getter: () => Text ?? string.Empty,
            setter: value => Text = value);

        // Ensure the text reflects the initial value.
        UpdateTextFromValue();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberBox{T}"/> class with an initial value.
    /// </summary>
    /// <param name="value">The initial value.</param>
    public NumberBox(T value) : this()
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the current numeric value.
    /// </summary>
    [Bindable]
    public partial T Value { get; set; }

    /// <summary>
    /// Gets or sets the current text displayed by the editor.
    /// </summary>
    /// <remarks>
    /// This property is primarily intended for diagnostics and advanced scenarios. In typical usage, bind to
    /// <see cref="Value"/> instead.
    /// </remarks>
    [Bindable]
    internal partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of the text within the editor.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validation messages are shown.
    /// </summary>
    [Bindable]
    public partial bool ShowValidationMessage { get; set; }

    /// <summary>
    /// Gets or sets the message displayed when the text cannot be parsed as a valid <typeparamref name="T"/>.
    /// </summary>
    [Bindable]
    public partial string? InvalidNumberMessage { get; set; }

    /// <summary>
    /// Gets or sets a formatter used to format <see cref="Value"/> into <see cref="Text"/>.
    /// </summary>
    [Bindable]
    public partial Delegator<Func<T, string>> ValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets the number styles used when parsing input.
    /// </summary>
    [Bindable]
    public partial NumberStyles ParseStyles { get; set; }

    /// <summary>
    /// Gets or sets the format provider used when parsing and formatting values.
    /// </summary>
    [Bindable]
    public partial IFormatProvider? FormatProvider { get; set; }

    /// <summary>
    /// Gets or sets an optional validation callback for parsed values.
    /// </summary>
    /// <remarks>
    /// The callback should return <c>null</c> when the value is valid, or a non-empty message when the value is invalid.
    /// </remarks>
    [Bindable]
    public partial Delegator<Func<T, string?>> ValueValidator { get; set; }

    /// <inheritdoc />
    protected override bool IsSingleLine => true;

    /// <inheritdoc />
    protected override bool AcceptsReturn => false;

    /// <inheritdoc />
    protected override TextAlignment Alignment => TextAlignment;

    /// <inheritdoc />
    protected override bool ShowPlaceholderWhenUnfocusedOnly => true;

    /// <summary>
    /// Gets the style used to render the editor portion of the number box.
    /// </summary>
    /// <returns>The current <see cref="TextBoxStyle"/>.</returns>
    protected virtual TextBoxStyle GetTextBoxStyle() => GetStyle<TextBoxStyle>();

    partial void OnInvalidNumberMessageChanged(string? value)
    {
        _ = value;
        ValidateAndUpdateValueFromText();
    }

    partial void OnShowValidationMessageChanged(bool value)
    {
        _ = value;
        UpdateValidationHost();
    }

    partial void OnParseStylesChanged(NumberStyles value)
    {
        _ = value;
        ValidateAndUpdateValueFromText();
    }

    partial void OnFormatProviderChanged(IFormatProvider? value)
    {
        _ = value;
        ValidateAndUpdateValueFromText();
        UpdateTextFromValue();
    }

    partial void OnValueValidatorChanged(Delegator<Func<T, string?>> value)
    {
        _ = value;
        ValidateAndUpdateValueFromText();
    }

    partial void OnValueFormatterChanged(Delegator<Func<T, string>> value)
    {
        _ = value;
        UpdateTextFromValue();
    }

    partial void OnTextChanged(string? value)
    {
        if (_updatingTextFromValue)
        {
            return;
        }

        _ = value;
        _hasUserEditedText = true;
        ValidateAndUpdateValueFromText();
    }

    partial void OnValueChanged(T value)
    {
        if (_updatingValueFromText)
        {
            return;
        }

        _ = value;
        UpdateTextFromValue();
        SetValidationMessage(null);
    }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _validationHost : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var width = Math.Max(10, Math.Min(availableSize.Width, 24));

        var textBoxStyle = GetTextBoxStyle();
        var padding = textBoxStyle.Padding;
        var editorHeight = Math.Max(1, 1 + padding.Vertical);

        var height = editorHeight;

        if (ShowValidationMessage && _validationHost.HasMessage)
        {
            var validationStyle = GetStyle<ValidationStyle>();
            var validationConstraints = new LayoutConstraints(
                0,
                width,
                0,
                Math.Max(0, constraints.MaxHeight - editorHeight));

            var validationHints = _validationHost.Measure(validationConstraints);
            var validationHeight = validationHints.Natural.Height;
            if (validationHeight > 0)
            {
                height += Math.Max(0, validationStyle.Gap) + validationHeight;
            }
        }

        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, height)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;

        var rect = finalRect;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            _editorRect = new Rectangle(0, 0, 0, 0);
            _editorOuterRect = new Rectangle(0, 0, 0, 0);
            _validationHost.Arrange(new Rectangle(0, 0, 0, 0));
            return;
        }

        var showValidation = ShowValidationMessage && _validationHost.HasMessage;
        var validationGap = 0;
        var validationHeight = 0;

        if (showValidation)
        {
            var validationStyle = GetStyle<ValidationStyle>();
            validationGap = Math.Max(0, validationStyle.Gap);

            var validationConstraints = new LayoutConstraints(0, rect.Width, 0, rect.Height);
            var validationHints = _validationHost.Measure(validationConstraints);
            validationHeight = Math.Min(rect.Height, validationHints.Natural.Height);

            if (validationHeight <= 0)
            {
                showValidation = false;
                validationGap = 0;
                validationHeight = 0;
            }
            else if (validationHeight + validationGap > rect.Height)
            {
                validationGap = 0;
            }
        }

        var reserved = showValidation ? validationGap + validationHeight : 0;
        var editorHeight = Math.Max(0, rect.Height - reserved);
        var editorRect = new Rectangle(rect.X, rect.Y, rect.Width, editorHeight);

        var validationRect = showValidation
            ? new Rectangle(rect.X, rect.Y + editorHeight + validationGap, rect.Width, validationHeight)
            : new Rectangle(0, 0, 0, 0);

        _validationHost.Arrange(validationRect);
        _editorOuterRect = editorRect;

        var style = GetTextBoxStyle();
        var padding = style.Padding;

        var baseRect = new Rectangle(
            editorRect.X + padding.Left,
            editorRect.Y + padding.Top,
            Math.Max(0, editorRect.Width - padding.Horizontal),
            Math.Max(0, editorRect.Height - padding.Vertical));

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

        // Keep the displayed text in sync with Value when not focused. This is especially important when Value is
        // bound to a State/Binding: the binding value can change without invoking OnValueChanged.
        if (!IsFocused && !_updatingTextFromValue && !_updatingValueFromText)
        {
            UpdateTextFromValue();
        }

        var isFocused = IsFocused;
        var theme = GetTheme();
        var textBoxStyle = GetTextBoxStyle();
        var selectionStyle = textBoxStyle.SelectionStyle(theme);
        var backgroundStyle = textBoxStyle.BackgroundStyle(theme, isFocused);
        var placeholderStyle = textBoxStyle.PlaceholderStyle(theme, isFocused);
        var padding = textBoxStyle.Padding;

        var editorRect = _editorOuterRect.Width <= 0 || _editorOuterRect.Height <= 0
            ? new Rectangle(rect.X, rect.Y, rect.Width, rect.Height)
            : _editorOuterRect;

        var baseRect = new Rectangle(
            editorRect.X + padding.Left,
            editorRect.Y + padding.Top,
            Math.Max(0, editorRect.Width - padding.Horizontal),
            Math.Max(0, editorRect.Height - padding.Vertical));

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

        var renderRect = _editorRect.Width <= 0 || _editorRect.Height <= 0 ? baseRect : _editorRect;
        RenderEditor(buffer, renderRect, backgroundStyle, selectionStyle, placeholderStyle);

        if (renderRect.Width > 0 && renderRect.Height > 0)
        {
            var y = renderRect.Y;
            var indicatorStyle = textBoxStyle.OverflowIndicatorStyle(theme);

            if (_showOverflowIndicatorLeft && textBoxStyle.OverflowIndicatorLeft is { } left)
            {
                var x = renderRect.X - 1;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, left, indicatorStyle);
                }
            }

            if (_showOverflowIndicatorRight && textBoxStyle.OverflowIndicatorRight is { } right)
            {
                var x = renderRect.X + renderRect.Width;
                if (x >= baseRect.X && x < baseRect.X + baseRect.Width)
                {
                    buffer.SetCell(x, y, right, indicatorStyle);
                }
            }
        }
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

    private void ValidateAndUpdateValueFromText()
    {
        if (!_hasUserEditedText && !IsFocused && !_updatingTextFromValue && !_updatingValueFromText)
        {
            // Before the user edits the content, treat Value as the source-of-truth.
            // This avoids overwriting a bound state when another property (e.g. InvalidNumberMessage) changes and
            // triggers validation while the initial text still reflects the default Value.
            UpdateTextFromValue();
            SetValidationMessage(null);
            return;
        }

        var text = Text ?? string.Empty;

        // Avoid showing the parse error when the user hasn't entered anything yet.
        if (text.Length == 0)
        {
            SetValidationMessage(null);
            return;
        }

        var provider = FormatProvider;
        var parseStyles = ParseStyles;
        var ok = T.TryParse(text.AsSpan(), parseStyles, provider, out var parsed);
        if (!ok)
        {
            SetValidationMessage(InvalidNumberMessage ?? "Invalid number");
            return;
        }

        var validator = ValueValidator.Invoke;
        var message = validator?.Invoke(parsed);
        if (!string.IsNullOrEmpty(message))
        {
            SetValidationMessage(message);
            return;
        }

        SetValidationMessage(null);
        if (!Value.Equals(parsed))
        {
            _updatingValueFromText = true;
            try
            {
                Value = parsed;
            }
            finally
            {
                _updatingValueFromText = false;
            }
        }
    }

    private void SetValidationMessage(string? message)
    {
        if (string.Equals(_validationMessage, message, StringComparison.Ordinal))
        {
            return;
        }

        _validationMessage = message;
        UpdateValidationHost();
    }

    private void UpdateTextFromValue()
    {
        var formatter = ValueFormatter.Invoke;
        string text;
        if (formatter is not null)
        {
            text = formatter(Value) ?? string.Empty;
        }
        else if (Value is IFormattable formattable)
        {
            text = formattable.ToString(null, FormatProvider) ?? string.Empty;
        }
        else
        {
            text = Value.ToString() ?? string.Empty;
        }

        if (string.Equals(Text, text, StringComparison.Ordinal))
        {
            return;
        }

        _updatingTextFromValue = true;
        try
        {
            Text = text;
            _hasUserEditedText = false;
        }
        finally
        {
            _updatingTextFromValue = false;
        }
    }

    private void UpdateValidationHost()
    {
        if (!ShowValidationMessage || string.IsNullOrEmpty(_validationMessage))
        {
            if (_validationHost.HasMessage)
            {
                _validationHost.SetMessage(null);
            }

            return;
        }

        _validationText.Text = _validationMessage;
        _validationHost.SetMessage(new ValidationMessage(ValidationSeverity.Error, _validationText));
    }
}
