// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Geometry;
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

        var width = Math.Max(1, terminal.Size.Columns);

        visual.Measure(new Size(width, int.MaxValue / 4));
        visual.Arrange(new Rectangle(0, 0, width, visual.DesiredSize.Height));

        var height = Math.Max(1, visual.DesiredSize.Height);
        var buffer = new CellBuffer(width, height);
        buffer.Clear(visual.GetTheme().ForegroundTextStyle());
        visual.RenderTree(buffer);

        var caps = CreateAnsiCapabilities(terminal.Capabilities);
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

    private static AnsiStyle MapStyle(CellStyle cellStyle)
    {
        cellStyle = cellStyle.WithoutContinuation();
        var deco = cellStyle.ToAnsiDecorations();

        AnsiColor? fg = null;
        AnsiColor? bg = null;

        if (cellStyle.TryGetForeground(out var fgColor))
        {
            fg = fgColor;
        }

        if (cellStyle.TryGetBackground(out var bgColor))
        {
            bg = bgColor;
        }

        if (deco == AnsiDecorations.None && fg is null && bg is null)
        {
            return AnsiStyle.Default;
        }

        return new AnsiStyle
        {
            Foreground = fg ?? AnsiColor.Default,
            Background = bg ?? AnsiColor.Default,
            Decorations = deco,
        };
    }

    private static AnsiCapabilities CreateAnsiCapabilities(TerminalCapabilities caps)
    {
        var colorLevel = caps.ColorLevel switch
        {
            TerminalColorLevel.None => AnsiColorLevel.None,
            TerminalColorLevel.Color16 => AnsiColorLevel.Colors16,
            TerminalColorLevel.Color256 => AnsiColorLevel.Colors256,
            _ => AnsiColorLevel.TrueColor,
        };

        return new AnsiCapabilities
        {
            AnsiEnabled = caps.AnsiEnabled,
            ColorLevel = colorLevel,
            SupportsOsc8 = caps.SupportsOsc8Links,
            Prefer7BitC1 = true,
            SafeMode = false,
            OscTermination = AnsiOscTermination.StringTerminator,
        };
    }
}
