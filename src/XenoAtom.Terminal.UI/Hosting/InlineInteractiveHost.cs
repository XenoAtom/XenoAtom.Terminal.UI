// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Hosting;

/// <summary>
/// Hosts inline interactive rendering using a single buffered output.
/// </summary>
public sealed class InlineInteractiveHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private readonly AnsiBuilder _builder = new(initialCapacity: 4096);
    private int _reservedHeight;
    private int? _liveRegionTopRow;
    private bool _hasSavedCursorPosition;
    private int[]? _lastScalars;
    private Style[]? _lastCells;
    private ulong[]? _lastHyperlinks;
    private Dictionary<ulong, string>? _lastHyperlinkTable;
    private Dictionary<int, string>? _lastTextElementTable;
    private int _lastWidth;
    private int _lastHeight;
    private bool _lastCursorVisible;
    private int _lastRenderedCursorX;
    private int _lastRenderedCursorY;
    private bool _lastWantsCursor;
    private int _lastCursorX;
    private int _lastCursorY;

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineInteractiveHost"/> class.
    /// </summary>
    /// <param name="terminal">The terminal instance.</param>
    public InlineInteractiveHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>
    /// Gets the number of reserved rows for the live region.
    /// </summary>
    public int ReservedHeight => _reservedHeight;

    /// <summary>
    /// Gets the top row of the live region if known.
    /// </summary>
    public int? LiveRegionTopRow => _liveRegionTopRow;

    internal void HandleResize()
    {
        // A resize can cause terminal emulators to reflow the previously rendered lines, which means the live region
        // from the last frame might be wrapped and pushed below the expected area. We keep the saved cursor position
        // (so we can still navigate relative to the region), but we invalidate all cached diff state so the next frame
        // will repaint fully.
        _lastScalars = null;
        _lastCells = null;
        _lastHyperlinks = null;
        _lastHyperlinkTable = null;
        _lastTextElementTable = null;
    }

    internal void PrepareForUserUpdate()
    {
        if (_reservedHeight <= 0)
        {
            return;
        }

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var regionHeight = Math.Min(_reservedHeight, visibleHeight);
        var cursorOffset = _lastCursorVisible
            ? Math.Clamp(_lastRenderedCursorY, 0, Math.Max(0, regionHeight - 1))
            : regionHeight;

        _terminal.WriteAtomic(writer =>
        {
            writer.PrivateMode(2026, enabled: true);

            if (_lastWantsCursor || _lastCursorVisible)
            {
                writer.ShowCursor(false);
            }

            if (cursorOffset > 0)
            {
                writer.CursorUp(cursorOffset);
            }
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
        if (_reservedHeight <= 0)
        {
            return;
        }

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var regionHeight = Math.Min(_reservedHeight, visibleHeight);
        var cursorOffset = _lastCursorVisible
            ? Math.Clamp(_lastRenderedCursorY, 0, Math.Max(0, regionHeight - 1))
            : regionHeight;

        _terminal.WriteAtomic(writer =>
        {
            writer.PrivateMode(2026, enabled: true);

            // Ensure the cursor is not left inside the live region (e.g. focused TextBox).
            if (_lastWantsCursor || _lastCursorVisible)
            {
                writer.ShowCursor(false);
            }

            if (_lastCursorVisible)
            {
                writer.CursorDown(regionHeight - cursorOffset);
            }
            writer.CursorHorizontalAbsolute(1);
            writer.ResetStyle();
            writer.ShowCursor(true);

            writer.PrivateMode(2026, enabled: false);
        });

        _lastCursorVisible = true;
        _lastWantsCursor = false;
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Writes a single markup line above the live region.
    /// </summary>
    /// <param name="markup">The markup line.</param>
    public void WriteMarkupLine(string markup)
    {
        ArgumentNullException.ThrowIfNull(markup);

        WriteFlowLines([markup]);
    }

    /// <summary>
    /// Writes markup lines above the live region.
    /// </summary>
    /// <param name="markupLines">The markup lines.</param>
    public void WriteMarkupLines(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);
        WriteFlowLines(markupLines);
    }

    /// <summary>
    /// Renders the live region using a cell buffer.
    /// </summary>
    /// <param name="buffer">The buffer to render.</param>
    /// <param name="wantsCursor">Whether the cursor should be visible.</param>
    /// <param name="cursorX">The cursor X position.</param>
    /// <param name="cursorY">The cursor Y position.</param>
    public void Render(CellBuffer buffer, bool wantsCursor, int cursorX, int cursorY)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _lastWantsCursor = wantsCursor;
        _lastCursorX = cursorX;
        _lastCursorY = cursorY;

        var viewportWidth = Math.Max(1, _terminal.Size.Columns);
        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var existingHeight = _reservedHeight <= 0 ? 0 : Math.Min(_reservedHeight, visibleHeight);
        var height = Math.Clamp(Math.Max(1, buffer.Height), 1, visibleHeight);

        // It's possible for the terminal size to change between when the caller sized the buffer
        // and when we actually render it (e.g., during resize). Avoid throwing and let the next
        // frame repaint with the new viewport size.
        if (buffer.Width != viewportWidth)
        {
            HandleResize();
            return;
        }

        var width = buffer.Width;

        var viewportWidthChanged = _lastWidth != 0 && _lastWidth != width;

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

        var forceFull = _lastScalars is null || _lastCells is null || _lastWidth != width || _lastHeight != height || existingHeight != height;
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

        if (_hasSavedCursorPosition && existingHeight > 0)
        {
            writer.CursorUp(existingHeight);
        }

        if (!_hasSavedCursorPosition && existingHeight > 0)
        {
            var cursorOffset = _lastCursorVisible
                ? Math.Clamp(_lastRenderedCursorY, 0, Math.Max(0, existingHeight - 1))
                : existingHeight;

            if (cursorOffset > 0)
            {
                writer.CursorUp(cursorOffset);
            }
        }

        writer.CursorHorizontalAbsolute(1);

        if (existingHeight > 0 && viewportWidthChanged)
        {
            // On horizontal resize, many terminals reflow prior output. Clearing from the live-region start downwards
            // prevents wrapped leftovers from "flooding" below the region on the next repaint.
            writer.EraseInDisplay(0);
            writer.CursorHorizontalAbsolute(1);
            forceFull = true;
        }

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
                lastChanged = AdjustEndForWideGlyph(buffer, scalars, cells, rowIndex, lastChanged, width);

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

        if (existingHeight > height)
        {
            for (var i = height; i < existingHeight; i++)
            {
                writer.EraseLine(2);
                writer.NextLine();
            }

            writer.CursorUp(existingHeight - height);
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
        _lastTextElementTable ??= new Dictionary<int, string>();
        buffer.CopyTextElementTableTo(_lastTextElementTable);
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
            _lastCells = new Style[length];
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
        var textElementTable = _lastTextElementTable;

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

                if (scalar < 0 && textElementTable is not null && textElementTable.TryGetValue(scalar, out var textElement))
                {
                    writer.Write(textElement);
                    xPos += Math.Max(1, TerminalTextUtility.GetWidth(textElement.AsSpan()));
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
    private static int AdjustStartForWideGlyph(ReadOnlySpan<Style> cells, int rowIndex, int x)
    {
        var cell = cells[rowIndex + x];
        if (cell.IsContinuation && x > 0)
        {
            return x - 1;
        }

        return x;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AdjustEndForWideGlyph(CellBuffer buffer, ReadOnlySpan<int> scalars, ReadOnlySpan<Style> cells, int rowIndex, int x, int width)
    {
        var cell = cells[rowIndex + x];
        if (cell.IsContinuation)
        {
            return x;
        }

        var scalar = scalars[rowIndex + x];
        if (scalar < 0 && buffer.TryGetTextElement(scalar, out _, out var elementWidth) && elementWidth > 1)
        {
            return Math.Min(width - 1, x + 1);
        }

        if (scalar > 0 && TerminalTextUtility.GetRuneWidth(new Rune(scalar)) > 1)
        {
            return Math.Min(width - 1, x + 1);
        }

        return x;
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
