// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a display-only paragraph control supporting style runs and hyperlink runs.
/// </summary>
public sealed partial class Paragraph : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);

    private LayoutLine[] _layoutLines = Array.Empty<LayoutLine>();
    private int _layoutLineCount;
    private int _layoutWidth = -1;
    private string? _layoutText;
    private int _layoutIndent;
    private int _layoutHangingIndent;
    private string? _layoutLinePrefix;
    private string? _layoutContinuationPrefix;
    private bool _layoutCacheValid;

    private StyledRun[] _normalizedRuns = Array.Empty<StyledRun>();
    private HyperlinkRun[] _normalizedHyperlinks = Array.Empty<HyperlinkRun>();
    private bool _spansDirty = true;
    private int _spansTextLength = -1;
    private Dictionary<string, ulong>? _hyperlinkTokenCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class.
    /// </summary>
    public Paragraph()
    {
        Wrap = true;
        TextAlignment = TextAlignment.Left;
        Trimming = TextTrimming.Clip;
        Runs = Array.Empty<StyledRun>();
        Hyperlinks = Array.Empty<HyperlinkRun>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class with text.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    public Paragraph(string text) : this()
    {
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class with a dynamic text provider.
    /// </summary>
    /// <param name="text">The text provider.</param>
    public Paragraph(Func<string> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Paragraph"/> class with a bound text provider.
    /// </summary>
    /// <param name="text">The text binding.</param>
    public Paragraph(Binding<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Gets or sets the text to render.
    /// </summary>
    [Bindable]
    public partial string? Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether text wraps to the available width.
    /// </summary>
    [Bindable]
    public partial bool Wrap { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of rendered text.
    /// </summary>
    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the trimming mode applied when <see cref="Wrap"/> is <see langword="false"/>.
    /// </summary>
    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    /// <summary>
    /// Gets or sets style runs over <see cref="Text"/>.
    /// </summary>
    [Bindable]
    public partial StyledRun[] Runs { get; set; }

    /// <summary>
    /// Gets or sets hyperlink runs over <see cref="Text"/>.
    /// </summary>
    [Bindable]
    public partial HyperlinkRun[] Hyperlinks { get; set; }

    /// <summary>
    /// Gets or sets the indentation (in cells) applied to the first line.
    /// </summary>
    [Bindable]
    public partial int Indent { get; set; }

    /// <summary>
    /// Gets or sets the extra indentation (in cells) applied to wrapped continuation lines.
    /// </summary>
    [Bindable]
    public partial int HangingIndent { get; set; }

    /// <summary>
    /// Gets or sets the prefix rendered before the first physical line.
    /// </summary>
    [Bindable]
    public partial string? LinePrefix { get; set; }

    /// <summary>
    /// Gets or sets the prefix rendered before continuation lines.
    /// </summary>
    [Bindable]
    public partial string? ContinuationPrefix { get; set; }

    /// <summary>
    /// Gets or sets the style used for line prefixes.
    /// </summary>
    [Bindable]
    public partial Style PrefixStyle { get; set; }

    partial void OnTextChanged(string? value)
    {
        _ = value;
        InvalidateLayoutCache();
        InvalidateSpanCache();
    }

    partial void OnWrapChanged(bool value)
    {
        _ = value;
        InvalidateLayoutCache();
    }

    partial void OnIndentChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnIndentChanged(int value)
    {
        _ = value;
        InvalidateLayoutCache();
    }

    partial void OnHangingIndentChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnHangingIndentChanged(int value)
    {
        _ = value;
        InvalidateLayoutCache();
    }

    partial void OnLinePrefixChanged(string? value)
    {
        _ = value;
        InvalidateLayoutCache();
    }

    partial void OnContinuationPrefixChanged(string? value)
    {
        _ = value;
        InvalidateLayoutCache();
    }

    partial void OnRunsChanging(ref StyledRun[] value)
    {
        value ??= Array.Empty<StyledRun>();
    }

    partial void OnRunsChanged(StyledRun[] value)
    {
        _ = value;
        InvalidateSpanCache();
    }

    partial void OnHyperlinksChanging(ref HyperlinkRun[] value)
    {
        value ??= Array.Empty<HyperlinkRun>();
    }

    partial void OnHyperlinksChanged(HyperlinkRun[] value)
    {
        _ = value;
        InvalidateSpanCache();
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var maxWidth = Math.Max(0, constraints.MaxWidth);
        var maxHeight = Math.Max(0, constraints.MaxHeight);
        var text = Text ?? string.Empty;
        var span = text.AsSpan();

        if (!Wrap)
        {
            var naturalWidth = GetSingleLineNaturalWidth(span);
            var width = Math.Max(0, Math.Min(maxWidth, naturalWidth));
            return SizeHints.Fixed(new Size(width, Math.Min(maxHeight, 1)));
        }

        var naturalParagraphWidth = GetParagraphNaturalWidth(span);
        var wrappedWidth = Math.Max(0, Math.Min(maxWidth, naturalParagraphWidth));
        if (wrappedWidth <= 0)
        {
            return SizeHints.Fixed(new Size(0, Math.Min(maxHeight, 1)));
        }

        EnsureWrappedLayout(wrappedWidth, text);
        var wrappedHeight = Math.Max(1, _layoutLineCount);
        return SizeHints.Fixed(new Size(wrappedWidth, Math.Min(maxHeight, wrappedHeight)));
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var text = Text ?? string.Empty;
        EnsureSpanCache(text.Length);

        _hyperlinkTokenCache?.Clear();

        if (!Wrap)
        {
            RenderSingleLine(buffer, rect, text);
            return;
        }

        EnsureWrappedLayout(rect.Width, text);
        var lineCount = Math.Min(rect.Height, _layoutLineCount);
        var textSpan = text.AsSpan();

        for (var index = 0; index < lineCount; index++)
        {
            var line = _layoutLines[index];
            var y = rect.Y + index;
            WritePrefix(buffer, rect, line, y);

            var textX = rect.X + line.Indent + line.PrefixWidth;
            var textWidth = Math.Max(0, rect.Width - line.Indent - line.PrefixWidth);
            if (textWidth <= 0 || line.Length <= 0)
            {
                continue;
            }

            var alignment = TextAlignment == TextAlignment.Justify ? TextAlignment.Left : TextAlignment;
            var renderedWidth = Math.Min(textWidth, line.TextWidth);
            var alignedX = AlignX(textX, textWidth, renderedWidth, alignment);

            WriteStyledSpan(buffer, alignedX, y, textSpan, line.Start, line.Start + line.Length);
        }
    }

    private void RenderSingleLine(CellBuffer buffer, in Rectangle rect, string text)
    {
        var firstLine = GetFirstHardLine(text.AsSpan());
        var line = new LayoutLine(
            Start: 0,
            Length: firstLine.Length,
            TextWidth: firstLine.Length == 0 ? 0 : TerminalTextUtility.GetWidth(firstLine),
            Indent: GetIndent(isFirstLine: true),
            PrefixWidth: GetPrefixWidth(isFirstLine: true),
            IsFirstLine: true,
            IsLastInHardLine: true);

        WritePrefix(buffer, rect, line, rect.Y);

        var textX = rect.X + line.Indent + line.PrefixWidth;
        var availableTextWidth = Math.Max(0, rect.Width - line.Indent - line.PrefixWidth);
        if (availableTextWidth <= 0 || firstLine.Length == 0)
        {
            return;
        }

        var textStart = 0;
        var textEnd = firstLine.Length;
        var alignment = TextAlignment == TextAlignment.Justify ? TextAlignment.Left : TextAlignment;
        var span = text.AsSpan();

        var trimming = Trimming;
        if (trimming == TextTrimming.Clip)
        {
            var visibleLength = GetEndIndexAtCell(firstLine, availableTextWidth);
            var visibleEnd = textStart + visibleLength;
            var visibleWidth = TerminalTextUtility.GetWidth(firstLine[..visibleLength]);
            var alignedX = AlignX(textX, availableTextWidth, visibleWidth, alignment);
            WriteStyledSpan(buffer, alignedX, rect.Y, span, textStart, visibleEnd);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(firstLine);
        if (fullWidth <= availableTextWidth)
        {
            var alignedX = AlignX(textX, availableTextWidth, fullWidth, alignment);
            WriteStyledSpan(buffer, alignedX, rect.Y, span, textStart, textEnd);
            return;
        }

        if (availableTextWidth == 1)
        {
            buffer.SetCell(textX, rect.Y, Ellipsis, Style.None);
            return;
        }

        var bodyWidth = availableTextWidth - 1;
        var fullAlignedX = AlignX(textX, availableTextWidth, availableTextWidth, alignment);

        if (trimming == TextTrimming.EndEllipsis)
        {
            var visibleLength = GetEndIndexAtCell(firstLine, bodyWidth);
            var visibleEnd = textStart + visibleLength;
            WriteStyledSpan(buffer, fullAlignedX, rect.Y, span, textStart, visibleEnd);
            buffer.SetCell(fullAlignedX + bodyWidth, rect.Y, Ellipsis, Style.None);
            return;
        }

        var suffixStart = textStart + GetStartIndexForSuffix(firstLine, bodyWidth);
        buffer.SetCell(fullAlignedX, rect.Y, Ellipsis, Style.None);
        WriteStyledSpan(buffer, fullAlignedX + 1, rect.Y, span, suffixStart, textEnd);
    }

    private void WriteStyledSpan(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, int start, int endExclusive)
    {
        if (start >= endExclusive)
        {
            return;
        }

        var runs = _normalizedRuns;
        var hyperlinks = _normalizedHyperlinks;

        if (runs.Length == 0 && hyperlinks.Length == 0)
        {
            buffer.WriteText(x, y, text.Slice(start, endExclusive - start), Style.None);
            return;
        }

        var runIndex = FindFirstPotentialRun(runs, start);
        var hyperlinkIndex = FindFirstPotentialHyperlink(hyperlinks, start);
        var position = start;
        var posX = x;

        while (position < endExclusive && posX < buffer.Width)
        {
            var nextBoundary = endExclusive;

            var runStyle = Style.None;
            if (TryGetRunAt(runs, ref runIndex, position, out var currentRun))
            {
                runStyle = currentRun.Style;
                nextBoundary = Math.Min(nextBoundary, currentRun.Start + currentRun.Length);
            }
            else if ((uint)runIndex < (uint)runs.Length)
            {
                nextBoundary = Math.Min(nextBoundary, runs[runIndex].Start);
            }

            ulong hyperlinkToken = 0;
            if (TryGetHyperlinkAt(hyperlinks, ref hyperlinkIndex, position, out var currentHyperlink))
            {
                hyperlinkToken = GetHyperlinkToken(buffer, currentHyperlink.Uri);
                nextBoundary = Math.Min(nextBoundary, currentHyperlink.Start + currentHyperlink.Length);
            }
            else if ((uint)hyperlinkIndex < (uint)hyperlinks.Length)
            {
                nextBoundary = Math.Min(nextBoundary, hyperlinks[hyperlinkIndex].Start);
            }

            if (nextBoundary <= position)
            {
                nextBoundary = Math.Min(endExclusive, position + 1);
            }

            var segment = text.Slice(position, nextBoundary - position);
            buffer.WriteText(posX, y, segment, runStyle, hyperlinkToken);
            posX += TerminalTextUtility.GetWidth(segment);
            position = nextBoundary;
        }
    }

    private ulong GetHyperlinkToken(CellBuffer buffer, string uri)
    {
        if (uri.Length == 0)
        {
            return 0;
        }

        _hyperlinkTokenCache ??= new Dictionary<string, ulong>(StringComparer.Ordinal);
        if (_hyperlinkTokenCache.TryGetValue(uri, out var token))
        {
            return token;
        }

        token = buffer.RegisterHyperlink(uri);
        _hyperlinkTokenCache[uri] = token;
        return token;
    }

    private void WritePrefix(CellBuffer buffer, in Rectangle rect, in LayoutLine line, int y)
    {
        var prefix = line.IsFirstLine ? LinePrefix : ContinuationPrefix;
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        var x = rect.X + line.Indent;
        if (x >= rect.Right)
        {
            return;
        }

        buffer.WriteText(x, y, prefix.AsSpan(), PrefixStyle);
    }

    private void EnsureWrappedLayout(int width, string text)
    {
        width = Math.Max(0, width);

        if (_layoutCacheValid &&
            _layoutWidth == width &&
            _layoutIndent == Indent &&
            _layoutHangingIndent == HangingIndent &&
            string.Equals(_layoutText, text, StringComparison.Ordinal) &&
            string.Equals(_layoutLinePrefix, LinePrefix, StringComparison.Ordinal) &&
            string.Equals(_layoutContinuationPrefix, ContinuationPrefix, StringComparison.Ordinal))
        {
            return;
        }

        _layoutWidth = width;
        _layoutText = text;
        _layoutIndent = Indent;
        _layoutHangingIndent = HangingIndent;
        _layoutLinePrefix = LinePrefix;
        _layoutContinuationPrefix = ContinuationPrefix;
        _layoutLineCount = 0;

        var span = text.AsSpan();
        if (span.Length == 0)
        {
            AppendLayoutLine(0, 0, isFirstLine: true, isLastInHardLine: true, width);
            _layoutCacheValid = true;
            return;
        }

        var firstPhysicalLine = true;
        var hardLineStart = 0;
        while (hardLineStart <= span.Length)
        {
            if (!TryGetNextHardLine(span, hardLineStart, out var hardLineEnd, out var nextHardLineStart))
            {
                break;
            }

            var hardLine = span.Slice(hardLineStart, Math.Max(0, hardLineEnd - hardLineStart));
            if (hardLine.Length == 0)
            {
                AppendLayoutLine(hardLineStart, 0, firstPhysicalLine, isLastInHardLine: true, width);
                firstPhysicalLine = false;
            }
            else
            {
                var localStart = 0;
                var producedAny = false;

                while (localStart < hardLine.Length)
                {
                    var availableTextWidth = GetTextAvailableWidth(width, firstPhysicalLine);
                    if (availableTextWidth <= 0)
                    {
                        AppendLayoutLine(hardLineStart + localStart, 0, firstPhysicalLine, isLastInHardLine: true, width);
                        firstPhysicalLine = false;
                        producedAny = true;
                        break;
                    }

                    if (!TryGetNextWrapSlice(hardLine, localStart, availableTextWidth, out var localEnd, out var localNext))
                    {
                        var fallbackLength = GetEndIndexAtCell(hardLine[localStart..], availableTextWidth);
                        AppendLayoutLine(hardLineStart + localStart, fallbackLength, firstPhysicalLine, isLastInHardLine: true, width);
                        firstPhysicalLine = false;
                        producedAny = true;
                        break;
                    }

                    var sliceLength = Math.Max(0, localEnd - localStart);
                    var isLastInHardLine = localNext >= hardLine.Length;
                    AppendLayoutLine(hardLineStart + localStart, sliceLength, firstPhysicalLine, isLastInHardLine, width);
                    firstPhysicalLine = false;
                    producedAny = true;

                    if (localNext <= localStart)
                    {
                        break;
                    }

                    localStart = localNext;
                }

                if (!producedAny)
                {
                    AppendLayoutLine(hardLineStart, 0, firstPhysicalLine, isLastInHardLine: true, width);
                    firstPhysicalLine = false;
                }
            }

            if (nextHardLineStart == hardLineStart)
            {
                break;
            }

            hardLineStart = nextHardLineStart;
        }

        if (_layoutLineCount == 0)
        {
            AppendLayoutLine(0, 0, isFirstLine: true, isLastInHardLine: true, width);
        }

        _layoutCacheValid = true;
    }

    private void AppendLayoutLine(int start, int length, bool isFirstLine, bool isLastInHardLine, int width)
    {
        EnsureLayoutLineCapacity(_layoutLineCount + 1);

        var text = Text ?? string.Empty;
        var clampedStart = Math.Clamp(start, 0, text.Length);
        var clampedLength = Math.Max(0, Math.Min(length, text.Length - clampedStart));

        var indent = GetIndent(isFirstLine);
        var prefixWidth = GetPrefixWidth(isFirstLine);
        var availableTextWidth = Math.Max(0, width - indent - prefixWidth);
        if (availableTextWidth <= 0)
        {
            clampedLength = 0;
        }

        var textWidth = clampedLength == 0
            ? 0
            : TerminalTextUtility.GetWidth(text.AsSpan(clampedStart, clampedLength));

        _layoutLines[_layoutLineCount++] = new LayoutLine(
            Start: clampedStart,
            Length: clampedLength,
            TextWidth: textWidth,
            Indent: indent,
            PrefixWidth: prefixWidth,
            IsFirstLine: isFirstLine,
            IsLastInHardLine: isLastInHardLine);
    }

    private static bool TryGetNextHardLine(ReadOnlySpan<char> text, int start, out int endExclusive, out int nextStart)
    {
        endExclusive = start;
        nextStart = start;

        if (start > text.Length)
        {
            return false;
        }

        var index = start;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '\n' || ch == '\r')
            {
                break;
            }

            index++;
        }

        endExclusive = index;
        nextStart = index;
        if (index < text.Length)
        {
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                nextStart = index + 2;
            }
            else
            {
                nextStart = index + 1;
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

        if (!TerminalTextUtility.TryGetIndexAtCell(text[start..], width, out var relativeEnd))
        {
            relativeEnd = text.Length - start;
        }

        var tentativeEnd = Math.Clamp(start + relativeEnd, start, text.Length);
        var wrapEnd = tentativeEnd;
        if (tentativeEnd < text.Length)
        {
            var lastSpace = -1;
            for (var index = tentativeEnd - 1; index > start; index--)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    lastSpace = index;
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

    private int GetSingleLineNaturalWidth(ReadOnlySpan<char> text)
    {
        var firstLine = GetFirstHardLine(text);
        var lineWidth = firstLine.Length == 0 ? 0 : TerminalTextUtility.GetWidth(firstLine);
        var inset = GetIndent(isFirstLine: true) + GetPrefixWidth(isFirstLine: true);
        return Math.Max(0, inset + lineWidth);
    }

    private int GetParagraphNaturalWidth(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return Math.Max(0, GetIndent(isFirstLine: true) + GetPrefixWidth(isFirstLine: true));
        }

        var maxWidth = 0;
        var isFirstLine = true;
        var hardLineStart = 0;
        while (hardLineStart <= text.Length)
        {
            if (!TryGetNextHardLine(text, hardLineStart, out var hardLineEnd, out var nextHardLineStart))
            {
                break;
            }

            var hardLine = text.Slice(hardLineStart, Math.Max(0, hardLineEnd - hardLineStart));
            var lineWidth = hardLine.Length == 0 ? 0 : TerminalTextUtility.GetWidth(hardLine);
            var inset = GetIndent(isFirstLine) + GetPrefixWidth(isFirstLine);
            maxWidth = Math.Max(maxWidth, inset + lineWidth);
            isFirstLine = false;

            if (nextHardLineStart == hardLineStart)
            {
                break;
            }

            hardLineStart = nextHardLineStart;
        }

        return Math.Max(0, maxWidth);
    }

    private static ReadOnlySpan<char> GetFirstHardLine(ReadOnlySpan<char> text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (ch == '\n' || ch == '\r')
            {
                return text[..index];
            }
        }

        return text;
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
            var previous = TerminalTextUtility.GetPreviousTextElementIndex(text, index);
            var elementWidth = TerminalTextUtility.GetWidth(text.Slice(previous, index - previous));
            if (width + elementWidth > maxCells)
            {
                break;
            }

            width += elementWidth;
            index = previous;
        }

        return index;
    }

    private static int AlignX(int x, int availableWidth, int contentWidth, TextAlignment alignment)
    {
        if (availableWidth <= contentWidth)
        {
            return x;
        }

        return alignment switch
        {
            TextAlignment.Center => x + ((availableWidth - contentWidth) / 2),
            TextAlignment.Right => x + (availableWidth - contentWidth),
            _ => x,
        };
    }

    private int GetTextAvailableWidth(int totalWidth, bool isFirstLine)
    {
        return Math.Max(0, totalWidth - GetIndent(isFirstLine) - GetPrefixWidth(isFirstLine));
    }

    private int GetIndent(bool isFirstLine)
    {
        if (isFirstLine)
        {
            return Math.Max(0, Indent);
        }

        return Math.Max(0, Indent + HangingIndent);
    }

    private int GetPrefixWidth(bool isFirstLine)
    {
        var prefix = isFirstLine ? LinePrefix : ContinuationPrefix;
        return string.IsNullOrEmpty(prefix) ? 0 : TerminalTextUtility.GetWidth(prefix.AsSpan());
    }

    private void EnsureLayoutLineCapacity(int size)
    {
        if (_layoutLines.Length >= size)
        {
            return;
        }

        var next = _layoutLines.Length == 0 ? 8 : _layoutLines.Length * 2;
        while (next < size)
        {
            next *= 2;
        }

        Array.Resize(ref _layoutLines, next);
    }

    private void EnsureSpanCache(int textLength)
    {
        if (!_spansDirty && _spansTextLength == textLength)
        {
            return;
        }

        _normalizedRuns = NormalizeRuns(Runs, textLength);
        _normalizedHyperlinks = NormalizeHyperlinks(Hyperlinks, textLength);
        _spansTextLength = textLength;
        _spansDirty = false;
    }

    private static StyledRun[] NormalizeRuns(StyledRun[]? runs, int textLength)
    {
        if (runs is null || runs.Length == 0 || textLength <= 0)
        {
            return Array.Empty<StyledRun>();
        }

        var alreadyNormalized = true;
        var previousStart = -1;
        for (var index = 0; index < runs.Length; index++)
        {
            var run = runs[index];
            if (run.Length <= 0 || run.Start < 0 || run.Start >= textLength || run.Start + run.Length > textLength || run.Start < previousStart)
            {
                alreadyNormalized = false;
                break;
            }

            previousStart = run.Start;
        }

        if (alreadyNormalized)
        {
            return runs;
        }

        var normalized = new StyledRun[runs.Length];
        var count = 0;
        for (var index = 0; index < runs.Length; index++)
        {
            var run = runs[index];
            var start = Math.Clamp(run.Start, 0, textLength);
            var maxLength = textLength - start;
            var length = Math.Clamp(run.Length, 0, maxLength);
            if (length <= 0)
            {
                continue;
            }

            normalized[count++] = new StyledRun(start, length, run.Style);
        }

        if (count == 0)
        {
            return Array.Empty<StyledRun>();
        }

        Array.Sort(normalized, 0, count, StyledRunStartComparer.Instance);
        if (count == normalized.Length)
        {
            return normalized;
        }

        var trimmed = new StyledRun[count];
        Array.Copy(normalized, trimmed, count);
        return trimmed;
    }

    private static HyperlinkRun[] NormalizeHyperlinks(HyperlinkRun[]? hyperlinks, int textLength)
    {
        if (hyperlinks is null || hyperlinks.Length == 0 || textLength <= 0)
        {
            return Array.Empty<HyperlinkRun>();
        }

        var alreadyNormalized = true;
        var previousStart = -1;
        for (var index = 0; index < hyperlinks.Length; index++)
        {
            var run = hyperlinks[index];
            if (run.Length <= 0 || run.Start < 0 || run.Start >= textLength || run.Start + run.Length > textLength || run.Start < previousStart || string.IsNullOrEmpty(run.Uri))
            {
                alreadyNormalized = false;
                break;
            }

            previousStart = run.Start;
        }

        if (alreadyNormalized)
        {
            return hyperlinks;
        }

        var normalized = new HyperlinkRun[hyperlinks.Length];
        var count = 0;
        for (var index = 0; index < hyperlinks.Length; index++)
        {
            var run = hyperlinks[index];
            if (string.IsNullOrEmpty(run.Uri))
            {
                continue;
            }

            var start = Math.Clamp(run.Start, 0, textLength);
            var maxLength = textLength - start;
            var length = Math.Clamp(run.Length, 0, maxLength);
            if (length <= 0)
            {
                continue;
            }

            normalized[count++] = new HyperlinkRun(start, length, run.Uri);
        }

        if (count == 0)
        {
            return Array.Empty<HyperlinkRun>();
        }

        Array.Sort(normalized, 0, count, HyperlinkRunStartComparer.Instance);
        if (count == normalized.Length)
        {
            return normalized;
        }

        var trimmed = new HyperlinkRun[count];
        Array.Copy(normalized, trimmed, count);
        return trimmed;
    }

    private static int FindFirstPotentialRun(StyledRun[] runs, int index)
    {
        for (var runIndex = 0; runIndex < runs.Length; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Start + run.Length > index)
            {
                return runIndex;
            }
        }

        return runs.Length;
    }

    private static int FindFirstPotentialHyperlink(HyperlinkRun[] hyperlinks, int index)
    {
        for (var hyperlinkIndex = 0; hyperlinkIndex < hyperlinks.Length; hyperlinkIndex++)
        {
            var hyperlink = hyperlinks[hyperlinkIndex];
            if (hyperlink.Start + hyperlink.Length > index)
            {
                return hyperlinkIndex;
            }
        }

        return hyperlinks.Length;
    }

    private static bool TryGetRunAt(StyledRun[] runs, ref int index, int textIndex, out StyledRun run)
    {
        while ((uint)index < (uint)runs.Length)
        {
            var current = runs[index];
            var currentEnd = current.Start + current.Length;
            if (currentEnd <= textIndex)
            {
                index++;
                continue;
            }

            if (current.Start > textIndex)
            {
                run = default;
                return false;
            }

            run = current;
            return true;
        }

        run = default;
        return false;
    }

    private static bool TryGetHyperlinkAt(HyperlinkRun[] hyperlinks, ref int index, int textIndex, out HyperlinkRun run)
    {
        while ((uint)index < (uint)hyperlinks.Length)
        {
            var current = hyperlinks[index];
            var currentEnd = current.Start + current.Length;
            if (currentEnd <= textIndex)
            {
                index++;
                continue;
            }

            if (current.Start > textIndex)
            {
                run = default;
                return false;
            }

            run = current;
            return true;
        }

        run = default;
        return false;
    }

    private void InvalidateLayoutCache()
    {
        _layoutCacheValid = false;
        _layoutLineCount = 0;
    }

    private void InvalidateSpanCache()
    {
        _spansDirty = true;
        _spansTextLength = -1;
    }

    private readonly record struct LayoutLine(
        int Start,
        int Length,
        int TextWidth,
        int Indent,
        int PrefixWidth,
        bool IsFirstLine,
        bool IsLastInHardLine);

    private sealed class StyledRunStartComparer : IComparer<StyledRun>
    {
        public static readonly StyledRunStartComparer Instance = new();

        public int Compare(StyledRun x, StyledRun y) => x.Start.CompareTo(y.Start);
    }

    private sealed class HyperlinkRunStartComparer : IComparer<HyperlinkRun>
    {
        public static readonly HyperlinkRunStartComparer Instance = new();

        public int Compare(HyperlinkRun x, HyperlinkRun y) => x.Start.CompareTo(y.Start);
    }
}
