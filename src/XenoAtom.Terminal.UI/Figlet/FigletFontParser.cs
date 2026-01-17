// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;

namespace XenoAtom.Terminal.UI.Figlet;

/// <summary>
/// Parses FIGlet <c>.flf</c> font files.
/// </summary>
internal static class FigletFontParser
{
    public static FigletFont Parse(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            throw new ArgumentException("Font content cannot be empty.", nameof(text));
        }

        var reader = new LineReader(text);

        var headerLine = reader.ReadLineOrThrow("Missing FIGlet header line.");
        if (headerLine.Length < 6 || !headerLine.StartsWith("flf2a", StringComparison.Ordinal))
        {
            throw new FormatException("Invalid FIGlet header. Expected signature 'flf2a'.");
        }

        var hardBlank = headerLine[5];

        var headerTail = headerLine.Slice(6);
        if (!TryReadNextToken(ref headerTail, out var heightToken) ||
            !TryReadNextToken(ref headerTail, out var baselineToken) ||
            !TryReadNextToken(ref headerTail, out var maxLengthToken) ||
            !TryReadNextToken(ref headerTail, out var oldLayoutToken) ||
            !TryReadNextToken(ref headerTail, out var commentLinesToken))
        {
            throw new FormatException("Invalid FIGlet header. Expected at least 5 numeric fields.");
        }

        var height = ParseInt(heightToken, "height");
        _ = ParseInt(baselineToken, "baseline"); // currently unused
        _ = ParseInt(maxLengthToken, "maxLength"); // currently unused
        _ = ParseInt(oldLayoutToken, "oldLayout"); // currently unused
        var commentLines = ParseInt(commentLinesToken, "commentLines");

        if (height <= 0)
        {
            throw new FormatException("Invalid FIGlet header: height must be > 0.");
        }

        if (commentLines < 0)
        {
            throw new FormatException("Invalid FIGlet header: commentLines must be >= 0.");
        }

        // Skip comment lines.
        for (var i = 0; i < commentLines; i++)
        {
            if (!reader.TryReadLine(out _))
            {
                throw new FormatException("Unexpected end of FIGlet font while reading comments.");
            }
        }

        // Determine endmark from the first glyph line.
        var firstGlyphLine = reader.ReadLineOrThrow("Unexpected end of FIGlet font while reading glyphs.");
        if (firstGlyphLine.IsEmpty)
        {
            throw new FormatException("Unexpected empty glyph line.");
        }

        var endMark = firstGlyphLine[^1];
        reader.PushBack(firstGlyphLine);

        var asciiGlyphs = new string[95][];
        for (var code = 32; code <= 126; code++)
        {
            asciiGlyphs[code - 32] = ReadGlyphLines(ref reader, height, endMark, hardBlank);
        }

        Dictionary<int, string[]>? codeTagged = null;
        while (reader.TryReadLine(out var line))
        {
            if (line.IsEmpty)
            {
                continue;
            }

            var codePoint = TryParseLeadingInt(line);
            if (codePoint is null)
            {
                // Unknown trailer; ignore.
                continue;
            }

            codeTagged ??= new Dictionary<int, string[]>();
            codeTagged[codePoint.Value] = ReadGlyphLines(ref reader, height, endMark, hardBlank);
        }

        return new FigletFont(name: null, height: height, hardBlank: hardBlank, asciiGlyphs: asciiGlyphs, codeTaggedGlyphs: codeTagged);
    }

    private static string[] ReadGlyphLines(ref LineReader reader, int height, char endMark, char hardBlank)
    {
        var lines = new string[height];

        for (var i = 0; i < height; i++)
        {
            var raw = i == 0
                ? reader.ReadLineOrThrow("Unexpected end of FIGlet glyph.")
                : reader.ReadLineOrThrow("Unexpected end of FIGlet glyph.");

            var trimmed = TrimEndMark(raw, endMark);
            if (hardBlank != ' ' && trimmed.IndexOf(hardBlank) >= 0)
            {
                trimmed = trimmed.Replace(hardBlank, ' ');
            }

            lines[i] = trimmed;
        }

        return lines;
    }

    private static string TrimEndMark(ReadOnlySpan<char> line, char endMark)
    {
        var end = line.Length;
        while (end > 0 && line[end - 1] == endMark)
        {
            end--;
        }

        // Also allow the common "double endmark" pattern on the last line.
        return new string(line.Slice(0, end));
    }

    private static bool TryReadNextToken(ref ReadOnlySpan<char> span, out ReadOnlySpan<char> token)
    {
        var i = 0;
        while (i < span.Length && span[i] == ' ')
        {
            i++;
        }

        if (i >= span.Length)
        {
            token = default;
            span = ReadOnlySpan<char>.Empty;
            return false;
        }

        var start = i;
        while (i < span.Length && span[i] != ' ')
        {
            i++;
        }

        token = span.Slice(start, i - start);
        span = i < span.Length ? span.Slice(i) : ReadOnlySpan<char>.Empty;
        return true;
    }

    private static int ParseInt(ReadOnlySpan<char> span, string name)
    {
        if (!int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"Invalid FIGlet header value for '{name}'.");
        }

        return value;
    }

    private static int? TryParseLeadingInt(ReadOnlySpan<char> line)
    {
        var i = 0;
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        var start = i;
        while (i < line.Length && char.IsDigit(line[i]))
        {
            i++;
        }

        if (i == start)
        {
            return null;
        }

        // Must be terminated by whitespace to avoid false positives on glyph lines.
        if (i < line.Length && line[i] != ' ')
        {
            return null;
        }

        if (!int.TryParse(line.Slice(start, i - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        return value;
    }

    private ref struct LineReader
    {
        private ReadOnlySpan<char> _text;
        private int _index;
        private ReadOnlySpan<char> _pushed;
        private bool _hasPushed;

        public LineReader(ReadOnlySpan<char> text)
        {
            _text = text;
            _index = 0;
            _pushed = default;
            _hasPushed = false;
        }

        public void PushBack(ReadOnlySpan<char> line)
        {
            if (_hasPushed)
            {
                throw new InvalidOperationException("Cannot push back multiple lines.");
            }

            _pushed = line;
            _hasPushed = true;
        }

        public bool TryReadLine(out ReadOnlySpan<char> line)
        {
            if (_hasPushed)
            {
                line = _pushed;
                _pushed = default;
                _hasPushed = false;
                return true;
            }

            if (_index >= _text.Length)
            {
                line = default;
                return false;
            }

            var start = _index;
            var end = _text.Slice(_index).IndexOf('\n');
            if (end < 0)
            {
                _index = _text.Length;
                line = TrimCr(_text.Slice(start));
                return true;
            }

            _index = start + end + 1;
            line = TrimCr(_text.Slice(start, end));
            return true;
        }

        public ReadOnlySpan<char> ReadLineOrThrow(string message)
        {
            if (!TryReadLine(out var line))
            {
                throw new FormatException(message);
            }
            return line;
        }

        private static ReadOnlySpan<char> TrimCr(ReadOnlySpan<char> line)
        {
            if (!line.IsEmpty && line[^1] == '\r')
            {
                return line[..^1];
            }
            return line;
        }
    }
}
