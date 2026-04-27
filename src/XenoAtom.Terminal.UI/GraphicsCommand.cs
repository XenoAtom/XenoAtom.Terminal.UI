// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Describes a graphics draw command emitted by a visual during the graphics render pass.
/// </summary>
/// <param name="VisualRenderId">The stable render identity of the visual that emitted the command.</param>
/// <param name="CellBounds">The requested destination rectangle, in terminal cells.</param>
/// <param name="ClipBounds">The effective clip rectangle inherited from the visual tree, in terminal cells.</param>
/// <param name="Content">The graphics content descriptor.</param>
/// <param name="ScaleMode">The scale mode used to map the content to <paramref name="CellBounds"/>.</param>
/// <param name="PreserveAspectRatio">Whether the presenter should preserve the content aspect ratio where applicable.</param>
/// <param name="PaintOrder">The deterministic paint order assigned by the visual-tree traversal.</param>
/// <param name="ReserveCells">Whether the visual reserves its cell rectangle during the text render pass.</param>
/// <param name="AccessibilityText">Optional descriptive text for assistive or fallback output.</param>
public readonly record struct GraphicsCommand(
    ulong VisualRenderId,
    Rectangle CellBounds,
    Rectangle ClipBounds,
    TerminalGraphicContent Content,
    ImageScaleMode ScaleMode,
    bool PreserveAspectRatio,
    int PaintOrder,
    bool ReserveCells,
    string? AccessibilityText);

/// <summary>
/// Controls how graphics content is mapped to a terminal cell rectangle.
/// </summary>
public enum ImageScaleMode
{
    /// <summary>
    /// Preserve the source size where possible, clipping any overflow.
    /// </summary>
    None = 0,

    /// <summary>
    /// Preserve aspect ratio and fit inside the destination rectangle.
    /// </summary>
    Fit = 1,

    /// <summary>
    /// Preserve aspect ratio and cover the destination rectangle, cropping overflow.
    /// </summary>
    Fill = 2,

    /// <summary>
    /// Stretch to exactly the destination rectangle.
    /// </summary>
    Stretch = 3,

    /// <summary>
    /// Center at natural size where possible, clipping any overflow.
    /// </summary>
    Center = 4,
}
