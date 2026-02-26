// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;
using Table = XenoAtom.Terminal.UI.Controls.Table;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

internal sealed class MarkdownDocumentBuilder
{
    private readonly MarkdownStyle _style;
    private readonly MarkdownRenderOptions _options;
    private readonly Uri? _baseUri;
    private readonly List<DocumentFlowBlock> _blocks;

    public MarkdownDocumentBuilder(MarkdownStyle style, MarkdownRenderOptions options, Uri? baseUri)
    {
        _style = style;
        _options = options;
        _baseUri = baseUri;
        _blocks = new List<DocumentFlowBlock>(64);
    }

    public DocumentFlowBlock[] Build(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        RenderBlocks(document, indent: 0, quotePrefix: null);
        return _blocks.Count == 0 ? Array.Empty<DocumentFlowBlock>() : _blocks.ToArray();
    }

    private void RenderBlocks(ContainerBlock container, int indent, string? quotePrefix)
    {
        foreach (var block in container)
        {
            RenderBlock(block, indent, quotePrefix);
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
                AddLeafParagraphBlock(
                    heading,
                    _style.ResolveHeadingStyle(heading.Level),
                    indent,
                    quotePrefix,
                    marginTop: _blocks.Count == 0 ? 0 : 1,
                    marginBottom: 1);
                return;

            case ParagraphBlock paragraph:
                AddLeafParagraphBlock(paragraph, _style.ParagraphStyle, indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case ListBlock list:
                RenderList(list, indent, quotePrefix);
                return;

            case QuoteBlock quote:
                RenderBlocks(quote, indent, AppendPrefix(quotePrefix, _style.QuotePrefix));
                return;

            case Markdig.Extensions.Tables.Table table:
                AddVisualBlock(CreateTableVisual(table), indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case FencedCodeBlock fencedCode:
                AddVisualBlock(CreateCodeVisual(fencedCode), indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case CodeBlock codeBlock:
                AddVisualBlock(CreateCodeVisual(codeBlock), indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case ThematicBreakBlock:
                AddVisualBlock(new Rule(), indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case HtmlBlock htmlBlock when _options.RenderHtmlBlocksAsText:
                AddLeafParagraphBlock(htmlBlock, _style.HtmlStyle, indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case HtmlBlock:
                return;

            case LeafBlock leaf:
                AddLeafParagraphBlock(leaf, _style.ParagraphStyle, indent, quotePrefix, marginTop: 0, marginBottom: 1);
                return;

            case ContainerBlock nested:
                RenderBlocks(nested, indent, quotePrefix);
                return;

            default:
                return;
        }
    }

    private void RenderList(ListBlock list, int indent, string? quotePrefix)
    {
        var ordered = TryParseInt(list.OrderedStart, out var startValue) ? startValue : 1;
        var orderedDelimiter = list.OrderedDelimiter == default ? '.' : list.OrderedDelimiter;

        foreach (var child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            var bullet = list.IsOrdered
                ? $"{ordered}{orderedDelimiter}"
                : _style.UnorderedListBullet;

            if (list.IsOrdered)
            {
                ordered++;
            }

            RenderListItem(item, indent, quotePrefix, bullet);
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
                    var result = RenderLeafInline(paragraph, _style.ParagraphStyle);
                    AddParagraphBlock(
                        result,
                        indent,
                        hangingIndent: consumedMarker ? 0 : bulletWidth,
                        linePrefix: prefix,
                        continuationPrefix: continuationPrefix,
                        prefixStyle: quotePrefix is not null ? _style.QuotePrefixStyle : Style.None,
                        marginTop: 0,
                        marginBottom: 0);
                    consumedMarker = true;
                    emittedAny = true;
                    break;
                }

                case ListBlock nestedList:
                    RenderList(nestedList, indent + bulletWidth, quotePrefix);
                    consumedMarker = true;
                    emittedAny = true;
                    break;

                default:
                {
                    var visual = CreateVisualFromBlock(child);
                    if (visual is null)
                    {
                        break;
                    }

                    var visualIndent = indent + bulletWidth;
                    if (!consumedMarker)
                    {
                        // Render marker row once when an item starts with a non-paragraph block.
                        AddParagraphBlock(
                            new InlineRenderResult(string.Empty, Array.Empty<StyledRun>(), Array.Empty<HyperlinkRun>()),
                            indent,
                            hangingIndent: bulletWidth,
                            linePrefix: firstParagraphPrefix,
                            continuationPrefix: continuationPrefix,
                            prefixStyle: quotePrefix is not null ? _style.QuotePrefixStyle : Style.None,
                            marginTop: 0,
                            marginBottom: 0);
                    }

                    AddVisualBlock(visual, visualIndent, quotePrefix, marginTop: 0, marginBottom: 0);
                    consumedMarker = true;
                    emittedAny = true;
                    break;
                }
            }
        }

        if (!emittedAny)
        {
            AddParagraphBlock(
                new InlineRenderResult(string.Empty, Array.Empty<StyledRun>(), Array.Empty<HyperlinkRun>()),
                indent,
                hangingIndent: bulletWidth,
                linePrefix: firstParagraphPrefix,
                continuationPrefix: continuationPrefix,
                prefixStyle: quotePrefix is not null ? _style.QuotePrefixStyle : Style.None,
                marginTop: 0,
                marginBottom: 0);
        }
    }

    private void RenderAlert(AlertBlock alert, int indent, string? quotePrefix)
    {
        var kind = alert.Kind.ToString().ToUpperInvariant();
        var alertStyle = _style.ResolveAlertStyle(kind);

        var body = ExtractContainerText(alert).Trim();
        var paragraph = new Paragraph(body)
        {
            Wrap = _options.WrapText,
            Runs = body.Length == 0 || _style.ParagraphStyle == Style.None
                ? Array.Empty<StyledRun>()
                : [new StyledRun(0, body.Length, _style.ParagraphStyle)],
            HorizontalAlignment = Align.Stretch,
        };

        var title = new TextBlock(kind);
        title.SetStyle(TextBlockStyle.Default with { TextStyle = alertStyle.TitleStyle.TextStyle });

        var group = new Group(title, paragraph)
        {
            HorizontalAlignment = Align.Stretch,
            Padding = new Thickness(1),
        };
        group.SetStyle(GroupStyle.Rounded with
        {
            BorderCellStyle = alertStyle.BorderStyle,
            BackgroundStyle = alertStyle.BackgroundStyle,
        });

        AddVisualBlock(group, indent, quotePrefix, marginTop: 0, marginBottom: 1);
    }

    private void AddLeafParagraphBlock(LeafBlock leaf, Style style, int indent, string? quotePrefix, int marginTop, int marginBottom)
    {
        var result = RenderLeafInline(leaf, style);
        AddParagraphBlock(
            result,
            indent,
            hangingIndent: 0,
            linePrefix: quotePrefix,
            continuationPrefix: quotePrefix,
            prefixStyle: quotePrefix is not null ? _style.QuotePrefixStyle : Style.None,
            marginTop: marginTop,
            marginBottom: marginBottom);
    }

    private void AddParagraphBlock(
        InlineRenderResult result,
        int indent,
        int hangingIndent,
        string? linePrefix,
        string? continuationPrefix,
        Style prefixStyle,
        int marginTop,
        int marginBottom)
    {
        var text = result.Text ?? string.Empty;
        var paragraphBlock = new MarkdownParagraphBlock(
            text,
            result.Runs,
            result.Hyperlinks,
            _options.WrapText,
            indent,
            hangingIndent,
            linePrefix,
            continuationPrefix,
            prefixStyle,
            marginTop,
            marginBottom);
        _blocks.Add(paragraphBlock);
    }

    private void AddVisualBlock(Visual visual, int indent, string? quotePrefix, int marginTop, int marginBottom)
    {
        ArgumentNullException.ThrowIfNull(visual);

        var leftPadding = Math.Max(0, indent + GetTextWidth(quotePrefix));
        visual.HorizontalAlignment = Align.Stretch;

        Visual effectiveVisual = visual;
        if (leftPadding > 0)
        {
            effectiveVisual = new Padder(visual)
            {
                Padding = new Thickness(leftPadding, 0, 0, 0),
                HorizontalAlignment = Align.Stretch,
            };
        }

        _blocks.Add(new MarkdownVisualBlock(effectiveVisual, marginTop, marginBottom));
    }

    private Visual? CreateVisualFromBlock(Block block)
    {
        return block switch
        {
            ParagraphBlock paragraph => CreateParagraphVisual(RenderLeafInline(paragraph, _style.ParagraphStyle), indent: 0, hangingIndent: 0, linePrefix: null, continuationPrefix: null, Style.None),
            HeadingBlock heading => CreateParagraphVisual(RenderLeafInline(heading, _style.ResolveHeadingStyle(heading.Level)), indent: 0, hangingIndent: 0, linePrefix: null, continuationPrefix: null, Style.None),
            FencedCodeBlock fenced => CreateCodeVisual(fenced),
            CodeBlock code => CreateCodeVisual(code),
            Markdig.Extensions.Tables.Table table => CreateTableVisual(table),
            ThematicBreakBlock => new Rule(),
            LeafBlock leaf => CreateParagraphVisual(RenderLeafInline(leaf, _style.ParagraphStyle), indent: 0, hangingIndent: 0, linePrefix: null, continuationPrefix: null, Style.None),
            _ => null,
        };
    }

    private Visual CreateParagraphVisual(
        InlineRenderResult result,
        int indent,
        int hangingIndent,
        string? linePrefix,
        string? continuationPrefix,
        Style prefixStyle)
    {
        return new Paragraph(result.Text ?? string.Empty)
        {
            Wrap = _options.WrapText,
            Runs = result.Runs,
            Hyperlinks = result.Hyperlinks,
            Indent = Math.Max(0, indent),
            HangingIndent = Math.Max(0, hangingIndent),
            LinePrefix = linePrefix,
            ContinuationPrefix = continuationPrefix,
            PrefixStyle = prefixStyle,
            HorizontalAlignment = Align.Stretch,
        };
    }

    private Visual CreateCodeVisual(CodeBlock block)
    {
        var code = NormalizeLeafText(block);
        var log = new LogControl
        {
            WrapText = _options.WrapCodeBlocks,
            HorizontalAlignment = Align.Stretch,
        };

        var maxCodeBlockHeight = Math.Max(0, _options.MaxCodeBlockHeight);
        if (maxCodeBlockHeight > 0)
        {
            log.MaxHeight = maxCodeBlockHeight;
        }

        if (code.Length > 0)
        {
            log.AppendLine(code);
        }

        if (block is not FencedCodeBlock fenced || string.IsNullOrWhiteSpace(fenced.Info))
        {
            return log;
        }

        var language = fenced.Info!.Trim();
        var header = new TextBlock(language);
        header.SetStyle(TextBlockStyle.Default with { TextStyle = TextStyle.Bold });

        return new VStack(header, log)
        {
            Spacing = 0,
            HorizontalAlignment = Align.Stretch,
        };
    }

    private Visual CreateTableVisual(Markdig.Extensions.Tables.Table table)
    {
        var uiTable = new Table
        {
            HorizontalAlignment = Align.Stretch,
        };
        uiTable.SetStyle(_options.TableStyle);

        var columnCount = table.ColumnDefinitions.Count;
        foreach (var rowBlock in table)
        {
            if (rowBlock is not TableRow row)
            {
                continue;
            }

            var rowCount = Math.Max(columnCount, row.Count);
            var visuals = new Visual[rowCount];
            for (var columnIndex = 0; columnIndex < rowCount; columnIndex++)
            {
                var alignment = ResolveColumnAlignment(table, columnIndex);
                var cell = columnIndex < row.Count ? row[columnIndex] as TableCell : null;
                visuals[columnIndex] = CreateTableCellVisual(cell, alignment);
            }

            if (row.IsHeader && uiTable.HeaderCells.Count == 0)
            {
                for (var index = 0; index < visuals.Length; index++)
                {
                    uiTable.HeaderCells.Add(visuals[index]);
                }
            }
            else
            {
                var rowCells = new VisualList<Visual>(uiTable, "Markdown.TableRow");
                for (var index = 0; index < visuals.Length; index++)
                {
                    rowCells.Add(visuals[index]);
                }

                uiTable.RowCells.Add(rowCells);
            }
        }

        return uiTable;
    }

    private Visual CreateTableCellVisual(TableCell? cell, TextAlignment alignment)
    {
        if (cell is null)
        {
            return new Paragraph(string.Empty)
            {
                Wrap = true,
                TextAlignment = alignment,
                HorizontalAlignment = Align.Stretch,
            };
        }

        if (cell.Count == 1 && cell[0] is LeafBlock leaf)
        {
            var result = RenderLeafInline(leaf, _style.ParagraphStyle);
            return new Paragraph(result.Text ?? string.Empty)
            {
                Wrap = true,
                TextAlignment = alignment,
                Runs = result.Runs,
                Hyperlinks = result.Hyperlinks,
                HorizontalAlignment = Align.Stretch,
            };
        }

        var plain = ExtractContainerText(cell);
        return new Paragraph(plain)
        {
            Wrap = true,
            TextAlignment = alignment,
            Runs = plain.Length == 0 || _style.ParagraphStyle == Style.None
                ? Array.Empty<StyledRun>()
                : [new StyledRun(0, plain.Length, _style.ParagraphStyle)],
            HorizontalAlignment = Align.Stretch,
        };
    }

    private InlineRenderResult RenderLeafInline(LeafBlock leaf, Style baseStyle)
    {
        var inline = leaf.Inline;
        if (inline is null)
        {
            var text = NormalizeLeafText(leaf);
            if (text.Length == 0)
            {
                return new InlineRenderResult(string.Empty, Array.Empty<StyledRun>(), Array.Empty<HyperlinkRun>());
            }

            var runs = baseStyle == Style.None ? Array.Empty<StyledRun>() : [new StyledRun(0, text.Length, baseStyle)];
            return new InlineRenderResult(text, runs, Array.Empty<HyperlinkRun>());
        }

        var accumulator = new InlineAccumulator(this, baseStyle);
        accumulator.AppendContainer(inline, baseStyle, activeHyperlink: null);
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

    private static TextAlignment ResolveColumnAlignment(Markdig.Extensions.Tables.Table table, int index)
    {
        if (index < 0 || index >= table.ColumnDefinitions.Count)
        {
            return TextAlignment.Left;
        }

        return table.ColumnDefinitions[index].Alignment switch
        {
            TableColumnAlign.Center => TextAlignment.Center,
            TableColumnAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };
    }

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

    private readonly record struct InlineRenderResult(string Text, StyledRun[] Runs, HyperlinkRun[] Hyperlinks);

    private sealed class InlineAccumulator
    {
        private readonly MarkdownDocumentBuilder _owner;
        private readonly StringBuilder _text;
        private readonly List<StyledRun> _runs;
        private readonly List<HyperlinkRun> _hyperlinks;

        public InlineAccumulator(MarkdownDocumentBuilder owner, Style _)
        {
            _owner = owner;
            _text = new StringBuilder(256);
            _runs = new List<StyledRun>(32);
            _hyperlinks = new List<HyperlinkRun>(16);
        }

        public InlineRenderResult ToResult()
        {
            return new InlineRenderResult(
                _text.ToString(),
                _runs.Count == 0 ? Array.Empty<StyledRun>() : _runs.ToArray(),
                _hyperlinks.Count == 0 ? Array.Empty<HyperlinkRun>() : _hyperlinks.ToArray());
        }

        public void AppendContainer(ContainerInline container, Style currentStyle, string? activeHyperlink)
        {
            for (Inline? child = container.FirstChild; child is not null; child = child.NextSibling)
            {
                AppendInline(child, currentStyle, activeHyperlink);
            }
        }

        private void AppendInline(Inline inline, Style currentStyle, string? activeHyperlink)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    AppendText(literal.Content.AsSpan(), currentStyle, activeHyperlink);
                    return;

                case CodeInline code:
                    AppendText(code.ContentSpan, currentStyle | _owner._style.InlineCodeStyle, activeHyperlink);
                    return;

                case EmphasisInline emphasis:
                {
                    var style = ResolveEmphasisStyle(emphasis);
                    AppendContainer(emphasis, currentStyle | style, activeHyperlink);
                    return;
                }

                case LinkInline link:
                    AppendLink(link, currentStyle, activeHyperlink);
                    return;

                case LineBreakInline lineBreak:
                    AppendText(lineBreak.IsHard ? "\n".AsSpan() : " ".AsSpan(), currentStyle, activeHyperlink);
                    return;

                case HtmlInline html when _owner._options.RenderHtmlInlinesAsText:
                    AppendText(html.Tag.AsSpan(), currentStyle | _owner._style.HtmlStyle, activeHyperlink);
                    return;

                case HtmlInline:
                    return;

                case ContainerInline nested:
                    AppendContainer(nested, currentStyle, activeHyperlink);
                    return;
            }
        }

        private void AppendLink(LinkInline link, Style currentStyle, string? activeHyperlink)
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
                AppendText(label.AsSpan(), currentStyle | _owner._style.LinkStyle, resolvedLink ?? activeHyperlink);
                return;
            }

            var childStart = _text.Length;
            var linkStyle = currentStyle | _owner._style.LinkStyle;
            AppendContainer(link, linkStyle, resolvedLink ?? activeHyperlink);

            if (_text.Length == childStart && resolvedLink is not null)
            {
                AppendText(resolvedLink.AsSpan(), linkStyle, resolvedLink);
            }
        }

        private Style ResolveEmphasisStyle(EmphasisInline emphasis)
        {
            if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount >= 2)
            {
                return Style.None | TextStyle.Strikethrough;
            }

            if (emphasis.DelimiterCount >= 2)
            {
                return _owner._style.StrongStyle;
            }

            return _owner._style.EmphasisStyle;
        }

        private void AppendText(ReadOnlySpan<char> text, Style style, string? hyperlink)
        {
            if (text.IsEmpty)
            {
                return;
            }

            var start = _text.Length;
            _text.Append(text);
            var length = text.Length;

            if (style != Style.None)
            {
                AddStyleRun(start, length, style);
            }

            if (!string.IsNullOrWhiteSpace(hyperlink))
            {
                AddHyperlinkRun(start, length, hyperlink!);
            }
        }

        private void AddStyleRun(int start, int length, Style style)
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

        private void AddHyperlinkRun(int start, int length, string uri)
        {
            if (length <= 0)
            {
                return;
            }

            if (_hyperlinks.Count > 0)
            {
                var last = _hyperlinks[_hyperlinks.Count - 1];
                if (string.Equals(last.Uri, uri, StringComparison.Ordinal) && last.Start + last.Length == start)
                {
                    _hyperlinks[_hyperlinks.Count - 1] = new HyperlinkRun(last.Start, last.Length + length, last.Uri);
                    return;
                }
            }

            _hyperlinks.Add(new HyperlinkRun(start, length, uri));
        }
    }

