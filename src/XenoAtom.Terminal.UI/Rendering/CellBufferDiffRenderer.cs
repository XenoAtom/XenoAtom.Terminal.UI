// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Rendering;

public sealed class CellBufferDiffRenderer : IDisposable
{
    private readonly AnsiBuilder _builder = new(initialCapacity: 4096);
    private int[]? _lastScalars;
    private CellStyle[]? _lastCells;
    private ulong[]? _lastHyperlinks;
    private int _lastWidth;
    private int _lastHeight;
    private bool _lastCursorVisible;
    private int _lastCursorX;
    private int _lastCursorY;

    public void Reset()
    {
        _lastScalars = null;
        _lastCells = null;
        _lastHyperlinks = null;
        _lastWidth = 0;
        _lastHeight = 0;
        _lastCursorVisible = false;
        _lastCursorX = 0;
        _lastCursorY = 0;
    }

    public void Dispose() => _builder.Dispose();

    public void RenderFullscreen(TerminalInstance terminal, CellBuffer buffer, bool wantsCursor, int cursorX, int cursorY)
    {
        ArgumentNullException.ThrowIfNull(terminal);
        ArgumentNullException.ThrowIfNull(buffer);

        // Use the buffer size as the render viewport.
        // Terminal size can change between consecutive reads while a resize is in progress, so relying on
        // terminal.Size here can cause spurious mismatches/crashes even though the caller sized the buffer
        // based on the terminal size it observed.
        var width = Math.Max(1, buffer.Width);
        var height = Math.Max(1, buffer.Height);

        var forceFull = _lastScalars is null || _lastWidth != width || _lastHeight != height;
        EnsureLastBuffers(width, height);

        var cursorChanged = wantsCursor != _lastCursorVisible;
        if (!cursorChanged && wantsCursor && (_lastCursorX != cursorX || _lastCursorY != cursorY))
        {
            cursorChanged = true;
        }

        var caps = CreateAnsiCapabilities(terminal.Capabilities);

        _builder.Clear();
        var writer = new AnsiWriter(_builder, caps);

        var currentStyle = AnsiStyle.Default;

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var hyperlinks = buffer.UnsafeHyperlinks;

        var lastScalars = _lastScalars!;
        var lastCells = _lastCells!;
        var lastHyperlinks = _lastHyperlinks!;

        Span<char> runeBuffer = stackalloc char[2];
        var anyCellChanges = forceFull;
        var hasOutput = false;
        ulong currentHyperlink = 0;

        void BeginOutput()
        {
            if (hasOutput)
            {
                return;
            }

            writer.PrivateMode(2026, enabled: true);

            if (forceFull)
            {
                writer.CursorPosition(1, 1);
                writer.EraseDisplay(2);
            }

            // Hide cursor while writing to avoid cursor artifacts/flicker.
            if (wantsCursor || _lastCursorVisible)
            {
                writer.ShowCursor(false);
            }

            hasOutput = true;
        }

        if (forceFull)
        {
            BeginOutput();
        }

        for (var y = 0; y < height; y++)
        {
            var rowIndex = y * width;
            var firstChanged = -1;
            var lastChanged = -1;

            for (var x = 0; x < width; x++)
            {
                var i = rowIndex + x;
                if (forceFull || scalars[i] != lastScalars[i] || cells[i] != lastCells[i] || hyperlinks[i] != lastHyperlinks[i])
                {
                    firstChanged = x;
                    break;
                }
            }

            if (firstChanged < 0)
            {
                continue;
            }

            anyCellChanges = true;
            for (var x = width - 1; x >= firstChanged; x--)
            {
                var i = rowIndex + x;
                if (forceFull || scalars[i] != lastScalars[i] || cells[i] != lastCells[i] || hyperlinks[i] != lastHyperlinks[i])
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

            BeginOutput();
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
        }

        if (!anyCellChanges && !cursorChanged)
        {
            return;
        }

        BeginOutput();

        if (currentHyperlink != 0)
        {
            writer.EndLink();
            currentHyperlink = 0;
        }

        if (currentStyle != AnsiStyle.Default)
        {
            writer.StyleTransition(currentStyle, AnsiStyle.Default);
        }

        if (wantsCursor && cursorChanged)
        {
            var cx = Math.Clamp(cursorX, 0, width - 1);
            var cy = Math.Clamp(cursorY, 0, height - 1);
            writer.CursorPosition(cy + 1, cx + 1);
            writer.ShowCursor(true);
        }

        writer.PrivateMode(2026, enabled: false);

        terminal.WriteAtomic((TextWriter w) =>
        {
            w.Write(_builder.UnsafeAsSpan());
        });

        if (anyCellChanges)
        {
            scalars.CopyTo(lastScalars.AsSpan());
            cells.CopyTo(lastCells.AsSpan());
            hyperlinks.CopyTo(lastHyperlinks.AsSpan());
        }

        _lastCursorVisible = wantsCursor;
        _lastCursorX = cursorX;
        _lastCursorY = cursorY;
    }

    private void EnsureLastBuffers(int width, int height)
    {
        var length = checked(width * height);
        if (_lastScalars is null || _lastCells is null || _lastHyperlinks is null || _lastScalars.Length != length)
        {
            _lastScalars = new int[length];
            _lastCells = new CellStyle[length];
            _lastHyperlinks = new ulong[length];
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
