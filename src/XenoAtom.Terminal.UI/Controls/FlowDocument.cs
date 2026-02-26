// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Represents a simple block list implementation of <see cref="IDocumentFlowContent"/>.
/// </summary>
public sealed class FlowDocument : IDocumentFlowContent
{
    private readonly List<DocumentFlowBlock> _blocks;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlowDocument"/> class.
    /// </summary>
    public FlowDocument()
    {
        _blocks = new List<DocumentFlowBlock>();
    }

    /// <inheritdoc />
    public int Version => _version;

    /// <inheritdoc />
    public int BlockCount => _blocks.Count;

    /// <inheritdoc />
    public DocumentFlowBlock GetBlock(int index) => _blocks[index];

    /// <summary>
    /// Adds a block to this document.
    /// </summary>
    /// <param name="block">The block to add.</param>
    /// <returns>The same document instance.</returns>
    public FlowDocument Add(DocumentFlowBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _blocks.Add(block);
        _version++;
        return this;
    }

    /// <summary>
    /// Adds a visual-backed block to this document.
    /// </summary>
    /// <param name="visual">The visual to host.</param>
    /// <returns>The same document instance.</returns>
    public FlowDocument Add(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        return Add(new VisualDocumentFlowBlock(visual));
    }

    /// <summary>
    /// Adds a paragraph block to this document.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The same document instance.</returns>
    public FlowDocument AddParagraph(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Add(new ParagraphDocumentFlowBlock(text));
    }

    /// <summary>
    /// Adds a paragraph block with style and hyperlink runs.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <param name="runs">Style runs.</param>
    /// <param name="hyperlinks">Hyperlink runs.</param>
    /// <returns>The same document instance.</returns>
    public FlowDocument AddParagraph(string text, StyledRun[] runs, HyperlinkRun[] hyperlinks)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(hyperlinks);
        return Add(new ParagraphDocumentFlowBlock(text, runs, hyperlinks));
    }
}

/// <summary>
/// Hosts an existing visual inside a <see cref="DocumentFlow"/>.
/// </summary>
public sealed class VisualDocumentFlowBlock : DocumentFlowBlock
{
    private readonly Visual _visual;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualDocumentFlowBlock"/> class.
    /// </summary>
    /// <param name="visual">The hosted visual.</param>
    public VisualDocumentFlowBlock(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        _visual = visual;
    }

    /// <inheritdoc />
    public override object? ReuseKey => _visual;

    /// <inheritdoc />
    public override Visual CreateVisual() => _visual;

    /// <inheritdoc />
    public override bool TryUpdate(Visual visual) => ReferenceEquals(visual, _visual);
}

/// <summary>
/// Represents a paragraph-backed block for <see cref="FlowDocument"/>.
/// </summary>
public sealed class ParagraphDocumentFlowBlock : DocumentFlowBlock
{
    private readonly string _text;
    private readonly StyledRun[] _runs;
    private readonly HyperlinkRun[] _hyperlinks;
    private readonly bool _wrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphDocumentFlowBlock"/> class.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    public ParagraphDocumentFlowBlock(string text)
        : this(text, Array.Empty<StyledRun>(), Array.Empty<HyperlinkRun>(), wrap: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphDocumentFlowBlock"/> class.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <param name="runs">Style runs.</param>
    /// <param name="hyperlinks">Hyperlink runs.</param>
    public ParagraphDocumentFlowBlock(string text, StyledRun[] runs, HyperlinkRun[] hyperlinks)
        : this(text, runs, hyperlinks, wrap: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphDocumentFlowBlock"/> class.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <param name="runs">Style runs.</param>
    /// <param name="hyperlinks">Hyperlink runs.</param>
    /// <param name="wrap">Whether wrapping is enabled.</param>
    public ParagraphDocumentFlowBlock(string text, StyledRun[] runs, HyperlinkRun[] hyperlinks, bool wrap)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(hyperlinks);

        _text = text;
        _runs = runs;
        _hyperlinks = hyperlinks;
        _wrap = wrap;
    }

    /// <inheritdoc />
    public override Visual CreateVisual()
    {
        var paragraph = new Paragraph(_text)
        {
            Wrap = _wrap,
            Runs = _runs,
            Hyperlinks = _hyperlinks,
            HorizontalAlignment = Align.Stretch,
        };
        return paragraph;
    }
}