    private sealed class MarkdownParagraphBlock : DocumentFlowBlock
    {
        private static readonly object ReuseKeyValue = new();
        private readonly string _text;
        private readonly StyledRun[] _runs;
        private readonly HyperlinkRun[] _hyperlinks;
        private readonly bool _wrap;
        private readonly int _indent;
        private readonly int _hangingIndent;
        private readonly string? _linePrefix;
        private readonly string? _continuationPrefix;
        private readonly Style _prefixStyle;
        private readonly int _marginTop;
        private readonly int _marginBottom;

        public MarkdownParagraphBlock(
            string text,
            StyledRun[] runs,
            HyperlinkRun[] hyperlinks,
            bool wrap,
            int indent,
            int hangingIndent,
            string? linePrefix,
            string? continuationPrefix,
            Style prefixStyle,
            int marginTop,
            int marginBottom)
        {
            _text = text ?? string.Empty;
            _runs = runs ?? Array.Empty<StyledRun>();
            _hyperlinks = hyperlinks ?? Array.Empty<HyperlinkRun>();
            _wrap = wrap;
            _indent = Math.Max(0, indent);
            _hangingIndent = Math.Max(0, hangingIndent);
            _linePrefix = linePrefix;
            _continuationPrefix = continuationPrefix;
            _prefixStyle = prefixStyle;
            _marginTop = Math.Max(0, marginTop);
            _marginBottom = Math.Max(0, marginBottom);
        }

