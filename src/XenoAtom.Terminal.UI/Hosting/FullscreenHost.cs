// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Hosting;

/// <summary>
/// Hosts fullscreen rendering using a diff renderer.
/// </summary>
public sealed class FullscreenHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private readonly CellBufferDiffRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="FullscreenHost"/> class.
    /// </summary>
    /// <param name="terminal">The terminal instance.</param>
    public FullscreenHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _renderer = new CellBufferDiffRenderer();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _renderer.Dispose();
    }

    /// <summary>
    /// Renders a buffer to the terminal using diff-based updates.
    /// </summary>
    /// <param name="buffer">The frame buffer.</param>
    /// <param name="wantsCursor">Whether the cursor should be visible.</param>
    /// <param name="cursorX">The cursor X position.</param>
    /// <param name="cursorY">The cursor Y position.</param>
    public void Render(CellBuffer buffer, bool wantsCursor, int cursorX, int cursorY)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _renderer.RenderFullscreen(_terminal, buffer, wantsCursor, cursorX, cursorY);
    }

    /// <summary>
    /// Resets the diff renderer state.
    /// </summary>
    public void Reset() => _renderer.Reset();
}
