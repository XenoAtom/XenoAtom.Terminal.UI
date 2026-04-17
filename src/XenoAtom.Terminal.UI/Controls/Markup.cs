// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a control that renders ANSI markup into styled text.
/// </summary>
public sealed partial class Markup : Visual, ISelectionOwner
{
    private static readonly Rune Ellipsis = new(0x2026);

    private readonly MarkupTextParser _parser;

    private int _selectionAnchor = -1;
    private int _selectionActive = -1;
    private bool _pendingPointerSelection;
    private bool _dragSelecting;

    private string? _cachedMarkup;
    private Dictionary<string, AnsiStyle>? _cachedMarkupStyles;
    private string _plainText = string.Empty;
    private StyledRun[] _runs = Array.Empty<StyledRun>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class.
    /// </summary>
    public Markup()
    {
        _parser = new MarkupTextParser();
        IsSelectable = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class with markup text.
    /// </summary>
    /// <param name="markup">The markup text to render.</param>
    public Markup(string markup) : this()
    {
        Text = markup;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class from an interpolated markup handler.
    /// </summary>
    /// <param name="handler">The interpolated handler.</param>
    public Markup(ref AnsiMarkupInterpolatedStringHandler handler) : this()
    {
        Text = handler.WrittenSpan.ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class with a dynamic markup provider.
    /// </summary>
    /// <param name="markup">The markup provider.</param>
    public Markup(Func<string> markup) : this()
    {
        this.Text(markup);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class with an interpolated markup provider.
    /// </summary>
    /// <param name="handler">The interpolated handler provider.</param>
    public Markup(Func<AnsiMarkupInterpolatedStringHandler> handler) : this()
    {
        this.Text(() => handler().WrittenSpan.ToString());
    }

    /// <summary>
    /// Initializes a new instance of the Markup class and binds its text content to the specified markup value.
    /// </summary>
    /// <remarks>Use this constructor to create a Markup element whose text content updates automatically when
    /// the bound value changes.</remarks>
    /// <param name="markup">A binding that supplies the markup text to be displayed. The binding may provide a null value, which results in
    /// no content being shown.</param>
    public Markup(Binding<string?> markup) : this()
    {
        this.Text(markup);
    }

    /// <summary>
    /// Gets or sets the markup text.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether text should wrap to the available width.
    /// </summary>
    [Bindable]
    public partial bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of the rendered text.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the trimming mode used when text exceeds the available width.
    /// </summary>
    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the markup control participates in selection ownership.
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
        EnsureParsed();

        if (!TryGetOrderedSelection(_plainText.Length, out var start, out var end) || end <= start)
        {
            text = string.Empty;
            return false;
        }

        text = _plainText.AsSpan(start, end - start).ToString();
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
        EnsureParsed();

        var text = _plainText.AsSpan();
        var availableWidth = Math.Max(0, availableSize.Width);
        var availableHeight = Math.Max(0, availableSize.Height);
        var naturalWidth = GetMaxLineWidth(text);
        var width = Math.Max(0, Math.Min(availableWidth, naturalWidth));
        var minWidth = naturalWidth > 0 ? 1 : 0;

        if (!Wrap)
        {
            var height = Math.Min(availableHeight, CountHardLines(text));
            return SizeHints.Flex(
                new Size(minWidth, height),
                new Size(naturalWidth, height),
                new Size(naturalWidth, height),
                growX: 0,
                growY: 0,
                shrinkX: naturalWidth > minWidth ? 1 : 0,
                shrinkY: 0);
        }

        if (width == 0)
        {
            return SizeHints.Fixed(new Size(0, Math.Min(availableHeight, 1)));
        }

        var wrappedHeight = CountWrappedLines(text, Math.Max(1, width));
        return SizeHints.Flex(
            new Size(minWidth, Math.Min(availableHeight, Math.Max(1, wrappedHeight))),
            new Size(width, Math.Min(availableHeight, Math.Max(1, wrappedHeight))),
            new Size(naturalWidth, LayoutConstants.Infinite),
            growX: 0,
            growY: 0,
            shrinkX: width > minWidth ? 1 : 0,
            shrinkY: 0);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        _ = InteractionVersion;
        EnsureParsed();

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var hasSelection = TryGetOrderedSelection(_plainText.Length, out var selectionStart, out var selectionEnd);
        var selectionStyle = hasSelection ? GetTheme().SelectionStyle() : Style.None;

        var text = _plainText.AsSpan();
        var y = rect.Y;
        var start = 0;

        while (y < rect.Bottom && start <= text.Length)
        {
            if (!TryGetNextHardLine(text, start, out var hardEnd, out var nextStart))
            {
                break;
            }

            if (!Wrap)
            {
                WriteSingleLine(buffer, rect, y, start, hardEnd, hasSelection, selectionStart, selectionEnd, selectionStyle);
                y++;
            }
            else
            {
                var hardLine = text.Slice(start, Math.Max(0, hardEnd - start));
                if (hardLine.IsEmpty || IsAllWhitespace(hardLine))
                {
                    y++;
                }
                else
                {
                    var rel = 0;
                    var any = false;
                    while (y < rect.Bottom && rel < hardLine.Length)
                    {
                        if (!TryGetNextWrapSlice(hardLine, rel, rect.Width, out var relEnd, out var relNext))
                        {
                            break;
                        }

                        any = true;
                        var isLastLine = relNext >= hardLine.Length;
                        WriteAlignedLine(buffer, rect, y, start + rel, start + relEnd, isLastLine, hasSelection, selectionStart, selectionEnd, selectionStyle);
                        y++;
                        rel = relNext;
                    }

                    if (!any)
                    {
                        y++;
                    }
                }
            }

            if (nextStart == start)
            {
                break;
            }

            start = nextStart;
        }
    }

    private void WriteSingleLine(CellBuffer buffer, Rectangle rect, int y, int lineStartIndex, int lineEndIndex, bool hasSelection, int selectionStart, int selectionEnd, Style selectionStyle)
    {
        var maxWidth = rect.Width;
        if (maxWidth <= 0)
        {
            return;
        }

        var alignment = TextAlignment;
        var trimming = Trimming;

        if (alignment == TextAlignment.Justify)
        {
            alignment = TextAlignment.Left;
        }

        var text = _plainText.AsSpan(lineStartIndex, Math.Max(0, lineEndIndex - lineStartIndex));

        if (trimming == TextTrimming.Clip)
        {
            var clipEndIndex = GetEndIndexAtCell(text, maxWidth);
            var cells = TerminalTextUtility.GetWidth(text[..clipEndIndex]);
            var x = AlignX(rect, alignment, maxWidth, cells);
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + clipEndIndex, hasSelection, selectionStart, selectionEnd, selectionStyle);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + text.Length, hasSelection, selectionStart, selectionEnd, selectionStyle);
            return;
        }

        if (maxWidth == 1)
        {
            buffer.SetCell(rect.X, y, Ellipsis, Style.None);
            return;
        }

        if (trimming == TextTrimming.EndEllipsis)
        {
            var bodyWidth = maxWidth - 1;
            var bodyEndIndex = GetEndIndexAtCell(text, bodyWidth);
            var bodyCells = TerminalTextUtility.GetWidth(text[..bodyEndIndex]);
            var contentWidth = Math.Min(maxWidth, bodyCells + 1);
            var x = AlignX(rect, alignment, maxWidth, contentWidth);
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + bodyEndIndex, hasSelection, selectionStart, selectionEnd, selectionStyle);
            buffer.SetCell(x + bodyCells, y, Ellipsis, Style.None);
            return;
        }

        // StartEllipsis
        var suffixWidth = maxWidth - 1;
        var suffixStart = GetStartIndexForSuffix(text, suffixWidth);
        var suffix = text[suffixStart..];
        var suffixCells = TerminalTextUtility.GetWidth(suffix);
        var contentW = Math.Min(maxWidth, 1 + suffixCells);
        var x0 = AlignX(rect, alignment, maxWidth, contentW);
        buffer.SetCell(x0, y, Ellipsis, Style.None);
        WriteStyledSpan(buffer, x0 + 1, y, lineStartIndex + suffixStart, lineStartIndex + text.Length, hasSelection, selectionStart, selectionEnd, selectionStyle);
    }

