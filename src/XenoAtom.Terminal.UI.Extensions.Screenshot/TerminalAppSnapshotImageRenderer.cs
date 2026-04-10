using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Renders a <see cref="Visual"/> tree through <see cref="TerminalApp"/> to a raster screenshot using an in-memory backend.
/// </summary>
public static class TerminalAppSnapshotImageRenderer
{
    /// <summary>
    /// Renders <paramref name="root"/> and returns the encoded screenshot bytes.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="format">The encoded image format.</param>
    /// <param name="width">The viewport width in cells.</param>
    /// <param name="height">The viewport height in cells.</param>
    /// <param name="theme">An optional theme to apply when the root has no local theme.</param>
    /// <param name="hoverPredicate">An optional predicate used to mark visuals as hovered before capture.</param>
    /// <param name="options">The export options.</param>
    /// <returns>The encoded screenshot bytes.</returns>
    public static byte[] Render(
        Visual root,
        ScreenshotImageFormat format,
        int width,
        int height,
        Theme? theme = null,
        Func<Visual, bool>? hoverPredicate = null,
        CellBufferImageExportOptions? options = null)
    {
        return Render(root, format, width, height, theme, hoverPredicate, beforeCapture: null, options);
    }

    /// <summary>
    /// Renders <paramref name="root"/> and returns the encoded screenshot bytes.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="format">The encoded image format.</param>
    /// <param name="width">The viewport width in cells.</param>
    /// <param name="height">The viewport height in cells.</param>
    /// <param name="theme">An optional theme to apply when the root has no local theme.</param>
    /// <param name="hoverPredicate">An optional predicate used to mark visuals as hovered before capture.</param>
    /// <param name="beforeCapture">An optional callback used to drive the in-memory app into a richer state before capture.</param>
    /// <param name="options">The export options.</param>
    /// <returns>The encoded screenshot bytes.</returns>
    public static byte[] Render(
        Visual root,
        ScreenshotImageFormat format,
        int width,
        int height,
        Theme? theme,
        Func<Visual, bool>? hoverPredicate,
        Action<TerminalApp, InMemoryTerminalBackend>? beforeCapture,
        CellBufferImageExportOptions? options)
    {
        return Run(root, width, height, theme, hoverPredicate, beforeCapture, app => app.CaptureScreenshot(format, options));
    }

    /// <summary>
    /// Renders <paramref name="root"/> and saves the screenshot to <paramref name="path"/>.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="path">The destination image path.</param>
    /// <param name="width">The viewport width in cells.</param>
    /// <param name="height">The viewport height in cells.</param>
    /// <param name="theme">An optional theme to apply when the root has no local theme.</param>
    /// <param name="hoverPredicate">An optional predicate used to mark visuals as hovered before capture.</param>
    /// <param name="options">The export options.</param>
    public static void Save(
        Visual root,
        string path,
        int width,
        int height,
        Theme? theme = null,
        Func<Visual, bool>? hoverPredicate = null,
        CellBufferImageExportOptions? options = null)
    {
        Save(root, path, width, height, theme, hoverPredicate, beforeCapture: null, options);
    }

    /// <summary>
    /// Renders <paramref name="root"/> and saves the screenshot to <paramref name="path"/>.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="path">The destination image path.</param>
    /// <param name="width">The viewport width in cells.</param>
    /// <param name="height">The viewport height in cells.</param>
    /// <param name="theme">An optional theme to apply when the root has no local theme.</param>
    /// <param name="hoverPredicate">An optional predicate used to mark visuals as hovered before capture.</param>
    /// <param name="beforeCapture">An optional callback used to drive the in-memory app into a richer state before capture.</param>
    /// <param name="options">The export options.</param>
    public static void Save(
        Visual root,
        string path,
        int width,
        int height,
        Theme? theme,
        Func<Visual, bool>? hoverPredicate,
        Action<TerminalApp, InMemoryTerminalBackend>? beforeCapture,
        CellBufferImageExportOptions? options)
    {
        Run(root, width, height, theme, hoverPredicate, beforeCapture, app =>
        {
            app.SaveScreenshot(path, options);
            return 0;
        });
    }

    private static T Run<T>(
        Visual root,
        int width,
        int height,
        Theme? theme,
        Func<Visual, bool>? hoverPredicate,
        Action<TerminalApp, InMemoryTerminalBackend>? beforeCapture,
        Func<TerminalApp, T> capture)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        using var backend = new InMemoryTerminalBackend(new TerminalSize(width, height));
        using var session = Terminal.Open(backend);
        var terminal = Terminal.Instance;

        var appOptions = new TerminalAppOptions
        {
            HostKind = TerminalHostKind.Fullscreen,
            EnableMouse = false,
            EnableBracketedPaste = false,
            DisableInputEcho = true,
            RawMode = TerminalRawModeKind.CBreak,
        };

        if (theme is not null && !root.HasLocalStyle(Theme.Key))
        {
            root.Style(theme);
        }

        var app = new TerminalApp(root, terminal, appOptions);
        app.BeginRun();
        try
        {
            var timestamp = Stopwatch.GetTimestamp();
            if (beforeCapture is null)
            {
                if (hoverPredicate is not null)
                {
                    foreach (var visual in app.Root.EnumerateVisualsDepthFirst())
                    {
                        if (hoverPredicate(visual))
                        {
                            visual.IsHovered = true;
                        }
                    }
                }

                app.Tick(timestamp);
                app.Tick(timestamp + 2);
                return capture(app);
            }

            app.Tick(timestamp);

            if (hoverPredicate is not null)
            {
                foreach (var visual in app.Root.EnumerateVisualsDepthFirst())
                {
                    if (hoverPredicate(visual))
                    {
                        visual.IsHovered = true;
                    }
                }
            }

            beforeCapture(app, backend);

            app.Tick(timestamp + 2);
            app.Tick(timestamp + 4);

            return capture(app);
        }
        finally
        {
            app.EndRun();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