        public override int MarginTop => _marginTop;

        public override int MarginBottom => _marginBottom;

        public override object? ReuseKey => ReuseKeyValue;

        public override Visual CreateVisual()
        {
            var paragraph = new Paragraph();
            TryUpdate(paragraph);
            return paragraph;
        }

        public override bool TryUpdate(Visual visual)
        {
            if (visual is not Paragraph paragraph)
            {
                return false;
            }

            paragraph.HorizontalAlignment = Align.Stretch;
            paragraph.Wrap = _wrap;
            paragraph.Text = _text;
            paragraph.Runs = _runs;
            paragraph.Hyperlinks = _hyperlinks;
            paragraph.Indent = _indent;
            paragraph.HangingIndent = _hangingIndent;
            paragraph.LinePrefix = _linePrefix;
            paragraph.ContinuationPrefix = _continuationPrefix;
            paragraph.PrefixStyle = _prefixStyle;
            return true;
        }
    }

    private sealed class MarkdownVisualBlock : DocumentFlowBlock
    {
        private readonly Visual _visual;
        private readonly int _marginTop;
        private readonly int _marginBottom;

        public MarkdownVisualBlock(Visual visual, int marginTop, int marginBottom)
        {
            _visual = visual;
            _marginTop = Math.Max(0, marginTop);
            _marginBottom = Math.Max(0, marginBottom);
        }

        public override int MarginTop => _marginTop;

        public override int MarginBottom => _marginBottom;

        public override object? ReuseKey => _visual;

        public override Visual CreateVisual() => _visual;

        public override bool TryUpdate(Visual visual) => ReferenceEquals(visual, _visual);
    }
}
