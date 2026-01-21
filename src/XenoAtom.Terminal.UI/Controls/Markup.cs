// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a control that renders ANSI markup into styled text.
/// </summary>
public sealed partial class Markup : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);

    private readonly MarkupTextParser _parser;

    private string? _cachedMarkup;
    private string _plainText = string.Empty;
    private StyledRun[] _runs = Array.Empty<StyledRun>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Markup"/> class.
    /// </summary>
    public Markup()
    {
        _parser = new MarkupTextParser();
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

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        EnsureParsed();

        var text = _plainText.AsSpan();
        var availableWidth = Math.Max(0, availableSize.Width);
        var availableHeight = Math.Max(0, availableSize.Height);

        var width = Math.Max(0, Math.Min(availableWidth, GetMaxLineWidth(text)));
        if (!Wrap)
        {
            var height = Math.Min(availableHeight, CountHardLines(text));
            return SizeHints.Fixed(new Size(width, height));
        }

        if (width == 0)
        {
            return SizeHints.Fixed(new Size(0, Math.Min(availableHeight, 1)));
        }

        var wrappedHeight = CountWrappedLines(text, Math.Max(1, width));
        return SizeHints.Fixed(new Size(width, Math.Min(availableHeight, Math.Max(1, wrappedHeight))));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        EnsureParsed();

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

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
                WriteSingleLine(buffer, rect, y, start, hardEnd);
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
                        WriteAlignedLine(buffer, rect, y, start + rel, start + relEnd, isLastLine);
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

    private void WriteSingleLine(CellBuffer buffer, Rectangle rect, int y, int lineStartIndex, int lineEndIndex)
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
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + clipEndIndex);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + text.Length);
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
            WriteStyledSpan(buffer, x, y, lineStartIndex, lineStartIndex + bodyEndIndex);
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
        WriteStyledSpan(buffer, x0 + 1, y, lineStartIndex + suffixStart, lineStartIndex + text.Length);
    }

    private void WriteAlignedLine(CellBuffer buffer, Rectangle rect, int y, int startIndex, int endExclusive, bool isLastLine)
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
        WriteStyledSpan(buffer, x, y, startIndex, startIndex + end);
    }

    private void EnsureParsed()
    {
        var text = Text ?? string.Empty;
        if (ReferenceEquals(_cachedMarkup, text))
        {
            return;
        }

        _cachedMarkup = text;
        _plainText = _parser.Parse(text, out _runs);
    }

    private void WriteStyledSpan(CellBuffer buffer, int x, int y, int startIndex, int endIndex)
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
            buffer.WriteText(posX, y, slice, run.Style);
            posX += TerminalTextUtility.GetWidth(slice);
        }
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
