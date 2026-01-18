// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Figlet;

/// <summary>
/// Represents a FIGlet font (typically loaded from a <c>.flf</c> file).
/// </summary>
/// <remarks>
/// <para>
/// FIGlet fonts define a fixed <see cref="Height"/> and a set of glyphs for characters.
/// Rendering concatenates the glyph lines for each input character to produce a multi-line text banner.
/// </para>
/// <para>
/// This implementation focuses on the common FIGlet <c>.flf</c> format and supports:
/// </para>
/// <list type="bullet">
/// <item><description>Standard ASCII glyphs (code points 32..126).</description></item>
/// <item><description>Optional code-tagged glyphs (additional code points).</description></item>
/// </list>
/// </remarks>
public sealed partial class FigletFont
{
    private readonly string[][] _asciiGlyphs;
    private readonly Dictionary<int, string[]>? _codeTaggedGlyphs;

    internal FigletFont(FigletFontInfo? fontInfo, int height, char hardBlank, string[][] asciiGlyphs, Dictionary<int, string[]>? codeTaggedGlyphs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Info = fontInfo;
        Height = height;
        HardBlank = hardBlank;
        _asciiGlyphs = asciiGlyphs;
        _codeTaggedGlyphs = codeTaggedGlyphs;
    }

    /// <summary>
    /// Gets optional information about the loaded FIGlet font (name, author, URL).
    /// </summary>
    public FigletFontInfo? Info { get; }

    /// <summary>
    /// Gets the glyph height in rows.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the FIGlet hardblank character from the font header.
    /// </summary>
    /// <remarks>
    /// Hardblanks are converted to spaces when glyph lines are loaded.
    /// </remarks>
    public char HardBlank { get; }

    /// <inheritdoc/>
    public override string ToString() => Info?.Name ?? "UnknownFont";

    /// <summary>
    /// Parses a FIGlet font from a string.
    /// </summary>
    /// <param name="text">The font text.</param>
    /// <param name="fontInfo">The optional font info.</param>
    public static FigletFont Parse(string text, FigletFontInfo? fontInfo = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Parse(text.AsSpan(), fontInfo);
    }

    /// <summary>
    /// Parses a FIGlet font from a character span.
    /// </summary>
    /// <param name="text">The font text.</param>
    /// <param name="fontInfo">The optional font info.</param>
    public static FigletFont Parse(ReadOnlySpan<char> text, FigletFontInfo? fontInfo = null)
        => FigletFontParser.Parse(text, fontInfo);

    /// <summary>
    /// Loads a Figlet font from the specified file path.
    /// </summary>
    /// <param name="path">The path to the Figlet font file to load. The file must be encoded in UTF-8. Cannot be null.</param>
    /// <returns>A FigletFont instance representing the font loaded from the specified file.</returns>
    /// <param name="encoding">The encoding used to read the stream. Default is UTF8.</param>
    /// <param name="fontInfo">The optional font info.</param>
    public static FigletFont Load(string path, Encoding? encoding = null, FigletFontInfo? fontInfo = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        var content = File.ReadAllText(path, encoding ?? Encoding.UTF8);
        return Parse(content.AsSpan(), fontInfo);
    }

    /// <summary>
    /// Loads a FIGlet font from a stream.
    /// </summary>
    /// <param name="stream">The font stream.</param>
    /// <param name="encoding">The encoding used to read the stream.</param>
    /// <param name="fontInfo">The optional font info.</param>
    public static FigletFont Load(Stream stream, Encoding? encoding = null, FigletFontInfo? fontInfo = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        encoding ??= Encoding.UTF8;
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        var content = reader.ReadToEnd();
        return Parse(content.AsSpan(), fontInfo);
    }

    /// <summary>
    /// Creates a simple built-in block font where each glyph is a rectangle of the input character.
    /// </summary>
    /// <param name="height">Glyph height.</param>
    /// <param name="width">Glyph width.</param>
    public static FigletFont CreateBlockFont(int height = 4, int width = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        var ascii = new string[95][];
        for (var c = 32; c <= 126; c++)
        {
            var ch = (char)c;
            var lines = new string[height];
            var fill = ch == ' ' ? ' ' : ch;
            var line = new string(fill, width);
            for (var i = 0; i < height; i++)
            {
                lines[i] = line;
            }
            ascii[c - 32] = lines;
        }

        return new FigletFont(new($"Block-{width}x{height}"), height, ' ', asciiGlyphs: ascii, codeTaggedGlyphs: null);
    }

