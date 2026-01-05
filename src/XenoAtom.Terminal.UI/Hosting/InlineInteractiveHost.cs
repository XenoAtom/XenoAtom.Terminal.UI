// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed class InlineInteractiveHost : IDisposable
{
    private readonly TerminalInstance _terminal;
    private int _reservedHeight;

    public InlineInteractiveHost(TerminalInstance terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
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
    }

    public void WriteMarkupLine(string markup)
    {
        ArgumentNullException.ThrowIfNull(markup);

        if (_reservedHeight > 0)
        {
            ClearReservedRegion();
            _reservedHeight = 0;
        }

        _terminal.WriteMarkupLine(markup);
    }

    public void Render(IReadOnlyList<string> markupLines)
    {
        ArgumentNullException.ThrowIfNull(markupLines);

        _terminal.ShowCursor(false);

        var height = Math.Max(1, markupLines.Count);

        if (_reservedHeight > 0)
        {
            ClearReservedRegion();
            _reservedHeight = 0;
        }

        for (var i = 0; i < height; i++)
        {
            _terminal.EraseLine(2);

            if (i < markupLines.Count)
            {
                _terminal.WriteMarkup(markupLines[i]);
            }

            _terminal.NextLine();
        }

        _reservedHeight = height;
    }

    private void ClearReservedRegion()
    {
        _terminal.CursorUp(_reservedHeight);

        for (var i = 0; i < _reservedHeight; i++)
        {
            _terminal.EraseLine(2);
            _terminal.NextLine();
        }

        _terminal.CursorUp(_reservedHeight);
    }
}

