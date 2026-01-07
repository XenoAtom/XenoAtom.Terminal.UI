// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Table : Visual
{
    private readonly List<Visual> _visualChildren = new();
    private int[]? _columnWidths;

    public Table()
    {
        ShowHeaderSeparator = true;
    }

    [Bindable]
    public partial IReadOnlyList<string>? Headers { get; set; }

    [Bindable]
    public partial IReadOnlyList<IReadOnlyList<string>>? Rows { get; set; }

    private IReadOnlyList<Visual>? _headerCells;

    public IReadOnlyList<Visual>? HeaderCells
    {
        get => _headerCells;
        set
        {
            if (ReferenceEquals(_headerCells, value))
            {
                return;
            }

            _headerCells = value;
            RebuildVisualChildren();
            App?.RequestRender();
        }
    }

    private IReadOnlyList<IReadOnlyList<Visual>>? _rowCells;

    public IReadOnlyList<IReadOnlyList<Visual>>? RowCells
    {
        get => _rowCells;
        set
        {
            if (ReferenceEquals(_rowCells, value))
            {
                return;
            }

            _rowCells = value;
            RebuildVisualChildren();
            App?.RequestRender();
        }
    }

    [Bindable]
    public partial bool ShowHeaderSeparator { get; set; }

    protected override int ChildrenCount => _visualChildren.Count;

    protected override Visual GetChild(int index) => _visualChildren[index];

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Max(1, availableSize.Width);
        var headerCells = _headerCells;
        var rowCells = _rowCells;

        var headers = Headers;
        var rows = Rows;

        var columns = 0;
        if (headerCells is not null)
        {
            columns = Math.Max(columns, headerCells.Count);
        }
        else if (headers is not null)
        {
            columns = Math.Max(columns, headers.Count);
        }

        if (rowCells is not null)
        {
            for (var i = 0; i < rowCells.Count; i++)
            {
                columns = Math.Max(columns, rowCells[i].Count);
            }
        }
        else if (rows is not null)
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

        if (headerCells is not null)
        {
            for (var c = 0; c < columns; c++)
            {
                var cell = c < headerCells.Count ? headerCells[c] : null;
                if (cell is null)
                {
                    continue;
                }

                cell.Measure(new Size(int.MaxValue / 4, 1));
                widths[c] = Math.Max(widths[c], cell.DesiredSize.Width);
            }
        }
        else if (headers is not null)
        {
            for (var c = 0; c < columns; c++)
            {
                var text = c < headers.Count ? headers[c] : string.Empty;
                widths[c] = Math.Max(widths[c], TerminalTextUtility.GetWidth(text.AsSpan()));
            }
        }

        if (rowCells is not null)
        {
            for (var r = 0; r < rowCells.Count; r++)
            {
                var row = rowCells[r];
                for (var c = 0; c < columns; c++)
                {
                    var cell = c < row.Count ? row[c] : null;
                    if (cell is null)
                    {
                        continue;
                    }

                    cell.Measure(new Size(int.MaxValue / 4, 1));
                    widths[c] = Math.Max(widths[c], cell.DesiredSize.Width);
                }
            }
        }
        else if (rows is not null)
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
        if (headerCells is not null || headers is not null)
        {
            height += 1;
            if (ShowHeaderSeparator)
            {
                height += 1;
            }
        }
        height += rowCells?.Count ?? rows?.Count ?? 0;
        if ((headerCells is not null || headers is not null) && ShowHeaderSeparator)
        {
            // already included
        }

        return new Size(width, Math.Min(availableSize.Height, height));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var widths = _columnWidths;
        if (widths is null || widths.Length == 0)
        {
            return;
        }

        var y = finalRect.Y + 1;

        var hasHeader = _headerCells is not null || Headers is not null;
        if (hasHeader)
        {
            if (_headerCells is not null)
            {
                ArrangeRow(finalRect, y, widths, _headerCells);
            }
            y++;

            if (ShowHeaderSeparator)
            {
                y++;
            }
        }

        if (_rowCells is not null)
        {
            for (var r = 0; r < _rowCells.Count; r++, y++)
            {
                ArrangeRow(finalRect, y, widths, _rowCells[r]);
            }
        }
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
        var surface = CellStyle.None;

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

        var hasHeader = _headerCells is not null || Headers is not null;
        if (hasHeader)
        {
            if (_headerCells is not null)
            {
                DrawRowFrame(buffer, rect, y, widths, border, glyphs);
            }
            else
            {
                var headers = Headers;
                WriteRow(buffer, rect, y, headers ?? Array.Empty<string>(), widths, border, glyphs, CellStyle.None | TextStyle.Bold);
            }
            y++;

            if (ShowHeaderSeparator && y < rect.Y + rect.Height)
            {
                DrawLine(buffer, rect, y, widths, border, glyphs, glyphs.TeeLeft, glyphs.Cross, glyphs.TeeRight);
                y++;
            }
        }

        if (_rowCells is not null)
        {
            for (var r = 0; r < _rowCells.Count && y < rect.Y + rect.Height - 1; r++, y++)
            {
                DrawRowFrame(buffer, rect, y, widths, border, glyphs);
            }
        }
        else
        {
            var rows = Rows;
            if (rows is not null)
            {
                for (var r = 0; r < rows.Count && y < rect.Y + rect.Height - 1; r++, y++)
                {
                    WriteRow(buffer, rect, y, rows[r], widths, border, glyphs, CellStyle.None);
                }
            }
        }

        if (y < rect.Y + rect.Height)
        {
            DrawLine(buffer, rect, rect.Y + rect.Height - 1, widths, border, glyphs, glyphs.BottomLeft, glyphs.TeeBottom, glyphs.BottomRight);
        }
    }

    private void RebuildVisualChildren()
    {
        for (var i = 0; i < _visualChildren.Count; i++)
        {
            DetachChild(_visualChildren[i]);
        }

        _visualChildren.Clear();

        if (_headerCells is not null)
        {
            for (var i = 0; i < _headerCells.Count; i++)
            {
                var cell = _headerCells[i];
                if (cell is null)
                {
                    continue;
                }

                AttachChild(cell);
                _visualChildren.Add(cell);
            }
        }

        if (_rowCells is not null)
        {
            for (var r = 0; r < _rowCells.Count; r++)
            {
                var row = _rowCells[r];
                for (var c = 0; c < row.Count; c++)
                {
                    var cell = row[c];
                    if (cell is null)
                    {
                        continue;
                    }

                    AttachChild(cell);
                    _visualChildren.Add(cell);
                }
            }
        }
    }

    private static void ArrangeRow(Rectangle rect, int y, IReadOnlyList<int> widths, IReadOnlyList<Visual> rowCells)
    {
        var x = rect.X + 1; // after left border

        for (var c = 0; c < widths.Count; c++)
        {
            var contentWidth = widths[c];
            var contentX = x + 1; // skip left padding

            if (c < rowCells.Count)
            {
                var cell = rowCells[c];
                cell.Arrange(new Rectangle(contentX, y, Math.Max(0, contentWidth), 1));
            }

            x += contentWidth + 3; // padding + content + padding + separator

            if (x >= rect.X + rect.Width)
            {
                break;
            }
        }
    }

    private static void DrawRowFrame(CellBuffer buffer, Rectangle rect, int y, IReadOnlyList<int> widths, CellStyle border, LineGlyphs glyphs)
    {
        var x = rect.X;
        buffer.SetCell(x, y, glyphs.Vertical, border);
        x++;

        for (var c = 0; c < widths.Count; c++)
        {
            x += widths[c] + 2; // padding + content + padding
            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, glyphs.Vertical, border);
            x++;
        }
    }

    private static void DrawLine(CellBuffer buffer, Rectangle rect, int y, IReadOnlyList<int> widths, CellStyle border, LineGlyphs glyphs, Rune left, Rune middle, Rune right)
    {
        var x = rect.X;
        buffer.SetCell(x, y, left, border);
        x++;

        for (var c = 0; c < widths.Count; c++)
        {
            var w = widths[c] + 2;
            for (var i = 0; i < w && x < rect.X + rect.Width; i++, x++)
            {
                buffer.SetCell(x, y, glyphs.Horizontal, border);
            }

            if (x >= rect.X + rect.Width)
            {
                break;
            }

            buffer.SetCell(x, y, c + 1 < widths.Count ? middle : right, border);
            x++;
        }
    }

    private static void WriteRow(CellBuffer buffer, Rectangle rect, int y, IReadOnlyList<string> row, IReadOnlyList<int> widths, CellStyle border, LineGlyphs glyphs, CellStyle cellStyleStyle)
    {
        var x = rect.X;

        buffer.SetCell(x, y, glyphs.Vertical, border);
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

            buffer.SetCell(x, y, glyphs.Vertical, border);
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
