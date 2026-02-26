// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

/// <summary>
/// Represents markdown content materialized as <see cref="DocumentFlowBlock"/> entries.
/// </summary>
public sealed class MarkdownDocumentContent : IDocumentFlowContent
{
    private readonly DocumentFlowBlock[] _blocks;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownDocumentContent"/> class.
    /// </summary>
    /// <param name="markdown">The markdown source text.</param>
    /// <param name="pipeline">The markdown pipeline to use. When null, the default pipeline is used.</param>
    /// <param name="baseUri">Optional base URI used to resolve relative links.</param>
    /// <param name="options">Optional render options.</param>
    public MarkdownDocumentContent(string markdown, MarkdownPipeline? pipeline = null, Uri? baseUri = null, MarkdownRenderOptions? options = null)
        : this(markdown, pipeline, baseUri, options, style: null)
    {
    }

    internal MarkdownDocumentContent(string markdown, MarkdownPipeline? pipeline, Uri? baseUri, MarkdownRenderOptions? options, MarkdownStyle? style)
    {
        markdown ??= string.Empty;
        var effectivePipeline = pipeline ?? MarkdownDefaults.Pipeline;
        var effectiveOptions = options ?? MarkdownRenderOptions.Default;
        var effectiveStyle = style ?? MarkdownStyle.Default;

        var document = Markdig.Markdown.Parse(markdown, effectivePipeline);
        var builder = new MarkdownDocumentBuilder(effectiveStyle, effectiveOptions, baseUri);
        _blocks = builder.Build(document);
    }

    /// <inheritdoc />
    public int Version => 0;

    /// <inheritdoc />
    public int BlockCount => _blocks.Length;

    /// <inheritdoc />
    public DocumentFlowBlock GetBlock(int index) => _blocks[index];
}

