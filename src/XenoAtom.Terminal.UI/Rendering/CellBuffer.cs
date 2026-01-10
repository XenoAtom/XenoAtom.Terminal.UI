// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

public sealed class CellBuffer
{
    private readonly int[] _scalars;
    private readonly CellStyle[] _cells;
    private readonly ulong[] _hyperlinks;
    private Dictionary<ulong, string>? _hyperlinkTable;

    private Rectangle _clipRect;
    private Rectangle[]? _clipStack;
    private int _clipDepth;

    public CellBuffer(int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;

        _scalars = new int[width * height];
        _cells = new CellStyle[width * height];
        _hyperlinks = new ulong[width * height];
        _clipRect = new Rectangle(0, 0, width, height);
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    internal ReadOnlySpan<int> UnsafeScalars => _scalars;

    internal ReadOnlySpan<CellStyle> UnsafeCells => _cells;

    internal ReadOnlySpan<ulong> UnsafeHyperlinks => _hyperlinks;

    public void Clear()
    {
        Array.Fill(_scalars, ' ');
        Array.Fill(_cells, CellStyle.None);
        Array.Fill(_hyperlinks, 0ul);
        _hyperlinkTable?.Clear();
    }

    public void Clear(CellStyle cellStyle)
    {
        Array.Fill(_scalars, ' ');
        Array.Fill(_cells, cellStyle);
        Array.Fill(_hyperlinks, 0ul);
        _hyperlinkTable?.Clear();
    }

    public void PushClip(Rectangle rect)
    {
        var next = Intersect(_clipRect, rect);

        _clipStack ??= new Rectangle[8];
        if (_clipDepth == _clipStack.Length)
        {
            Array.Resize(ref _clipStack, _clipStack.Length * 2);
        }

        _clipStack[_clipDepth++] = _clipRect;
        _clipRect = next;
    }

    public void PopClip()
    {
        if (_clipDepth <= 0)
        {
            throw new InvalidOperationException("Clip stack underflow.");
        }

        _clipRect = _clipStack![--_clipDepth];
    }

    public void SetCell(int x, int y, Rune rune, CellStyle cellStyle)
        => SetCell(x, y, rune, cellStyle, hyperlinkToken: 0);

    public void SetCell(int x, int y, Rune rune, CellStyle cellStyle, ulong hyperlinkToken)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || !_clipRect.Contains(x, y))
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
        var style = cellStyle.WithoutContinuation().MergeUnspecified(_cells[index].WithoutContinuation());
        _cells[index] = style;
        _hyperlinks[index] = hyperlinkToken;

