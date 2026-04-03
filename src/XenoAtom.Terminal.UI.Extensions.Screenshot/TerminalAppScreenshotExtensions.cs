using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Adds raster screenshot capture helpers to <see cref="TerminalApp"/>.
/// </summary>
public static class TerminalAppScreenshotExtensions
{
    /// <summary>
    /// Captures the current frame buffer as a PNG image and copies it to the clipboard.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="options">The screenshot export options.</param>
    /// <returns><see langword="true"/> if the clipboard was updated; otherwise <see langword="false"/>.</returns>
    public static bool TryCopyScreenshotToClipboard(this TerminalApp app, CellBufferImageExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bytes = app.CaptureScreenshot(ScreenshotImageFormat.Png, options);
        return app.Terminal.Clipboard.TrySetData(TerminalClipboardFormats.Png, bytes);
    }

    /// <summary>
    /// Registers a global command that captures the current frame buffer as a PNG image and copies it to the clipboard.
    /// </summary>
    /// <param name="app">The source app.</param>
    /// <param name="options">The command options.</param>
    public static void RegisterClipboardScreenshotCommand(this TerminalApp app, ScreenshotClipboardCommandOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.AddGlobalCommand(CreateClipboardScreenshotCommand(options ?? ScreenshotClipboardCommandOptions.Default));
    }

    /// <summary>
    /// Registers a command on the specified visual that captures the current app frame buffer as a PNG image and copies it to the clipboard.
    /// Register this on the root visual before running the app when you want an app-wide shortcut without manually constructing a <see cref="TerminalApp"/>.
    /// </summary>
    /// <param name="visual">The visual that owns the command.</param>
    /// <param name="options">The command options.</param>
    public static void RegisterClipboardScreenshotCommand(this Visual visual, ScreenshotClipboardCommandOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(visual);
        var effectiveOptions = options ?? ScreenshotClipboardCommandOptions.Default;
        visual.AddCommand(CreateClipboardScreenshotCommand(effectiveOptions));
        TerminalApp? lastRegisteredApp = null;

        void TryRegister(Visual owner)
        {
            if (owner.App is not { } app || ReferenceEquals(app, lastRegisteredApp))
            {
                return;
            }

            app.AddGlobalCommand(CreateClipboardScreenshotCommand(effectiveOptions));
            lastRegisteredApp = app;
        }

        TryRegister(visual);
        visual.RegisterDynamicUpdate(TryRegister);
    }

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

    private static Command CreateClipboardScreenshotCommand(ScreenshotClipboardCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new Command
        {
            Id = options.CommandId,
            LabelMarkup = options.LabelMarkup,
            Name = options.Name,
            DescriptionMarkup = options.DescriptionMarkup,
            SearchText = options.SearchText,
            Gesture = options.Gesture,
            Importance = options.Importance,
            Presentation = options.Presentation,
            CanExecute = static visual => visual.App?.Terminal.Clipboard.CanSetFormats == true,
            ConsumesGestureWhenUnavailable = options.ConsumesGestureWhenUnavailable,
            Execute = visual =>
            {
                if (visual.App is { } app)
                {
                    _ = app.TryCopyScreenshotToClipboard(options.ImageOptions);
                }
            },
        };
    }
}
