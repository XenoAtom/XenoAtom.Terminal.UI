// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

/// <summary>
/// Provides an extension point for rendering Markdown code blocks.
/// </summary>
/// <remarks>
/// Returning <see langword="null"/> from <see cref="CreateVisual"/> lets <see cref="MarkdownControl"/> fall back to
/// the built-in plain-text code-block renderer.
/// </remarks>
public interface IMarkdownCodeBlockRenderer
{
    /// <summary>
    /// Attempts to create a visual for a Markdown code block.
    /// </summary>
    /// <param name="context">The code-block rendering context.</param>
    /// <returns>A custom visual, or <see langword="null"/> to use the built-in renderer.</returns>
    Visual? CreateVisual(in MarkdownCodeBlockRenderContext context);
}

/// <summary>
/// Describes a Markdown code block being rendered.
/// </summary>
/// <param name="Code">The normalized code text with <c>\n</c> line endings.</param>
/// <param name="FenceInfo">The raw fenced-code info string when present.</param>
/// <param name="Language">The parsed language token derived from <paramref name="FenceInfo"/> when present.</param>
/// <param name="IsFenced">A value indicating whether the source block is a fenced code block.</param>
/// <param name="Theme">The current terminal UI theme.</param>
/// <param name="Style">The resolved markdown style.</param>
/// <param name="Options">The active markdown render options.</param>
public readonly record struct MarkdownCodeBlockRenderContext(
    string Code,
    string? FenceInfo,
    string? Language,
    bool IsFenced,
    Theme Theme,
    MarkdownStyle Style,
    MarkdownRenderOptions Options);
