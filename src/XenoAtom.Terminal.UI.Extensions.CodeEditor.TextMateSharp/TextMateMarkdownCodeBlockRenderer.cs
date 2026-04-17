// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateSharp.Grammars;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

/// <summary>
/// Renders Markdown fenced code blocks with TextMateSharp token colors.
/// </summary>
public sealed class TextMateMarkdownCodeBlockRenderer : IMarkdownCodeBlockRenderer
{
    private readonly TextMateLanguageCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMateMarkdownCodeBlockRenderer"/> class.
    /// </summary>
    public TextMateMarkdownCodeBlockRenderer()
        : this(new TextMateMarkdownRendererOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMateMarkdownCodeBlockRenderer"/> class.
    /// </summary>
    /// <param name="options">The language-resolution and theme-selection options.</param>
    public TextMateMarkdownCodeBlockRenderer(TextMateMarkdownRendererOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _catalog = TextMateLanguageCatalog.Default;
    }

    /// <summary>
    /// Gets the options used by this renderer instance.
    /// </summary>
    public TextMateMarkdownRendererOptions Options { get; }

    /// <inheritdoc />
    public Visual? CreateVisual(in MarkdownCodeBlockRenderContext context)
    {
        if (!_catalog.TryResolveMarkdownScope(context.Language, Options, out var scopeName))
        {
            return null;
        }

        var themeName = TextMateThemePalette.IsLightTheme(context.Theme) ? Options.LightTheme : Options.DarkTheme;
        var session = _catalog.CreateSession(scopeName, themeName);
        var palette = _catalog.GetPalette(themeName);

        var log = new LogControl
        {
            WrapText = context.Options.WrapCodeBlocks,
            HorizontalAlignment = Align.Stretch,
            FollowTail = false,
        };

        var maxCodeBlockHeight = Math.Max(0, context.Options.MaxCodeBlockHeight);
        if (maxCodeBlockHeight > 0)
        {
            log.MaxHeight = maxCodeBlockHeight;
        }

        var code = context.Code ?? string.Empty;
        if (code.Length == 0)
        {
            log.AppendLine(string.Empty);
        }
        else
        {
            var runs = TokenizeCode(code, session, palette);
            log.AppendLine(code, runs.Count == 0 ? null : runs.ToArray());
        }

        var label = !string.IsNullOrWhiteSpace(context.Language)
            ? context.Language
            : Options.DefaultLanguageId;
        if (string.IsNullOrWhiteSpace(label))
        {
            return log;
        }

        var header = new TextBlock(label);
        header.SetStyle(TextBlockStyle.Default with { TextStyle = TextStyle.Bold });

        return new VStack(header, log)
        {
            Spacing = 0,
            HorizontalAlignment = Align.Stretch,
        };
    }

    private static List<StyledRun> TokenizeCode(string code, TextMateTokenizationSession session, TextMateThemePalette palette)
    {
        var runs = new List<StyledRun>(32);
        IStateStack? currentState = null;
        var lineStart = 0;
        var baseOffset = 0;

        while (lineStart <= code.Length)
        {
            var lineEnd = lineStart < code.Length
                ? code.IndexOf('\n', lineStart)
                : -1;
            var hasLineBreak = lineEnd >= 0;
            if (!hasLineBreak)
            {
                lineEnd = code.Length;
            }

            var contentLength = Math.Max(0, lineEnd - lineStart);
            var tokenLength = contentLength + (hasLineBreak ? 1 : 0);
            var lineText = tokenLength > 0
                ? code.AsMemory(lineStart, tokenLength)
                : ReadOnlyMemory<char>.Empty;
            var result = session.TokenizeLine2(lineText, currentState);
            var tokenizedLine = TextMateTokenizedLine.Create(contentLength, result.Tokens);
            TextMateRunBuilder.AddStyledRuns(runs, baseOffset, tokenizedLine.Segments, palette);
            currentState = result.RuleStack;

            if (!hasLineBreak)
            {
                break;
            }

            baseOffset += tokenLength;
            lineStart = lineEnd + 1;
        }

        return runs;
    }
}
