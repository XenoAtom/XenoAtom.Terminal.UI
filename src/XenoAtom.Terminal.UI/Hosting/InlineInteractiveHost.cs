// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Hosting;

public sealed class InlineInteractiveHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private readonly AnsiBuilder _builder = new(initialCapacity: 4096);
    private int _reservedHeight;
    private int? _liveRegionTopRow;
    private bool _hasSavedCursorPosition;
    private int[]? _lastScalars;
    private CellStyle[]? _lastCells;
    private ulong[]? _lastHyperlinks;
    private Dictionary<ulong, string>? _lastHyperlinkTable;
    private int _lastWidth;
    private int _lastHeight;
    private bool _lastCursorVisible;
    private int _lastRenderedCursorX;
    private int _lastRenderedCursorY;
    private bool _lastWantsCursor;
    private int _lastCursorX;
    private int _lastCursorY;

    public InlineInteractiveHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    public int ReservedHeight => _reservedHeight;

    public int? LiveRegionTopRow => _liveRegionTopRow;

    internal void PrepareForUserUpdate()
    {
        if (!_hasSavedCursorPosition || _reservedHeight <= 0)
        {
            return;
        }

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var regionHeight = Math.Min(_reservedHeight, visibleHeight);

        _terminal.WriteAtomic(writer =>
        {
            writer.PrivateMode(2026, enabled: true);

            if (_lastWantsCursor || _lastCursorVisible)
            {
                writer.ShowCursor(false);
            }

            writer.RestoreCursorPosition();
            writer.CursorUp(regionHeight);
            writer.CursorHorizontalAbsolute(1);

            for (var i = 0; i < regionHeight; i++)
            {
                writer.EraseLine(2);
                writer.NextLine();
            }

            writer.CursorUp(regionHeight);
            writer.CursorHorizontalAbsolute(1);
            writer.ResetStyle();

            writer.PrivateMode(2026, enabled: false);
        });

        _hasSavedCursorPosition = false;
        _reservedHeight = 0;
        _liveRegionTopRow = null;
    }

    internal void FinalizeAfterLive()
    {
        if (!_hasSavedCursorPosition)
        {
            return;
        }

        _terminal.WriteAtomic(writer =>
        {
            writer.PrivateMode(2026, enabled: true);

            // Ensure the cursor is not left inside the live region (e.g. focused TextBox).
            if (_lastWantsCursor || _lastCursorVisible)
            {
                writer.ShowCursor(false);
            }

            writer.RestoreCursorPosition();
            writer.CursorHorizontalAbsolute(1);
            writer.ResetStyle();
            writer.ShowCursor(true);

            writer.PrivateMode(2026, enabled: false);
        });

        _lastCursorVisible = true;
        _lastWantsCursor = false;
    }

    public void Dispose()
    {
        try
        {
            _terminal.ResetStyle();
            _terminal.ShowCursor(true);
        }
        catch
        {
            // Best effort.
        }

        try
        {
            _builder.Dispose();
        }
        catch
        {
            // Best effort.
        }
    }

    public void WriteMarkupLine(string markup)
    {
        ArgumentNullException.ThrowIfNull(markup);

        WriteFlowLines([markup]);
    }

    public void WriteMarkupLines(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);
        WriteFlowLines(markupLines);
    }

    public void Render(CellBuffer buffer, bool wantsCursor, int cursorX, int cursorY)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _lastWantsCursor = wantsCursor;
        _lastCursorX = cursorX;
        _lastCursorY = cursorY;

        var width = Math.Max(1, _terminal.Size.Columns);
        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var previousHeight = _hasSavedCursorPosition ? Math.Min(_reservedHeight, visibleHeight) : 0;
        var height = Math.Clamp(Math.Max(1, buffer.Height), 1, visibleHeight);

        if (_liveRegionTopRow is null)
        {
            if (_terminal.Capabilities.SupportsCursorPositionGet && _terminal.TryGetCursorPosition(out var position))
            {
                _liveRegionTopRow = Math.Clamp(position.Row, 0, visibleHeight - 1);
            }
            else
            {
                _liveRegionTopRow = Math.Clamp(_terminal.Cursor.Position.Row, 0, visibleHeight - 1);
            }
        }

        if (buffer.Width != width)
        {
            throw new InvalidOperationException("Inline render requires a buffer sized to the current viewport width.");
        }

        var forceFull = _lastScalars is null || _lastCells is null || _lastWidth != width || _lastHeight != height || previousHeight != height;
        EnsureLastBuffers(width, height);

        var cursorChanged = wantsCursor != _lastCursorVisible;
        if (wantsCursor && _lastCursorVisible && (_lastRenderedCursorX != cursorX || _lastRenderedCursorY != cursorY))
        {
            cursorChanged = true;
        }

        var scalars = buffer.UnsafeScalars;
        var cells = buffer.UnsafeCells;
        var hyperlinks = buffer.UnsafeHyperlinks;
        var lastScalars = _lastScalars!;
        var lastCells = _lastCells!;
        var lastHyperlinks = _lastHyperlinks!;

        if (!forceFull)
        {
            var length = width * height;
            var anyCellChanged = false;
            for (var i = 0; i < length; i++)
            {
                if (scalars[i] != lastScalars[i] || cells[i] != lastCells[i] || hyperlinks[i] != lastHyperlinks[i])
                {
                    anyCellChanged = true;
                    break;
                }
            }

            if (!anyCellChanged)
            {
                if (!cursorChanged)
                {
                    return;
                }

                if (_hasSavedCursorPosition)
                {
                    var capsLocal = CreateAnsiCapabilities(_terminal.Capabilities);
                    _builder.Clear();
                    var writerLocal = new AnsiWriter(_builder, capsLocal);

                    writerLocal.PrivateMode(2026, enabled: true);

                    var hideCursorDuringWriteLocal = wantsCursor || _lastCursorVisible;
                    if (hideCursorDuringWriteLocal)
                    {
                        writerLocal.ShowCursor(false);
                    }

                    writerLocal.RestoreCursorPosition();

                    if (wantsCursor)
                    {
                        var cx = Math.Clamp(cursorX, 0, width - 1);
                        var cy = Math.Clamp(cursorY, 0, height - 1);
                        writerLocal.CursorUp(height - cy);
                        writerLocal.CursorHorizontalAbsolute(cx + 1);
                        writerLocal.ShowCursor(true);
                    }

                    writerLocal.PrivateMode(2026, enabled: false);

                    _terminal.Write(_builder.UnsafeAsSpan()); // atomic write with a single span

                    _lastCursorVisible = wantsCursor;
                    _lastRenderedCursorX = cursorX;
                    _lastRenderedCursorY = cursorY;
                    return;
                }
            }
        }

        var caps = CreateAnsiCapabilities(_terminal.Capabilities);
        _builder.Clear();
        var writer = new AnsiWriter(_builder, caps);

        writer.PrivateMode(2026, enabled: true);

        var hideCursorDuringWrite = wantsCursor || _lastCursorVisible;
        if (hideCursorDuringWrite)
        {
            writer.ShowCursor(false);
        }

        if (_hasSavedCursorPosition)
        {
            writer.RestoreCursorPosition();
        }

        if (previousHeight > 0)
        {
            writer.CursorUp(previousHeight);
        }

        writer.CursorHorizontalAbsolute(1);

        var currentStyle = AnsiStyle.Default;
        ulong currentHyperlink = 0;
        Span<char> runeBuffer = stackalloc char[2];

        if (forceFull)
        {
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

                writer.NextLine();
            }
        }
        else
        {
            var currentRow = 0;
            var anyRowChanged = false;

            for (var y = 0; y < height; y++)
            {
                var rowIndex = y * width;
                var firstChanged = -1;
                var lastChanged = -1;

                for (var x = 0; x < width; x++)
                {
                    var i = rowIndex + x;
                    if (scalars[i] != lastScalars[i] || cells[i] != lastCells[i] || hyperlinks[i] != lastHyperlinks[i])
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
                    if (scalars[i] != lastScalars[i] || cells[i] != lastCells[i] || hyperlinks[i] != lastHyperlinks[i])
                    {
                        lastChanged = x;
                        break;
                    }
                }

                if (lastChanged < 0)
                {
                    continue;
                }

                anyRowChanged = true;

                if (y > currentRow)
                {
                    writer.NextLine(y - currentRow);
                    currentRow = y;
                }

                firstChanged = AdjustStartForWideGlyph(cells, rowIndex, firstChanged);
                lastChanged = AdjustEndForWideGlyph(scalars, cells, rowIndex, lastChanged, width);

                writer.CursorHorizontalAbsolute(firstChanged + 1);

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

                currentRow = y;
            }

            if (!anyRowChanged)
            {
                return;
            }

            if (currentRow < height)
            {
                writer.NextLine(height - currentRow);
            }
        }

        if (previousHeight > height)
        {
            for (var i = height; i < previousHeight; i++)
            {
                writer.EraseLine(2);
                writer.NextLine();
            }

            writer.CursorUp(previousHeight - height);
            writer.CursorHorizontalAbsolute(1);
        }

        if (currentStyle != AnsiStyle.Default)
        {
            writer.StyleTransition(currentStyle, AnsiStyle.Default);
            currentStyle = AnsiStyle.Default;
        }

        if (currentHyperlink != 0)
        {
            writer.EndLink();
            currentHyperlink = 0;
        }

        writer.SaveCursorPosition();

        if (wantsCursor)
        {
            var cx = Math.Clamp(cursorX, 0, width - 1);
            var cy = Math.Clamp(cursorY, 0, height - 1);
            writer.CursorUp(height - cy);
            writer.CursorHorizontalAbsolute(cx + 1);
            writer.ShowCursor(true);
        }

        writer.PrivateMode(2026, enabled: false);

        _terminal.Write(_builder.UnsafeAsSpan()); // atomic write with a single span

        scalars.Slice(0, width * height).CopyTo(lastScalars.AsSpan());
        cells.Slice(0, width * height).CopyTo(lastCells.AsSpan());
        hyperlinks.Slice(0, width * height).CopyTo(lastHyperlinks.AsSpan());
        _lastHyperlinkTable ??= new Dictionary<ulong, string>();
        buffer.CopyHyperlinkTableTo(_lastHyperlinkTable);
        _hasSavedCursorPosition = true;
        _reservedHeight = height;
        _liveRegionTopRow = Math.Min(_liveRegionTopRow.GetValueOrDefault(), visibleHeight - height);
        _lastWidth = width;
        _lastHeight = height;
        _lastCursorVisible = wantsCursor;
        _lastRenderedCursorX = cursorX;
        _lastRenderedCursorY = cursorY;
    }

    private void WriteFlowLines(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);

        if (markupLines.Count == 0)
        {
            return;
        }

        if (_reservedHeight == 0)
        {
            WritePlainFlowLines(markupLines);
            return;
        }

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var regionHeight = Math.Min(_reservedHeight, visibleHeight);
        if (_lastScalars is null || _lastCells is null || _lastWidth <= 0 || _lastHeight <= 0)
        {
            WritePlainFlowLines(markupLines);
            return;
        }

        var width = Math.Max(1, _terminal.Size.Columns);
        var caps = CreateAnsiCapabilities(_terminal.Capabilities);

        _builder.Clear();
        var writer = new AnsiWriter(_builder, caps);
        var formatter = new AnsiMarkup(writer);

        writer.PrivateMode(2026, enabled: true);

        if (_lastWantsCursor || _lastCursorVisible)
        {
            writer.ShowCursor(false);
        }

        if (_hasSavedCursorPosition)
        {
            writer.RestoreCursorPosition();
        }

        writer.CursorUp(regionHeight);
        writer.CursorHorizontalAbsolute(1);

        for (var i = 0; i < regionHeight; i++)
        {
            writer.EraseLine(2);
            writer.NextLine();
        }

        writer.CursorUp(regionHeight);
        writer.CursorHorizontalAbsolute(1);

        foreach (var line in markupLines)
        {
            writer.EraseLine(2);
            if (!string.IsNullOrEmpty(line))
            {
                formatter.Write(line);
            }
            writer.NextLine();
        }

        WriteStoredRegion(writer, width, regionHeight);

        writer.SaveCursorPosition();

        if (_lastWantsCursor)
        {
            var cx = Math.Clamp(_lastCursorX, 0, width - 1);
            var cy = Math.Clamp(_lastCursorY, 0, regionHeight - 1);
            writer.CursorUp(regionHeight - cy);
            writer.CursorHorizontalAbsolute(cx + 1);
            writer.ShowCursor(true);
        }

        writer.PrivateMode(2026, enabled: false);

        _terminal.WriteAtomic((TextWriter w) =>
        {
            w.Write(_builder.UnsafeAsSpan());
        });

        _hasSavedCursorPosition = true;
        _reservedHeight = regionHeight;
        if (_liveRegionTopRow is not null)
        {
            _liveRegionTopRow = Math.Min(_liveRegionTopRow.Value + markupLines.Count, visibleHeight - regionHeight);
        }
    }

    private void WritePlainFlowLines(IReadOnlyList<string> markupLines)
    {
        var caps = CreateAnsiCapabilities(_terminal.Capabilities);
        _builder.Clear();
        var writer = new AnsiWriter(_builder, caps);
        var formatter = new AnsiMarkup(writer);

        writer.PrivateMode(2026, enabled: true);

        foreach (var line in markupLines)
        {
            writer.EraseLine(2);
            if (!string.IsNullOrEmpty(line))
            {
                formatter.Write(line);
            }
            writer.NextLine();
        }

        writer.PrivateMode(2026, enabled: false);

        _terminal.WriteAtomic((TextWriter w) =>
        {
            w.Write(_builder.UnsafeAsSpan());
        });
    }

    private void EnsureLastBuffers(int width, int height)
    {
        var length = width * height;
        if (_lastScalars is null || _lastCells is null || _lastHyperlinks is null || _lastScalars.Length != length)
        {
            _lastScalars = new int[length];
            _lastCells = new CellStyle[length];
            _lastHyperlinks = new ulong[length];
        }

        _lastWidth = width;
        _lastHeight = height;
    }

    private void WriteStoredRegion(AnsiWriter writer, int width, int height)
    {
        var scalars = _lastScalars!;
        var cells = _lastCells!;
        var hyperlinks = _lastHyperlinks!;
        var linkTable = _lastHyperlinkTable;

        var currentStyle = AnsiStyle.Default;
        ulong currentHyperlink = 0;
        Span<char> runeBuffer = stackalloc char[2];

        for (var y = 0; y < height; y++)
        {
            var rowIndex = y * width;
            writer.CursorHorizontalAbsolute(1);

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
                    if (nextHyperlink != 0 && linkTable is not null && linkTable.TryGetValue(nextHyperlink, out var uri))
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

            writer.NextLine();
        }

        if (currentHyperlink != 0)
        {
            writer.EndLink();
        }

        if (currentStyle != AnsiStyle.Default)
        {
            writer.StyleTransition(currentStyle, AnsiStyle.Default);
        }
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
