// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a text display control with optional wrapping and trimming.
/// </summary>
public sealed partial class TextBlock : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class.
    /// </summary>
    public TextBlock()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBlock"/> class with text.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public TextBlock(string text)
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

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var availableSize = new Size(constraints.MaxWidth, constraints.MaxHeight);
        var text = Text ?? string.Empty;
        var naturalWidth = TerminalTextUtility.GetWidth(text.AsSpan());
        var width = Math.Max(0, Math.Min(availableSize.Width, naturalWidth));

        if (!Wrap || width == 0)
        {
            return SizeHints.Fixed(new Size(width, Math.Min(Math.Max(0, availableSize.Height), 1)));
        }

        var height = CountWrappedLines(text.AsSpan(), Math.Max(1, width));
        return SizeHints.Fixed(new Size(width, Math.Min(availableSize.Height, Math.Max(1, height))));
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var text = Text ?? string.Empty;
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var textBlockStyle = GetStyle<TextBlockStyle>();
        var style = textBlockStyle.ResolveTextStyle(theme);
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
            WriteSingleLine(buffer, rect, text.AsSpan(), style, in rect, foregroundBrush, backgroundBrush, defaultMixSpace);
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
            WriteAlignedLine(buffer, rect, lineY, slice, style, in lineBrushRect, foregroundBrush, backgroundBrush, defaultMixSpace, isLastLine: nextStart >= span.Length);
            lineIndex++;
            start = nextStart;
        }
    }

    private void WriteSingleLine(
        CellBuffer buffer,
        Rectangle rect,
        ReadOnlySpan<char> text,
        Style style,
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
            buffer.WriteTextWithBrush(x, rect.Y, span, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            buffer.WriteTextWithBrush(x, rect.Y, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
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
            buffer.WriteTextWithBrush(x, rect.Y, span, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
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
            buffer.WriteTextWithBrush(x + 1, rect.Y, suffix, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var clipped = Clip(text, maxWidth);
        var clippedWidth = TerminalTextUtility.GetWidth(clipped);
        var defaultX = AlignX(rect, alignment, maxWidth, clippedWidth);
        buffer.WriteTextWithBrush(defaultX, rect.Y, clipped, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
    }

    private void WriteAlignedLine(
        CellBuffer buffer,
        Rectangle rect,
        int y,
        ReadOnlySpan<char> text,
        Style style,
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
        if (alignment == TextAlignment.Justify && !isLastLine)
        {
            if (TryWriteJustified(buffer, rect.X, y, width, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace))
            {
                return;
            }
        }

        var clipped = Clip(text, width);
        var cells = TerminalTextUtility.GetWidth(clipped);
        var x = AlignX(rect, alignment, width, cells);
        buffer.WriteTextWithBrush(x, y, clipped, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
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
