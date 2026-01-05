// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using System.Text;

namespace XenoAtom.Terminal.UI;

public sealed partial class Table : Visual
{
    private int[]? _columnWidths;

    public Table()
    {
        ShowHeaderSeparator = true;
    }

    [Bindable]
    public partial IReadOnlyList<string>? Headers { get; set; }

    [Bindable]
    public partial IReadOnlyList<IReadOnlyList<string>>? Rows { get; set; }

    [Bindable]
    public partial bool ShowHeaderSeparator { get; set; }

    protected override CellSize MeasureOverride(CellSize availableSize)
    {
        var width = Math.Max(1, availableSize.Width);
        var headers = Headers;
        var rows = Rows;

        var columns = 0;
        if (headers is not null)
        {
            columns = Math.Max(columns, headers.Count);
        }
        if (rows is not null)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                columns = Math.Max(columns, rows[i].Count);
            }
        }

        if (columns == 0)
        {
            _columnWidths = Array.Empty<int>();
            return new CellSize(0, 0);
        }

        var widths = new int[columns];

        if (headers is not null)
        {
            for (var c = 0; c < columns; c++)
            {
                var text = c < headers.Count ? headers[c] : string.Empty;
                widths[c] = Math.Max(widths[c], TerminalTextUtility.GetWidth(text.AsSpan()));
            }
        }

        if (rows is not null)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (var c = 0; c < columns; c++)
                {
                    var text = c < row.Count ? row[c] : string.Empty;
                    widths[c] = Math.Max(widths[c], TerminalTextUtility.GetWidth(text.AsSpan()));
                }
            }
        }

        // Border + separators: | c0 | c1 | => columns + 1 separators, plus 2 spaces per column.
        var required = 1 + columns + (columns * 2);
        for (var c = 0; c < columns; c++)
        {
            required += widths[c];
        }

        if (required > width)
        {
            var availableForText = Math.Max(0, width - (1 + columns + (columns * 2)));
            var perColumn = Math.Max(1, availableForText / columns);
            for (var c = 0; c < columns; c++)
            {
                widths[c] = Math.Min(widths[c], perColumn);
            }
        }

        _columnWidths = widths;

        var height = (headers is not null ? 1 : 0) + (rows?.Count ?? 0);
        if (headers is not null && ShowHeaderSeparator)
        {
            height++;
        }

        return new CellSize(width, Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(CellRect finalRect)
    {
        Bounds = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var widths = _columnWidths;
        if (widths is null || widths.Length == 0)
        {
            return;
        }

        var theme = GetTheme();
        var border = theme.BorderStyle(focused: false);

        var y = rect.Y;
        var headers = Headers;
        if (headers is not null)
        {
            WriteRow(buffer, rect, y, headers, widths, border, CellStyle.Bold);
            y++;

            if (ShowHeaderSeparator && y < rect.Y + rect.Height)
            {
                DrawSeparator(buffer, rect, y, widths, border);
                y++;
            }
        }

        var rows = Rows;
        if (rows is null)
        {
            return;
        }

        for (var r = 0; r < rows.Count && y < rect.Y + rect.Height; r++, y++)
        {
            WriteRow(buffer, rect, y, rows[r], widths, border, CellStyle.None);
        }
    }

    private static void DrawSeparator(CellBuffer buffer, CellRect rect, int y, IReadOnlyList<int> widths, CellStyle border)
    {
        var x = rect.X;
        buffer.SetCell(x, y, new Rune('+'), border);
        x++;

        for (var c = 0; c < widths.Count; c++)
        {
            var w = widths[c] + 2;
            for (var i = 0; i < w && x < rect.X + rect.Width; i++, x++)
            {
                buffer.SetCell(x, y, new Rune('-'), border);
            }

            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune('+'), border);
            x++;
        }
    }

    private static void WriteRow(CellBuffer buffer, CellRect rect, int y, IReadOnlyList<string> row, IReadOnlyList<int> widths, CellStyle border, CellStyle cellStyle)
    {
        var x = rect.X;

        buffer.SetCell(x, y, new Rune('|'), border);
        x++;

        for (var c = 0; c < widths.Count && x < rect.X + rect.Width; c++)
        {
            buffer.SetCell(x, y, new Rune(' '), cellStyle);
            x++;

            var contentWidth = widths[c];
            var text = c < row.Count ? row[c] : string.Empty;
            WriteClipped(buffer, x, y, text.AsSpan(), contentWidth, cellStyle);

            x += contentWidth;
            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune(' '), cellStyle);
            x++;

            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune('|'), border);
            x++;
        }
    }

    private static void WriteClipped(CellBuffer buffer, int x, int y, ReadOnlySpan<char> text, int maxCells, CellStyle style)
    {
        if (maxCells <= 0)
        {
            return;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text, maxCells, out var endIndex))
        {
            endIndex = text.Length;
        }

        var clipped = text[..Math.Clamp(endIndex, 0, text.Length)];
        buffer.WriteText(x, y, clipped, style);

        var writtenWidth = TerminalTextUtility.GetWidth(clipped);
        for (var i = writtenWidth; i < maxCells; i++)
        {
            buffer.SetCell(x + i, y, new Rune(' '), style);
        }
    }
}
