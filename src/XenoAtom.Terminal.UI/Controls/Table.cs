// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Table : Visuals.Visual
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

    protected override Size MeasureOverride(Size availableSize)
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
            return new Size(0, 0);
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

        // Full box:
        // Top/bottom lines: ┌─┬─┐, inner separators: │ ... │
        // For N columns: N content areas with 2 padding spaces, plus N+1 vertical separators.
        // Also includes an outer border and optional header separator row.
        var required = 1 + columns + (columns * 2);
        for (var c = 0; c < columns; c++)
        {
            required += widths[c];
        }
        required = Math.Min(width, Math.Max(0, required));

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

        var height = 2; // top + bottom
        if (headers is not null)
        {
            height += 1;
            if (ShowHeaderSeparator)
            {
                height += 1;
            }
        }
        height += rows?.Count ?? 0;
        if (headers is not null && ShowHeaderSeparator)
        {
            // already included
        }

        return new Size(width, Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
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
        var glyphs = theme.Lines;
        var border = theme.BorderStyle(focused: false);
        var surface = theme.SurfaceStyle();

        // Fill background.
        for (var yFill = rect.Y; yFill < rect.Y + rect.Height; yFill++)
        {
            for (var xFill = rect.X; xFill < rect.X + rect.Width; xFill++)
            {
                buffer.SetCell(xFill, yFill, new Rune(' '), surface);
            }
        }

        var y = rect.Y;
        DrawLine(buffer, rect, y, widths, border, glyphs, glyphs.TopLeft, glyphs.TeeTop, glyphs.TopRight);
        y++;

        var headers = Headers;
        if (headers is not null)
        {
            WriteRow(buffer, rect, y, headers, widths, border, glyphs, CellStyle.None | TextStyle.Bold);
            y++;

            if (ShowHeaderSeparator && y < rect.Y + rect.Height)
            {
                DrawLine(buffer, rect, y, widths, border, glyphs, glyphs.TeeLeft, glyphs.Cross, glyphs.TeeRight);
                y++;
            }
        }

        var rows = Rows;
        if (rows is not null)
        {
            for (var r = 0; r < rows.Count && y < rect.Y + rect.Height - 1; r++, y++)
            {
                WriteRow(buffer, rect, y, rows[r], widths, border, glyphs, CellStyle.None);
            }
        }

        if (y < rect.Y + rect.Height)
        {
            DrawLine(buffer, rect, rect.Y + rect.Height - 1, widths, border, glyphs, glyphs.BottomLeft, glyphs.TeeBottom, glyphs.BottomRight);
        }
    }

    private static void DrawLine(CellBuffer buffer, Rectangle rect, int y, IReadOnlyList<int> widths, CellStyle border, LineGlyphs glyphs, char left, char middle, char right)
    {
        var x = rect.X;
        buffer.SetCell(x, y, new Rune(left), border);
        x++;

        for (var c = 0; c < widths.Count; c++)
        {
            var w = widths[c] + 2;
            for (var i = 0; i < w && x < rect.X + rect.Width; i++, x++)
            {
                buffer.SetCell(x, y, new Rune(glyphs.Horizontal), border);
            }

            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune(c + 1 < widths.Count ? middle : right), border);
            x++;
        }
    }

    private static void WriteRow(CellBuffer buffer, Rectangle rect, int y, IReadOnlyList<string> row, IReadOnlyList<int> widths, CellStyle border, LineGlyphs glyphs, CellStyle cellStyleStyle)
    {
        var x = rect.X;

        buffer.SetCell(x, y, new Rune(glyphs.Vertical), border);
        x++;

        for (var c = 0; c < widths.Count && x < rect.X + rect.Width; c++)
        {
            buffer.SetCell(x, y, new Rune(' '), cellStyleStyle);
            x++;

            var contentWidth = widths[c];
            var text = c < row.Count ? row[c] : string.Empty;
            WriteClipped(buffer, x, y, text.AsSpan(), contentWidth, cellStyleStyle);

            x += contentWidth;
            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune(' '), cellStyleStyle);
            x++;

            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, new Rune(glyphs.Vertical), border);
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
