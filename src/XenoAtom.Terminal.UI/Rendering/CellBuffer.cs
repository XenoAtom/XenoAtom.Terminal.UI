// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed class CellBuffer
{
    private readonly int[] _scalars;
    private readonly CellStyle[] _styles;

    public CellBuffer(int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;

        _scalars = new int[width * height];
        _styles = new CellStyle[width * height];
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    internal ReadOnlySpan<int> UnsafeScalars => _scalars;

    internal ReadOnlySpan<CellStyle> UnsafeStyles => _styles;

    public void Clear()
    {
        Array.Fill(_scalars, ' ');
        Array.Fill(_styles, CellStyle.None);
    }

    public void SetCell(int x, int y, Rune rune, CellStyle style)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        var width = TerminalTextUtility.GetRuneWidth(rune);
        if (width <= 0)
        {
            return;
        }

        var index = (y * Width) + x;
        _scalars[index] = rune.Value;
        _styles[index] = style & ~CellStyle.Continuation;

        if (width > 1 && x + 1 < Width)
        {
            _scalars[index + 1] = ' ';
            _styles[index + 1] = (style & ~CellStyle.Continuation) | CellStyle.Continuation;
        }
    }

    public void WriteText(int x, int y, ReadOnlySpan<char> text, CellStyle style)
    {
        var posX = x;
        var index = 0;
        while (index < text.Length && posX < Width)
        {
            if (Rune.DecodeFromUtf16(text[index..], out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
            {
                rune = Rune.ReplacementChar;
                consumed = 1;
            }

            var w = TerminalTextUtility.GetRuneWidth(rune);
            if (w > 0)
            {
                if (posX + w > Width)
                {
                    break;
                }

                SetCell(posX, y, rune, style);
                posX += w;
            }

            index += consumed;
        }
    }

    public IReadOnlyList<string> ToMarkupLines()
    {
        var lines = new string[Height];
        var sb = new StringBuilder(Width + 16);
        Span<char> runeBuffer = stackalloc char[2];

        for (var y = 0; y < Height; y++)
        {
            sb.Clear();

            var currentStyle = CellStyle.None;
            var hasOpenStyle = false;

            for (var x = 0; x < Width; x++)
            {
                var i = (y * Width) + x;
                var style = _styles[i];
                if ((style & CellStyle.Continuation) != 0)
                {
                    continue;
                }

                style &= ~CellStyle.Continuation;

                if (style != currentStyle)
                {
                    if (hasOpenStyle)
                    {
                        sb.Append("[/]");
                        hasOpenStyle = false;
                    }

                    currentStyle = style;
                    if (currentStyle != CellStyle.None)
                    {
                        sb.Append('[');
                        var first = true;
                        AppendStyleToken(ref first, sb, currentStyle, CellStyle.Invert, "invert");
                        AppendStyleToken(ref first, sb, currentStyle, CellStyle.Dim, "dim");
                        AppendStyleToken(ref first, sb, currentStyle, CellStyle.Bold, "bold");
                        sb.Append(']');
                        hasOpenStyle = true;
                    }
                }

                var scalar = _scalars[i];
                if (scalar == 0)
                {
                    sb.Append(' ');
                    continue;
                }

                var rune = new Rune(scalar);
                var written = rune.EncodeToUtf16(runeBuffer);
                sb.Append(AnsiMarkup.Escape(runeBuffer[..written]));
            }

            if (hasOpenStyle)
            {
                sb.Append("[/]");
            }

            lines[y] = sb.ToString();
        }

        return lines;
    }

    private static void AppendStyleToken(ref bool first, StringBuilder sb, CellStyle value, CellStyle flag, string token)
    {
        if ((value & flag) == 0)
        {
            return;
        }

        if (!first)
        {
            sb.Append(' ');
        }

        sb.Append(token);
        first = false;
    }
}
