// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

public sealed class CellBufferDiffRenderer
{
    private int[]? _lastScalars;
    private CellStyle[]? _lastCells;
    private int _lastWidth;
    private int _lastHeight;

    public void Reset()
    {
        _lastScalars = null;
        _lastCells = null;
        _lastWidth = 0;
        _lastHeight = 0;
    }

    public void RenderFullscreen(TerminalInstance terminal, CellBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(buffer);

        var width = Math.Max(1, terminal.Size.Columns);
        var height = Math.Max(1, terminal.Size.Rows);

        if (buffer.Width != width || buffer.Height != height)
        {
            throw new InvalidOperationException("Fullscreen render requires a buffer sized to the current viewport.");
        }

        var forceFull = _lastScalars is null || _lastWidth != width || _lastHeight != height;
        EnsureLastBuffers(width, height);

        var caps = CreateAnsiCapabilities(terminal.Capabilities);

        using var builder = new AnsiBuilder(initialCapacity: width * height + 128);
        var writer = new AnsiWriter(builder, caps);

        if (forceFull)
        {
            writer.CursorPosition(1, 1);
            writer.EraseDisplay(2);
        }

        var currentStyle = AnsiStyle.Default;

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;

        var lastScalars = _lastScalars!;
        var lastCells = _lastCells!;

        Span<char> runeBuffer = stackalloc char[2];

        for (var y = 0; y < height; y++)
        {
            var rowIndex = y * width;
            var firstChanged = -1;
            var lastChanged = -1;

            for (var x = 0; x < width; x++)
            {
                var i = rowIndex + x;
                if (forceFull || scalars[i] != lastScalars[i] || cells[i] != lastCells[i])
                {
                    firstChanged = x;
                    break;
                }
            }

            if (firstChanged < 0)
            {
                continue;
            }

            for (var x = width - 1; x >= firstChanged; x--)
            {
                var i = rowIndex + x;
                if (forceFull || scalars[i] != lastScalars[i] || cells[i] != lastCells[i])
                {
                    lastChanged = x;
                    break;
                }
            }

            if (lastChanged < 0)
            {
                continue;
            }

            firstChanged = AdjustStartForWideGlyph(cells, rowIndex, firstChanged);
            lastChanged = AdjustEndForWideGlyph(scalars, cells, rowIndex, lastChanged, width);

            writer.CursorPosition(y + 1, firstChanged + 1);

            var xPos = firstChanged;
            while (xPos <= lastChanged)
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
        }

        if (currentStyle != AnsiStyle.Default)
        {
            writer.StyleTransition(currentStyle, AnsiStyle.Default);
        }

        terminal.WriteAtomic((TextWriter w) =>
        {
            w.Write(builder.UnsafeAsSpan());
        });

        scalars.CopyTo(lastScalars.AsSpan());
        cells.CopyTo(lastCells.AsSpan());
    }

    private void EnsureLastBuffers(int width, int height)
    {
        var length = checked(width * height);
        if (_lastScalars is null || _lastCells is null || _lastScalars.Length != length)
        {
            _lastScalars = new int[length];
            _lastCells = new CellStyle[length];
        }

        _lastWidth = width;
        _lastHeight = height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AdjustStartForWideGlyph(ReadOnlySpan<CellStyle> cells, int rowIndex, int x)
    {
        var cell = cells[rowIndex + x];
        if (cell.IsContinuation && x > 0)
        {
            return x - 1;
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AdjustEndForWideGlyph(ReadOnlySpan<int> scalars, ReadOnlySpan<CellStyle> cells, int rowIndex, int x, int width)
    {
        var cell = cells[rowIndex + x];
        if (cell.IsContinuation)
        {
            return x;
        }

        var scalar = scalars[rowIndex + x];
        if (scalar != 0 && TerminalTextUtility.GetRuneWidth(new Rune(scalar)) > 1)
        {
            return Math.Min(width - 1, x + 1);
        }

        return x;
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
