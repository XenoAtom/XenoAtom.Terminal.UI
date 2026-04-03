using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Adds raster screenshot capture helpers to <see cref="TerminalApp"/>.
/// </summary>
public static class TerminalAppScreenshotExtensions
{
    /// <summary>
    /// Captures the current frame buffer and returns the encoded screenshot bytes.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="format">The encoded image format.</param>
    /// <param name="options">The export options.</param>
    /// <returns>The encoded screenshot bytes.</returns>
    public static byte[] CaptureScreenshot(this TerminalApp app, ScreenshotImageFormat format = ScreenshotImageFormat.Png, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        return CellBufferImageExporter.Export(app.GetRequiredRenderBuffer(), format, options);
    }

    /// <summary>
    /// Captures the specified <paramref name="visual"/> from the current frame buffer and returns the encoded screenshot bytes.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="visual">The visual to capture.</param>
    /// <param name="padding">Additional padding around the visual bounds in cells.</param>
    /// <param name="format">The encoded image format.</param>
    /// <param name="options">The export options.</param>
    /// <returns>The encoded screenshot bytes.</returns>
    public static byte[] CaptureScreenshot(this TerminalApp app, Visual visual, Thickness padding, ScreenshotImageFormat format = ScreenshotImageFormat.Png, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(visual);

        var merged = (options ?? CellBufferImageExportOptions.Default) with
        {
            Crop = app.GetVisualCaptureBounds(visual),
            Padding = padding,
            AutoCrop = false,
        };

        return CellBufferImageExporter.Export(app.GetRequiredRenderBuffer(), format, merged);
    }

    /// <summary>
    /// Captures the current frame buffer and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="path">The destination image path.</param>
    /// <param name="options">The export options.</param>
    public static void SaveScreenshot(this TerminalApp app, string path, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        CellBufferImageExporter.Export(app.GetRequiredRenderBuffer(), path, options);
    }

    /// <summary>
    /// Captures the specified <paramref name="visual"/> from the current frame buffer and writes it to <paramref name="path"/>.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="visual">The visual to capture.</param>
    /// <param name="path">The destination image path.</param>
    /// <param name="padding">Additional padding around the visual bounds in cells.</param>
    /// <param name="options">The export options.</param>
    public static void SaveScreenshot(this TerminalApp app, Visual visual, string path, Thickness padding, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(visual);

        var merged = (options ?? CellBufferImageExportOptions.Default) with
        {
            Crop = app.GetVisualCaptureBounds(visual),
            Padding = padding,
            AutoCrop = false,
        };

        CellBufferImageExporter.Export(app.GetRequiredRenderBuffer(), path, merged);
    }
}