    private void WriteAlignedLine(CellBuffer buffer, Rectangle rect, int y, int startIndex, int endExclusive, bool isLastLine, bool hasSelection, int selectionStart, int selectionEnd, Style selectionStyle)
    {
        var width = rect.Width;
        if (width <= 0)
        {
            return;
        }

        var alignment = TextAlignment;
        if (alignment == TextAlignment.Justify)
        {
            alignment = isLastLine ? TextAlignment.Left : TextAlignment.Left;
        }

        var line = _plainText.AsSpan(startIndex, Math.Max(0, endExclusive - startIndex));
        var end = GetEndIndexAtCell(line, width);
        var clipped = line[..end];
        var cells = TerminalTextUtility.GetWidth(clipped);
        var x = AlignX(rect, alignment, width, cells);
        WriteStyledSpan(buffer, x, y, startIndex, startIndex + end, hasSelection, selectionStart, selectionEnd, selectionStyle);
    }

    private void EnsureParsed()
    {
        var text = Text ?? string.Empty;
        var styles = GetTheme().GetMarkupStyles();
        if (ReferenceEquals(_cachedMarkup, text) && ReferenceEquals(_cachedMarkupStyles, styles))
        {
            return;
        }

        _cachedMarkup = text;
        _cachedMarkupStyles = styles;
        _plainText = _parser.Parse(text, out _runs, styles);
    }

