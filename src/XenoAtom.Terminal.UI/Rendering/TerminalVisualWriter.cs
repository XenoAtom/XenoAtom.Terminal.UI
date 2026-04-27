// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

internal static class TerminalVisualWriter
{
    public static void Write(TerminalInstance terminal, Visual visual)
        => Write(terminal, visual, options: null);

    public static void Write(TerminalInstance terminal, Visual visual, TerminalWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(visual);

        if (visual.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be written as flow output.");
        }

        var graphicsPresenter = options?.GraphicsPresenter;
        ThemedHost? themedHost = null;
        var root = visual;
        if (!visual.HasLocalStyle(Theme.Key))
        {
            themedHost = new ThemedHost(visual, Theme.Terminal);
            root = themedHost;
        }
        else if (graphicsPresenter is not null)
        {
            // TerminalApp applies host-level flow defaults to its root. Wrap already-themed visuals too so one-shot
            // graphics rendering does not mutate user-owned style state.
            themedHost = new ThemedHost(visual, Theme.Terminal);
            root = themedHost;
        }

        var width = Math.Max(1, terminal.Size.Columns);
        TerminalApp? graphicsApp = null;
        TerminalGraphicsPresentContext? graphicsPresentContext = null;
        GraphicsCommandBuffer? graphicsCommands = null;

        try
        {
            if (graphicsPresenter is not null)
            {
                graphicsApp = new TerminalApp(root, terminal, new TerminalAppOptions
                {
                    HostKind = TerminalHostKind.Inline,
                    EnableMouse = false,
                    EnableBracketedPaste = false,
                    DisableInputEcho = false,
                    GraphicsPresenter = new NonDisposingGraphicsPresenter(graphicsPresenter),
                });
                graphicsApp.Root.AttachToApp(graphicsApp);
            }

            root.Measure(new LayoutConstraints(0, width, 0, LayoutConstants.Infinite));
            root.Arrange(new Rectangle(0, 0, width, root.DesiredSize.Height));

            var height = Math.Max(1, root.DesiredSize.Height);
            var buffer = new CellBuffer(width, height);
            buffer.Clear(root.GetTheme().BaseTextStyle());
            root.RenderTree(buffer);

            if (graphicsPresenter is not null && graphicsApp is not null)
            {
                graphicsCommands = new GraphicsCommandBuffer();
                var graphicsRenderContext = new GraphicsRenderContext(graphicsCommands);
                var viewportBounds = ResolveViewportBounds(terminal, width, height);
                var textRepaintBounds = new Rectangle(0, 0, width, height);
                CollectGraphicsCommands(root, graphicsRenderContext, textRepaintBounds);
                graphicsPresentContext = new TerminalGraphicsPresentContext(
                    graphicsApp,
                    terminal,
                    TerminalHostKind.Inline,
                    viewportBounds,
                    frameIndex: 1,
                    textRepaintBounds,
                    TerminalGraphicsTextFrameKind.Full);
            }

            var caps = AnsiCapabilitiesFactory.Create(terminal.Capabilities);
            using var builder = new AnsiBuilder(initialCapacity: (width * height) + 128);
            var writer = new AnsiWriter(builder, caps);

            writer.PrivateMode(2026, enabled: true);

            var currentStyle = AnsiStyle.Default;
            ulong currentHyperlink = 0;
            Span<char> runeBuffer = stackalloc char[2];

            var scalars = buffer.UnsafeScalars;
            var cells = buffer.UnsafeCells;
            var hyperlinks = buffer.UnsafeHyperlinks;

            for (var y = 0; y < height; y++)
            {
                var rowIndex = y * width;
                var xPos = 0;
                while (xPos < width)
                {
                    var i = rowIndex + xPos;
                    var cell = cells[i];
                    if (cell.IsContinuation)
                    {
                        xPos++;
                        continue;
                    }

                    var nextStyle = MapStyle(cell);
                    if (nextStyle != currentStyle)
                    {
                        writer.StyleTransition(currentStyle, nextStyle);
                        currentStyle = nextStyle;
                    }

                    var nextHyperlink = hyperlinks[i];
                    if (nextHyperlink != currentHyperlink)
                    {
                        if (currentHyperlink != 0)
                        {
                            writer.EndLink();
                        }

                        currentHyperlink = 0;
                        if (nextHyperlink != 0 && buffer.TryGetHyperlinkUri(nextHyperlink, out var uri))
                        {
                            writer.BeginLink(uri);
                            currentHyperlink = nextHyperlink;
                        }
                    }

                    var scalar = scalars[i];
                    if (scalar == 0)
                    {
                        writer.Write(" ");
                        xPos++;
                        continue;
                    }

                    if (scalar < 0 && buffer.TryGetTextElement(scalar, out var textElement, out var elementWidth))
                    {
                        writer.Write(textElement);
                        xPos += Math.Max(1, elementWidth);
                        continue;
                    }

                    var rune = new Rune(scalar);
                    var written = rune.EncodeToUtf16(runeBuffer);
                    writer.Write(runeBuffer[..written]);

                    var runeWidth = buffer.GetRuneWidth(rune);
                    xPos += Math.Max(1, runeWidth);
                }

                if (currentHyperlink != 0)
                {
                    writer.EndLink();
                    currentHyperlink = 0;
                }

                writer.Write("\n");
            }

            if (currentHyperlink != 0)
            {
                writer.EndLink();
            }

            if (currentStyle != AnsiStyle.Default)
            {
                writer.StyleTransition(currentStyle, AnsiStyle.Default);
            }

            if (graphicsPresenter is IBufferedTerminalGraphicsPresenter bufferedGraphicsPresenter &&
                graphicsCommands is not null &&
                graphicsPresentContext is not null &&
                bufferedGraphicsPresenter.HasPendingOutput(graphicsCommands, graphicsPresentContext))
            {
                PresentBufferedGraphics(bufferedGraphicsPresenter, graphicsCommands, graphicsPresentContext, writer);
            }

            writer.PrivateMode(2026, enabled: false);

            terminal.WriteAtomic((TextWriter w) => w.Write(builder.UnsafeAsSpan()));

            if (graphicsPresenter is not IBufferedTerminalGraphicsPresenter && graphicsCommands is not null && graphicsPresentContext is not null)
            {
                PresentGraphics(graphicsPresenter!, graphicsCommands, graphicsPresentContext);
            }
        }
        finally
        {
            if (graphicsApp is not null)
            {
                graphicsApp.Root.DetachFromApp();
                graphicsApp.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            if (themedHost is not null)
            {
                themedHost.Content = null;
            }
        }
    }

    private static AnsiStyle MapStyle(Style style)
    {
        style = style.WithoutContinuation();
        var deco = style.ToAnsiDecorations();

        Color? fg = null;
        Color? bg = null;

        if (style.TryGetForeground(out var fgColor))
        {
            fg = fgColor;
        }

        if (style.TryGetBackground(out var bgColor))
        {
            bg = bgColor;
        }

        if (deco == AnsiDecorations.None && fg is null && bg is null)
        {
            return AnsiStyle.Default;
        }

        return new AnsiStyle
        {
            Foreground = fg ?? Color.Default,
            Background = bg ?? Color.Default,
            Decorations = deco,
        };
    }

    private static void CollectGraphicsCommands(Visual root, GraphicsRenderContext context, in Rectangle clipBounds)
    {
        context.BeginFrame();
        if (root.GraphicsRenderableSubtreeCount <= 0)
        {
            return;
        }

        CollectGraphicsCommandsRecursive(root, context, clipBounds);
    }

    private static void CollectGraphicsCommandsRecursive(Visual visual, GraphicsRenderContext context, in Rectangle clipBounds)
    {
        if (visual.GraphicsRenderableSubtreeCount <= 0 || !visual.IsVisible)
        {
            return;
        }

        var bounds = visual.Bounds;
        var effectiveClip = Intersect(clipBounds, bounds);
        if (effectiveClip.Width <= 0 || effectiveClip.Height <= 0)
        {
            return;
        }

        var childrenCount = visual.GetChildrenCount();
        if (visual is IGraphicsRenderableVisual graphics)
        {
            using var session = BindingManager.Current.StartTracking();
            context.BeginVisual(visual.GraphicsRenderId, effectiveClip);
            graphics.RenderGraphics(context);
            visual.UpdateGraphicsRenderDependencies(session.Reads);
        }

        for (var i = 0; i < childrenCount; i++)
        {
            CollectGraphicsCommandsRecursive(visual.GetChildUnsafe(i), context, effectiveClip);
        }
    }

    private static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        var x1 = Math.Max(a.X, b.X);
        var y1 = Math.Max(a.Y, b.Y);
        var x2 = Math.Min(a.Right, b.Right);
        var y2 = Math.Min(a.Bottom, b.Bottom);
        return x2 <= x1 || y2 <= y1 ? default : new Rectangle(x1, y1, x2 - x1, y2 - y1);
    }

