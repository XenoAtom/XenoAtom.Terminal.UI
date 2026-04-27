// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Marks a visual as capable of emitting graphics commands during the graphics render pass.
/// </summary>
/// <remarks>
/// Implement this interface only on visuals that can produce graphics output. The framework detects implementations
/// when visuals attach to a <see cref="TerminalApp"/> and skips unmarked subtrees during graphics collection.
/// </remarks>
public interface IGraphicsRenderableVisual
{
    /// <summary>
    /// Emits graphics commands for this visual into <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The graphics render context for the current frame.</param>
    void RenderGraphics(GraphicsRenderContext context);
}
