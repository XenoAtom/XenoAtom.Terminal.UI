// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Table : Visual
{
    private int[]? _columnWidths;

    public Table()
    {
        ShowHeaderSeparator = true;

        HeaderCells = new VisualList<Visual>(this, "Table.HeaderCells");
        RowCells = new BindableList<VisualList<Visual>>(this, "Table.RowCells", onAdding: ValidateRowOwner, onRemoving: DetachRow);
    }

    public VisualList<Visual> HeaderCells { get; }

    public BindableList<VisualList<Visual>> RowCells { get; }

    [Bindable]
    public partial bool ShowHeaderSeparator { get; set; }

    public VisualList<Visual> AddRow(params Visual[] cells)
    {
        var row = new VisualList<Visual>(this, "Table.Row");
        row.AddRange(cells);
        RowCells.Add(row);
        return row;
    }

    protected override int ChildrenCount
    {
        get
        {
            var count = HeaderCells.Count;
            for (var r = 0; r < RowCells.Count; r++)
            {
                count += RowCells[r].Count;
            }

            return count;
        }
    }

    protected override Visual GetChild(int index)
    {
        var headerCount = HeaderCells.Count;
        if ((uint)index < (uint)headerCount)
        {
            return HeaderCells[index];
        }

        index -= headerCount;

        for (var r = 0; r < RowCells.Count; r++)
        {
            var row = RowCells[r];
            var rowCount = row.Count;
            if ((uint)index < (uint)rowCount)
            {
                return row[index];
            }

            index -= rowCount;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Max(1, availableSize.Width);

        var columns = 0;
        columns = Math.Max(columns, HeaderCells.Count);

        for (var i = 0; i < RowCells.Count; i++)
        {
            columns = Math.Max(columns, RowCells[i].Count);
        }

        if (columns == 0)
        {
            _columnWidths = Array.Empty<int>();
            return new Size(0, 0);
        }

        var widths = new int[columns];

        for (var c = 0; c < columns; c++)
        {
            if (c < HeaderCells.Count)
            {
                var cell = HeaderCells[c];
                cell.Measure(new Size(int.MaxValue / 4, 1));
                widths[c] = Math.Max(widths[c], cell.DesiredSize.Width);
            }
        }

        for (var r = 0; r < RowCells.Count; r++)
        {
            var row = RowCells[r];
            for (var c = 0; c < columns; c++)
            {
                if (c >= row.Count)
                {
                    continue;
                }

                var cell = row[c];
                cell.Measure(new Size(int.MaxValue / 4, 1));
                widths[c] = Math.Max(widths[c], cell.DesiredSize.Width);
            }
        }

        // Full box:
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
        if (HeaderCells.Count > 0)
        {
            height += 1;
            if (ShowHeaderSeparator)
            {
                height += 1;
            }
        }
        height += RowCells.Count;

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

        if (HeaderCells.Count > 0)
        {
            ArrangeRow(finalRect, y, widths, HeaderCells);
            y++;

            if (ShowHeaderSeparator)
            {
                y++;
            }
        }

        for (var r = 0; r < RowCells.Count; r++, y++)
        {
            ArrangeRow(finalRect, y, widths, RowCells[r]);
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

        if (HeaderCells.Count > 0 && y < rect.Y + rect.Height)
        {
            FillInteriorRow(buffer, rect, y, CellStyle.None | TextStyle.Bold);
            DrawRowFrame(buffer, rect, y, widths, border, glyphs);
            y++;

            if (ShowHeaderSeparator && y < rect.Y + rect.Height)
            {
                DrawLine(buffer, rect, y, widths, border, glyphs, glyphs.TeeLeft, glyphs.Cross, glyphs.TeeRight);
                y++;
            }
        }

        for (var r = 0; r < RowCells.Count && y < rect.Y + rect.Height - 1; r++, y++)
        {
            DrawRowFrame(buffer, rect, y, widths, border, glyphs);
        }

        if (y < rect.Y + rect.Height)
        {
            DrawLine(buffer, rect, rect.Y + rect.Height - 1, widths, border, glyphs, glyphs.BottomLeft, glyphs.TeeBottom, glyphs.BottomRight);
        }
    }

    private static void FillInteriorRow(CellBuffer buffer, Rectangle rect, int y, CellStyle style)
    {
        for (var x = rect.X + 1; x < rect.X + rect.Width - 1; x++)
        {
            buffer.SetCell(x, y, new Rune(' '), style);
        }
    }

    private void ValidateRowOwner(VisualList<Visual> row)
    {
        if (!ReferenceEquals(row.VisualOwner, this))
        {
            throw new InvalidOperationException("RowCells can only contain rows created for this Table instance.");
        }
    }

    private void DetachRow(VisualList<Visual> row)
    {
        if (row.Count > 0)
        {
            row.Clear();
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
}

