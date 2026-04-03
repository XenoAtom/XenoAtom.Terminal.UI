// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI;

public sealed partial class TerminalApp
{
    /// <summary>
    /// Captures the current frame buffer as an SVG document.
    /// </summary>
    /// <param name="options">Export options.</param>
    /// <returns>SVG document as a string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no frame has been rendered yet.</exception>
    public string CaptureSvg(CellBufferSvgExportOptions? options = null)
    {
        return CellBufferSvgExporter.Export(GetRequiredRenderBuffer(), options);
    }

    /// <summary>
    /// Captures a screenshot of the specified <paramref name="visual"/> as an SVG document.
    /// </summary>
    /// <remarks>
    /// The crop region is computed from the visual's arranged bounds within this app.
    /// </remarks>
    /// <param name="visual">The visual to capture.</param>
    /// <param name="padding">Additional padding applied around the captured bounds (in cells).</param>
    /// <param name="options">Additional export options.</param>
    public string CaptureSvg(Visual visual, Thickness padding, CellBufferSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(visual);
        var buffer = GetRequiredRenderBuffer();
        var merged = (options ?? CellBufferSvgExportOptions.Default) with { Crop = GetVisualCaptureBounds(visual), Padding = padding, AutoCrop = false };
        return CellBufferSvgExporter.Export(buffer, merged);
    }

    internal CellBuffer GetRequiredRenderBuffer()
    {
        VerifyAccess();

        var buffer = _renderBuffer;
        if (buffer is null)
        {
            throw new InvalidOperationException("No rendered frame buffer is available yet. The app must render at least one frame before capturing a screenshot.");
        }

        return buffer;
    }

    internal Rectangle GetVisualCaptureBounds(Visual visual)
    {
        ArgumentNullException.ThrowIfNull(visual);
        VerifyAccess();
        return visual.GetAbsoluteBounds();
    }
}
