// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TextBlock : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);

    public TextBlock()
    {
    }

    public TextBlock(string text)
    {
        Text = text;
    }

    [Bindable]
    public partial string? Text { get; set; }

    [Bindable]
    public partial bool Wrap { get; set; }

    [Bindable]
    public partial TextAlignment TextAlignment { get; set; }

    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text ?? string.Empty;
        var width = Wrap ? availableSize.Width : Math.Min(availableSize.Width, TerminalTextUtility.GetWidth(text.AsSpan()));
        width = Math.Max(0, width);

        if (!Wrap || width == 0)
        {
            return new Size(width, 1);
        }

        var height = CountWrappedLines(text.AsSpan(), Math.Max(1, width));
        return new Size(width, Math.Min(availableSize.Height, Math.Max(1, height)));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var text = Text ?? string.Empty;
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (!Wrap || rect.Height == 1)
        {
            WriteSingleLine(buffer, rect, text.AsSpan(), CellStyle.None);
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
            WriteAlignedLine(buffer, rect, rect.Y + lineIndex, slice, CellStyle.None, isLastLine: nextStart >= span.Length);
            lineIndex++;
            start = nextStart;
        }
    }

    private void WriteSingleLine(CellBuffer buffer, Rectangle rect, ReadOnlySpan<char> text, CellStyle style)
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
            buffer.WriteText(x, rect.Y, span, style);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            buffer.WriteText(x, rect.Y, text, style);
            return;
        }

        if (maxWidth == 1)
        {
            buffer.SetCell(rect.X, rect.Y, Ellipsis, style);
            return;
        }

        var bodyWidth = maxWidth - 1;
        if (trimming == TextTrimming.EndEllipsis)
        {
            var span = Clip(text, bodyWidth);
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            buffer.WriteText(x, rect.Y, span, style);
            buffer.SetCell(x + bodyWidth, rect.Y, Ellipsis, style);
            return;
        }

        if (trimming == TextTrimming.StartEllipsis)
        {
            var startIndex = GetStartIndexForSuffix(text, bodyWidth);
            var suffix = text[startIndex..];
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            buffer.SetCell(x, rect.Y, Ellipsis, style);
            buffer.WriteText(x + 1, rect.Y, suffix, style);
            return;
        }

        var clipped = Clip(text, maxWidth);
        var clippedWidth = TerminalTextUtility.GetWidth(clipped);
        var defaultX = AlignX(rect, alignment, maxWidth, clippedWidth);
        buffer.WriteText(defaultX, rect.Y, clipped, style);
    }

    private void WriteAlignedLine(CellBuffer buffer, Rectangle rect, int y, ReadOnlySpan<char> text, CellStyle style, bool isLastLine)
    {
        var width = rect.Width;
        if (width <= 0)
        {
            return;
        }

        var alignment = TextAlignment;
        if (alignment == TextAlignment.Justify && !isLastLine)
        {
            if (TryWriteJustified(buffer, rect.X, y, width, text, style))
            {
                return;
            }
        }

        var clipped = Clip(text, width);
        var cells = TerminalTextUtility.GetWidth(clipped);
        var x = AlignX(rect, alignment, width, cells);
        buffer.WriteText(x, y, clipped, style);
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
            var prev = TerminalTextUtility.GetPreviousRuneIndex(text, index);
            if (Rune.DecodeFromUtf16(text[prev..], out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
            {
                rune = Rune.ReplacementChar;
            }

            var w = TerminalTextUtility.GetRuneWidth(rune);
            if (width + w > maxCells)
            {
                break;
            }

            width += w;
            index = prev;
        }

        return index;
    }

    private static bool TryWriteJustified(CellBuffer buffer, int x, int y, int width, ReadOnlySpan<char> text, CellStyle style)
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
            buffer.WriteText(posX, y, slice, style);
            posX += TerminalTextUtility.GetWidth(slice);

            if (w + 1 < wordCount)
            {
                var spaces = 1 + extraPerGap + (w < remainder ? 1 : 0);
                for (var s = 0; s < spaces; s++)
                {
                    buffer.SetCell(posX++, y, new Rune(' '), style);
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
