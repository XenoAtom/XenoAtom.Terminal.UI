// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

/// <summary>
/// Renders a <see cref="Visual"/> tree through <see cref="TerminalApp"/> to a <see cref="CellBuffer"/>-backed SVG snapshot.
/// </summary>
/// <remarks>
/// This is intended for deterministic screenshot generation (docs) and tests.
/// It runs a minimal fullscreen app on an in-memory terminal backend so app-dependent visuals (window layer, command bar, tooltips)
/// can render correctly.
/// </remarks>
public static class TerminalAppSnapshotRenderer
{
    /// <summary>
    /// Renders <paramref name="root"/> to an SVG document using a fullscreen <see cref="TerminalApp"/> on an in-memory backend.
    /// </summary>
    /// <param name="root">The root visual.</param>
    /// <param name="width">Viewport width (cells).</param>
    /// <param name="height">Viewport height (cells).</param>
    /// <param name="theme">Optional theme to apply when the root doesn't define one.</param>
    /// <param name="hoverPredicate">Optional predicate selecting visuals that should be considered hovered during the snapshot.</param>
    /// <param name="options">SVG export options (cropping/padding, colors, cell size).</param>
    public static string RenderSvg(
        Visual root,
        int width,
        int height,
        Theme? theme = null,
        Func<Visual, bool>? hoverPredicate = null,
        CellBufferSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        // Use an in-memory backend to avoid interacting with the real terminal.
        using var session = Terminal.Open(new InMemoryTerminalBackend(new TerminalSize(width, height)));
        var terminal = Terminal.Instance;

        var appOptions = new TerminalAppOptions
        {
            HostKind = TerminalHostKind.Fullscreen,
            EnableMouse = false,
            EnableBracketedPaste = false,
            DisableInputEcho = true,
            RawMode = TerminalRawModeKind.CBreak,
        };

        // If the caller didn't set a theme in the tree, apply the provided one so base clears and styles are stable.
        if (theme is not null && !root.HasLocalStyle(Theme.Key))
        {
            root.Style(theme);
        }

        var app = new TerminalApp(root, terminal, appOptions);
        app.BeginRun();
        try
        {
            if (hoverPredicate is not null)
            {
                foreach (var v in app.Root.EnumerateVisualsDepthFirst())
                {
                    if (hoverPredicate(v))
                    {
                        v.IsHovered = true;
                    }
                }
            }

            // Tooltips schedule opening on the first tick, and only open after the delay threshold is passed.
            // Use two deterministic ticks to allow that transition (even when delay=0, the minimum is 1 tick).
            var t0 = Stopwatch.GetTimestamp();
            app.Tick(t0);
            app.Tick(t0 + 2);

            return app.CaptureSvg(options);
        }
        finally
        {
            app.EndRun();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
