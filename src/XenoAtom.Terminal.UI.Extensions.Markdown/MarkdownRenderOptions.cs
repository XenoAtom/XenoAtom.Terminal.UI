// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

/// <summary>
/// Specifies rendering options for Markdown conversion into terminal visuals.
/// </summary>
public sealed record MarkdownRenderOptions
{
    /// <summary>
    /// Gets the default Markdown render options.
    /// </summary>
    public static MarkdownRenderOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether paragraph-like text wraps to available width.
    /// </summary>
    public bool WrapText { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether code blocks wrap text.
    /// </summary>
    public bool WrapCodeBlocks { get; init; }

    /// <summary>
    /// Gets the maximum height for rendered code blocks. A value of 0 disables capping.
    /// </summary>
    public int MaxCodeBlockHeight { get; init; } = 12;

    /// <summary>
    /// Gets a value indicating whether HTML blocks are rendered as plain text.
    /// </summary>
    public bool RenderHtmlBlocksAsText { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inline HTML is rendered as plain text.
    /// </summary>
    public bool RenderHtmlInlinesAsText { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether Markdown images are rendered as textual link placeholders.
    /// </summary>
    public bool RenderImagesAsLinks { get; init; } = true;

    /// <summary>
    /// Gets the default table style for Markdown tables.
    /// </summary>
    public TableStyle TableStyle { get; init; } = TableStyle.RoundedGrid;
}

