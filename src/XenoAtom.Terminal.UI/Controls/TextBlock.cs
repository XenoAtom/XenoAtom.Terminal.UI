// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a text display control with optional wrapping and trimming.
/// </summary>
public sealed partial class TextBlock : Visual, ISelectionOwner
{
    private static readonly Rune Ellipsis = new(0x2026);

    private int _selectionAnchor = -1;
    private int _selectionActive = -1;
    private int _interactionVersionCounter;
    private bool _pendingPointerSelection;
    private bool _dragSelecting;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    public TextBlock()
    {
        IsSelectable = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class with text.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public TextBlock(string text) : this()
    {
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class with a dynamic text provider.
    /// </summary>
    /// <param name="text">The text provider.</param>
    public TextBlock(Func<string> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the TextBlock class and binds its text content to the specified string value.
    /// </summary>
    /// <param name="text">A binding that supplies the text content to display. The binding may provide a null value, in which case the
    /// TextBlock will display nothing.</param>
    public TextBlock(Binding<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wrapping is enabled.
    /// </summary>
    [Bindable]
    public partial bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets the horizontal text alignment.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the trimming mode when text exceeds available width.
    /// </summary>
    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the text block participates in selection ownership.
    /// </summary>
    [Bindable]
    public partial bool IsSelectable { get; set; }

    [Bindable]
    private partial int InteractionVersion { get; set; }

    /// <inheritdoc />
    public bool HasSelection => _selectionAnchor >= 0 && _selectionActive >= 0 && _selectionAnchor != _selectionActive;

    void ISelectionOwner.ClearSelection() => ClearSelection();

    /// <inheritdoc />
    public bool TryCopySelection(out string text)
    {
        var value = Text ?? string.Empty;
        if (!TryGetOrderedSelection(value.Length, out var start, out var end) || end <= start)
        {
            text = string.Empty;
            return false;
        }

        text = value.AsSpan(start, end - start).ToString();
        return text.Length > 0;
    }

    partial void OnTextChanged(string? value)
    {
        _ = value;
        ClearSelection();
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var text = Text ?? string.Empty;
        var naturalWidth = TerminalTextUtility.GetWidth(text.AsSpan());
        var width = Math.Max(0, Math.Min(availableSize.Width, naturalWidth));
        var lineHeight = Math.Min(Math.Max(0, availableSize.Height), 1);

        if (!Wrap)
        {
            var minWidth = naturalWidth > 0 ? 1 : 0;
            return SizeHints.Flex(
                new Size(minWidth, lineHeight),
                new Size(naturalWidth, lineHeight),
                new Size(naturalWidth, lineHeight),
                growX: 0,
                growY: 0,
                shrinkX: naturalWidth > minWidth ? 1 : 0,
                shrinkY: 0);
        }

        if (width == 0)
        {
            return SizeHints.Fixed(new Size(width, lineHeight));
        }

        var height = CountWrappedLines(text.AsSpan(), Math.Max(1, width));
        var wrappedHeight = Math.Min(availableSize.Height, Math.Max(1, height));
        var wrapMinWidth = naturalWidth > 0 ? 1 : 0;
        return SizeHints.Flex(
            new Size(wrapMinWidth, lineHeight),
            new Size(width, wrappedHeight),
            new Size(naturalWidth, LayoutConstants.Infinite),
            growX: 0,
            growY: 0,
            shrinkX: width > wrapMinWidth ? 1 : 0,
            shrinkY: 0);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        _ = InteractionVersion;
        var text = Text ?? string.Empty;
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var textBlockStyle = GetStyle<TextBlockStyle>();
        var style = textBlockStyle.ResolveTextStyle(theme);
        var hasSelection = TryGetOrderedSelection(text.Length, out var selectionStart, out var selectionEnd);
        var selectionStyle = hasSelection ? theme.SelectionStyle() : Style.None;
        var foregroundBrush = textBlockStyle.ForegroundBrush;
        var backgroundBrush = textBlockStyle.BackgroundBrush;
        var defaultMixSpace = theme.GradientMixSpace;

        if (textBlockStyle.FillBackground && (textBlockStyle.Background is not null || backgroundBrush is not null))
        {
            var fill = textBlockStyle.ResolveFillStyle(theme);
            if (backgroundBrush is { } brush)
            {
                buffer.FillRectWithBrush(rect, fill, foregroundBrush: null, backgroundBrush: brush, defaultMixSpace);
            }
            else
            {
                for (var y = rect.Y; y < rect.Y + rect.Height; y++)
                {
                    for (var x = rect.X; x < rect.X + rect.Width; x++)
                    {
                        buffer.SetCell(x, y, new Rune(' '), fill);
                    }
                }
            }
        }

        if (!Wrap || rect.Height == 1)
        {
            WriteSingleLine(buffer, rect, text.AsSpan(), style, selectionStyle, hasSelection, selectionStart, selectionEnd, in rect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var lineIndex = 0;
        var start = 0;
        var span = text.AsSpan();
        var maxWidth = rect.Width;

        while (start < span.Length && lineIndex < rect.Height)
        {
            if (!TryGetNextWrapSlice(span, start, maxWidth, out var endExclusive, out var nextStart))
            {
                break;
            }

            var slice = span.Slice(start, Math.Max(0, endExclusive - start));
            var lineY = rect.Y + lineIndex;
            var lineBrushRect = new Rectangle(rect.X, lineY, rect.Width, 1);
            WriteAlignedLine(buffer, rect, lineY, slice, start, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in lineBrushRect, foregroundBrush, backgroundBrush, defaultMixSpace, isLastLine: nextStart >= span.Length);
            lineIndex++;
            start = nextStart;
        }
    }

    private void WriteSingleLine(
        CellBuffer buffer,
        Rectangle rect,
        ReadOnlySpan<char> text,
        Style style,
        Style selectionStyle,
        bool hasSelection,
        int selectionStart,
        int selectionEnd,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace)
    {
        var maxWidth = rect.Width;
        if (maxWidth <= 0)
        {
            return;
        }

        var alignment = TextAlignment;
        var trimming = Trimming;

        if (trimming == TextTrimming.Clip)
        {
            var span = Clip(text, maxWidth);
            var cells = TerminalTextUtility.GetWidth(span);
            var x = AlignX(rect, alignment, maxWidth, cells);
            WriteSpanWithSelection(buffer, x, rect.Y, span, baseIndex: 0, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            WriteSpanWithSelection(buffer, x, rect.Y, text, baseIndex: 0, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        if (maxWidth == 1)
        {
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, rect.X, rect.Y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(rect.X, rect.Y, Ellipsis, ellipsisStyle);
            return;
        }

        var bodyWidth = maxWidth - 1;
        if (trimming == TextTrimming.EndEllipsis)
        {
            var span = Clip(text, bodyWidth);
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            WriteSpanWithSelection(buffer, x, rect.Y, span, baseIndex: 0, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, x + bodyWidth, rect.Y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(x + bodyWidth, rect.Y, Ellipsis, ellipsisStyle);
            return;
        }

        if (trimming == TextTrimming.StartEllipsis)
        {
            var startIndex = GetStartIndexForSuffix(text, bodyWidth);
            var suffix = text[startIndex..];
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, x, rect.Y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(x, rect.Y, Ellipsis, ellipsisStyle);
            WriteSpanWithSelection(buffer, x + 1, rect.Y, suffix, baseIndex: startIndex, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var clipped = Clip(text, maxWidth);
        var clippedWidth = TerminalTextUtility.GetWidth(clipped);
        var defaultX = AlignX(rect, alignment, maxWidth, clippedWidth);
        WriteSpanWithSelection(buffer, defaultX, rect.Y, clipped, baseIndex: 0, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
    }

    private void WriteAlignedLine(
        CellBuffer buffer,
        Rectangle rect,
        int y,
        ReadOnlySpan<char> text,
        int baseIndex,
        Style style,
        Style selectionStyle,
        bool hasSelection,
        int selectionStart,
        int selectionEnd,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace,
        bool isLastLine)
    {
        var width = rect.Width;
        if (width <= 0)
        {
            return;
        }

        var alignment = TextAlignment;
        if (!hasSelection && alignment == TextAlignment.Justify && !isLastLine)
        {
            if (TryWriteJustified(buffer, rect.X, y, width, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace))
            {
                return;
            }
        }

        var clipped = Clip(text, width);
        var cells = TerminalTextUtility.GetWidth(clipped);
        var x = AlignX(rect, alignment, width, cells);
        WriteSpanWithSelection(buffer, x, y, clipped, baseIndex, style, selectionStyle, hasSelection, selectionStart, selectionEnd, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
    }

    private static void WriteSpanWithSelection(
        CellBuffer buffer,
        int x,
        int y,
        ReadOnlySpan<char> text,
        int baseIndex,
        Style style,
        Style selectionStyle,
        bool hasSelection,
        int selectionStart,
        int selectionEnd,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace)
    {
        if (!hasSelection || selectionEnd <= baseIndex || selectionStart >= baseIndex + text.Length)
        {
            buffer.WriteTextWithBrush(x, y, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var localStart = Math.Clamp(selectionStart - baseIndex, 0, text.Length);
        var localEnd = Math.Clamp(selectionEnd - baseIndex, 0, text.Length);
        if (localEnd <= localStart)
        {
            buffer.WriteTextWithBrush(x, y, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var posX = x;

        var before = text[..localStart];
        if (!before.IsEmpty)
        {
            buffer.WriteTextWithBrush(posX, y, before, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            posX += TerminalTextUtility.GetWidth(before);
        }

        var selected = text.Slice(localStart, localEnd - localStart);
        if (!selected.IsEmpty)
        {
            buffer.WriteTextWithBrush(posX, y, selected, style | selectionStyle, in brushRect, foregroundBrush, backgroundBrush: null, defaultMixSpace);
            posX += TerminalTextUtility.GetWidth(selected);
        }

        var after = text[localEnd..];
        if (!after.IsEmpty)
        {
            buffer.WriteTextWithBrush(posX, y, after, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (!IsEnabled || !IsSelectable || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var text = Text ?? string.Empty;
        var index = GetTextIndexFromPosition(text, e.LocalX, e.LocalY);
        var isDoubleClick = e.ClickCount >= 2 || e.Kind == TerminalMouseKind.DoubleClick;
        if (isDoubleClick)
        {
            var (start, end) = GetWordSelection(text.AsSpan(), index);
            SetSelection(start, end);
            _pendingPointerSelection = false;
            _dragSelecting = false;
            e.Handled = true;
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Shift) != 0 && _selectionAnchor >= 0)
        {
            SetSelection(_selectionAnchor, index);
            _pendingPointerSelection = true;
            _dragSelecting = false;
            e.Handled = true;
            return;
        }

        SetSelection(index, index);
        _pendingPointerSelection = true;
        _dragSelecting = false;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!IsEnabled || !IsSelectable || !_pendingPointerSelection || e.Kind != TerminalMouseKind.Drag)
        {
            return;
        }

        _dragSelecting = true;
        var text = Text ?? string.Empty;
        var index = GetTextIndexFromPosition(text, e.LocalX, e.LocalY);
        SetSelection(_selectionAnchor >= 0 ? _selectionAnchor : index, index);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!IsEnabled || !IsSelectable || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_dragSelecting)
        {
            var text = Text ?? string.Empty;
            var index = GetTextIndexFromPosition(text, e.LocalX, e.LocalY);
            SetSelection(_selectionAnchor >= 0 ? _selectionAnchor : index, index);
            e.Handled = true;
        }

        _pendingPointerSelection = false;
        _dragSelecting = false;
    }

    private void SetSelection(int anchor, int active)
    {
        var text = Text ?? string.Empty;
        var span = text.AsSpan();
        var length = span.Length;

        var normalizedAnchor = NormalizeIndexToTextElementBoundary(span, Math.Clamp(anchor, 0, length));
        var normalizedActive = NormalizeIndexToTextElementBoundary(span, Math.Clamp(active, 0, length));

        if (_selectionAnchor == normalizedAnchor && _selectionActive == normalizedActive)
        {
            return;
        }

        _selectionAnchor = normalizedAnchor;
        _selectionActive = normalizedActive;
        IncrementInteractionVersion();
    }

    private void ClearSelection()
    {
        if (_selectionAnchor < 0 && _selectionActive < 0)
        {
            _pendingPointerSelection = false;
            _dragSelecting = false;
            return;
        }

        _selectionAnchor = -1;
        _selectionActive = -1;
        _pendingPointerSelection = false;
        _dragSelecting = false;
        IncrementInteractionVersion();
    }

    private void IncrementInteractionVersion()
    {
        _interactionVersionCounter++;
        InteractionVersion = _interactionVersionCounter;
    }

    private bool TryGetOrderedSelection(int textLength, out int start, out int end)
    {
        start = 0;
        end = 0;

        if (_selectionAnchor < 0 || _selectionActive < 0)
        {
            return false;
        }

        var anchor = Math.Clamp(_selectionAnchor, 0, textLength);
        var active = Math.Clamp(_selectionActive, 0, textLength);
        start = Math.Min(anchor, active);
        end = Math.Max(anchor, active);
        return end > start;
    }

    private static (int Start, int End) GetWordSelection(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        var start = TerminalTextUtility.GetWordStart(text, index);
        var end = TerminalTextUtility.GetWordEnd(text, index);
        return (start, end);
    }

    private int GetTextIndexFromPosition(string text, int localX, int localY)
    {
        var rect = Bounds;
        if (rect.Width <= 0)
        {
            return 0;
        }

        var x = localX;
        var y = localY;
        var span = text.AsSpan();

        if (y < 0)
        {
            return 0;
        }

        if (!Wrap || rect.Height == 1)
        {
            return GetTextIndexSingleLine(span, x, rect.Width);
        }

        if (y >= rect.Height)
        {
            return span.Length;
        }

        var start = 0;
        var lineIndex = 0;
        while (start < span.Length)
        {
            if (!TryGetNextWrapSlice(span, start, rect.Width, out var endExclusive, out var nextStart))
            {
                break;
            }

            if (lineIndex == y)
            {
                var slice = span.Slice(start, Math.Max(0, endExclusive - start));
                return start + GetTextIndexInVisibleLine(slice, x, rect.Width, TextAlignment);
            }

            lineIndex++;
            if (lineIndex >= rect.Height)
            {
                break;
            }

            start = nextStart;
        }

        return span.Length;
    }

    private int GetTextIndexSingleLine(ReadOnlySpan<char> text, int localX, int availableWidth)
    {
        var width = Math.Max(0, availableWidth);
        if (width <= 0)
        {
            return 0;
        }

        var alignment = TextAlignment;
        var trimming = Trimming;

        if (trimming == TextTrimming.Clip)
        {
            var span = Clip(text, width);
            return GetTextIndexInVisibleLine(span, localX, width, alignment);
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= width)
        {
            return GetTextIndexInVisibleLine(text, localX, width, alignment);
        }

        if (width == 1)
        {
            return text.Length;
        }

        var bodyWidth = width - 1;
        if (trimming == TextTrimming.EndEllipsis)
        {
            var span = Clip(text, bodyWidth);
            var xStart = AlignXLocal(alignment, width, width);
            if (localX <= xStart)
            {
                return 0;
            }

            if (localX >= xStart + bodyWidth)
            {
                return span.Length;
            }

            return GetTextIndexInVisibleLine(span, localX - xStart, bodyWidth, TextAlignment.Left);
        }

        if (trimming == TextTrimming.StartEllipsis)
        {
            var startIndex = GetStartIndexForSuffix(text, bodyWidth);
            var suffix = text[startIndex..];
            var xStart = AlignXLocal(alignment, width, width);
            if (localX <= xStart)
            {
                return startIndex;
            }

            // Ellipsis occupies the first cell; the suffix starts at xStart + 1.
            var localInside = localX - xStart - 1;
            if (localInside < 0)
            {
                return startIndex;
            }

            if (localInside >= bodyWidth)
            {
                return text.Length;
            }

            return startIndex + GetTextIndexInVisibleLine(suffix, localInside, bodyWidth, TextAlignment.Left);
        }

        var clipped = Clip(text, width);
        return GetTextIndexInVisibleLine(clipped, localX, width, alignment);
    }

    private static int GetTextIndexInVisibleLine(ReadOnlySpan<char> text, int localX, int availableWidth, TextAlignment alignment)
    {
        if (text.IsEmpty || availableWidth <= 0)
        {
            return 0;
        }

        if (alignment == TextAlignment.Justify)
        {
            alignment = TextAlignment.Left;
        }

        var cells = TerminalTextUtility.GetWidth(text);
        var xStart = AlignXLocal(alignment, availableWidth, cells);
        var cellOffset = localX - xStart;
        if (cellOffset <= 0)
        {
            return 0;
        }

        if (cellOffset >= cells)
        {
            return text.Length;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text, cellOffset, out var index))
        {
            return text.Length;
        }

        return Math.Clamp(index, 0, text.Length);
    }

    private static int AlignXLocal(TextAlignment alignment, int availableWidth, int contentWidth)
    {
        if (availableWidth <= contentWidth)
        {
            return 0;
        }

        return alignment switch
        {
            TextAlignment.Center => (availableWidth - contentWidth) / 2,
            TextAlignment.Right => availableWidth - contentWidth,
            _ => 0,
        };
    }

    private static int NormalizeIndexToTextElementBoundary(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        if (index == 0 || index == text.Length)
        {
            return index;
        }

        var prev = TerminalTextUtility.GetPreviousTextElementIndex(text, index);
        if (prev == index)
        {
            return index;
        }

        var next = TerminalTextUtility.GetNextTextElementIndex(text, prev);
        if (index == next)
        {
            return index;
        }

        return next;
    }

    private static int AlignX(Rectangle rect, TextAlignment alignment, int availableWidth, int contentWidth)
    {
        if (availableWidth <= contentWidth)
        {
            return rect.X;
        }

        return alignment switch
        {
            TextAlignment.Center => rect.X + ((availableWidth - contentWidth) / 2),
            TextAlignment.Right => rect.X + (availableWidth - contentWidth),
            _ => rect.X,
        };
    }

    private static ReadOnlySpan<char> Clip(ReadOnlySpan<char> text, int maxCells)
    {
        if (maxCells <= 0 || text.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text, maxCells, out var endIndex))
        {
            endIndex = text.Length;
        }

        return text[..Math.Clamp(endIndex, 0, text.Length)];
    }

    private static int GetStartIndexForSuffix(ReadOnlySpan<char> text, int maxCells)
    {
        if (maxCells <= 0 || text.IsEmpty)
        {
            return text.Length;
        }

        var width = 0;
        var index = text.Length;
        while (index > 0)
        {
            var prev = TerminalTextUtility.GetPreviousTextElementIndex(text, index);
            var w = TerminalTextUtility.GetWidth(text.Slice(prev, index - prev));
            if (width + w > maxCells)
            {
                break;
            }

            width += w;
            index = prev;
        }

        return index;
    }

    private static bool TryWriteJustified(
        CellBuffer buffer,
        int x,
        int y,
        int width,
        ReadOnlySpan<char> text,
        Style style,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace)
    {
        Span<(int Start, int Length)> words = stackalloc (int, int)[32];
        var wordCount = 0;

        var i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= text.Length)
            {
                break;
            }

            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (wordCount < words.Length)
            {
                words[wordCount++] = (start, i - start);
            }
            else
            {
                return false;
            }
        }

        if (wordCount <= 1)
        {
            return false;
        }

        var wordsWidth = 0;
        for (var w = 0; w < wordCount; w++)
        {
            wordsWidth += TerminalTextUtility.GetWidth(text.Slice(words[w].Start, words[w].Length));
        }

        var gaps = wordCount - 1;
        var baseLineWidth = wordsWidth + gaps;
        if (baseLineWidth > width)
        {
            return false;
        }

        var extra = width - baseLineWidth;
        var extraPerGap = gaps == 0 ? 0 : extra / gaps;
        var remainder = gaps == 0 ? 0 : extra % gaps;

        var posX = x;
        for (var w = 0; w < wordCount; w++)
        {
            var slice = text.Slice(words[w].Start, words[w].Length);
            buffer.WriteTextWithBrush(posX, y, slice, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            posX += TerminalTextUtility.GetWidth(slice);

            if (w + 1 < wordCount)
            {
                var spaces = 1 + extraPerGap + (w < remainder ? 1 : 0);
                for (var s = 0; s < spaces; s++)
                {
                    var spacedStyle = CellBufferBrushExtensions.ApplyBrushes(style, posX, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
                    buffer.SetCell(posX++, y, new Rune(' '), spacedStyle);
                }
            }
        }

        return true;
    }

    private static int CountWrappedLines(ReadOnlySpan<char> text, int width)
    {
        if (text.IsEmpty)
        {
            return 1;
        }

        var lines = 0;
        var start = 0;
        while (start < text.Length)
        {
            if (!TryGetNextWrapSlice(text, start, width, out _, out var nextStart))
            {
                break;
            }

            lines++;
            start = nextStart;
        }

        return Math.Max(1, lines);
    }

    private static bool TryGetNextWrapSlice(ReadOnlySpan<char> text, int start, int width, out int endExclusive, out int nextStart)
    {
        endExclusive = start;
        nextStart = start;

        if (start >= text.Length)
        {
            return false;
        }

        // Skip leading whitespace on new line.
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        if (start >= text.Length)
        {
            return false;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text[start..], width, out var relEnd))
        {
            relEnd = text.Length - start;
        }

        var tentativeEnd = Math.Clamp(start + relEnd, start, text.Length);

        // If we didn't hit the end, try to wrap on the last whitespace.
        var wrapEnd = tentativeEnd;
        if (tentativeEnd < text.Length)
        {
            var lastSpace = -1;
            for (var i = tentativeEnd - 1; i > start; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    lastSpace = i;
                    break;
                }
            }

            if (lastSpace > start)
            {
                wrapEnd = lastSpace;
            }
        }

        endExclusive = wrapEnd;

        nextStart = wrapEnd;
        while (nextStart < text.Length && char.IsWhiteSpace(text[nextStart]))
        {
            nextStart++;
        }

        return endExclusive > start;
    }
}
