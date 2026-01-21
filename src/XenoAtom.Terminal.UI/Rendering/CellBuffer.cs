// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Represents a 2D buffer of terminal cells (glyph + style + hyperlink) used for rendering.
/// </summary>
public sealed class CellBuffer
{
    private const float OneOverMaxByte = 1.0f / byte.MaxValue;

    private readonly int[] _scalars;
    private readonly Style[] _cells;
    private readonly ulong[] _hyperlinks;
    private Dictionary<ulong, string>? _hyperlinkTable;
    private Dictionary<int, TextElementEntry>? _textElementTable;

    private Rectangle _clipRect;
    private Rectangle[]? _clipStack;
    private int _clipDepth;

    private readonly record struct TextElementEntry(string Text, int Width);

    private static readonly float[] SrgbToLinear = CreateSrgbToLinearLut();

    private static readonly byte[] LinearToSrgb = CreateLinearToSrgbLut();

    /// <summary>
    /// Initializes a new instance of the <see cref="CellBuffer"/> class.
    /// </summary>
    /// <param name="width">The buffer width in cells.</param>
    /// <param name="height">The buffer height in cells.</param>
    public CellBuffer(int width, int height)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;

        _scalars = new int[width * height];
        _cells = new Style[width * height];
        _hyperlinks = new ulong[width * height];
        _clipRect = new Rectangle(0, 0, width, height);
        Clear();
    }

    /// <summary>
    /// Gets the buffer width in cells.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the buffer height in cells.
    /// </summary>
    public int Height { get; }

    internal ReadOnlySpan<int> UnsafeScalars => _scalars;

    internal ReadOnlySpan<Style> UnsafeCells => _cells;

    internal ReadOnlySpan<ulong> UnsafeHyperlinks => _hyperlinks;

    /// <summary>
    /// Clears the buffer using a blank glyph and <see cref="Style.None"/>.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_scalars, ' ');
        Array.Fill(_cells, Style.None);
        Array.Fill(_hyperlinks, 0ul);
        _hyperlinkTable?.Clear();
        _textElementTable?.Clear();
    }

    /// <summary>
    /// Clears the buffer using a blank glyph and the specified <paramref name="style"/>.
    /// </summary>
    /// <param name="style">The style applied to every cell.</param>
    public void Clear(Style style)
    {
        Array.Fill(_scalars, ' ');
        Array.Fill(_cells, style);
        Array.Fill(_hyperlinks, 0ul);
        _hyperlinkTable?.Clear();
        _textElementTable?.Clear();
    }

    /// <summary>
    /// Pushes a clipping rectangle that limits subsequent writes to the intersection with the current clip.
    /// </summary>
    /// <param name="rect">The clipping rectangle.</param>
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

    /// <summary>
    /// Pops the most recent clipping rectangle.
    /// </summary>
    public void PopClip()
    {
        if (_clipDepth <= 0)
        {
            throw new InvalidOperationException("Clip stack underflow.");
        }

        _clipRect = _clipStack![--_clipDepth];
    }

    /// <summary>
    /// Sets a cell at the given position.
    /// </summary>
    /// <param name="x">The x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <param name="rune">The glyph to write.</param>
    /// <param name="style">The cell style.</param>
    public void SetCell(int x, int y, Rune rune, Style style)
        => SetCell(x, y, rune, style, hyperlinkToken: 0);

    /// <summary>
    /// Sets a cell at the given position with an associated hyperlink token.
    /// </summary>
    /// <param name="x">The x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <param name="rune">The glyph to write.</param>
    /// <param name="style">The cell style.</param>
    /// <param name="hyperlinkToken">A hyperlink token registered via <see cref="RegisterHyperlink"/>.</param>
    public void SetCell(int x, int y, Rune rune, Style style, ulong hyperlinkToken)
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
        var under = _cells[index].WithoutContinuation();
        var overlay = style.WithoutContinuation();
        var mergedStyle = ApplyAlphaBlending(overlay.MergeUnspecified(under), overlay, under);
        _cells[index] = mergedStyle;
        _hyperlinks[index] = hyperlinkToken;

        if (width > 1 && x + 1 < Width)
        {
            if (!_clipRect.Contains(x + 1, y))
            {
                return;
            }
            _scalars[index + 1] = ' ';
            _cells[index + 1] = mergedStyle.WithContinuation();
            _hyperlinks[index + 1] = hyperlinkToken;
        }
    }

    internal bool TryGetTextElement(int token, out string text, out int width)
    {
        if (token >= 0 || _textElementTable is null || !_textElementTable.TryGetValue(token, out var entry))
        {
            text = string.Empty;
            width = 0;
            return false;
        }

        text = entry.Text;
        width = entry.Width;
        return true;
    }

    private void SetTextElementToken(int x, int y, int token, int width, Style style, ulong hyperlinkToken)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || !_clipRect.Contains(x, y))
        {
            return;
        }

        if (width <= 0)
        {
            return;
        }

        var index = (y * Width) + x;
        _scalars[index] = token;
        var under = _cells[index].WithoutContinuation();
        var overlay = style.WithoutContinuation();
        var mergedStyle = ApplyAlphaBlending(overlay.MergeUnspecified(under), overlay, under);
        _cells[index] = mergedStyle;
        _hyperlinks[index] = hyperlinkToken;

        if (width > 1 && x + 1 < Width)
        {
            if (!_clipRect.Contains(x + 1, y))
            {
                return;
            }

            _scalars[index + 1] = ' ';
            _cells[index + 1] = mergedStyle.WithContinuation();
            _hyperlinks[index + 1] = hyperlinkToken;
        }
    }

    /// <summary>
    /// Writes a UTF-16 text span to the buffer, applying the given style.
    /// </summary>
    /// <param name="x">The start x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <param name="text">The text to write.</param>
    /// <param name="style">The cell style.</param>
    public void WriteText(int x, int y, ReadOnlySpan<char> text, Style style)
        => WriteText(x, y, text, style, hyperlinkToken: 0);

    /// <summary>
    /// Writes a UTF-16 text span to the buffer, applying the given style and hyperlink.
    /// </summary>
    /// <param name="x">The start x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <param name="text">The text to write.</param>
    /// <param name="style">The cell style.</param>
    /// <param name="hyperlinkToken">A hyperlink token registered via <see cref="RegisterHyperlink"/>.</param>
    public void WriteText(int x, int y, ReadOnlySpan<char> text, Style style, ulong hyperlinkToken)
    {
        var posX = x;
        var index = 0;
        while (index < text.Length && posX < Width)
        {
            var elementLength = StringInfo.GetNextTextElementLength(text[index..]);
            if (elementLength <= 0)
            {
                elementLength = 1;
            }

            var elementSpan = text.Slice(index, Math.Min(elementLength, text.Length - index));
            if (elementSpan.Length == 1 && elementSpan[0] == '\t')
            {
                // Expand tab using a deterministic tab stop so that cell-buffer rendering remains consistent.
                var tabWidth = TerminalTextUtility.DefaultTabWidth;
                var spaces = tabWidth - (posX % tabWidth);
                spaces = Math.Max(1, spaces);
                for (var s = 0; s < spaces && posX < Width; s++)
                {
                    SetCell(posX++, y, new Rune(' '), style, hyperlinkToken);
                }

                index += elementLength;
                continue;
            }

            var w = GetTextElementWidth(elementSpan);
            if (w > 0)
            {
                if (posX + w > Width)
                {
                    break;
                }

                if (TryDecodeSingleRune(elementSpan, out var rune))
                {
                    SetCell(posX, y, rune, style, hyperlinkToken);
                }
                else
                {
                    var token = RegisterTextElement(elementSpan, w);
                    SetTextElementToken(posX, y, token, w, style, hyperlinkToken);
                }
                posX += w;
            }

            index += elementLength;
        }
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

    private static int GetTextElementWidth(ReadOnlySpan<char> element)
    {
        if (element.IsEmpty)
        {
            return 0;
        }

        // Some grapheme clusters (notably emoji presentation sequences) are composed of narrow code points
        // (e.g. U+2328 KEYBOARD) combined with U+FE0F (VS16), ZWJ sequences, or keycap combining marks.
        // Most terminals render such clusters as a 2-cell glyph. Treat them as width 2 to avoid visual "shifts"
        // where the following characters appear displaced because the cluster occupies more cells than measured.
        var forceDoubleWidth = false;
        for (var j = 0; j < element.Length; j++)
        {
            var ch = element[j];
            if (ch is '\uFE0F' or '\u200D' or '\u20E3')
            {
                forceDoubleWidth = true;
                break;
            }
        }

        // Preserve the behavior of treating a tab as a fixed number of cells.
        // Text editors generally expand tabs themselves; this is a safe fallback.
        if (element.Length == 1 && element[0] == '\t')
        {
            return TerminalTextUtility.DefaultTabWidth;
        }

        if (Rune.DecodeFromUtf16(element, out var rune, out var consumed) != OperationStatus.Done || consumed <= 0)
        {
            return Math.Max(1, TerminalTextUtility.GetRuneWidth(Rune.ReplacementChar));
        }

        if (element.Length == consumed)
        {
            var width = Math.Max(1, TerminalTextUtility.GetRuneWidth(rune));
            return forceDoubleWidth ? Math.Max(2, width) : width;
        }

        var maxWidth = Math.Max(0, TerminalTextUtility.GetRuneWidth(rune));
        var i = consumed;
        while (i < element.Length)
        {
            if (Rune.DecodeFromUtf16(element[i..], out var next, out var c) != OperationStatus.Done || c <= 0)
            {
                maxWidth = Math.Max(maxWidth, TerminalTextUtility.GetRuneWidth(Rune.ReplacementChar));
                i++;
                continue;
            }

            maxWidth = Math.Max(maxWidth, TerminalTextUtility.GetRuneWidth(next));
            i += c;
        }

        maxWidth = Math.Max(0, maxWidth);
        return forceDoubleWidth ? Math.Max(2, maxWidth) : maxWidth;
    }

    private int RegisterTextElement(ReadOnlySpan<char> element, int width)
    {
        // Note: the token is stored directly in the scalar buffer (negative int), so it must be stable across frames.
        // We use a deterministic hash with collision detection (same approach as hyperlinks).
        var text = element.ToString();
        _textElementTable ??= new Dictionary<int, TextElementEntry>();

        var seed = 14695981039346656037ul;
        var token = ComputeTextToken(text.AsSpan(), seed);

        if (_textElementTable.TryGetValue(token, out var existing) && !string.Equals(existing.Text, text, StringComparison.Ordinal))
        {
            // Extremely unlikely; use a second deterministic seed.
            token = ComputeTextToken(text.AsSpan(), seed ^ 0x9E3779B97F4A7C15ul);
        }

        if (_textElementTable.TryGetValue(token, out existing) && !string.Equals(existing.Text, text, StringComparison.Ordinal))
        {
            // Fall back to a unique negative id. This makes diffs noisier but preserves correctness.
            var fallback = -1;
            while (_textElementTable.ContainsKey(fallback))
            {
                fallback--;
            }
            token = fallback;
        }

        _textElementTable[token] = new TextElementEntry(text, width);
        return token;
    }

    private static int ComputeTextToken(ReadOnlySpan<char> text, ulong seed)
    {
        var hash = ComputeFnv1a64(text, seed);
        var token = (int)(hash & 0x7FFFFFFFul);
        if (token == 0)
        {
            token = 1;
        }

        return -token;
    }

    /// <summary>
    /// Registers a hyperlink URI and returns a token that can be stored per cell.
    /// </summary>
    /// <param name="uri">The hyperlink URI.</param>
    /// <returns>A non-zero token when a valid URI is provided; otherwise <c>0</c>.</returns>
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

    internal void CopyTextElementTableTo(Dictionary<int, string> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear();

        if (_textElementTable is null || _textElementTable.Count == 0)
        {
            return;
        }

        foreach (var pair in _textElementTable)
        {
            target[pair.Key] = pair.Value.Text;
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

    /// <summary>
    /// Converts the buffer content to ANSI markup lines.
    /// </summary>
    /// <remarks>
    /// This method allocates and is intended primarily for tests and diagnostics.
    /// </remarks>
    public IReadOnlyList<string> ToMarkupLines()
    {
        var lines = new string[Height];
        var sb = new StringBuilder(Width + 16);
        Span<char> runeBuffer = stackalloc char[2];

        for (var y = 0; y < Height; y++)
        {
            sb.Clear();

            var currentCell = Style.None;
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
                    if (currentCell != Style.None)
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

                if (scalar < 0 && TryGetTextElement(scalar, out var textElement, out _))
                {
                    sb.Append(AnsiMarkup.Escape(textElement.AsSpan()));
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

    private static void AppendStyle(StringBuilder sb, Style style)
    {
        sb.Append('[');
        var first = true;

        var textStyle = style.TextStyle;
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Bold, "bold");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Dim, "dim");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Italic, "italic");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Underline, "underline");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Blink, "blink");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Invert, "invert");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Hidden, "hidden");
        AppendStyleToken(ref first, sb, textStyle, TextStyle.Strikethrough, "strikethrough");

        if (style.TryGetForeground(out var fg))
        {
            AppendToken(ref first, sb, ToMarkupColor(fg));
        }

        if (style.TryGetBackground(out var bg))
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

    private static string ToMarkupColor(Color color)
    {
        if (color.Kind is ColorKind.Rgb or ColorKind.RgbA)
        {
            var packed = (uint)((color.R << 16) | (color.G << 8) | color.B);
            return $"#{packed:x6}";
        }

        if (color.Kind == ColorKind.Basic16)
        {
            var (r, g, b) = AnsiPalettes.GetBasic16Rgb(color.Index);
            var packed = (uint)((r << 16) | (g << 8) | b);
            return $"#{packed:x6}";
        }

        if (color.Kind == ColorKind.Indexed256)
        {
            var (r, g, b) = AnsiPalettes.GetXterm256Rgb(color.Index);
            var packed = (uint)((r << 16) | (g << 8) | b);
            return $"#{packed:x6}";
        }

        return "#000000";
    }

    private static Style ApplyAlphaBlending(Style merged, Style overlay, Style under)
    {
        // Only apply blending when the overlay contains explicit RGBA colors.
        // Indexed colors (Default/Basic16/Indexed256) are treated as opaque and never blended.
        if (overlay.TryGetBackground(out var overlayBg) && overlayBg.Kind == ColorKind.RgbA)
        {
            var bg = ApplyAlphaToBackground(overlayBg, under.GetBackgroundOrDefault());
            merged = merged.WithBackground(bg);
        }

        if (overlay.TryGetForeground(out var overlayFg) && overlayFg.Kind == ColorKind.RgbA)
        {
            var bgForFg = merged.GetBackgroundOrDefault();
            var fg = ApplyAlphaToForeground(overlayFg, bgForFg);
            merged = merged.WithForeground(fg);
        }

        return merged;
    }

    private static Color ApplyAlphaToBackground(Color overlayBg, Color underBg)
    {
        // If the destination background is not RGB, we cannot blend. Treat the alpha channel as ignored.
        if (underBg.Kind is not ColorKind.Rgb and not ColorKind.RgbA)
        {
            return Color.Rgb(overlayBg.R, overlayBg.G, overlayBg.B);
        }

        if (overlayBg.A >= byte.MaxValue)
        {
            return Color.Rgb(overlayBg.R, overlayBg.G, overlayBg.B);
        }

        if (overlayBg.A == 0)
        {
            return Color.Rgb(underBg.R, underBg.G, underBg.B);
        }

        return BlendRgbAOverRgb(overlayBg, underBg);
    }

    private static Color ApplyAlphaToForeground(Color overlayFg, Color background)
    {
        // Foreground is drawn "over" the resolved background, so the background is the destination for blending.
        // If the background is not RGB, we cannot blend. Treat alpha as ignored.
        if (background.Kind is not ColorKind.Rgb and not ColorKind.RgbA)
        {
            return Color.Rgb(overlayFg.R, overlayFg.G, overlayFg.B);
        }

        if (overlayFg.A >= byte.MaxValue)
        {
            return Color.Rgb(overlayFg.R, overlayFg.G, overlayFg.B);
        }

        if (overlayFg.A == 0)
        {
            return Color.Rgb(background.R, background.G, background.B);
        }

        return BlendRgbAOverRgb(overlayFg, background);
    }

    private static Color BlendRgbAOverRgb(Color src, Color dst)
    {
        // This routine blends src over dst in linear space using pre-multiplied alpha:
        // out = src * sa + dst * (1 - sa)
        // The output is returned as an opaque RGB color (alpha is not representable in SGR).
        var sa = src.A * OneOverMaxByte;
        var invSa = 1.0f - sa;

        ref var srgbLinear = ref MemoryMarshal.GetArrayDataReference(SrgbToLinear);
        var srcR = Unsafe.Add(ref srgbLinear, src.R);
        var srcG = Unsafe.Add(ref srgbLinear, src.G);
        var srcB = Unsafe.Add(ref srgbLinear, src.B);
        var dstR = Unsafe.Add(ref srgbLinear, dst.R);
        var dstG = Unsafe.Add(ref srgbLinear, dst.G);
        var dstB = Unsafe.Add(ref srgbLinear, dst.B);

        var outR = (srcR * sa) + (dstR * invSa);
        var outG = (srcG * sa) + (dstG * invSa);
        var outB = (srcB * sa) + (dstB * invSa);
        
        ref var linearToSrgb = ref MemoryMarshal.GetArrayDataReference(LinearToSrgb);
        var r = Unsafe.Add(ref linearToSrgb, ClampLinearToIndex(outR));
        var g = Unsafe.Add(ref linearToSrgb, ClampLinearToIndex(outG));
        var b = Unsafe.Add(ref linearToSrgb, ClampLinearToIndex(outB));

        return Color.Rgb(r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ClampLinearToIndex(float linear)
    {
        // Linear space is clamped to [0..1] and mapped to a small LUT.
        var index = (int)(linear * (LinearToSrgb.Length - 1) + 0.5f);
        if ((uint)index >= (uint)LinearToSrgb.Length)
        {
            return linear < 0 ? 0 : LinearToSrgb.Length - 1;
        }

        return index;
    }

    private static float[] CreateSrgbToLinearLut()
    {
        var lut = new float[256];
        for (var i = 0; i < lut.Length; i++)
        {
            var srgb = i * OneOverMaxByte;
            lut[i] = srgb <= 0.04045f ? srgb / 12.92f : MathF.Pow((srgb + 0.055f) / 1.055f, 2.4f);
        }

        return lut;
    }

    private static byte[] CreateLinearToSrgbLut()
    {
        // The LUT size is a trade-off between speed and precision. 4096 samples are enough for stable output
        // when simulating alpha blending in terminal cells.
        const int lutSize = 4096;
        var lut = new byte[lutSize + 1];
        for (var i = 0; i < lut.Length; i++)
        {
            var linear = (float)i / lutSize;
            var srgb = linear <= 0.0031308f ? 12.92f * linear : (1.055f * MathF.Pow(linear, 1.0f / 2.4f)) - 0.055f;
            var value = (int)(srgb * byte.MaxValue + 0.5f);
            lut[i] = (byte)Math.Clamp(value, 0, byte.MaxValue);
        }

        return lut;
    }
}