        if (width > 1 && x + 1 < Width)
        {
            if (!_clipRect.Contains(x + 1, y))
            {
                return;
            }
            _scalars[index + 1] = ' ';
            _cells[index + 1] = style.WithContinuation();
            _hyperlinks[index + 1] = hyperlinkToken;
        }
    }

    public void WriteText(int x, int y, ReadOnlySpan<char> text, CellStyle cellStyle)
        => WriteText(x, y, text, cellStyle, hyperlinkToken: 0);

    public void WriteText(int x, int y, ReadOnlySpan<char> text, CellStyle cellStyle, ulong hyperlinkToken)
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

                SetCell(posX, y, rune, cellStyle, hyperlinkToken);
                posX += w;
            }

            index += consumed;
        }
    }

    public ulong RegisterHyperlink(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.Length == 0)
        {
            return 0;
        }

        var token = ComputeFnv1a64(uri.AsSpan(), seed: 14695981039346656037ul);
        if (token == 0)
        {
            token = 1;
        }

        _hyperlinkTable ??= new Dictionary<ulong, string>();
        if (_hyperlinkTable.TryGetValue(token, out var existing) && !string.Equals(existing, uri, StringComparison.Ordinal))
        {
            // Extremely unlikely; use a second deterministic seed.
            token = ComputeFnv1a64(uri.AsSpan(), seed: 14695981039346656037ul ^ 0x9E3779B97F4A7C15ul);
            if (token == 0)
            {
                token = 2;
            }
        }

        _hyperlinkTable[token] = uri;
        return token;
    }

    internal bool TryGetHyperlinkUri(ulong token, out string uri)
    {
        if (token == 0 || _hyperlinkTable is null)
        {
            uri = string.Empty;
            return false;
        }

        return _hyperlinkTable.TryGetValue(token, out uri!);
    }

    internal void CopyHyperlinkTableTo(Dictionary<ulong, string> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear();

        if (_hyperlinkTable is null || _hyperlinkTable.Count == 0)
        {
            return;
        }

        foreach (var pair in _hyperlinkTable)
        {
            target[pair.Key] = pair.Value;
        }
    }

    private static ulong ComputeFnv1a64(ReadOnlySpan<char> text, ulong seed)
    {
        const ulong prime = 1099511628211ul;
        var hash = seed;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            hash ^= (byte)ch;
            hash *= prime;
            hash ^= (byte)(ch >> 8);
            hash *= prime;
        }

        return hash;
    }

    private static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        var x0 = Math.Max(a.X, b.X);
        var y0 = Math.Max(a.Y, b.Y);
        var x1 = Math.Min(a.Right, b.Right);
        var y1 = Math.Min(a.Bottom, b.Bottom);

        var w = Math.Max(0, x1 - x0);
        var h = Math.Max(0, y1 - y0);
        return new Rectangle(x0, y0, w, h);
    }

    public IReadOnlyList<string> ToMarkupLines()
    {
        var lines = new string[Height];
        var sb = new StringBuilder(Width + 16);
        Span<char> runeBuffer = stackalloc char[2];

        for (var y = 0; y < Height; y++)
        {
            sb.Clear();

            var currentCell = CellStyle.None;
            var hasOpenStyle = false;

            for (var x = 0; x < Width; x++)
            {
                var i = (y * Width) + x;
                var cell = _cells[i];
                if (cell.IsContinuation)
                {
                    continue;
                }

                cell = cell.WithoutContinuation();

                if (cell != currentCell)
                {
                    if (hasOpenStyle)
                    {
                        sb.Append("[/]");
                        hasOpenStyle = false;
                    }

                    currentCell = cell;
                    if (currentCell != CellStyle.None)
                    {
                        AppendStyle(sb, currentCell);
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

    private static void AppendStyle(StringBuilder sb, CellStyle cellStyle)
    {
        sb.Append('[');
        var first = true;

        var style = cellStyle.TextStyle;
        AppendStyleToken(ref first, sb, style, TextStyle.Bold, "bold");
        AppendStyleToken(ref first, sb, style, TextStyle.Dim, "dim");
        AppendStyleToken(ref first, sb, style, TextStyle.Italic, "italic");
        AppendStyleToken(ref first, sb, style, TextStyle.Underline, "underline");
        AppendStyleToken(ref first, sb, style, TextStyle.Blink, "blink");
        AppendStyleToken(ref first, sb, style, TextStyle.Invert, "invert");
        AppendStyleToken(ref first, sb, style, TextStyle.Hidden, "hidden");
        AppendStyleToken(ref first, sb, style, TextStyle.Strikethrough, "strikethrough");

        if (cellStyle.TryGetForeground(out var fg))
        {
            AppendToken(ref first, sb, ToMarkupColor(fg));
        }

        if (cellStyle.TryGetBackground(out var bg))
        {
            AppendToken(ref first, sb, "on");
            AppendToken(ref first, sb, ToMarkupColor(bg));
        }

        sb.Append(']');
    }

    private static void AppendStyleToken(ref bool first, StringBuilder sb, TextStyle value, TextStyle flag, string token)
    {
        if ((value & flag) == 0)
        {
            return;
        }

        AppendToken(ref first, sb, token);
    }

    private static void AppendToken(ref bool first, StringBuilder sb, string token)
    {
        if (!first)
        {
            sb.Append(' ');
        }

        sb.Append(token);
        first = false;
    }

    private static string ToMarkupColor(AnsiColor color)
    {
        if (color.Kind == AnsiColorKind.Rgb)
        {
            var packed = (uint)((color.R << 16) | (color.G << 8) | color.B);
            return $"#{packed:x6}";
        }

        if (color.Kind == AnsiColorKind.Basic16)
        {
            var (r, g, b) = AnsiPalettes.GetBasic16Rgb(color.Index);
            var packed = (uint)((r << 16) | (g << 8) | b);
            return $"#{packed:x6}";
        }

        if (color.Kind == AnsiColorKind.Indexed256)
        {
            var (r, g, b) = AnsiPalettes.GetXterm256Rgb(color.Index);
            var packed = (uint)((r << 16) | (g << 8) | b);
            return $"#{packed:x6}";
        }

        return "#000000";
    }
}
