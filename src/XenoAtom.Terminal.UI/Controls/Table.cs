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
        this.ShowHeaderSeparator(true);

        HeaderCells = new VisualList<Visual>(this, "Table.HeaderCells");
        RowCells = new BindableList<VisualList<Visual>>(this, "Table.RowCells", onAdding: ValidateRowOwner, onRemoving: DetachRow);
    }

    public VisualList<Visual> HeaderCells { get; }

    public BindableList<VisualList<Visual>> RowCells { get; }

    [Bindable]
    public partial bool ShowHeaderSeparator { get; set; }

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
        var columns = GetColumnCount();
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
                cell.Measure(new Size(LayoutConstants.Infinite, LayoutConstants.Infinite));
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
                cell.Measure(new Size(LayoutConstants.Infinite, LayoutConstants.Infinite));
                widths[c] = Math.Max(widths[c], cell.DesiredSize.Width);
            }
        }

        var tableStyle = Get<TableStyle>();
        var (showOuterBorder, showVerticalLines, showRowSeparators, showHeaderSeparator, padding) = ResolveOptions(tableStyle);

        FitColumnWidthsToWidth(widths, availableSize.Width, padding.Horizontal, showOuterBorder, showVerticalLines, expandToAvailable: false);

        _columnWidths = widths;

        var desiredWidth = ComputeRequiredWidth(widths, padding.Horizontal, showOuterBorder, showVerticalLines);

        var rowHeight = GetRowHeight(padding);
        var desiredHeight = ComputeRequiredHeight(
            headerCount: HeaderCells.Count,
            rowCount: RowCells.Count,
            rowHeight,
            showOuterBorder,
            showHeaderSeparator,
            showRowSeparators);

        return new Size(
            Math.Min(availableSize.Width, desiredWidth),
            Math.Min(availableSize.Height, desiredHeight));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

        var widths = _columnWidths;
        if (widths is null || widths.Length == 0)
        {
            return;
        }

        var tableStyle = Get<TableStyle>();
        var (showOuterBorder, showVerticalLines, showRowSeparators, showHeaderSeparator, padding) = ResolveOptions(tableStyle);

        FitColumnWidthsToWidth(widths, finalRect.Width, padding.Horizontal, showOuterBorder, showVerticalLines, expandToAvailable: true);

        var columns = widths.Length;
        var rowHeight = GetRowHeight(padding);

        var xStart = finalRect.X + (showOuterBorder ? 1 : 0);
        var y = finalRect.Y + (showOuterBorder ? 1 : 0);

        if (HeaderCells.Count > 0)
        {
            ArrangeRow(xStart, y, rowHeight, widths, padding, showVerticalLines, HeaderCells);
            y += rowHeight;

            if (showHeaderSeparator)
            {
                y += 1;
            }
        }

        for (var r = 0; r < RowCells.Count; r++)
        {
            ArrangeRow(xStart, y, rowHeight, widths, padding, showVerticalLines, RowCells[r]);
            y += rowHeight;

            if (showRowSeparators && r + 1 < RowCells.Count)
            {
                y += 1;
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

        var tableStyle = Get<TableStyle>();
        var (showOuterBorder, showVerticalLines, showRowSeparators, showHeaderSeparator, padding) = ResolveOptions(tableStyle);

        var theme = GetTheme();
        var glyphs = tableStyle.Glyphs ?? theme.Lines;
        var focused = IsFocusedInScope();

        var borderStyle = tableStyle.ResolveBorderStyle(theme, focused);
        var cellStyle = tableStyle.ResolveCellStyle(theme);
        var headerStyle = tableStyle.ResolveHeaderStyle(theme);

        var columns = widths.Length;
        var rowHeight = GetRowHeight(padding);

        var y = rect.Y;
        if (showOuterBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            DrawOuterBorderLine(buffer, rect, y, widths, padding, showVerticalLines, glyphs, borderStyle, isTop: true);
            y++;
        }

        var rowTop = rect.Y + (showOuterBorder ? 1 : 0);
        if (HeaderCells.Count > 0 && rowTop < rect.Y + rect.Height)
        {
            RenderRowArea(buffer, rect, rowTop, rowHeight, widths, padding, showOuterBorder, showVerticalLines, glyphs, headerStyle, borderStyle);
            rowTop += rowHeight;

            if (showHeaderSeparator && rowTop < rect.Y + rect.Height)
            {
                DrawSeparatorLine(buffer, rect, rowTop, widths, padding, showOuterBorder, showVerticalLines, glyphs, borderStyle);
                rowTop += 1;
            }
        }

        for (var r = 0; r < RowCells.Count && rowTop < rect.Y + rect.Height; r++)
        {
            RenderRowArea(buffer, rect, rowTop, rowHeight, widths, padding, showOuterBorder, showVerticalLines, glyphs, cellStyle, borderStyle);
            rowTop += rowHeight;

            if (showRowSeparators && r + 1 < RowCells.Count && rowTop < rect.Y + rect.Height)
            {
                DrawSeparatorLine(buffer, rect, rowTop, widths, padding, showOuterBorder, showVerticalLines, glyphs, borderStyle);
                rowTop += 1;
            }
        }

        if (showOuterBorder && rect.Width >= 2 && rect.Height >= 2)
        {
            DrawOuterBorderLine(buffer, rect, rect.Y + rect.Height - 1, widths, padding, showVerticalLines, glyphs, borderStyle, isTop: false);
        }
    }

    private static void ArrangeRow(int xStart, int rowTop, int rowHeight, IReadOnlyList<int> widths, Thickness padding, bool showVerticalLines, IReadOnlyList<Visual> rowCells)
    {
        var padLeft = Math.Max(0, padding.Left);
        var padTop = Math.Max(0, padding.Top);
        var padRight = Math.Max(0, padding.Right);
        var padBottom = Math.Max(0, padding.Bottom);

        var contentHeight = Math.Max(0, rowHeight - (padTop + padBottom));
        var cellPaddingWidth = padLeft + padRight;

        var x = xStart;
        for (var c = 0; c < widths.Count; c++)
        {
            var contentWidth = Math.Max(0, widths[c]);

            if (c < rowCells.Count)
            {
                var cell = rowCells[c];
                cell.Arrange(new Rectangle(x + padLeft, rowTop + padTop, contentWidth, contentHeight));
            }

            x += contentWidth + cellPaddingWidth;

            if (showVerticalLines && c + 1 < widths.Count)
            {
                x += 1;
            }
        }
    }

    private static void RenderRowArea(
        CellBuffer buffer,
        Rectangle rect,
        int rowTop,
        int rowHeight,
        IReadOnlyList<int> widths,
        Thickness padding,
        bool showOuterBorder,
        bool showVerticalLines,
        LineGlyphs glyphs,
        CellStyle cellStyle,
        CellStyle borderStyle)
    {
        var padLeft = Math.Max(0, padding.Left);
        var padRight = Math.Max(0, padding.Right);
        var cellPaddingWidth = padLeft + padRight;

        var yEnd = Math.Min(rect.Y + rect.Height, rowTop + rowHeight);
        for (var y = rowTop; y < yEnd; y++)
        {
            var x = rect.X;
            if (showOuterBorder)
            {
                buffer.SetCell(x, y, glyphs.Vertical, borderStyle);
                x++;
            }

            for (var c = 0; c < widths.Count; c++)
            {
                var contentWidth = Math.Max(0, widths[c]);
                var cellWidth = contentWidth + cellPaddingWidth;

                for (var i = 0; i < cellWidth; i++)
                {
                    buffer.SetCell(x + i, y, new Rune(' '), cellStyle);
                }

                x += cellWidth;

                if (showVerticalLines && c + 1 < widths.Count)
                {
                    buffer.SetCell(x, y, glyphs.Vertical, borderStyle);
                    x++;
                }
            }

            if (showOuterBorder && rect.Width >= 1)
            {
                buffer.SetCell(rect.X + rect.Width - 1, y, glyphs.Vertical, borderStyle);
            }
        }
    }

    private static void DrawOuterBorderLine(
        CellBuffer buffer,
        Rectangle rect,
        int y,
        IReadOnlyList<int> widths,
        Thickness padding,
        bool showVerticalLines,
        LineGlyphs glyphs,
        CellStyle borderStyle,
        bool isTop)
    {
        if (rect.Width <= 0)
        {
            return;
        }

        var padLeft = Math.Max(0, padding.Left);
        var padRight = Math.Max(0, padding.Right);
        var cellPaddingWidth = padLeft + padRight;

        var left = isTop ? glyphs.TopLeft : glyphs.BottomLeft;
        var right = isTop ? glyphs.TopRight : glyphs.BottomRight;
        var middle = isTop ? glyphs.TeeTop : glyphs.TeeBottom;

        var x = rect.X;
        buffer.SetCell(x++, y, left, borderStyle);

        for (var c = 0; c < widths.Count; c++)
        {
            var contentWidth = Math.Max(0, widths[c]);
            var cellWidth = contentWidth + cellPaddingWidth;
            for (var i = 0; i < cellWidth; i++)
            {
                buffer.SetCell(x + i, y, glyphs.Horizontal, borderStyle);
            }
            x += cellWidth;

            if (showVerticalLines && c + 1 < widths.Count)
            {
                buffer.SetCell(x++, y, middle, borderStyle);
            }
        }

        buffer.SetCell(x, y, right, borderStyle);
    }

    private static void DrawSeparatorLine(
        CellBuffer buffer,
        Rectangle rect,
        int y,
        IReadOnlyList<int> widths,
        Thickness padding,
        bool showOuterBorder,
        bool showVerticalLines,
        LineGlyphs glyphs,
        CellStyle borderStyle)
    {
        if (rect.Width <= 0)
        {
            return;
        }

        var padLeft = Math.Max(0, padding.Left);
        var padRight = Math.Max(0, padding.Right);
        var cellPaddingWidth = padLeft + padRight;

        var x = rect.X;
        if (showOuterBorder)
        {
            buffer.SetCell(x++, y, glyphs.TeeLeft, borderStyle);
        }

        for (var c = 0; c < widths.Count; c++)
        {
            var contentWidth = Math.Max(0, widths[c]);
            var cellWidth = contentWidth + cellPaddingWidth;
            for (var i = 0; i < cellWidth; i++)
            {
                buffer.SetCell(x + i, y, glyphs.Horizontal, borderStyle);
            }
            x += cellWidth;

            if (showVerticalLines && c + 1 < widths.Count)
            {
                buffer.SetCell(x++, y, glyphs.Cross, borderStyle);
            }
        }

        if (showOuterBorder)
        {
            buffer.SetCell(x, y, glyphs.TeeRight, borderStyle);
        }
    }

    private static int GetRowHeight(Thickness padding)
    {
        var vertical = Math.Max(0, padding.Top) + Math.Max(0, padding.Bottom);
        return Math.Max(1, 1 + vertical);
    }

    private static int ComputeRequiredWidth(IReadOnlyList<int> widths, int paddingHorizontal, bool showOuterBorder, bool showVerticalLines)
    {
        var columns = widths.Count;
        if (columns == 0)
        {
            return 0;
        }

        var separators = (showOuterBorder ? 2 : 0) + (showVerticalLines ? Math.Max(0, columns - 1) : 0);

        var total = (long)separators + ((long)columns * Math.Max(0, paddingHorizontal));
        for (var c = 0; c < columns; c++)
        {
            total += Math.Max(0, widths[c]);
        }

        return (int)Math.Clamp(total, 0, int.MaxValue);
    }

    private static int ComputeRequiredHeight(int headerCount, int rowCount, int rowHeight, bool showOuterBorder, bool showHeaderSeparator, bool showRowSeparators)
    {
        var height = 0;
        if (showOuterBorder)
        {
            height += 2;
        }

        if (headerCount > 0)
        {
            height += rowHeight;
            if (showHeaderSeparator)
            {
                height += 1;
            }
        }

        height += rowCount * rowHeight;

        if (showRowSeparators && rowCount > 1)
        {
            height += rowCount - 1;
        }

        return Math.Max(0, height);
    }

    private static void FitColumnWidthsToWidth(int[] widths, int availableWidth, int paddingHorizontal, bool showOuterBorder, bool showVerticalLines, bool expandToAvailable)
    {
        if (widths.Length == 0)
        {
            return;
        }

        var pad = Math.Max(0, paddingHorizontal);
        var available = Math.Max(0, availableWidth);

        var required = ComputeRequiredWidth(widths, pad, showOuterBorder, showVerticalLines);

        if (required < available)
        {
            if (expandToAvailable)
            {
                widths[^1] = Math.Max(0, widths[^1]) + (available - required);
            }
            return;
        }

        if (required == available)
        {
            return;
        }

        var columns = widths.Length;
        var separators = (showOuterBorder ? 2 : 0) + (showVerticalLines ? Math.Max(0, columns - 1) : 0);
        var availableContent = Math.Max(0, available - separators - (columns * pad));
        var perColumn = columns == 0 ? 0 : availableContent / columns;

        for (var c = 0; c < columns; c++)
        {
            widths[c] = Math.Min(Math.Max(0, widths[c]), perColumn);
        }

        if (expandToAvailable)
        {
            var after = ComputeRequiredWidth(widths, pad, showOuterBorder, showVerticalLines);
            if (after < available)
            {
                widths[^1] = Math.Max(0, widths[^1]) + (available - after);
            }
        }
    }

    private int GetColumnCount()
    {
        var columns = HeaderCells.Count;
        for (var i = 0; i < RowCells.Count; i++)
        {
            columns = Math.Max(columns, RowCells[i].Count);
        }

        return columns;
    }

    private (bool showOuterBorder, bool showVerticalLines, bool showRowSeparators, bool showHeaderSeparator, Thickness padding) ResolveOptions(TableStyle tableStyle)
    {
        var showOuterBorder = tableStyle.ShowOuterBorder;
        var showVerticalLines = tableStyle.ShowVerticalLines;
        var showRowSeparators = tableStyle.ShowRowSeparators;
        var showHeaderSeparator = ShowHeaderSeparator && tableStyle.ShowHeaderSeparator;
        var padding = tableStyle.CellPadding;
        return (showOuterBorder, showVerticalLines, showRowSeparators, showHeaderSeparator, padding);
    }

    private bool IsFocusedInScope()
    {
        for (var v = App?.FocusedElement; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, this))
            {
                return true;
            }
        }

        return false;
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
}
