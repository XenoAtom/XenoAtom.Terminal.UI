// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Provides the context used by graphics-capable visuals to emit graphics commands for the current frame.
/// </summary>
public sealed class GraphicsRenderContext
{
    private int _nextPaintOrder;

    internal GraphicsRenderContext(GraphicsCommandBuffer commands)
    {
        Commands = commands;
    }

    /// <summary>
    /// Gets the frame command buffer being populated.
    /// </summary>
    public GraphicsCommandBuffer Commands { get; }

    /// <summary>
    /// Gets the stable render identity of the visual currently being collected.
    /// </summary>
    public ulong CurrentVisualRenderId { get; private set; }

    /// <summary>
    /// Gets the effective clip rectangle for the visual currently being collected, in terminal cells.
    /// </summary>
    public Rectangle ClipBounds { get; private set; }

    internal void BeginFrame()
    {
        _nextPaintOrder = 0;
        CurrentVisualRenderId = 0;
        ClipBounds = default;
        Commands.Clear();
    }

    internal void BeginVisual(ulong visualRenderId, in Rectangle clipBounds)
    {
        CurrentVisualRenderId = visualRenderId;
        ClipBounds = clipBounds;
    }

    /// <summary>
    /// Adds a graphics command for the current visual.
    /// </summary>
    /// <param name="cellBounds">The destination rectangle in terminal cells.</param>
    /// <param name="content">The graphics content descriptor.</param>
    /// <param name="scaleMode">The scale mode used to map the content to <paramref name="cellBounds"/>.</param>
    /// <param name="preserveAspectRatio">Whether the presenter should preserve aspect ratio where applicable.</param>
    /// <param name="reserveCells">Whether the visual reserves the destination cells during the text render pass.</param>
    /// <param name="accessibilityText">Optional descriptive text for assistive or fallback output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
    public void Add(
        Rectangle cellBounds,
        TerminalGraphicContent content,
        ImageScaleMode scaleMode = ImageScaleMode.Fit,
        bool preserveAspectRatio = true,
        bool reserveCells = true,
        string? accessibilityText = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Kind == TerminalGraphicContentKind.None || cellBounds.Width <= 0 || cellBounds.Height <= 0 || !cellBounds.Intersects(ClipBounds))
        {
            return;
        }

        var command = new GraphicsCommand(
            CurrentVisualRenderId,
            cellBounds,
            ClipBounds,
            content,
            scaleMode,
            preserveAspectRatio,
            _nextPaintOrder++,
            reserveCells,
            accessibilityText);
        Commands.Add(command);
    }
}
