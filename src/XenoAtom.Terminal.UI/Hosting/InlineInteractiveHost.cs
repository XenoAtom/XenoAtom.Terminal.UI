// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI;

public sealed class InlineInteractiveHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private int _reservedHeight;
    private readonly List<string> _lastLines = new();
    private bool _cursorHidden;

    public InlineInteractiveHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    public int ReservedHeight => _reservedHeight;

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

    public void Render(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);

        EnsureCursorHidden();

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var previousHeight = Math.Min(_reservedHeight, visibleHeight);
        var height = Math.Clamp(Math.Max(1, markupLines.Count), 1, visibleHeight);

        var anyChanges = previousHeight != height;
        if (!anyChanges)
        {
            for (var i = 0; i < height; i++)
            {
                var newLine = i < markupLines.Count ? markupLines[i] : string.Empty;
                var oldLine = i < _lastLines.Count ? _lastLines[i] : string.Empty;
                if (!string.Equals(oldLine, newLine, StringComparison.Ordinal))
                {
                    anyChanges = true;
                    break;
                }
            }
        }

        if (!anyChanges)
        {
            return;
        }

        _terminal.WriteAtomic(writer =>
        {
            var formatter = new AnsiMarkup(writer);

            if (previousHeight > 0)
            {
                writer.CursorUp(previousHeight);
            }

            writer.CursorHorizontalAbsolute(1);

            for (var i = 0; i < height; i++)
            {
                var newLine = i < markupLines.Count ? markupLines[i] : string.Empty;
                var oldLine = i < _lastLines.Count ? _lastLines[i] : string.Empty;

                if (!string.Equals(oldLine, newLine, StringComparison.Ordinal))
                {
                    writer.EraseLine(2);
                    if (newLine.Length > 0)
                    {
                        formatter.Write(newLine);
                    }
                }

                writer.NextLine();
            }

            if (previousHeight > height)
            {
                writer.SaveCursorPosition();

                for (var i = height; i < previousHeight; i++)
                {
                    writer.EraseLine(2);
                    writer.NextLine();
                }

                writer.RestoreCursorPosition();
                writer.CursorHorizontalAbsolute(1);
            }
        });

        _reservedHeight = height;
        _lastLines.Clear();
        for (var i = 0; i < height; i++)
        {
            _lastLines.Add(i < markupLines.Count ? markupLines[i] : string.Empty);
        }
    }

    private void EnsureCursorHidden()
    {
        if (_cursorHidden)
        {
            return;
        }

        _terminal.ShowCursor(false);
        _cursorHidden = true;
    }

    private void WriteFlowLines(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);

        if (markupLines.Count == 0)
        {
            return;
        }

        EnsureCursorHidden();

        if (_reservedHeight == 0)
        {
            foreach (var line in markupLines)
            {
                _terminal.WriteMarkupLine(line);
            }
            return;
        }

        var visibleHeight = Math.Max(1, _terminal.Size.Rows);
        var regionHeight = Math.Min(_reservedHeight, visibleHeight);

        var lastRegionLines = new string[regionHeight];
        for (var i = 0; i < regionHeight; i++)
        {
            lastRegionLines[i] = i < _lastLines.Count ? _lastLines[i] : string.Empty;
        }

        _terminal.WriteAtomic(writer =>
        {
            var formatter = new AnsiMarkup(writer);

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
                formatter.Write(line);
                writer.NextLine();
            }

            for (var i = 0; i < regionHeight; i++)
            {
                writer.EraseLine(2);
                if (i < lastRegionLines.Length && lastRegionLines[i].Length > 0)
                {
                    formatter.Write(lastRegionLines[i]);
                }
                writer.NextLine();
            }
        });

        _reservedHeight = regionHeight;
        _lastLines.Clear();
        _lastLines.AddRange(lastRegionLines);
    }
}