    /// <summary>
    /// Gets a built-in block font suitable for demos and basic banners.
    /// </summary>
    public static FigletFont Block => FigletFontBlockHolder.Instance;

    /// <summary>
    /// Retrieves a list of predefined Figlet fonts available for use.
    /// </summary>
    /// <returns>A list of <see cref="FigletFont"/> objects representing the predefined fonts. The list will be empty if no
    /// predefined fonts are available.</returns>
    public static List<FigletFont> GetPredefinedFonts() =>
    [
        Banner3D,
        Big,
        Block,
        Bubble,
        BulbHead,
        CyberLarge,
        CyberMedium,
        Digital,
        Doh,
        Doom,
        DotMatrix,
        Isometric1,
        Isometric2,
        Isometric3,
        Lcd,
        Ogre,
        Shadow,
        Slant,
        Small,
        Smslant,
        Standard,
        ThreeD,
        ThreeXFive
    ];

    // NativeAOT lazy initialization pattern
    private static class FigletFontBlockHolder
    {
        public static readonly FigletFont Instance = CreateBlockFont();
    }

    /// <summary>
    /// Tries to get glyph lines for the specified Unicode code point.
    /// </summary>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="lines">The glyph lines (one string per row).</param>
    /// <returns><c>true</c> if the glyph is defined; otherwise <c>false</c>.</returns>
    public bool TryGetGlyph(int codePoint, out string[] lines)
    {
        if (codePoint >= 32 && codePoint <= 126)
        {
            lines = _asciiGlyphs[codePoint - 32];
            return true;
        }

        if (_codeTaggedGlyphs is not null && _codeTaggedGlyphs.TryGetValue(codePoint, out lines!))
        {
            return true;
        }

        lines = Array.Empty<string>();
        return false;
    }

    /// <summary>
    /// Renders the specified text into FIGlet lines.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="options">Rendering options.</param>
    /// <returns>An array of lines of length <see cref="Height"/> (or empty if the input is empty).</returns>
    public string[] RenderLines(string? text, FigletRenderOptions options = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return RenderLines(text.AsSpan(), options);
    }

    /// <summary>
    /// Renders the specified text span into FIGlet lines.
    /// </summary>
    public string[] RenderLines(ReadOnlySpan<char> text, FigletRenderOptions options = default)
    {
        if (text.IsEmpty)
        {
            return Array.Empty<string>();
        }

        var spacing = Math.Max(0, options.LetterSpacing);

        var builders = new StringBuilder[Height];
        for (var i = 0; i < builders.Length; i++)
        {
            builders[i] = new StringBuilder(text.Length * 4);
        }

        var wroteAny = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                continue;
            }

            if (ch == '\n')
            {
                // For now, treat newlines as spaces to avoid multi-paragraph rendering in v1.
                // Users can render multiple figlets by splitting input and stacking controls.
                ch = ' ';
            }

            var codePoint = ch;
            if (!TryGetGlyph(codePoint, out var glyph))
            {
                TryGetGlyph(options.MissingGlyph, out glyph);
            }

            if (!wroteAny)
            {
                wroteAny = true;
            }
            else if (spacing > 0)
            {
                for (var row = 0; row < Height; row++)
                {
                    builders[row].Append(' ', spacing);
                }
            }

            for (var row = 0; row < Height; row++)
            {
                var line = row < glyph.Length ? glyph[row] : string.Empty;
                builders[row].Append(line);
            }
        }

        if (!wroteAny)
        {
            return Array.Empty<string>();
        }

        var result = new string[Height];
        for (var row = 0; row < Height; row++)
        {
            var s = builders[row].ToString();
            if (options.TrimTrailingSpaces)
            {
                s = s.TrimEnd(' ');
            }
            result[row] = s;
        }

        return result;
    }
}