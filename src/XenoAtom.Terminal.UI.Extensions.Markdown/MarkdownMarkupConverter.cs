// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;
using CellStyle = XenoAtom.Terminal.UI.Style;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

/// <summary>
/// Converts Markdown text into ANSI markup text consumable by <see cref="Controls.Markup"/>.
/// </summary>
/// <remarks>
/// This converter keeps an internal reusable buffer to reduce allocations across repeated conversions.
/// </remarks>
public sealed class MarkdownMarkupConverter
{
    private readonly StringBuilder _buffer;
    private readonly List<StyledRun> _sourceRuns;
    private Theme _theme;
    private MarkdownRenderOptions _renderOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownMarkupConverter"/> class.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the internal reusable buffer.</param>
    public MarkdownMarkupConverter(int initialCapacity = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _buffer = new StringBuilder(initialCapacity);
        _sourceRuns = new List<StyledRun>(128);
        _theme = Theme.Default;
        _renderOptions = MarkdownRenderOptions.Default;
    }

    /// <summary>
    /// Gets or sets the markdown pipeline used for conversion.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the default markdown pipeline from <see cref="MarkdownDefaults.Pipeline"/> is used.
    /// </remarks>
    public MarkdownPipeline? Pipeline { get; set; }

    /// <summary>
    /// Gets or sets the markdown pipeline used by source-preserving highlighting APIs.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, <see cref="MarkdownDefaults.PreciseSourcePipeline"/> is used.
    /// For user-defined pipelines, call <c>UsePreciseSourceLocation()</c> on the pipeline builder
    /// so inline source spans are precise.
    /// </remarks>
    public MarkdownPipeline? SourcePipeline { get; set; }

    /// <summary>
    /// Gets or sets the markdown render options.
    /// </summary>
    public MarkdownRenderOptions RenderOptions
    {
        get => _renderOptions;
        set => _renderOptions = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the theme used to resolve default markdown style colors.
    /// </summary>
    public Theme Theme
    {
        get => _theme;
        set => _theme = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets optional markdown style overrides.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, <see cref="MarkdownStyle.Default"/> is used.
    /// </remarks>
    public MarkdownStyle? Style { get; set; }

    /// <summary>
    /// Gets or sets an optional base URI used to resolve relative links.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// Converts markdown text to ANSI markup text.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <returns>The converted ANSI markup text.</returns>
    public string Convert(string? markdown)
    {
        _buffer.Clear();
        Convert(markdown, _buffer);
        return _buffer.ToString();
    }

    /// <summary>
    /// Converts markdown text to ANSI markup text and appends the result to <paramref name="destination"/>.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <param name="destination">The destination builder receiving converted markup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    public void Convert(string? markdown, StringBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var sourceStyle = Style ?? MarkdownStyle.Default;
        var resolvedStyle = MarkdownDefaults.ResolveStyle(Theme, sourceStyle);
        var pipeline = Pipeline ?? MarkdownDefaults.Pipeline;
        var document = Markdig.Markdown.Parse(markdown ?? string.Empty, pipeline);
        var renderer = new Renderer(destination, resolvedStyle, RenderOptions, BaseUri, Theme);
        renderer.Render(document);
    }

    /// <summary>
    /// Produces syntax highlighting style runs over the original markdown source text.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <returns>The style runs mapped to the original source indices.</returns>
    public StyledRun[] Highlight(string? markdown)
    {
        _sourceRuns.Clear();
        Highlight(markdown, _sourceRuns);
        return _sourceRuns.Count == 0 ? Array.Empty<StyledRun>() : _sourceRuns.ToArray();
    }

    /// <summary>
    /// Produces syntax highlighting style runs over the original markdown source text.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <param name="destination">The destination list receiving style runs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    public void Highlight(string? markdown, List<StyledRun> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();

        var source = markdown ?? string.Empty;
        if (source.Length == 0)
        {
            return;
        }

        var sourceStyle = Style ?? MarkdownStyle.Default;
        var resolvedStyle = MarkdownDefaults.ResolveStyle(Theme, sourceStyle);
        var pipeline = SourcePipeline ?? Pipeline ?? MarkdownDefaults.PreciseSourcePipeline;
        var document = Markdig.Markdown.Parse(source, pipeline);

        var collector = new SourceStyleCollector(resolvedStyle, RenderOptions);
        collector.Collect(source, document, destination);
    }

    /// <summary>
    /// Converts markdown to ANSI markup while preserving the exact original markdown character sequence.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <returns>ANSI markup with styles mapped to the original markdown source.</returns>
    public string ConvertPreservingSource(string? markdown)
    {
        _buffer.Clear();
        ConvertPreservingSource(markdown, _buffer);
        return _buffer.ToString();
    }

    /// <summary>
    /// Converts markdown to ANSI markup while preserving the exact original markdown character sequence and appends to <paramref name="destination"/>.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <param name="destination">The destination builder receiving converted markup.</param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    public void ConvertPreservingSource(string? markdown, StringBuilder destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var source = markdown ?? string.Empty;
        if (source.Length == 0)
        {
            return;
        }

