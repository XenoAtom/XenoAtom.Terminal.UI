// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

internal static class TerminalVisualWriter
{
    public static void Write(TerminalInstance terminal, Visual visual)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(visual);

        if (visual.Parent is not null)
        {
            throw new InvalidOperationException("A visual that is already in the UI tree cannot be written as flow output.");
        }

        ThemedHost? themedHost = null;
        var root = visual;
        if (!visual.HasLocalStyle(Theme.Key))
        {
            themedHost = new ThemedHost(visual, Theme.Terminal);
            root = themedHost;
        }

        var width = Math.Max(1, terminal.Size.Columns);

        try
        {
            root.Measure(new LayoutConstraints(0, width, 0, LayoutConstants.Infinite));
            root.Arrange(new Rectangle(0, 0, width, root.DesiredSize.Height));

            var height = Math.Max(1, root.DesiredSize.Height);
            var buffer = new CellBuffer(width, height);
            buffer.Clear(root.GetTheme().BaseTextStyle());
            root.RenderTree(buffer);

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

                    var runeWidth = TerminalTextUtility.GetRuneWidth(rune);
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

            writer.PrivateMode(2026, enabled: false);

            terminal.WriteAtomic((TextWriter w) => w.Write(builder.UnsafeAsSpan()));
        }
        finally
        {
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

}
