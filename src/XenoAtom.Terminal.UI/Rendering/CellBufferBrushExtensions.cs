// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Provides brush-based rendering helpers for <see cref="CellBuffer"/>.
/// </summary>
public static class CellBufferBrushExtensions
{
    private static readonly Rune SpaceRune = new(' ');

    /// <summary>
    /// Fills a rectangle by sampling optional foreground and background brushes per cell.
    /// </summary>
    /// <param name="buffer">The target cell buffer.</param>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="style">The base style.</param>
    /// <param name="foregroundBrush">An optional foreground brush override.</param>
    /// <param name="backgroundBrush">An optional background brush override.</param>
    /// <param name="defaultMixSpace">The default interpolation space used when a brush does not provide an override.</param>
    /// <param name="hyperlinkToken">An optional hyperlink token.</param>
    public static void FillRectWithBrush(
        this CellBuffer buffer,
        in Rectangle rect,
        Style style,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace,
        ulong hyperlinkToken = 0)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (!TryGetEffectiveRect(buffer, in rect, out var left, out var top, out var right, out var bottom))
        {
            return;
        }

        if (foregroundBrush is null && backgroundBrush is null)
        {
            for (var y = top; y < bottom; y++)
            {
                for (var x = left; x < right; x++)
                {
                    buffer.SetCell(x, y, SpaceRune, style, hyperlinkToken);
                }
            }
            return;
        }

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                var sampled = ApplyBrushes(style, x, y, in rect, foregroundBrush, backgroundBrush, defaultMixSpace);
                buffer.SetCell(x, y, SpaceRune, sampled, hyperlinkToken);
            }
        }
    }

    /// <summary>
    /// Writes text while sampling optional foreground and background brushes per rendered grapheme.
    /// </summary>
    /// <param name="buffer">The target cell buffer.</param>
    /// <param name="x">The text origin x coordinate.</param>
    /// <param name="y">The text origin y coordinate.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="style">The base style.</param>
    /// <param name="brushRect">The rectangle used for brush sampling coordinates.</param>
    /// <param name="foregroundBrush">An optional foreground brush override.</param>
    /// <param name="backgroundBrush">An optional background brush override.</param>
    /// <param name="defaultMixSpace">The default interpolation space used when a brush does not provide an override.</param>
    /// <param name="hyperlinkToken">An optional hyperlink token.</param>
    public static void WriteTextWithBrush(
        this CellBuffer buffer,
        int x,
        int y,
        ReadOnlySpan<char> text,
        Style style,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace,
        ulong hyperlinkToken = 0)
        => WriteTextWithBrush(buffer, x, y, text, style, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace, tabSize: 4, startColumn: x, hyperlinkToken);

    /// <summary>
    /// Writes text while sampling optional foreground and background brushes per rendered grapheme.
    /// </summary>
    /// <param name="buffer">The target cell buffer.</param>
    /// <param name="x">The text origin x coordinate.</param>
    /// <param name="y">The text origin y coordinate.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="style">The base style.</param>
    /// <param name="brushRect">The rectangle used for brush sampling coordinates.</param>
    /// <param name="foregroundBrush">An optional foreground brush override.</param>
    /// <param name="backgroundBrush">An optional background brush override.</param>
    /// <param name="defaultMixSpace">The default interpolation space used when a brush does not provide an override.</param>
    /// <param name="tabSize">The tab size in cells.</param>
    /// <param name="startColumn">The starting visual column.</param>
    /// <param name="hyperlinkToken">An optional hyperlink token.</param>
    public static void WriteTextWithBrush(
        this CellBuffer buffer,
        int x,
        int y,
        ReadOnlySpan<char> text,
        Style style,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace,
        int tabSize,
        int startColumn,
        ulong hyperlinkToken = 0)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (foregroundBrush is null && backgroundBrush is null)
        {
            buffer.WriteText(x, y, text, style, hyperlinkToken);
            return;
        }

        var effectiveTabSize = Math.Max(1, tabSize);
        var column = Math.Max(0, startColumn);
        var index = 0;
        var posX = x;

        while (index < text.Length && posX < buffer.Width)
        {
            var elementLength = StringInfo.GetNextTextElementLength(text[index..]);
            if (elementLength <= 0)
            {
                elementLength = 1;
            }

            var elementSpan = text.Slice(index, Math.Min(elementLength, text.Length - index));
            if (elementSpan.Length == 1 && elementSpan[0] == '\t')
            {
                var spaces = effectiveTabSize - (column % effectiveTabSize);
                spaces = Math.Max(1, spaces);

                for (var i = 0; i < spaces && posX < buffer.Width; i++)
                {
                    var sampled = ApplyBrushes(style, posX, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
                    buffer.SetCell(posX++, y, SpaceRune, sampled, hyperlinkToken);
                }

                column += spaces;
                index += elementLength;
                continue;
            }

            var width = CellBuffer.GetTextElementWidth(elementSpan);
            if (width > 0)
            {
                if (posX + width > buffer.Width)
                {
                    break;
                }

                var sampled = ApplyBrushes(style, posX, y, in brushRect, foregroundBrush, backgroundBrush, defaultMixSpace);
                if (TryDecodeSingleRune(elementSpan, out var rune))
                {
                    buffer.SetCell(posX, y, rune, sampled, hyperlinkToken);
                }
                else
                {
                    buffer.WriteText(posX, y, elementSpan, sampled, hyperlinkToken);
                }

                posX += width;
                column += width;
            }

            index += elementLength;
        }
    }

    internal static Style ApplyBrushes(
        Style style,
        int x,
        int y,
        in Rectangle brushRect,
        Brush? foregroundBrush,
        Brush? backgroundBrush,
        ColorMixSpace defaultMixSpace)
    {
        var sampled = style;
        if (foregroundBrush is { } fg)
        {
            sampled = sampled.WithForeground(fg.Sample(x, y, in brushRect, defaultMixSpace));
        }

        if (backgroundBrush is { } bg)
        {
            sampled = sampled.WithBackground(bg.Sample(x, y, in brushRect, defaultMixSpace));
        }

        return sampled;
    }

    private static bool TryDecodeSingleRune(ReadOnlySpan<char> element, out Rune rune)
    {
        if (Rune.DecodeFromUtf16(element, out rune, out var consumed) == OperationStatus.Done && consumed > 0 && consumed == element.Length)
        {
            return true;
        }

        rune = Rune.ReplacementChar;
        return false;
    }

    private static bool TryGetEffectiveRect(CellBuffer buffer, in Rectangle rect, out int left, out int top, out int right, out int bottom)
    {
        var clip = buffer.CurrentClipRect;
        left = Math.Max(rect.X, clip.X);
        top = Math.Max(rect.Y, clip.Y);
        right = Math.Min(rect.Right, clip.Right);
        bottom = Math.Min(rect.Bottom, clip.Bottom);
        return left < right && top < bottom;
    }
}
