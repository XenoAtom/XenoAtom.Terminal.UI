// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Arranges content into rows and columns using grid definitions.
/// </summary>
public sealed partial class Grid : Visual
{
    private readonly VisualList<GridCell> _cells;

    /// <summary>
    /// Gets the row definitions.
    /// </summary>
    [Bindable]
    public BindableList<RowDefinition> RowDefinitions { get; }

    /// <summary>
    /// Gets the column definitions.
    /// </summary>
    [Bindable]
    public BindableList<ColumnDefinition> ColumnDefinitions { get; }

    /// <summary>
    /// Gets the grid cells collection.
    /// </summary>
    [Bindable]
    public BindableList<GridCell> Cells { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Grid"/> class.
    /// </summary>
    public Grid()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _cells = new VisualList<GridCell>(this, "Cells");
        Cells = _cells;
        RowDefinitions = new BindableList<RowDefinition>(this, "RowDefinitions");
        ColumnDefinitions = new BindableList<ColumnDefinition>(this, "ColumnDefinitions");
        AutoGrowRows = true;
        AutoGrowColumns = true;
    }

    /// <summary>
    /// Gets or sets the padding applied around the grid content.
    /// </summary>
    [Bindable]
    public partial Thickness Padding { get; set; }

    /// <summary>
    /// Gets or sets the gap between rows.
    /// </summary>
    [Bindable]
    public partial int RowGap { get; set; }

    /// <summary>
    /// Gets or sets the gap between columns.
    /// </summary>
    [Bindable]
    public partial int ColumnGap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether rows can be auto-added.
    /// </summary>
    [Bindable]
    public partial bool AutoGrowRows { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether columns can be auto-added.
    /// </summary>
    [Bindable]
    public partial bool AutoGrowColumns { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var padding = Padding;
        var rowGap = Math.Max(0, RowGap);
        var colGap = Math.Max(0, ColumnGap);

        var maxW = constraints.MaxWidth;
        var maxH = constraints.MaxHeight;

        var (rows, cols) = GetEffectiveCounts();
        var totalRowGaps = rows > 1 ? (rows - 1) * rowGap : 0;
        var totalColGaps = cols > 1 ? (cols - 1) * colGap : 0;

        var innerAvailW = maxW == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, maxW - padding.Horizontal - totalColGaps);
        var innerAvailH = maxH == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, maxH - padding.Vertical - totalRowGaps);

        var colDefs = GetEffectiveColumnDefinitions(cols);
        var rowDefs = GetEffectiveRowDefinitions(rows);

        var scratchLength = (cols * 6) + (rows * 5);
        int[]? rentedScratch = null;
        var scratch = scratchLength <= (11 * 128)
            ? stackalloc int[scratchLength]
            : (rentedScratch = ArrayPool<int>.Shared.Rent(scratchLength));

        var offset = 0;
        var colMin = scratch.Slice(offset, cols);
        offset += cols;
        var colNat = scratch.Slice(offset, cols);
        offset += cols;
        var colMax = scratch.Slice(offset, cols);
        offset += cols;
        var colGrow = scratch.Slice(offset, cols);
        offset += cols;
        var colShrink = scratch.Slice(offset, cols);
        offset += cols;

        var rowMin = scratch.Slice(offset, rows);
        offset += rows;
        var rowNat = scratch.Slice(offset, rows);
        offset += rows;
        var rowMax = scratch.Slice(offset, rows);
        offset += rows;
        var rowGrow = scratch.Slice(offset, rows);
        offset += rows;
        var rowShrink = scratch.Slice(offset, rows);
        offset += rows;
        var allocatedColWidths = scratch.Slice(offset, cols);

        try
        {
            InitTracks(colDefs, colMin, colNat, colMax, colGrow, colShrink);
            InitTracks(rowDefs, rowMin, rowNat, rowMax, rowGrow, rowShrink);

            // Initial measure pass to establish intrinsic track sizes (Min/Natural), based on child hints.
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                cell.Measure(new LayoutConstraints(0, innerAvailW, 0, innerAvailH));

                var placement = GetPlacementForLayout(cell, rows, cols);
                var hints = cell.MeasureHints;

                if (placement.ColumnSpan == 1)
                {
                    var col = placement.Column;
                    var type = colDefs[col].Width.Type;
                    if (type != GridUnitType.Fixed)
                    {
                        colMin[col] = Math.Max(colMin[col], hints.Min.Width);
                        colNat[col] = Math.Max(colNat[col], hints.Natural.Width);
                    }
                }

                if (placement.RowSpan == 1)
                {
                    var row = placement.Row;
                    var type = rowDefs[row].Height.Type;
                    if (type != GridUnitType.Fixed)
                    {
                        rowMin[row] = Math.Max(rowMin[row], hints.Min.Height);
                        rowNat[row] = Math.Max(rowNat[row], hints.Natural.Height);
                    }
                }
            }