    private void WriteStyledSpan(CellBuffer buffer, int x, int y, int startIndex, int endIndex, bool hasSelection, int selectionStart, int selectionEnd, Style selectionStyle)
    {
        if (startIndex >= endIndex || _runs.Length == 0)
        {
            return;
        }

        var posX = x;
        for (var i = 0; i < _runs.Length; i++)
        {
            var run = _runs[i];
            var runStart = run.Start;
            var runEnd = runStart + run.Length;

            if (runEnd <= startIndex)
            {
                continue;
            }

            if (runStart >= endIndex)
            {
                break;
            }

            var segStart = Math.Max(runStart, startIndex);
            var segEnd = Math.Min(runEnd, endIndex);
            var slice = _plainText.AsSpan(segStart, segEnd - segStart);

            if (hasSelection && segStart < selectionEnd && segEnd > selectionStart)
            {
                WriteSelectedRunSpan(buffer, posX, y, slice, segStart, run.Style, selectionStart, selectionEnd, selectionStyle);
            }
            else
            {
                buffer.WriteText(posX, y, slice, run.Style);
            }
            posX += TerminalTextUtility.GetWidth(slice);
        }
    }

    private void WriteSelectedRunSpan(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, int segmentStartIndex, Style runStyle, int selectionStart, int selectionEnd, Style selectionStyle)
    {
        var localStart = Math.Clamp(selectionStart - segmentStartIndex, 0, text.Length);
        var localEnd = Math.Clamp(selectionEnd - segmentStartIndex, 0, text.Length);
        if (localEnd <= localStart)
        {
            buffer.WriteText(x, y, text, runStyle);
            return;
        }

        var posX = x;
        var before = text[..localStart];
        if (!before.IsEmpty)
        {
            buffer.WriteText(posX, y, before, runStyle);
            posX += TerminalTextUtility.GetWidth(before);
        }

        var selected = text.Slice(localStart, localEnd - localStart);
        if (!selected.IsEmpty)
        {
            buffer.WriteText(posX, y, selected, runStyle | selectionStyle);
            posX += TerminalTextUtility.GetWidth(selected);
        }

        var after = text[localEnd..];
        if (!after.IsEmpty)
        {
            buffer.WriteText(posX, y, after, runStyle);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (!IsEnabled || !IsSelectable || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        EnsureParsed();
        var index = GetTextIndexFromPosition(e.LocalX, e.LocalY);
        var isDoubleClick = e.ClickCount >= 2 || e.Kind == TerminalMouseKind.DoubleClick;
        if (isDoubleClick)
        {
            var (start, end) = GetWordSelection(_plainText.AsSpan(), index);
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

        EnsureParsed();
        _dragSelecting = true;
        var index = GetTextIndexFromPosition(e.LocalX, e.LocalY);
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
            EnsureParsed();
            var index = GetTextIndexFromPosition(e.LocalX, e.LocalY);
            SetSelection(_selectionAnchor >= 0 ? _selectionAnchor : index, index);
            e.Handled = true;
        }

        _pendingPointerSelection = false;
        _dragSelecting = false;
    }

    private void SetSelection(int anchor, int active)
    {
        EnsureParsed();

        var text = _plainText.AsSpan();
        var length = text.Length;

        var normalizedAnchor = NormalizeIndexToTextElementBoundary(text, Math.Clamp(anchor, 0, length));
        var normalizedActive = NormalizeIndexToTextElementBoundary(text, Math.Clamp(active, 0, length));

        if (_selectionAnchor == normalizedAnchor && _selectionActive == normalizedActive)
        {
            return;
        }

        _selectionAnchor = normalizedAnchor;
        _selectionActive = normalizedActive;
        InteractionVersion++;
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
        InteractionVersion++;
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

    private int GetTextIndexFromPosition(int localX, int localY)
    {
        if (_plainText.Length == 0)
        {
            return 0;
        }

        var rect = Bounds;
        if (rect.Width <= 0)
        {
            return localY <= 0 ? 0 : _plainText.Length;
        }

        if (localY < 0)
        {
            return 0;
        }

        if (!Wrap)
        {
            return GetHardLineTextIndexFromPosition(_plainText.AsSpan(), localX, localY, rect.Width);
        }

        if (localY >= rect.Height)
        {
            return _plainText.Length;
        }

        var text = _plainText.AsSpan();
        var y = 0;
        var hardStart = 0;
        while (hardStart <= text.Length)
        {
            if (!TryGetNextHardLine(text, hardStart, out var hardEnd, out var hardNext))
            {
                break;
            }

            var hardLine = text.Slice(hardStart, Math.Max(0, hardEnd - hardStart));
            if (hardLine.IsEmpty || IsAllWhitespace(hardLine))
            {
                if (y == localY)
                {
                    return hardStart;
                }

                y++;
            }
            else
            {
                var rel = 0;
                var any = false;
                while (rel < hardLine.Length)
                {
                    if (!TryGetNextWrapSlice(hardLine, rel, rect.Width, out var relEnd, out var relNext))
                    {
                        break;
                    }

                    any = true;
                    if (y == localY)
                    {
                        return hardStart + rel + GetWrapSliceTextIndexFromPosition(hardLine.Slice(rel, Math.Max(0, relEnd - rel)), localX, rect.Width);
                    }

                    y++;
                    rel = relNext;
                }

                if (!any)
                {
                    if (y == localY)
                    {
                        return hardStart;
                    }

                    y++;
                }
            }

            if (y > localY)
            {
                break;
            }

            if (hardNext == hardStart)
            {
                break;
            }

            hardStart = hardNext;
        }

        return _plainText.Length;
    }

    private int GetHardLineTextIndexFromPosition(ReadOnlySpan<char> fullText, int localX, int localY, int availableWidth)
    {
        if (localY <= 0)
        {
            localY = 0;
        }

        var start = 0;
        var lineIndex = 0;
        while (start <= fullText.Length)
        {
            if (!TryGetNextHardLine(fullText, start, out var endExclusive, out var nextStart))
            {
                break;
            }

            if (lineIndex == localY)
            {
                var line = fullText.Slice(start, Math.Max(0, endExclusive - start));
                return start + GetSingleLineSliceIndexFromPosition(line, localX, availableWidth);
            }

            if (nextStart == start)
            {
                break;
            }

            start = nextStart;
            lineIndex++;
        }

        return fullText.Length;
    }

    private int GetSingleLineSliceIndexFromPosition(ReadOnlySpan<char> text, int localX, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return 0;
        }

        var alignment = TextAlignment;
        var trimming = Trimming;
        if (alignment == TextAlignment.Justify)
        {
            alignment = TextAlignment.Left;
        }

        if (trimming == TextTrimming.Clip)
        {
            var clipEndIndex = GetEndIndexAtCell(text, maxWidth);
            var cells = TerminalTextUtility.GetWidth(text[..clipEndIndex]);
            var x = AlignXLocal(alignment, maxWidth, cells);
            return GetIndexAtCell(text[..clipEndIndex], localX - x);
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignXLocal(alignment, maxWidth, fullWidth);
            return GetIndexAtCell(text, localX - x);
        }

        if (maxWidth == 1)
        {
            return text.Length;
        }

        if (trimming == TextTrimming.EndEllipsis)
        {
            var bodyWidth = maxWidth - 1;
            var bodyEndIndex = GetEndIndexAtCell(text, bodyWidth);
            var bodyCells = TerminalTextUtility.GetWidth(text[..bodyEndIndex]);
            var contentWidth = Math.Min(maxWidth, bodyCells + 1);
            var x = AlignXLocal(alignment, maxWidth, contentWidth);
            return GetIndexAtCell(text[..bodyEndIndex], localX - x);
        }

        // StartEllipsis
        var suffixWidth = maxWidth - 1;
        var suffixStart = GetStartIndexForSuffix(text, suffixWidth);
        var suffix = text[suffixStart..];
        var suffixCells = TerminalTextUtility.GetWidth(suffix);
        var contentW = Math.Min(maxWidth, 1 + suffixCells);
        var x0 = AlignXLocal(alignment, maxWidth, contentW);
        var inside = localX - x0 - 1;
        if (inside <= 0)
        {
            return suffixStart;
        }

        if (inside >= suffixWidth)
        {
            return text.Length;
        }

        return suffixStart + GetIndexAtCell(suffix, inside);
    }

    private int GetWrapSliceTextIndexFromPosition(ReadOnlySpan<char> slice, int localX, int width)
    {
        var end = GetEndIndexAtCell(slice, width);
        var clipped = slice[..end];
        var cells = TerminalTextUtility.GetWidth(clipped);
        var alignment = TextAlignment;
        if (alignment == TextAlignment.Justify)
        {
            alignment = TextAlignment.Left;
        }
        var x = AlignXLocal(alignment, width, cells);
        return GetIndexAtCell(clipped, localX - x);
    }

    private static int GetIndexAtCell(ReadOnlySpan<char> text, int cellOffset)
    {
        if (cellOffset <= 0 || text.IsEmpty)
        {
            return 0;
        }

        var total = TerminalTextUtility.GetWidth(text);
        if (cellOffset >= total)
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

    private static (int Start, int End) GetWordSelection(ReadOnlySpan<char> text, int index)
    {
        index = Math.Clamp(index, 0, text.Length);
        var start = TerminalTextUtility.GetWordStart(text, index);
        var end = TerminalTextUtility.GetWordEnd(text, index);
        return (start, end);
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

    private static int GetEndIndexAtCell(ReadOnlySpan<char> text, int maxCells)
    {
        if (maxCells <= 0 || text.IsEmpty)
        {
            return 0;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text, maxCells, out var endIndex))
        {
            endIndex = text.Length;
        }

        return Math.Clamp(endIndex, 0, text.Length);
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

    private static int CountWrappedLines(ReadOnlySpan<char> text, int width)
    {
        if (width <= 0)
        {
            return 1;
        }

        var lines = 0;
        var hardStart = 0;
        while (hardStart <= text.Length)
        {
            if (!TryGetNextHardLine(text, hardStart, out var hardEnd, out var hardNext))
            {
                break;
            }

            var hardLine = text.Slice(hardStart, Math.Max(0, hardEnd - hardStart));
            if (hardLine.IsEmpty || IsAllWhitespace(hardLine))
            {
                lines++;
            }
            else
            {
                var relStart = 0;
                var any = false;
                while (relStart < hardLine.Length)
                {
                    if (!TryGetNextWrapSlice(hardLine, relStart, width, out _, out var relNext))
                    {
                        break;
                    }

                    any = true;
                    lines++;
                    relStart = relNext;
                }

                if (!any)
                {
                    lines++;
                }
            }

            if (hardNext == hardStart)
            {
                break;
            }

            hardStart = hardNext;
        }

        return Math.Max(1, lines);
    }

    private static int GetMaxLineWidth(ReadOnlySpan<char> text)
    {
        var maxWidth = 0;
        var start = 0;
        while (start <= text.Length)
        {
            if (!TryGetNextHardLine(text, start, out var end, out var next))
            {
                break;
            }

            var line = text.Slice(start, Math.Max(0, end - start));
            maxWidth = Math.Max(maxWidth, TerminalTextUtility.GetWidth(line));

            if (next == start)
            {
                break;
            }

            start = next;
        }

        return maxWidth;
    }

    private static int CountHardLines(ReadOnlySpan<char> text)
    {
        var count = 1;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\n')
            {
                count++;
                continue;
            }

            if (ch == '\r')
            {
                count++;
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
        }

        return count;
    }

    private static bool TryGetNextHardLine(ReadOnlySpan<char> text, int start, out int endExclusive, out int nextStart)
    {
        endExclusive = start;
        nextStart = start;

        if (start > text.Length)
        {
            return false;
        }

        if (start == text.Length)
        {
            if (text.IsEmpty)
            {
                return true;
            }

            var last = text[^1];
            return last is '\n' or '\r';
        }

        var i = start;
        while (i < text.Length)
        {
            var ch = text[i];
            if (ch == '\n' || ch == '\r')
            {
                break;
            }

            i++;
        }

        endExclusive = i;
        nextStart = i;

        if (i < text.Length)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                nextStart = i + 2;
            }
            else
            {
                nextStart = i + 1;
            }
        }

        return true;
    }

    private static bool IsAllWhitespace(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch != ' ' && ch != '\t')
            {
                return false;
            }
        }
        return true;
    }

    private static bool TryGetNextWrapSlice(ReadOnlySpan<char> text, int start, int width, out int endExclusive, out int nextStart)
    {
        endExclusive = start;
        nextStart = start;

        if (start >= text.Length)
        {
            return false;
        }

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
