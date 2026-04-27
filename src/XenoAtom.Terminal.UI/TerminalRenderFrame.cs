// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Represents the text and graphics planes produced for a terminal UI render frame.
/// </summary>
public sealed class TerminalRenderFrame
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalRenderFrame"/> class.
    /// </summary>
    /// <param name="cells">The text-plane cell buffer.</param>
    /// <param name="graphics">The graphics-plane command buffer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cells"/> or <paramref name="graphics"/> is <see langword="null"/>.</exception>
    public TerminalRenderFrame(CellBuffer cells, GraphicsCommandBuffer graphics)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(graphics);
        Cells = cells;
        Graphics = graphics;
    }

    /// <summary>
    /// Gets the text-plane cell buffer.
    /// </summary>
    public CellBuffer Cells { get; }

    /// <summary>
    /// Gets the graphics-plane command buffer.
    /// </summary>
    public GraphicsCommandBuffer Graphics { get; }
}