            NormalizeTracks(colDefs, colMin, colNat, colMax);
            NormalizeTracks(rowDefs, rowMin, rowNat, rowMax);

            // Allocate column widths (for measuring width-dependent heights), without changing intrinsic Natural sizes.
            var availableForColumns = innerAvailW == LayoutConstants.Infinite
                ? Sum(colNat)
                : innerAvailW;
            FlexAllocator.Allocate(availableForColumns, colMin, colNat, colMax, colGrow, colShrink, allocatedColWidths);

            // Re-measure for row heights using allocated column widths (wrapping, etc.).
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var placement = GetPlacementForLayout(cell, rows, cols);

                var cellW = GetSpanSize(allocatedColWidths, placement.Column, placement.ColumnSpan, colGap);
                cell.Measure(new LayoutConstraints(0, cellW, 0, innerAvailH));

                if (placement.RowSpan == 1)
                {
                    var row = placement.Row;
                    var type = rowDefs[row].Height.Type;
                    if (type != GridUnitType.Fixed)
                    {
                        var hints = cell.MeasureHints;
                        rowMin[row] = Math.Max(rowMin[row], hints.Min.Height);
                        rowNat[row] = Math.Max(rowNat[row], hints.Natural.Height);
                    }
                }
            }

            NormalizeTracks(rowDefs, rowMin, rowNat, rowMax);

            var padH = Math.Max(0, padding.Horizontal);
            var padV = Math.Max(0, padding.Vertical);

            var gridMinW = padH + totalColGaps + Sum(colMin);
            var gridNatW = padH + totalColGaps + Sum(colNat);
            var gridMaxW = HasInfinite(colMax) ? LayoutConstants.Infinite : padH + totalColGaps + Sum(colMax);

            var gridMinH = padV + totalRowGaps + Sum(rowMin);
            var gridNatH = padV + totalRowGaps + Sum(rowNat);
            var gridMaxH = HasInfinite(rowMax) ? LayoutConstants.Infinite : padV + totalRowGaps + Sum(rowMax);

            var minSize = new Size(LayoutConstants.ClampFinite(gridMinW), LayoutConstants.ClampFinite(gridMinH));
            var naturalSize = new Size(LayoutConstants.ClampFinite(gridNatW), LayoutConstants.ClampFinite(gridNatH));
            var maxSize = new Size(
                gridMaxW == LayoutConstants.Infinite ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(gridMaxW),
                gridMaxH == LayoutConstants.Infinite ? LayoutConstants.Infinite : LayoutConstants.ClampFinite(gridMaxH));

            var growX = Sum(colGrow);
            var growY = Sum(rowGrow);

            var shrinkX = naturalSize.Width > minSize.Width ? Sum(colShrink) : 0;
            var shrinkY = naturalSize.Height > minSize.Height ? Sum(rowShrink) : 0;

            return SizeHints.Flex(minSize, naturalSize, maxSize, growX: growX, growY: growY, shrinkX: shrinkX, shrinkY: shrinkY).Normalize();
        }
        finally
        {
            if (rentedScratch is not null)
            {
                ArrayPool<int>.Shared.Return(rentedScratch);
            }
        }
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var padding = Padding;
        var rowGap = Math.Max(0, RowGap);
        var colGap = Math.Max(0, ColumnGap);

        var (rows, cols) = GetEffectiveCounts();
        var totalRowGaps = rows > 1 ? (rows - 1) * rowGap : 0;
        var totalColGaps = cols > 1 ? (cols - 1) * colGap : 0;

        var innerW = Math.Max(0, finalRect.Width - padding.Horizontal - totalColGaps);
        var innerH = Math.Max(0, finalRect.Height - padding.Vertical - totalRowGaps);

        var colDefs = GetEffectiveColumnDefinitions(cols);
        var rowDefs = GetEffectiveRowDefinitions(rows);

        var scratchLength = (cols * 7) + (rows * 7);
        int[]? rentedScratch = null;
        var scratch = scratchLength <= (14 * 128)
            ? stackalloc int[scratchLength]
            : (rentedScratch = ArrayPool<int>.Shared.Rent(scratchLength));

        var offset = 0;
        var colMin = scratch.Slice(offset, cols);
        offset += cols;
        var colNat = scratch.Slice(offset, cols);
        offset += cols;
        var colMax = scratch.Slice(offset, cols);
        offset += cols;
        var colGrow = scratch.Slice(offset, cols);
        offset += cols;
        var colShrink = scratch.Slice(offset, cols);
        offset += cols;
        var colWidths = scratch.Slice(offset, cols);
        offset += cols;
        var colOffsets = scratch.Slice(offset, cols);
        offset += cols;

        var rowMin = scratch.Slice(offset, rows);
        offset += rows;
        var rowNat = scratch.Slice(offset, rows);
        offset += rows;
        var rowMax = scratch.Slice(offset, rows);
        offset += rows;
        var rowGrow = scratch.Slice(offset, rows);
        offset += rows;
        var rowShrink = scratch.Slice(offset, rows);
        offset += rows;
        var rowHeights = scratch.Slice(offset, rows);
        offset += rows;
        var rowOffsets = scratch.Slice(offset, rows);

        try
        {
            InitTracks(colDefs, colMin, colNat, colMax, colGrow, colShrink);
            InitTracks(rowDefs, rowMin, rowNat, rowMax, rowGrow, rowShrink);

            // Measure cells against the available inner rect to establish track requirements.
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                cell.Measure(new LayoutConstraints(0, innerW, 0, innerH));

                var placement = GetPlacementForLayout(cell, rows, cols);
                var hints = cell.MeasureHints;

                if (placement.ColumnSpan == 1)
                {
                    var col = placement.Column;
                    if (colDefs[col].Width.Type != GridUnitType.Fixed)
                    {
                        colMin[col] = Math.Max(colMin[col], hints.Min.Width);
                        colNat[col] = Math.Max(colNat[col], hints.Natural.Width);
                    }
                }

                if (placement.RowSpan == 1)
                {
                    var row = placement.Row;
                    if (rowDefs[row].Height.Type != GridUnitType.Fixed)
                    {
                        rowMin[row] = Math.Max(rowMin[row], hints.Min.Height);
                        rowNat[row] = Math.Max(rowNat[row], hints.Natural.Height);
                    }
                }
            }

            NormalizeTracks(colDefs, colMin, colNat, colMax);
            NormalizeTracks(rowDefs, rowMin, rowNat, rowMax);

            // Allocate widths first so we can re-measure for row heights (wrapping).
            FlexAllocator.Allocate(innerW, colMin, colNat, colMax, colGrow, colShrink, colWidths);

            // Re-measure for row heights using allocated widths.
            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var placement = GetPlacementForLayout(cell, rows, cols);

                var cellW = GetSpanSize(colWidths, placement.Column, placement.ColumnSpan, colGap);
                cell.Measure(new LayoutConstraints(0, cellW, 0, innerH));

                if (placement.RowSpan == 1)
                {
                    var row = placement.Row;
                    if (rowDefs[row].Height.Type != GridUnitType.Fixed)
                    {
                        var hints = cell.MeasureHints;
                        rowMin[row] = Math.Max(rowMin[row], hints.Min.Height);
                        rowNat[row] = Math.Max(rowNat[row], hints.Natural.Height);
                    }
                }
            }

            NormalizeTracks(rowDefs, rowMin, rowNat, rowMax);

            FlexAllocator.Allocate(innerH, rowMin, rowNat, rowMax, rowGrow, rowShrink, rowHeights);

            var x0 = finalRect.X + padding.Left;
            var y0 = finalRect.Y + padding.Top;

            var x = x0;
            for (var c = 0; c < cols; c++)
            {
                colOffsets[c] = x;
                x += colWidths[c] + colGap;
            }

            var y = y0;
            for (var r = 0; r < rows; r++)
            {
                rowOffsets[r] = y;
                y += rowHeights[r] + rowGap;
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var p = GetPlacementForLayout(cell, rows, cols);

                var cellX = colOffsets[p.Column];
                var cellY = rowOffsets[p.Row];
                var cellW = GetSpanSize(colWidths, p.Column, p.ColumnSpan, colGap);
                var cellH = GetSpanSize(rowHeights, p.Row, p.RowSpan, rowGap);

                cell.Arrange(new Rectangle(cellX, cellY, cellW, cellH));
            }
        }
        finally
        {
            if (rentedScratch is not null)
            {
                ArrayPool<int>.Shared.Return(rentedScratch);
            }
        }
    }

    private (int Rows, int Columns) GetEffectiveCounts()
    {
        var minRows = Math.Max(1, RowDefinitions.Count);
        var minCols = Math.Max(1, ColumnDefinitions.Count);

        var requiredRows = minRows;
        var requiredCols = minCols;

        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            var p = GetPlacement(cell);
            requiredRows = Math.Max(requiredRows, p.Row + p.RowSpan);
            requiredCols = Math.Max(requiredCols, p.Column + p.ColumnSpan);
        }

        if (!AutoGrowRows)
        {
            requiredRows = minRows;
        }

        if (!AutoGrowColumns)
        {
            requiredCols = minCols;
        }

        return (requiredRows, requiredCols);
    }

    private readonly record struct GridPlacement(int Row, int Column, int RowSpan, int ColumnSpan);

    private static GridPlacement GetPlacement(GridCell cell)
        => new(cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan);

    private static GridPlacement GetPlacementForLayout(GridCell cell, int maxRows, int maxCols)
    {
        var row = Math.Clamp(cell.Row, 0, Math.Max(0, maxRows - 1));
        var col = Math.Clamp(cell.Column, 0, Math.Max(0, maxCols - 1));
        var rowSpan = Math.Clamp(cell.RowSpan, 1, Math.Max(1, maxRows - row));
        var colSpan = Math.Clamp(cell.ColumnSpan, 1, Math.Max(1, maxCols - col));
        return new GridPlacement(row, col, rowSpan, colSpan);
    }

    private ColumnDefinition[] GetEffectiveColumnDefinitions(int columns)
    {
        if (ColumnDefinitions.Count == 0)
        {
            var arr = new ColumnDefinition[columns];
            for (var i = 0; i < columns; i++)
            {
                arr[i] = new ColumnDefinition { Width = GridLength.Star(1) };
            }
            return arr;
        }

        if (ColumnDefinitions.Count >= columns)
        {
            var arr = new ColumnDefinition[columns];
            for (var i = 0; i < columns; i++)
            {
                arr[i] = ColumnDefinitions[i];
            }
            return arr;
        }

        var expanded = new ColumnDefinition[columns];
        for (var i = 0; i < ColumnDefinitions.Count; i++)
        {
            expanded[i] = ColumnDefinitions[i];
        }

        for (var i = ColumnDefinitions.Count; i < columns; i++)
        {
            expanded[i] = new ColumnDefinition { Width = GridLength.Star(1) };
        }

        return expanded;
    }

    private RowDefinition[] GetEffectiveRowDefinitions(int rows)
    {
        if (RowDefinitions.Count == 0)
        {
            var arr = new RowDefinition[rows];
            for (var i = 0; i < rows; i++)
            {
                arr[i] = new RowDefinition { Height = GridLength.Star(1) };
            }
            return arr;
        }

        if (RowDefinitions.Count >= rows)
        {
            var arr = new RowDefinition[rows];
            for (var i = 0; i < rows; i++)
            {
                arr[i] = RowDefinitions[i];
            }
            return arr;
        }

        var expanded = new RowDefinition[rows];
        for (var i = 0; i < RowDefinitions.Count; i++)
        {
            expanded[i] = RowDefinitions[i];
        }

        for (var i = RowDefinitions.Count; i < rows; i++)
        {
            expanded[i] = new RowDefinition { Height = GridLength.Star(1) };
        }

        return expanded;
    }

    private static void InitTracks(ColumnDefinition[] defs, Span<int> min, Span<int> natural, Span<int> max, Span<int> grow, Span<int> shrink)
    {
        for (var i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            var type = def.Width.Type;

            var minW = Math.Max(0, def.MinWidth);
            var maxW = def.MaxWidth;
            maxW = maxW == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(0, maxW);

            if (type == GridUnitType.Fixed)
            {
                var w = Clamp(minW, (int)def.Width.Value, maxW);
                min[i] = w;
                natural[i] = w;
                max[i] = w;
                grow[i] = 0;
                shrink[i] = 0;
                continue;
            }

            min[i] = minW;
            natural[i] = minW;

            max[i] = maxW == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(maxW, minW);

            grow[i] = type == GridUnitType.Star ? GetStarWeight(def.Width.Value) : 0;
            shrink[i] = 1;
        }
    }

    private static void InitTracks(RowDefinition[] defs, Span<int> min, Span<int> natural, Span<int> max, Span<int> grow, Span<int> shrink)
    {
        for (var i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            var type = def.Height.Type;

            var minH = Math.Max(0, def.MinHeight);
            var maxH = def.MaxHeight;
            maxH = maxH == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(0, maxH);

            if (type == GridUnitType.Fixed)
            {
                var h = Clamp(minH, (int)def.Height.Value, maxH);
                min[i] = h;
                natural[i] = h;
                max[i] = h;
                grow[i] = 0;
                shrink[i] = 0;
                continue;
            }

            min[i] = minH;
            natural[i] = minH;
            max[i] = maxH == LayoutConstants.Infinite ? LayoutConstants.Infinite : Math.Max(maxH, minH);

            grow[i] = type == GridUnitType.Star ? GetStarWeight(def.Height.Value) : 0;
            shrink[i] = 1;
        }
    }

    private static void NormalizeTracks(ColumnDefinition[] defs, Span<int> min, Span<int> natural, Span<int> max)
    {
        for (var i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            if (def.Width.Type == GridUnitType.Fixed)
            {
                continue;
            }

            var maxW = max[i];
            var minW = min[i];

            if (maxW != LayoutConstants.Infinite && maxW < minW)
            {
                maxW = minW;
                max[i] = maxW;
            }

            var nat = natural[i];
            if (maxW != LayoutConstants.Infinite)
            {
                nat = Math.Min(nat, maxW);
            }
            nat = Math.Max(nat, minW);
            natural[i] = nat;
        }
    }

    private static void NormalizeTracks(RowDefinition[] defs, Span<int> min, Span<int> natural, Span<int> max)
    {
        for (var i = 0; i < defs.Length; i++)
        {
            var def = defs[i];
            if (def.Height.Type == GridUnitType.Fixed)
            {
                continue;
            }

            var maxH = max[i];
            var minH = min[i];

            if (maxH != LayoutConstants.Infinite && maxH < minH)
            {
                maxH = minH;
                max[i] = maxH;
            }

            var nat = natural[i];
            if (maxH != LayoutConstants.Infinite)
            {
                nat = Math.Min(nat, maxH);
            }
            nat = Math.Max(nat, minH);
            natural[i] = nat;
        }
    }

    private static int Sum(ReadOnlySpan<int> sizes)
    {
        var sum = 0;
        for (var i = 0; i < sizes.Length; i++)
        {
            sum += Math.Max(0, sizes[i]);
        }

        return sum;
    }

    private static bool HasInfinite(ReadOnlySpan<int> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (LayoutConstants.IsInfinite(values[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetStarWeight(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return 1;
        }

        var w = (int)Math.Round(value);
        return w <= 0 ? 1 : w;
    }

    private static int GetSpanSize(ReadOnlySpan<int> sizes, int start, int span, int gap)
    {
        var sum = 0;
        for (var i = 0; i < span; i++)
        {
            var index = start + i;
            if ((uint)index >= (uint)sizes.Length)
            {
                break;
            }
            sum += sizes[index];
        }
        if (span > 1)
        {
            sum += gap * (span - 1);
        }
        return sum;
    }

    private static int Clamp(int min, int value, int max)
    {
        min = Math.Max(0, min);
        max = Math.Max(min, max == int.MaxValue ? int.MaxValue : Math.Max(0, max));
        return Math.Clamp(value, min, max);
    }

    /// <inheritdoc />
    protected override int ChildrenCount => _cells.Count;

    /// <inheritdoc />
    protected override Visual GetChild(int index) => _cells[index];
}