        _sourceRuns.Clear();
        Highlight(source, _sourceRuns);

        var sourceStyle = Style ?? MarkdownStyle.Default;
        var resolvedStyle = MarkdownDefaults.ResolveStyle(Theme, sourceStyle);
        var renderer = new Renderer(destination, resolvedStyle, RenderOptions, BaseUri, Theme);
        renderer.RenderPreservedText(source, _sourceRuns);
    }

    private sealed class Renderer
    {
        private static readonly string[] HeadingPrefixes = ["", "# ", "## ", "### ", "#### ", "##### ", "###### "];

        private readonly StringBuilder _builder;
        private readonly MarkdownStyle _style;
        private readonly MarkdownRenderOptions _options;
        private readonly Uri? _baseUri;
        private readonly Color _themeBackground;

        public Renderer(StringBuilder builder, MarkdownStyle style, MarkdownRenderOptions options, Uri? baseUri, Theme theme)
        {
            _builder = builder;
            _style = style;
            _options = options;
            _baseUri = baseUri;
            _themeBackground = ResolveThemeBackground(theme);
        }

        public void Render(MarkdownDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            RenderBlocks(document, indent: 0, quotePrefix: null, separatorLines: 2);
        }

        public void RenderPreservedText(string source, IReadOnlyList<StyledRun> runs)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(runs);

            var position = 0;
            for (var index = 0; index < runs.Count; index++)
            {
                var run = runs[index];
                if (run.Length <= 0)
                {
                    continue;
                }

                var runStart = Math.Clamp(run.Start, 0, source.Length);
                var runEnd = Math.Clamp(run.Start + run.Length, 0, source.Length);
                if (runEnd <= runStart)
                {
                    continue;
                }

                if (runStart > position)
                {
                    AppendEscaped(source.AsSpan(position, runStart - position));
                }

                AppendStyledSpan(source.AsSpan(runStart, runEnd - runStart), run.Style);
                position = runEnd;
            }

            if (position < source.Length)
            {
                AppendEscaped(source.AsSpan(position));
            }
        }

        private void RenderBlocks(ContainerBlock container, int indent, string? quotePrefix, int separatorLines)
        {
            var first = true;
            foreach (var block in container)
            {
                if (!first)
                {
                    AppendNewLines(separatorLines);
                }

                RenderBlock(block, indent, quotePrefix);
                first = false;
            }
        }

        private void RenderBlock(Block block, int indent, string? quotePrefix)
        {
            switch (block)
            {
                case AlertBlock alert:
                    RenderAlert(alert, indent, quotePrefix);
                    return;

                case HeadingBlock heading:
                    RenderHeading(heading, indent, quotePrefix);
                    return;

                case ParagraphBlock paragraph:
                    RenderParagraphBlock(paragraph, _style.ParagraphStyle, indent, quotePrefix, linePrefix: quotePrefix, continuationPrefix: quotePrefix);
                    return;

                case ListBlock list:
                    RenderList(list, indent, quotePrefix);
                    return;

                case QuoteBlock quote:
                    RenderBlocks(quote, indent, AppendPrefix(quotePrefix, _style.QuotePrefix), separatorLines: 1);
                    return;

                case Markdig.Extensions.Tables.Table table:
                    RenderTable(table, indent, quotePrefix);
                    return;

                case FencedCodeBlock fencedCode:
                    RenderCodeBlock(fencedCode, indent, quotePrefix);
                    return;

                case CodeBlock codeBlock:
                    RenderCodeBlock(codeBlock, indent, quotePrefix);
                    return;

                case ThematicBreakBlock:
                    RenderPlainLine("────────────────────────────────────────", _style.QuotePrefixStyle, indent, quotePrefix, quotePrefix);
                    return;

                case HtmlBlock htmlBlock when _options.RenderHtmlBlocksAsText:
                    RenderParagraphBlock(htmlBlock, _style.HtmlStyle, indent, quotePrefix, linePrefix: quotePrefix, continuationPrefix: quotePrefix);
                    return;

                case HtmlBlock:
                    return;

                case LeafBlock leaf:
                    RenderParagraphBlock(leaf, _style.ParagraphStyle, indent, quotePrefix, linePrefix: quotePrefix, continuationPrefix: quotePrefix);
                    return;

                case ContainerBlock nested:
                    RenderBlocks(nested, indent, quotePrefix, separatorLines: 1);
                    return;
            }
        }

        private void RenderHeading(HeadingBlock heading, int indent, string? quotePrefix)
        {
            var headingLevel = Math.Clamp(heading.Level, 1, 6);
            var inlineResult = RenderLeafInline(heading, _style.ResolveHeadingStyle(headingLevel));
            WriteParagraph(
                inlineResult,
                indent,
                linePrefix: AppendPrefix(quotePrefix, HeadingPrefixes[headingLevel]),
                continuationPrefix: AppendPrefix(quotePrefix, new string(' ', HeadingPrefixes[headingLevel].Length)),
                prefixStyle: quotePrefix is not null ? _style.QuotePrefixStyle : CellStyle.None);
        }

        private void RenderParagraphBlock(LeafBlock paragraph, CellStyle paragraphStyle, int indent, string? quotePrefix, string? linePrefix, string? continuationPrefix)
        {
            var inlineResult = RenderLeafInline(paragraph, paragraphStyle);
            WriteParagraph(
                inlineResult,
                indent,
                linePrefix,
                continuationPrefix,
                quotePrefix is not null ? _style.QuotePrefixStyle : CellStyle.None);
        }

        private void RenderList(ListBlock list, int indent, string? quotePrefix)
        {
            var ordered = TryParseInt(list.OrderedStart, out var startValue) ? startValue : 1;
            var orderedDelimiter = list.OrderedDelimiter == default ? '.' : list.OrderedDelimiter;

            var firstItem = true;
            foreach (var child in list)
            {
                if (child is not ListItemBlock item)
                {
                    continue;
                }

                if (!firstItem)
                {
                    AppendNewLines(1);
                }

                var bullet = list.IsOrdered ? $"{ordered}{orderedDelimiter}" : _style.UnorderedListBullet;
                if (list.IsOrdered)
                {
                    ordered++;
                }

                RenderListItem(item, indent, quotePrefix, bullet);
                firstItem = false;
            }
        }

        private void RenderListItem(ListItemBlock item, int indent, string? quotePrefix, string bullet)
        {
            var bulletPrefix = string.Concat(bullet, " ");
            var bulletWidth = Math.Max(1, GetTextWidth(bulletPrefix));
            var continuationPadding = new string(' ', bulletWidth);
            var firstParagraphPrefix = AppendPrefix(quotePrefix, bulletPrefix);
            var continuationPrefix = AppendPrefix(quotePrefix, continuationPadding);

            var emittedAny = false;
            var consumedMarker = false;

            foreach (var child in item)
            {
                switch (child)
                {
                    case ParagraphBlock paragraph:
                    {
                        var prefix = consumedMarker ? continuationPrefix : firstParagraphPrefix;
                        RenderParagraphBlock(paragraph, _style.ParagraphStyle, indent, quotePrefix, prefix, continuationPrefix);
                        consumedMarker = true;
                        emittedAny = true;
                        break;
                    }

                    case ListBlock nestedList:
                    {
                        if (!consumedMarker)
                        {
                            RenderPlainLine(string.Empty, CellStyle.None, indent, firstParagraphPrefix, continuationPrefix);
                        }

                        RenderList(nestedList, indent + bulletWidth, quotePrefix);
                        consumedMarker = true;
                        emittedAny = true;
                        break;
                    }

                    default:
                    {
                        if (!consumedMarker)
                        {
                            RenderPlainLine(string.Empty, CellStyle.None, indent, firstParagraphPrefix, continuationPrefix);
                        }

                        RenderBlock(child, indent + bulletWidth, quotePrefix);
                        consumedMarker = true;
                        emittedAny = true;
                        break;
                    }
                }
            }

            if (!emittedAny)
            {
                RenderPlainLine(string.Empty, CellStyle.None, indent, firstParagraphPrefix, continuationPrefix);
            }
        }

        private void RenderAlert(AlertBlock alert, int indent, string? quotePrefix)
        {
            var kind = alert.Kind.ToString().ToUpperInvariant();
            var alertStyle = _style.ResolveAlertStyle(kind);
            var alertPrefix = AppendPrefix(quotePrefix, "!");
            var alertPrefixStyle = alertStyle.BorderStyle | _style.QuotePrefixStyle;

            var titleResult = new InlineRenderResult(kind, [new StyledRun(0, kind.Length, alertStyle.TitleStyle)]);
            WriteParagraph(titleResult, indent, alertPrefix, alertPrefix, alertPrefixStyle);

            var body = ExtractContainerText(alert).Trim();
            if (body.Length > 0)
            {
                AppendNewLines(1);
                var bodyResult = new InlineRenderResult(body, [new StyledRun(0, body.Length, _style.ParagraphStyle | alertStyle.BackgroundStyle)]);
                var bodyPrefix = AppendPrefix(quotePrefix, "  ");
                WriteParagraph(bodyResult, indent, bodyPrefix, bodyPrefix, alertPrefixStyle);
            }
        }

        private void RenderCodeBlock(CodeBlock block, int indent, string? quotePrefix)
        {
            var code = NormalizeLeafText(block);
            if (block is FencedCodeBlock fenced && !string.IsNullOrWhiteSpace(fenced.Info))
            {
                RenderPlainLine(fenced.Info!.Trim(), _style.ResolveHeadingStyle(6), indent, quotePrefix, quotePrefix);
                if (code.Length > 0)
                {
                    AppendNewLines(1);
                }
            }

            var lines = code.Split('\n');
            var maxCodeBlockHeight = Math.Max(0, _options.MaxCodeBlockHeight);
            var limit = maxCodeBlockHeight > 0 ? Math.Min(lines.Length, maxCodeBlockHeight) : lines.Length;
            for (var index = 0; index < limit; index++)
            {
                if (index > 0)
                {
                    AppendNewLines(1);
                }

                RenderPlainLine(lines[index], _style.InlineCodeStyle, indent, quotePrefix, quotePrefix);
            }

            if (limit < lines.Length)
            {
                AppendNewLines(1);
                RenderPlainLine("…", _style.HtmlStyle, indent, quotePrefix, quotePrefix);
            }
        }

        private void RenderTable(Markdig.Extensions.Tables.Table table, int indent, string? quotePrefix)
        {
            var rowIndex = 0;
            foreach (var rowBlock in table)
            {
                if (rowBlock is not TableRow row)
                {
                    continue;
                }

                if (rowIndex > 0)
                {
                    AppendNewLines(1);
                }

                var firstCell = true;
                AppendIndentAndPrefix(indent, quotePrefix, quotePrefix, quotePrefix is not null ? _style.QuotePrefixStyle : CellStyle.None);

                foreach (var cellBlock in row)
                {
                    if (!firstCell)
                    {
                        AppendEscaped(" | ");
                    }

                    var cell = cellBlock as TableCell;
                    var cellResult = CreateTableCellInlineResult(cell, row.IsHeader ? _style.StrongStyle : _style.ParagraphStyle);
                    AppendInlineRange(cellResult.Text, cellResult.Runs, 0, cellResult.Text.Length);
                    firstCell = false;
                }

                rowIndex++;
            }
        }

        private InlineRenderResult CreateTableCellInlineResult(TableCell? cell, CellStyle style)
        {
            if (cell is null || cell.Count == 0)
            {
                return new InlineRenderResult(string.Empty, Array.Empty<StyledRun>());
            }

            if (cell.Count == 1 && cell[0] is LeafBlock leaf)
            {
                return RenderLeafInline(leaf, style);
            }

            var plain = ExtractContainerText(cell);
            return plain.Length == 0
                ? new InlineRenderResult(string.Empty, Array.Empty<StyledRun>())
                : new InlineRenderResult(plain, [new StyledRun(0, plain.Length, style)]);
        }

        private void RenderPlainLine(string text, CellStyle style, int indent, string? linePrefix, string? continuationPrefix)
        {
            var runs = style == CellStyle.None || text.Length == 0 ? Array.Empty<StyledRun>() : [new StyledRun(0, text.Length, style)];
            var result = new InlineRenderResult(text, runs);
            WriteParagraph(result, indent, linePrefix, continuationPrefix, linePrefix is not null ? _style.QuotePrefixStyle : CellStyle.None);
        }

        private void WriteParagraph(InlineRenderResult result, int indent, string? linePrefix, string? continuationPrefix, CellStyle prefixStyle)
        {
            var text = result.Text ?? string.Empty;
            var runs = result.Runs ?? Array.Empty<StyledRun>();

            var lineStart = 0;
            var firstLine = true;
            while (lineStart <= text.Length)
            {
                var lineEnd = lineStart;
                while (lineEnd < text.Length && text[lineEnd] != '\n')
                {
                    lineEnd++;
                }

                var prefix = firstLine ? linePrefix : continuationPrefix;
                AppendIndentAndPrefix(indent, prefix, continuationPrefix, prefixStyle);
                AppendInlineRange(text, runs, lineStart, lineEnd - lineStart);

                if (lineEnd >= text.Length)
                {
                    break;
                }

                AppendNewLines(1);
                lineStart = lineEnd + 1;
                firstLine = false;
            }
        }

        private void AppendIndentAndPrefix(int indent, string? linePrefix, string? continuationPrefix, CellStyle prefixStyle)
        {
            if (indent > 0)
            {
                _builder.Append(' ', indent);
            }

            if (!string.IsNullOrEmpty(linePrefix))
            {
                AppendStyledSpan(linePrefix.AsSpan(), prefixStyle);
            }
            else if (!string.IsNullOrEmpty(continuationPrefix))
            {
                _builder.Append(' ', GetTextWidth(continuationPrefix));
            }
        }

        private void AppendInlineRange(string text, StyledRun[] runs, int start, int length)
        {
            if (length <= 0)
            {
                return;
            }

            var end = start + length;
            var runIndex = 0;
            while (runIndex < runs.Length && runs[runIndex].Start + runs[runIndex].Length <= start)
            {
                runIndex++;
            }

            var position = start;
            while (position < end)
            {
                CellStyle style = CellStyle.None;
                var nextBoundary = end;

                if (runIndex < runs.Length)
                {
                    var run = runs[runIndex];
                    var runStart = run.Start;
                    var runEnd = run.Start + run.Length;
                    if (runStart <= position && runEnd > position)
                    {
                        style = run.Style;
                        nextBoundary = Math.Min(end, runEnd);
                    }
                    else
                    {
                        nextBoundary = Math.Min(end, runStart);
                    }
                }

                if (nextBoundary <= position)
                {
                    nextBoundary = position + 1;
                }

                AppendStyledSpan(text.AsSpan(position, nextBoundary - position), style);
                position = nextBoundary;

                while (runIndex < runs.Length && runs[runIndex].Start + runs[runIndex].Length <= position)
                {
                    runIndex++;
                }
            }
        }

        private void AppendStyledSpan(ReadOnlySpan<char> span, CellStyle style)
        {
            if (span.IsEmpty)
            {
                return;
            }

            var hasStyle = TryAppendOpenStyle(style);
            AppendEscaped(span);
            if (hasStyle)
            {
                _builder.Append("[/]");
            }
        }

        private bool TryAppendOpenStyle(CellStyle style)
        {
            if (style == CellStyle.None)
            {
                return false;
            }

            var startLength = _builder.Length;
            _builder.Append('[');
            var first = true;

            var textStyle = style.TextStyle;
            AppendStyleToken(ref first, textStyle, TextStyle.Bold, "bold");
            AppendStyleToken(ref first, textStyle, TextStyle.Dim, "dim");
            AppendStyleToken(ref first, textStyle, TextStyle.Italic, "italic");
            AppendStyleToken(ref first, textStyle, TextStyle.Underline, "underline");
            AppendStyleToken(ref first, textStyle, TextStyle.Blink, "blink");
            AppendStyleToken(ref first, textStyle, TextStyle.Invert, "invert");
            AppendStyleToken(ref first, textStyle, TextStyle.Hidden, "hidden");
            AppendStyleToken(ref first, textStyle, TextStyle.Strikethrough, "strikethrough");

            Color? resolvedBackground = null;
            if (style.TryGetBackground(out var bg) && TryResolveBackgroundForMarkup(bg, _themeBackground, out var resolvedBg))
            {
                resolvedBackground = resolvedBg;
            }

            if (style.TryGetForeground(out var fg) && TryResolveForegroundForMarkup(fg, resolvedBackground ?? _themeBackground, out var resolvedFg))
            {
                AppendToken(ref first);
                AppendColorToken(resolvedFg);
            }

            if (resolvedBackground is { } background)
            {
                AppendToken(ref first, "on");
                AppendToken(ref first);
                AppendColorToken(background);
            }

            if (first)
            {
                _builder.Length = startLength;
                return false;
            }

            _builder.Append(']');
            return true;
        }

        private void AppendStyleToken(ref bool first, TextStyle value, TextStyle flag, string token)
        {
            if ((value & flag) == 0)
            {
                return;
            }

            AppendToken(ref first, token);
        }

        private void AppendToken(ref bool first, string token)
        {
            AppendToken(ref first);
            _builder.Append(token);
        }

        private void AppendToken(ref bool first)
        {
            if (!first)
            {
                _builder.Append(' ');
            }

            first = false;
        }

        private void AppendColorToken(Color color)
        {
            color = color.ToRgb();
            _builder.Append('#');
            AppendHexByte(color.R);
            AppendHexByte(color.G);
            AppendHexByte(color.B);
        }

        private void AppendHexByte(byte value)
        {
            _builder.Append(GetHex(value >> 4));
            _builder.Append(GetHex(value & 0xF));
        }

        private static char GetHex(int value)
            => (char)(value < 10 ? '0' + value : 'a' + (value - 10));

        private static bool TryResolveBackgroundForMarkup(Color color, Color themeBackground, out Color resolved)
        {
            if (color.Kind == ColorKind.Default)
            {
                resolved = default;
                return false;
            }

            resolved = ResolveToRgb(color, themeBackground);
            return resolved.Kind == ColorKind.Rgb;
        }

        private static bool TryResolveForegroundForMarkup(Color color, Color fallbackBackground, out Color resolved)
        {
            if (color.Kind == ColorKind.Default)
            {
                resolved = default;
                return false;
            }

            resolved = ResolveToRgb(color, fallbackBackground);
            return resolved.Kind == ColorKind.Rgb;
        }

        private static Color ResolveThemeBackground(Theme theme)
        {
            var background = theme.Background ?? Color.Default;
            return ResolveToRgb(background, Colors.TerminalBlack.ToRgb());
        }

        private static Color ResolveToRgb(Color color, Color destination)
        {
            return color.Kind switch
            {
                ColorKind.Rgb => color,
                ColorKind.Basic16 or ColorKind.Indexed256 => color.ToRgb(),
                ColorKind.RgbA => ResolveRgbaToRgb(color, destination),
                _ => destination.Kind == ColorKind.Rgb ? destination : Colors.TerminalBlack.ToRgb(),
            };
        }

        private static Color ResolveRgbaToRgb(Color color, Color destination)
        {
            if (color.Kind != ColorKind.RgbA)
            {
                return ResolveToRgb(color, destination);
            }

            if (color.A == 0)
            {
                return ResolveToRgb(destination, Colors.TerminalBlack.ToRgb());
            }

            if (color.A >= byte.MaxValue)
            {
                return Color.Rgb(color.R, color.G, color.B);
            }

            var alpha = color.A / 255f;
            var baseColor = ResolveToRgb(destination, Colors.TerminalBlack.ToRgb());
            return baseColor.Mix(Color.Rgb(color.R, color.G, color.B), alpha, ColorMixSpace.LinearRgb);
        }

        private void AppendEscaped(string text)
            => AppendEscaped(text.AsSpan());

        private void AppendEscaped(ReadOnlySpan<char> text)
        {
            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c != '[' && c != ']')
                {
                    continue;
                }

                if (i > start)
                {
                    _builder.Append(text.Slice(start, i - start));
                }

                _builder.Append(c);
                _builder.Append(c);
                start = i + 1;
            }

            if (start < text.Length)
            {
                _builder.Append(text.Slice(start));
            }
        }

        private void AppendNewLines(int count)
        {
            if (count <= 0)
            {
                return;
            }

            _builder.Append('\n', count);
        }

        private InlineRenderResult RenderLeafInline(LeafBlock leaf, CellStyle baseStyle)
        {
            var inline = leaf.Inline;
            if (inline is null)
            {
                var text = NormalizeLeafText(leaf);
                if (text.Length == 0)
                {
                    return new InlineRenderResult(string.Empty, Array.Empty<StyledRun>());
                }

                var runs = baseStyle == CellStyle.None ? Array.Empty<StyledRun>() : [new StyledRun(0, text.Length, baseStyle)];
                return new InlineRenderResult(text, runs);
            }

            var accumulator = new InlineAccumulator(this);
            accumulator.AppendContainer(inline, baseStyle);
            return accumulator.ToResult();
        }

        private static string NormalizeLeafText(LeafBlock leaf)
        {
            var text = leaf.Lines.ToString();
            return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        }

        private static string AppendPrefix(string? prefix, string? extra)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return extra ?? string.Empty;
            }

            if (string.IsNullOrEmpty(extra))
            {
                return prefix;
            }

            return string.Concat(prefix, extra);
        }

        private static bool TryParseInt(string? value, out int result)
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out result))
            {
                return true;
            }

            result = 0;
            return false;
        }

        private static int GetTextWidth(string? text) => string.IsNullOrEmpty(text) ? 0 : TerminalTextUtility.GetWidth(text.AsSpan());

        private string ExtractContainerText(ContainerBlock container)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var child in container)
            {
                var text = ExtractBlockText(child);
                if (text.Length == 0)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append('\n');
                }

                builder.Append(text);
                first = false;
            }

            return builder.ToString();
        }

        private string ExtractBlockText(Block block)
        {
            return block switch
            {
                LeafBlock leaf when leaf.Inline is not null => ExtractInlinePlainText(leaf.Inline),
                LeafBlock leaf => NormalizeLeafText(leaf),
                ContainerBlock container => ExtractContainerText(container),
                _ => string.Empty,
            };
        }

        private static string ExtractInlinePlainText(ContainerInline inline)
        {
            var builder = new StringBuilder();
            var stack = new Stack<Inline>();
            for (Inline? child = inline.LastChild; child is not null; child = child.PreviousSibling)
            {
                stack.Push(child);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                switch (current)
                {
                    case LiteralInline literal:
                        builder.Append(literal.Content.ToString());
                        break;
                    case CodeInline code:
                        builder.Append(code.Content);
                        break;
                    case LineBreakInline lineBreak:
                        builder.Append(lineBreak.IsHard ? '\n' : ' ');
                        break;
                    case HtmlInline html:
                        builder.Append(html.Tag);
                        break;
                    case ContainerInline container:
                        for (Inline? child = container.LastChild; child is not null; child = child.PreviousSibling)
                        {
                            stack.Push(child);
                        }
                        break;
                }
            }

            return builder.ToString();
        }

        private string? ResolveLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }

            if (Uri.TryCreate(link, UriKind.Absolute, out var absolute))
            {
                return absolute.ToString();
            }

            if (_baseUri is not null && Uri.TryCreate(_baseUri, link, out var relative))
            {
                return relative.ToString();
            }

            return link;
        }

        private readonly record struct InlineRenderResult(string Text, StyledRun[] Runs);

        private sealed class InlineAccumulator
        {
            private readonly Renderer _owner;
            private readonly StringBuilder _text;
            private readonly List<StyledRun> _runs;

            public InlineAccumulator(Renderer owner)
            {
                _owner = owner;
                _text = new StringBuilder(256);
                _runs = new List<StyledRun>(32);
            }

            public InlineRenderResult ToResult()
            {
                return new InlineRenderResult(
                    _text.ToString(),
                    _runs.Count == 0 ? Array.Empty<StyledRun>() : _runs.ToArray());
            }

            public void AppendContainer(ContainerInline container, CellStyle currentStyle)
            {
                for (Inline? child = container.FirstChild; child is not null; child = child.NextSibling)
                {
                    AppendInline(child, currentStyle);
                }
            }

            private void AppendInline(Inline inline, CellStyle currentStyle)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        AppendText(literal.Content.AsSpan(), currentStyle);
                        return;

                    case CodeInline code:
                        AppendText(code.ContentSpan, currentStyle | _owner._style.InlineCodeStyle);
                        return;

                    case EmphasisInline emphasis:
                    {
                        var style = ResolveEmphasisStyle(emphasis);
                        AppendContainer(emphasis, currentStyle | style);
                        return;
                    }

                    case LinkInline link:
                        AppendLink(link, currentStyle);
                        return;

                    case LineBreakInline lineBreak:
                        AppendText(lineBreak.IsHard ? "\n".AsSpan() : " ".AsSpan(), currentStyle);
                        return;

                    case HtmlInline html when _owner._options.RenderHtmlInlinesAsText:
                        AppendText(html.Tag.AsSpan(), currentStyle | _owner._style.HtmlStyle);
                        return;

                    case HtmlInline:
                        return;

                    case ContainerInline nested:
                        AppendContainer(nested, currentStyle);
                        return;
                }
            }

            private void AppendLink(LinkInline link, CellStyle currentStyle)
            {
                var resolvedLink = _owner.ResolveLink(link.Url);
                if (link.IsImage)
                {
                    if (!_owner._options.RenderImagesAsLinks)
                    {
                        return;
                    }

                    var alt = ExtractInlinePlainText(link).Trim();
                    var label = alt.Length == 0 ? "[image]" : $"[image: {alt}]";
                    AppendText(label.AsSpan(), currentStyle | _owner._style.LinkStyle);
                    return;
                }

                var childStart = _text.Length;
                var linkStyle = currentStyle | _owner._style.LinkStyle;
                AppendContainer(link, linkStyle);

                if (_text.Length == childStart && resolvedLink is not null)
                {
                    AppendText(resolvedLink.AsSpan(), linkStyle);
                }
            }

            private CellStyle ResolveEmphasisStyle(EmphasisInline emphasis)
            {
                if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount >= 2)
                {
                    return CellStyle.None | TextStyle.Strikethrough;
                }

                if (emphasis.DelimiterCount >= 2)
                {
                    return _owner._style.StrongStyle;
                }

                return _owner._style.EmphasisStyle;
            }

            private void AppendText(ReadOnlySpan<char> text, CellStyle style)
            {
                if (text.IsEmpty)
                {
                    return;
                }

                var start = _text.Length;
                _text.Append(text);
                var length = text.Length;

                if (style != CellStyle.None)
                {
                    AddStyleRun(start, length, style);
                }
            }

            private void AddStyleRun(int start, int length, CellStyle style)
            {
                if (length <= 0)
                {
                    return;
                }

                if (_runs.Count > 0)
                {
                    var last = _runs[_runs.Count - 1];
                    if (last.Style == style && last.Start + last.Length == start)
                    {
                        _runs[_runs.Count - 1] = last with { Length = last.Length + length };
                        return;
                    }
                }

                _runs.Add(new StyledRun(start, length, style));
            }
        }
    }

    private sealed class SourceStyleCollector
    {
        private readonly MarkdownStyle _style;
        private readonly MarkdownRenderOptions _options;
        private readonly List<SourceStyledSpan> _spans;
        private readonly List<int> _boundaries;

        public SourceStyleCollector(MarkdownStyle style, MarkdownRenderOptions options)
        {
            _style = style;
            _options = options;
            _spans = new List<SourceStyledSpan>(128);
            _boundaries = new List<int>(256);
        }

        public void Collect(string source, MarkdownDocument document, List<StyledRun> destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(destination);

            _spans.Clear();
            _boundaries.Clear();
            destination.Clear();

            CollectBlocks(document, source.Length);
            NormalizeToRuns(source.Length, destination);
        }

        private void CollectBlocks(ContainerBlock container, int textLength)
        {
            foreach (var block in container)
            {
                CollectBlock(block, textLength);
            }
        }

        private void CollectBlock(Block block, int textLength)
        {
            switch (block)
            {
                case AlertBlock alert:
                {
                    var alertStyle = _style.ResolveAlertStyle(alert.Kind.ToString().ToUpperInvariant());
                    AddSpan(alert, alertStyle.BorderStyle | alertStyle.BackgroundStyle | alertStyle.TitleStyle, textLength);
                    CollectBlocks(alert, textLength);
                    return;
                }

                case HeadingBlock heading:
                    AddSpan(heading, _style.ResolveHeadingStyle(Math.Clamp(heading.Level, 1, 6)), textLength);
                    CollectInlineContainer(heading.Inline, textLength);
                    return;

                case FencedCodeBlock fencedCode:
                    AddSpan(fencedCode, _style.InlineCodeStyle, textLength);
                    return;

                case CodeBlock codeBlock:
                    AddSpan(codeBlock, _style.InlineCodeStyle, textLength);
                    return;

                case QuoteBlock quote:
                    AddSpan(quote, _style.QuotePrefixStyle, textLength);
                    CollectBlocks(quote, textLength);
                    return;

                case ThematicBreakBlock thematicBreak:
                    AddSpan(thematicBreak, _style.QuotePrefixStyle, textLength);
                    return;

                case HtmlBlock htmlBlock when _options.RenderHtmlBlocksAsText:
                    AddSpan(htmlBlock, _style.HtmlStyle, textLength);
                    return;

                case ParagraphBlock paragraph:
                    CollectInlineContainer(paragraph.Inline, textLength);
                    return;

                case ListBlock list:
                    CollectBlocks(list, textLength);
                    return;

                case Markdig.Extensions.Tables.Table table:
                    CollectTable(table, textLength);
                    return;

                case LeafBlock leaf:
                    CollectInlineContainer(leaf.Inline, textLength);
                    return;

                case ContainerBlock nested:
                    CollectBlocks(nested, textLength);
                    return;
            }
        }

        private void CollectTable(Markdig.Extensions.Tables.Table table, int textLength)
        {
            foreach (var rowBlock in table)
            {
                if (rowBlock is not TableRow row)
                {
                    continue;
                }

                if (row.IsHeader)
                {
                    AddSpan(row, _style.StrongStyle, textLength);
                }

                foreach (var cellBlock in row)
                {
                    if (cellBlock is not TableCell cell)
                    {
                        continue;
                    }

                    foreach (var nestedBlock in cell)
                    {
                        CollectBlock(nestedBlock, textLength);
                    }
                }
            }
        }

        private void CollectInlineContainer(ContainerInline? container, int textLength)
        {
            if (container is null)
            {
                return;
            }

            for (Inline? child = container.FirstChild; child is not null; child = child.NextSibling)
            {
                CollectInline(child, textLength);
            }
        }

        private void CollectInline(Inline inline, int textLength)
        {
            switch (inline)
            {
                case EmphasisInline emphasis:
                {
                    var emphasisStyle = ResolveEmphasisStyle(emphasis);
                    AddSpan(emphasis, emphasisStyle, textLength);
                    CollectInlineContainer(emphasis, textLength);
                    return;
                }

                case CodeInline code:
                    AddSpan(code, _style.InlineCodeStyle, textLength);
                    return;

                case LinkInline link:
                    AddSpan(link, _style.LinkStyle, textLength);
                    CollectInlineContainer(link, textLength);
                    return;

                case HtmlInline html when _options.RenderHtmlInlinesAsText:
                    AddSpan(html, _style.HtmlStyle, textLength);
                    return;

                case ContainerInline nested:
                    CollectInlineContainer(nested, textLength);
                    return;
            }
        }

        private CellStyle ResolveEmphasisStyle(EmphasisInline emphasis)
        {
            if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount >= 2)
            {
                return CellStyle.None | TextStyle.Strikethrough;
            }

            if (emphasis.DelimiterCount >= 2)
            {
                return _style.StrongStyle;
            }

            return _style.EmphasisStyle;
        }

        private void AddSpan(MarkdownObject markdownObject, CellStyle style, int textLength)
        {
            if (style == CellStyle.None)
            {
                return;
            }

            if (!TryGetSpan(markdownObject.Span, textLength, out var start, out var endExclusive))
            {
                return;
            }

            _spans.Add(new SourceStyledSpan(start, endExclusive, style));
        }

        private static bool TryGetSpan(SourceSpan span, int textLength, out int start, out int endExclusive)
        {
            if (span.IsEmpty || textLength <= 0)
            {
                start = 0;
                endExclusive = 0;
                return false;
            }

            start = Math.Clamp(span.Start, 0, textLength);
            endExclusive = Math.Clamp(span.End + 1, 0, textLength);
            return endExclusive > start;
        }

        private void NormalizeToRuns(int textLength, List<StyledRun> destination)
        {
            if (_spans.Count == 0 || textLength <= 0)
            {
                return;
            }

            _boundaries.Add(0);
            _boundaries.Add(textLength);
            foreach (var span in _spans)
            {
                _boundaries.Add(span.Start);
                _boundaries.Add(span.EndExclusive);
            }

            _boundaries.Sort();
            for (var index = _boundaries.Count - 2; index >= 0; index--)
            {
                if (_boundaries[index] == _boundaries[index + 1])
                {
                    _boundaries.RemoveAt(index + 1);
                }
            }

            for (var index = 0; index + 1 < _boundaries.Count; index++)
            {
                var start = _boundaries[index];
                var endExclusive = _boundaries[index + 1];
                if (endExclusive <= start)
                {
                    continue;
                }

                var style = CellStyle.None;
                for (var spanIndex = 0; spanIndex < _spans.Count; spanIndex++)
                {
                    var span = _spans[spanIndex];
                    if (span.Start <= start && span.EndExclusive >= endExclusive)
                    {
                        style |= span.Style;
                    }
                }

                if (style == CellStyle.None)
                {
                    continue;
                }

                if (destination.Count > 0)
                {
                    var last = destination[^1];
                    if (last.Style == style && last.Start + last.Length == start)
                    {
                        destination[^1] = new StyledRun(last.Start, last.Length + (endExclusive - start), style);
                        continue;
                    }
                }

                destination.Add(new StyledRun(start, endExclusive - start, style));
            }
        }

        private readonly record struct SourceStyledSpan(int Start, int EndExclusive, CellStyle Style);
    }
}