    private static Rectangle ResolveViewportBounds(TerminalInstance terminal, int width, int height)
    {
        var row = 0;
        var column = 0;
        if (terminal.TryGetCursorPosition(out var position))
        {
            row = Math.Max(0, position.Row);
            column = Math.Max(0, position.Column);
        }

        return new Rectangle(column, row, width, height);
    }

    private static void PresentBufferedGraphics(IBufferedTerminalGraphicsPresenter presenter, GraphicsCommandBuffer commands, TerminalGraphicsPresentContext context, AnsiWriter writer)
    {
        var presentTask = presenter.PresentAsync(commands, context, writer, CancellationToken.None);
        if (presentTask.IsCompletedSuccessfully)
        {
            presentTask.GetAwaiter().GetResult();
        }
        else
        {
            presentTask.AsTask().GetAwaiter().GetResult();
        }
    }

    private static void PresentGraphics(ITerminalGraphicsPresenter presenter, GraphicsCommandBuffer commands, TerminalGraphicsPresentContext context)
    {
        var presentTask = presenter.PresentAsync(commands, context, CancellationToken.None);
        if (presentTask.IsCompletedSuccessfully)
        {
            presentTask.GetAwaiter().GetResult();
        }
        else
        {
            presentTask.AsTask().GetAwaiter().GetResult();
        }
    }

    private sealed class NonDisposingGraphicsPresenter(ITerminalGraphicsPresenter inner) : ITerminalGraphicsPresenter
    {
        public TerminalGraphicsCapabilities Capabilities => inner.Capabilities;

        public bool CanPresent(TerminalGraphicsCapabilities capabilities) => inner.CanPresent(capabilities);

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
            => inner.PresentAsync(current, context, cancellationToken);

        public void Reset() => inner.Reset();

        public void Dispose()
        {
        }
    }

}
