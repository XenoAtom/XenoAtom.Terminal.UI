// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class TextBlock : Visual
{
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
            buffer.WriteText(rect.X, rect.Y, text.AsSpan(), CellStyle.None);
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
            buffer.WriteText(rect.X, rect.Y + lineIndex, slice, CellStyle.None);
            lineIndex++;
            start = nextStart;
        }
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
