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
/// Represents a lightweight placeholder surface with optional text and background fill.
/// </summary>
public sealed partial class Placeholder : Visual
{
    private static readonly Rune Ellipsis = new(0x2026);

    /// <summary>
    /// Initializes a new instance of the <see cref="Placeholder"/> class.
    /// </summary>
    public Placeholder()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;
        Wrap = true;
        TextAlignment = TextAlignment.Center;
        VerticalTextAlignment = Align.Center;
        Trimming = TextTrimming.Clip;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Placeholder"/> class with text.
    /// </summary>
    /// <param name="text">The text to render.</param>
    public Placeholder(string text) : this()
    {
        Text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Placeholder"/> class with dynamic text.
    /// </summary>
    /// <param name="text">A delegate providing the text to render.</param>
    public Placeholder(Func<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Placeholder"/> class with bound text.
    /// </summary>
    /// <param name="text">A binding that supplies the text to render.</param>
    public Placeholder(Binding<string?> text) : this()
    {
        this.Text(text);
    }

    /// <summary>
    /// Gets or sets the placeholder text.
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
    /// Gets or sets the vertical text alignment.
    /// </summary>
    [Bindable]
    public partial Align VerticalTextAlignment { get; set; }

    /// <summary>
    /// Gets or sets the trimming mode when text exceeds available width.
    /// </summary>
    [Bindable]
    public partial TextTrimming Trimming { get; set; }

    partial void OnVerticalTextAlignmentChanging(ref Align value)
    {
        if (value == Align.Stretch)
        {
            value = Align.Center;
        }
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var style = GetStyle<PlaceholderStyle>();
        var padding = style.Padding;

        var maxWidth = constraints.MaxWidth == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : constraints.MaxWidth;
        var maxHeight = constraints.MaxHeight == LayoutConstants.Infinite ? LayoutConstants.MaxFinite : constraints.MaxHeight;
        var innerMaxWidth = Math.Max(0, maxWidth - padding.Horizontal);
        var innerMaxHeight = Math.Max(0, maxHeight - padding.Vertical);

        var text = Text;
        if (string.IsNullOrEmpty(text))
        {
            var desiredNoText = new Size(1 + padding.Horizontal, 1 + padding.Vertical);
            return SizeHints.Fixed(constraints.Clamp(desiredNoText));
        }

        var span = text.AsSpan();
        var naturalWidth = TerminalTextUtility.GetWidth(span);
        var measuredWidth = Math.Max(0, Math.Min(innerMaxWidth, naturalWidth));

        int measuredHeight;
        if (!Wrap || measuredWidth == 0)
        {
            measuredHeight = 1;
        }
        else
        {
            measuredHeight = CountWrappedLines(span, Math.Max(1, measuredWidth));
            measuredHeight = Math.Max(1, Math.Min(innerMaxHeight, measuredHeight));
        }

        var desired = new Size(measuredWidth + padding.Horizontal, measuredHeight + padding.Vertical);
        return SizeHints.Fixed(constraints.Clamp(desired));
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var placeholderStyle = GetStyle<PlaceholderStyle>();
        var textStyle = placeholderStyle.ResolveTextStyle(theme);
        var fillStyle = placeholderStyle.ResolveFillStyle(theme);
        var foregroundBrush = placeholderStyle.ForegroundBrush;
        var backgroundBrush = placeholderStyle.BackgroundBrush;
        var defaultMixSpace = theme.GradientMixSpace;
        var padding = placeholderStyle.Padding;

        if (placeholderStyle.FillBackground && (placeholderStyle.Background is not null || backgroundBrush is not null))
        {
            if (backgroundBrush is { } brush)
            {
                buffer.FillRectWithBrush(rect, fillStyle, foregroundBrush: null, backgroundBrush: brush, defaultMixSpace);
            }
            else
            {
                for (var y = rect.Y; y < rect.Bottom; y++)
                {
                    for (var x = rect.X; x < rect.Right; x++)
                    {
                        buffer.SetCell(x, y, new Rune(' '), fillStyle);
                    }
                }
            }
        }

        var text = Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var contentRect = new Rectangle(
            rect.X + padding.Left,
            rect.Y + padding.Top,
            Math.Max(0, rect.Width - padding.Horizontal),
            Math.Max(0, rect.Height - padding.Vertical));

        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            return;
        }

        var span = text.AsSpan();
        if (!Wrap || contentRect.Height == 1)
        {
            var y = AlignY(contentRect, 1);
            WriteSingleLine(buffer, contentRect, y, span, textStyle, in contentRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var maxWidth = Math.Max(1, contentRect.Width);
        var totalLines = CountWrappedLines(span, maxWidth);
        var renderLines = Math.Max(1, Math.Min(contentRect.Height, totalLines));
        var startY = AlignY(contentRect, renderLines);

        var lineIndex = 0;
        var start = 0;
        while (start < span.Length && lineIndex < renderLines)
        {
            if (!TryGetNextWrapSlice(span, start, maxWidth, out var endExclusive, out var nextStart))
            {
                break;
            }

            var lineY = startY + lineIndex;
            var lineBrushRect = new Rectangle(contentRect.X, lineY, contentRect.Width, 1);
            var slice = span.Slice(start, Math.Max(0, endExclusive - start));
            WriteAlignedLine(buffer, contentRect, lineY, slice, textStyle, in lineBrushRect, foregroundBrush, backgroundBrush, defaultMixSpace, isLastLine: nextStart >= span.Length);
            lineIndex++;
            start = nextStart;
        }
    }

    private int AlignY(in Rectangle rect, int contentHeight)
    {
        if (rect.Height <= contentHeight)
        {
            return rect.Y;
        }

        return VerticalTextAlignment switch
        {
            Align.Center => rect.Y + ((rect.Height - contentHeight) / 2),
            Align.End => rect.Y + (rect.Height - contentHeight),
            _ => rect.Y,
        };
    }

    private void WriteSingleLine(
        CellBuffer buffer,
        in Rectangle rect,
        int y,
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
            var clipped = Clip(text, maxWidth);
            var cells = TerminalTextUtility.GetWidth(clipped);
            var x = AlignX(rect, alignment, maxWidth, cells);
            buffer.WriteTextWithBrush(x, y, clipped, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var fullWidth = TerminalTextUtility.GetWidth(text);
        if (fullWidth <= maxWidth)
        {
            var x = AlignX(rect, alignment, maxWidth, fullWidth);
            buffer.WriteTextWithBrush(x, y, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        if (maxWidth == 1)
        {
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, rect.X, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(rect.X, y, Ellipsis, ellipsisStyle);
            return;
        }

        var bodyWidth = maxWidth - 1;
        if (trimming == TextTrimming.EndEllipsis)
        {
            var clipped = Clip(text, bodyWidth);
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            buffer.WriteTextWithBrush(x, y, clipped, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, x + bodyWidth, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(x + bodyWidth, y, Ellipsis, ellipsisStyle);
            return;
        }

        if (trimming == TextTrimming.StartEllipsis)
        {
            var startIndex = GetStartIndexForSuffix(text, bodyWidth);
            var suffix = text[startIndex..];
            var x = AlignX(rect, alignment, maxWidth, maxWidth);
            var ellipsisStyle = CellBufferBrushExtensions.ApplyBrushes(style, x, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            buffer.SetCell(x, y, Ellipsis, ellipsisStyle);
            buffer.WriteTextWithBrush(x + 1, y, suffix, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
            return;
        }

        var fallback = Clip(text, maxWidth);
        var fallbackWidth = TerminalTextUtility.GetWidth(fallback);
        var fallbackX = AlignX(rect, alignment, maxWidth, fallbackWidth);
        buffer.WriteTextWithBrush(fallbackX, y, fallback, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
    }

    private void WriteAlignedLine(
        CellBuffer buffer,
        in Rectangle rect,
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

    private static int AlignX(in Rectangle rect, TextAlignment alignment, int availableWidth, int contentWidth)
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
        var index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
            {
                break;
            }

            var start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (wordCount < words.Length)
            {
                words[wordCount++] = (start, index - start);
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
                    var fillStyle = CellBufferBrushExtensions.ApplyBrushes(style, posX, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
                    buffer.SetCell(posX++, y, new Rune(' '), fillStyle);
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
