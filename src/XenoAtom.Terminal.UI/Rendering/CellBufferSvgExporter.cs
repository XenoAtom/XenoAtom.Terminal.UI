// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Exports a <see cref="CellBuffer"/> (or a cropped region) to an SVG fragment suitable for embedding in HTML pages.
/// </summary>
public static class CellBufferSvgExporter
{
    /// <summary>
    /// Exports the specified <paramref name="buffer"/> to an SVG string.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    /// <param name="options">The export options.</param>
    /// <returns>An SVG document as a string.</returns>
    public static string Export(CellBuffer buffer, CellBufferSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        options ??= CellBufferSvgExportOptions.Default;

        var crop = ResolveCrop(buffer, options);
        if (crop.Width <= 0 || crop.Height <= 0)
        {
            crop = new Rectangle(0, 0, Math.Max(1, buffer.Width), Math.Max(1, buffer.Height));
        }

        var cellWidth = Math.Max(1, options.CellWidthPx);
        var cellHeight = Math.Max(1, options.CellHeightPx);

        var svgWidth = crop.Width * cellWidth;
        var svgHeight = crop.Height * cellHeight;

        var sb = new StringBuilder(Math.Max(1024, (svgWidth * svgHeight) / 8));

        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ");
        sb.Append(svgWidth.ToString(CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(svgHeight.ToString(CultureInfo.InvariantCulture));
        sb.Append("\" width=\"");
        sb.Append(svgWidth.ToString(CultureInfo.InvariantCulture));
        sb.Append("\" height=\"");
        sb.Append(svgHeight.ToString(CultureInfo.InvariantCulture));
        sb.Append("\" shape-rendering=\"crispEdges\">");

        sb.Append("<style>");
        sb.Append(".t{font-family:");
        sb.Append(options.FontFamilyCss);
        sb.Append(";font-size:");
        sb.Append(cellHeight.ToString(CultureInfo.InvariantCulture));
        sb.Append("px;dominant-baseline:text-before-edge;white-space:pre;");
        // Ensure deterministic cell alignment across browsers/fallback fonts (avoid kerning/ligatures drift).
        sb.Append("font-variant-ligatures:none;font-kerning:none;");
        sb.Append("font-feature-settings:\"liga\" 0,\"calt\" 0;");
        sb.Append("letter-spacing:0}");
        sb.Append("</style>");

        var baseStyle = ResolveBaseStyle(buffer, crop, options);
        var baseBgCss = string.Empty;
        if (options.FillBackground && TryGetBackgroundColor(baseStyle, options, out baseBgCss))
        {
            sb.Append("<rect x=\"0\" y=\"0\" width=\"");
            sb.Append(svgWidth.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" height=\"");
            sb.Append(svgHeight.ToString(CultureInfo.InvariantCulture));
            sb.Append("\" fill=\"");
            sb.Append(baseBgCss);
            sb.Append("\"/>");
        }

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        Span<char> runeBuffer = stackalloc char[2];

        for (var row = 0; row < crop.Height; row++)
        {
            var y = crop.Y + row;
            if (y < 0 || y >= buffer.Height)
            {
                continue;
            }

            var rowBase = y * buffer.Width;
            var col = 0;
            while (col < crop.Width)
            {
                var x = crop.X + col;
                if (x < 0)
                {
                    col++;
                    continue;
                }

                if (x >= buffer.Width)
                {
                    break;
                }

                var i = rowBase + x;
                var cell = cells[i];
                if (cell.IsContinuation)
                {
                    col++;
                    continue;
                }

                var style = cell.WithoutContinuation();

                var scalar = scalars[i];
                var text = string.Empty;
                var w = 1;
                var hasInk = false;

                if (scalar < 0 && buffer.TryGetTextElement(scalar, out var textElement, out var elementWidth))
                {
                    text = textElement;
                    w = Math.Max(1, elementWidth);
                    hasInk = textElement.Length > 0;
                }
                else
                {
                    var value = scalar == 0 ? ' ' : scalar;
                    var rune = new Rune(value);
                    var written = rune.EncodeToUtf16(runeBuffer);
                    text = new string(runeBuffer[..written]);
                    w = Math.Max(1, TerminalTextUtility.GetRuneWidth(rune));
                    hasInk = value != ' ';
                }

                // Merge following cells if they share the same style and are not continuation cells.
                var runText = new StringBuilder(text.Length + 8);
                runText.Append(text);
                var runWidth = w;

                var nextCol = col + w;
                while (nextCol < crop.Width)
                {
                    var nx = crop.X + nextCol;
                    if ((uint)nx >= (uint)buffer.Width)
                    {
                        break;
                    }

                    var ni = rowBase + nx;
                    var nextCell = cells[ni];
                    if (nextCell.IsContinuation)
                    {
                        nextCol++;
                        continue;
                    }

                    var nextStyle = nextCell.WithoutContinuation();
                    if (nextStyle != style)
                    {
                        break;
                    }

                    var nextScalar = scalars[ni];
                    if (nextScalar < 0 && buffer.TryGetTextElement(nextScalar, out var nextElement, out var nextElementWidth))
                    {
                        runText.Append(nextElement);
                        runWidth += Math.Max(1, nextElementWidth);
                        nextCol += Math.Max(1, nextElementWidth);
                        hasInk |= nextElement.Length > 0;
                        continue;
                    }

                    var nextValue = nextScalar == 0 ? ' ' : nextScalar;
                    var nextRune = new Rune(nextValue);
                    var written = nextRune.EncodeToUtf16(runeBuffer);
                    runText.Append(runeBuffer[..written]);

                    var nextW = Math.Max(1, TerminalTextUtility.GetRuneWidth(nextRune));
                    runWidth += nextW;
                    nextCol += nextW;
                    hasInk |= nextValue != ' ';
                }

                var runXpx = col * cellWidth;
                var runYpx = row * cellHeight;
                var runWpx = runWidth * cellWidth;

                var hasBg = TryGetBackgroundColor(style, options, out var bgCss);
                var bgDiffersFromBase = hasBg && (!options.FillBackground || !StringComparer.Ordinal.Equals(bgCss, baseBgCss));

                // Background (only when it differs from the base background).
                if (bgDiffersFromBase)
                {
                    sb.Append("<rect x=\"");
                    sb.Append(runXpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" y=\"");
                    sb.Append(runYpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" width=\"");
                    sb.Append(runWpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" height=\"");
                    sb.Append(cellHeight.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" fill=\"");
                    sb.Append(bgCss);
                    sb.Append("\"/>");
                }

                // If the run contains only spaces and uses the base background, it is redundant: the base background rect already covers it.
                if (!hasInk && !bgDiffersFromBase)
                {
                    col = nextCol;
                    continue;
                }

                // Text (skip pure whitespace runs; backgrounds may still be meaningful)
                if (hasInk)
                {
                    var textCss = TryGetForegroundColor(style, options, out var fgCss) ? fgCss : options.DefaultForegroundCss;
                    var escaped = EscapeXml(runText.ToString());

                    sb.Append("<text class=\"t\" x=\"");
                    sb.Append(runXpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" y=\"");
                    sb.Append(runYpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" fill=\"");
                    sb.Append(textCss);
                    sb.Append("\"");
                    sb.Append(" textLength=\"");
                    sb.Append(runWpx.ToString(CultureInfo.InvariantCulture));
                    sb.Append("\" lengthAdjust=\"spacing\"");

                    var textStyle = style.TextStyle;
                    if ((textStyle & TextStyle.Bold) != 0)
                    {
                        sb.Append(" font-weight=\"700\"");
                    }
                    if ((textStyle & TextStyle.Italic) != 0)
                    {
                        sb.Append(" font-style=\"italic\"");
                    }
                    if ((textStyle & TextStyle.Dim) != 0)
                    {
                        sb.Append(" opacity=\"0.75\"");
                    }
                    if ((textStyle & TextStyle.Underline) != 0)
                    {
                        sb.Append(" text-decoration=\"underline\"");
                    }
                    if ((textStyle & TextStyle.Strikethrough) != 0)
                    {
                        sb.Append(" text-decoration=\"line-through\"");
                    }

                    sb.Append(" xml:space=\"preserve\">");
                    sb.Append(escaped);
                    sb.Append("</text>");
                }

                col = nextCol;
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static Rectangle ResolveCrop(CellBuffer buffer, CellBufferSvgExportOptions options)
    {
        var crop = options.Crop ?? new Rectangle(0, 0, buffer.Width, buffer.Height);
        crop = Clamp(crop, buffer.Width, buffer.Height);

        if (options.AutoCrop)
        {
            var auto = AutoCrop(buffer, crop, options);
            if (auto.Width > 0 && auto.Height > 0)
            {
                crop = auto;
            }
        }

        if (options.Padding != default)
        {
            crop = Inflate(crop, options.Padding);
            // Don't clamp after padding: screenshots often need a consistent 1-cell breathing room even when
            // the content touches the viewport edges (e.g. wide tables). Out-of-range cells are treated as empty.
        }

        return crop;
    }

    private static Rectangle AutoCrop(CellBuffer buffer, Rectangle crop, CellBufferSvgExportOptions options)
    {
        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        var baseStyle = ResolveBaseStyle(buffer, crop, options);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = crop.Y; y < crop.Bottom; y++)
        {
            if (y < 0 || y >= buffer.Height)
            {
                continue;
            }

            var rowBase = y * buffer.Width;
            for (var x = crop.X; x < crop.Right; x++)
            {
                if (x < 0 || x >= buffer.Width)
                {
                    continue;
                }

                var i = rowBase + x;
                var cell = cells[i];
                if (cell.IsContinuation)
                {
                    continue;
                }

                var style = cell.WithoutContinuation();
                var scalar = scalars[i];

                var isSpace = scalar == 0 || scalar == ' ';
                var isDefault = style == baseStyle;

                if (scalar < 0)
                {
                    isSpace = false;
                }

                if (isSpace && isDefault)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (minX == int.MaxValue)
        {
            return default;
        }

        return new Rectangle(minX, minY, Math.Max(1, (maxX - minX) + 1), Math.Max(1, (maxY - minY) + 1));
    }

    private static Style ResolveBaseStyle(CellBuffer buffer, Rectangle crop, CellBufferSvgExportOptions options)
    {
        if (options.BaseStyleOverride is { } s)
        {
            return s.WithoutContinuation();
        }

        // Prefer the most common "empty cell" style (space + non-continuation) within the crop area.
        // This is robust when the top-left cell contains text (e.g. a title line) but the background style
        // is what should drive auto-crop decisions.
        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        var counts = new Dictionary<Style, int>();
        for (var y = crop.Y; y < crop.Bottom; y++)
        {
            if (y < 0 || y >= buffer.Height)
            {
                continue;
            }

            var rowBase = y * buffer.Width;
            for (var x = crop.X; x < crop.Right; x++)
            {
                if (x < 0 || x >= buffer.Width)
                {
                    continue;
                }

                var i = rowBase + x;
                var cell = cells[i];
                if (cell.IsContinuation)
                {
                    continue;
                }

                var scalar = scalars[i];
                if (scalar < 0)
                {
                    continue;
                }

                if (scalar != 0 && scalar != ' ')
                {
                    continue;
                }

                var style = cell.WithoutContinuation();
                if (counts.TryGetValue(style, out var c))
                {
                    counts[style] = c + 1;
                }
                else
                {
                    counts.Add(style, 1);
                }
            }
        }

        if (counts.Count > 0)
        {
            var best = default(Style);
            var bestCount = -1;
            foreach (var (style, count) in counts)
            {
                if (count > bestCount)
                {
                    best = style;
                    bestCount = count;
                }
            }
            return best.WithoutContinuation();
        }

        // Fallback to the crop origin style.
        var x0 = Math.Clamp(crop.X, 0, Math.Max(0, buffer.Width - 1));
        var y0 = Math.Clamp(crop.Y, 0, Math.Max(0, buffer.Height - 1));
        var i0 = (y0 * buffer.Width) + x0;
        return cells[i0].WithoutContinuation();
    }

    private static bool TryGetForegroundColor(Style style, CellBufferSvgExportOptions options, out string css)
    {
        if (style.TryGetForeground(out var fg) && fg.Kind != ColorKind.Default)
        {
            css = ToCssColor(fg, options);
            return true;
        }

        css = string.Empty;
        return false;
    }

    private static bool TryGetBackgroundColor(Style style, CellBufferSvgExportOptions options, out string css)
    {
        if (style.TryGetBackground(out var bg) && bg.Kind != ColorKind.Default)
        {
            css = ToCssColor(bg, options);
            return true;
        }

        css = string.Empty;
        return false;
    }

    private static string ToCssColor(Color color, CellBufferSvgExportOptions options)
    {
        // In screenshots we want deterministic colors, so resolve palette colors to RGB using xterm palettes.
        if (color.Kind is ColorKind.Basic16 or ColorKind.Indexed256)
        {
            color = color.ToRgb();
        }

        if (color.Kind == ColorKind.RgbA && color.A < 255)
        {
            var a = (color.A / 255.0).ToString("0.###", CultureInfo.InvariantCulture);
            return $"rgba({color.R},{color.G},{color.B},{a})";
        }

        // Default to opaque RGB.
        return $"rgb({color.R},{color.G},{color.B})";
    }

    private static Rectangle Inflate(Rectangle rect, Thickness padding)
        => new(
            rect.X - Math.Max(0, padding.Left),
            rect.Y - Math.Max(0, padding.Top),
            rect.Width + Math.Max(0, padding.Left) + Math.Max(0, padding.Right),
            rect.Height + Math.Max(0, padding.Top) + Math.Max(0, padding.Bottom));

    private static Rectangle Clamp(Rectangle rect, int width, int height)
    {
        var x0 = Math.Clamp(rect.X, 0, width);
        var y0 = Math.Clamp(rect.Y, 0, height);
        var x1 = Math.Clamp(rect.Right, 0, width);
        var y1 = Math.Clamp(rect.Bottom, 0, height);
        return new Rectangle(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
    }

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Fast path: no special characters.
        var needsEscape = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch is '&' or '<' or '>' or '"' or '\'')
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 16);
        foreach (var ch in text)
        {
            sb.Append(ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => ch.ToString(),
            });
        }

        return sb.ToString();
    }
}

/// <summary>
/// Options for <see cref="CellBufferSvgExporter"/>.
/// </summary>
public sealed record CellBufferSvgExportOptions
{
    /// <summary>
    /// Gets the default export options.
    /// </summary>
    public static CellBufferSvgExportOptions Default { get; } = new();

    /// <summary>
    /// Gets the cell width in pixels.
    /// </summary>
    public int CellWidthPx { get; init; } = 9;

    /// <summary>
    /// Gets the cell height in pixels.
    /// </summary>
    public int CellHeightPx { get; init; } = 18;

    /// <summary>
    /// Gets the CSS font-family list to use in the SVG.
    /// </summary>
    public string FontFamilyCss { get; init; }
        = "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace";

    /// <summary>
    /// Gets the default foreground color CSS used when the buffer cell has no explicit foreground.
    /// </summary>
    public string DefaultForegroundCss { get; init; } = "rgb(220,220,220)";

    /// <summary>
    /// Gets an optional crop region (in cell coordinates).
    /// </summary>
    public Rectangle? Crop { get; init; }

    /// <summary>
    /// Gets an optional padding (in cells) applied around the crop region.
    /// </summary>
    public Thickness Padding { get; init; } = default;

    /// <summary>
    /// Gets a value indicating whether the exporter should auto-crop to non-empty content.
    /// </summary>
    public bool AutoCrop { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to paint a full background rectangle.
    /// </summary>
    public bool FillBackground { get; init; } = true;

    /// <summary>
    /// Gets an optional style to use as the “base” style for auto-crop and background decisions.
    /// </summary>
    public Style? BaseStyleOverride { get; init; }
}
