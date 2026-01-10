// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class Grid : Panel
{
    public BindableList<RowDefinition> RowDefinitions { get; }

    public BindableList<ColumnDefinition> ColumnDefinitions { get; }

    public Grid()
    {
        RowDefinitions = new BindableList<RowDefinition>(this, "RowDefinitions");
        ColumnDefinitions = new BindableList<ColumnDefinition>(this, "ColumnDefinitions");
        this.AutoGrowRows(true);
        this.AutoGrowColumns(true);
    }

    [Bindable]
    public partial Thickness Padding { get; set; }

    [Bindable]
    public partial int RowGap { get; set; }

    [Bindable]
    public partial int ColumnGap { get; set; }

    [Bindable]
    public partial bool AutoGrowRows { get; set; }

    [Bindable]
    public partial bool AutoGrowColumns { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var rowGap = Math.Max(0, RowGap);
        var colGap = Math.Max(0, ColumnGap);

        var (rows, cols) = GetEffectiveCounts();
        var totalRowGaps = rows > 1 ? (rows - 1) * rowGap : 0;
        var totalColGaps = cols > 1 ? (cols - 1) * colGap : 0;

        var innerAvailW = Math.Max(0, availableSize.Width - padding.Horizontal - totalColGaps);
        var innerAvailH = Math.Max(0, availableSize.Height - padding.Vertical - totalRowGaps);

        var colDefs = GetEffectiveColumnDefinitions(cols);
        var rowDefs = GetEffectiveRowDefinitions(rows);

        var colWidths = new int[cols];
        var rowHeights = new int[rows];

        // Fixed columns.
        for (var c = 0; c < cols; c++)
        {
            var def = colDefs[c];
            if (def.Width.Type == GridUnitType.Fixed)
            {
                colWidths[c] = Clamp(def.MinWidth, (int)def.Width.Value, def.MaxWidth);
            }
        }

        // Fixed rows.
        for (var r = 0; r < rows; r++)
        {
            var def = rowDefs[r];
            if (def.Height.Type == GridUnitType.Fixed)
            {
                rowHeights[r] = Clamp(def.MinHeight, (int)def.Height.Value, def.MaxHeight);
            }
        }

        // Initial measure pass for auto columns.
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(innerAvailW, innerAvailH));

            var placement = GetPlacementForLayout(child, rows, cols);
            if (placement.ColumnSpan != 1)
            {
                continue;
            }

            var col = placement.Column;
            if (colDefs[col].Width.Type == GridUnitType.Auto)
            {
                colWidths[col] = Math.Max(colWidths[col], child.DesiredSize.Width);
            }
        }

        // Clamp auto columns.
        for (var c = 0; c < cols; c++)
        {
            var def = colDefs[c];
            if (def.Width.Type == GridUnitType.Auto)
            {
                colWidths[c] = Clamp(def.MinWidth, colWidths[c], def.MaxWidth);
            }
        }

        // Allocate star columns.
        AllocateStar(colDefs, innerAvailW, colWidths);

        // Measure children with column widths to determine row sizes.
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var p = GetPlacementForLayout(child, rows, cols);

            var cellW = GetSpanSize(colWidths, p.Column, p.ColumnSpan, colGap);
            child.Measure(new Size(cellW, innerAvailH));

            if (p.RowSpan == 1 && rowDefs[p.Row].Height.Type == GridUnitType.Auto)
            {
                rowHeights[p.Row] = Math.Max(rowHeights[p.Row], child.DesiredSize.Height);
            }
        }

        // Clamp auto rows.
        for (var r = 0; r < rows; r++)
        {
            var def = rowDefs[r];
            if (def.Height.Type == GridUnitType.Auto)
            {
                rowHeights[r] = Clamp(def.MinHeight, rowHeights[r], def.MaxHeight);
            }
        }

        // Allocate star rows.
        AllocateStar(rowDefs, innerAvailH, rowHeights);

        var desiredW = padding.Horizontal + totalColGaps + Sum(colWidths);
        var desiredH = padding.Vertical + totalRowGaps + Sum(rowHeights);

        return new Size(Math.Min(availableSize.Width, desiredW), Math.Min(availableSize.Height, desiredH));
    }

    protected override void ArrangeOverride(Rectangle finalRect)
    {
        Bounds = finalRect;

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

        var colWidths = new int[cols];
        var rowHeights = new int[rows];

        // Fixed columns.
        for (var c = 0; c < cols; c++)
        {
            var def = colDefs[c];
            if (def.Width.Type == GridUnitType.Fixed)
            {
                colWidths[c] = Clamp(def.MinWidth, (int)def.Width.Value, def.MaxWidth);
            }
        }

        // Auto columns based on desired sizes from measure pass.
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var p = GetPlacementForLayout(child, rows, cols);
            if (p.ColumnSpan != 1)
            {
                continue;
            }

            var col = p.Column;
            if (colDefs[col].Width.Type == GridUnitType.Auto)
            {
                colWidths[col] = Math.Max(colWidths[col], child.DesiredSize.Width);
            }
        }

        // Clamp auto columns.
        for (var c = 0; c < cols; c++)
        {
            var def = colDefs[c];
            if (def.Width.Type == GridUnitType.Auto)
            {
                colWidths[c] = Clamp(def.MinWidth, colWidths[c], def.MaxWidth);
            }
        }

        AllocateStar(colDefs, innerW, colWidths);

        // Fixed rows.
        for (var r = 0; r < rows; r++)
        {
            var def = rowDefs[r];
            if (def.Height.Type == GridUnitType.Fixed)
            {
                rowHeights[r] = Clamp(def.MinHeight, (int)def.Height.Value, def.MaxHeight);
            }
        }

        // Auto rows.
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var p = GetPlacementForLayout(child, rows, cols);
            if (p.RowSpan != 1)
            {
                continue;
            }

            if (rowDefs[p.Row].Height.Type == GridUnitType.Auto)
            {
                rowHeights[p.Row] = Math.Max(rowHeights[p.Row], child.DesiredSize.Height);
            }
        }

        // Clamp auto rows.
        for (var r = 0; r < rows; r++)
        {
            var def = rowDefs[r];
            if (def.Height.Type == GridUnitType.Auto)
            {
                rowHeights[r] = Clamp(def.MinHeight, rowHeights[r], def.MaxHeight);
            }
        }

        AllocateStar(rowDefs, innerH, rowHeights);

        var x0 = finalRect.X + padding.Left;
        var y0 = finalRect.Y + padding.Top;

        Span<int> colOffsets = cols <= 64 ? stackalloc int[cols] : new int[cols];
        Span<int> rowOffsets = rows <= 64 ? stackalloc int[rows] : new int[rows];

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

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var p = GetPlacementForLayout(child, rows, cols);

            var cellX = colOffsets[p.Column];
            var cellY = rowOffsets[p.Row];
            var cellW = GetSpanSize(colWidths, p.Column, p.ColumnSpan, colGap);
            var cellH = GetSpanSize(rowHeights, p.Row, p.RowSpan, rowGap);

            child.Arrange(new Rectangle(cellX, cellY, cellW, cellH));
        }
    }

    private (int Rows, int Columns) GetEffectiveCounts()
    {
        var minRows = Math.Max(1, RowDefinitions.Count);
        var minCols = Math.Max(1, ColumnDefinitions.Count);

        var requiredRows = minRows;
        var requiredCols = minCols;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var p = GetPlacement(child);
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

    private static void AllocateStar(ColumnDefinition[] defs, int totalSize, int[] sizes)
    {
        var used = Sum(sizes);
        var remaining = Math.Max(0, totalSize - used);

        var totalWeight = 0.0;
        var starCount = 0;
        for (var i = 0; i < defs.Length; i++)
        {
            if (defs[i].Width.Type == GridUnitType.Star)
            {
                totalWeight += defs[i].Width.Value;
                starCount++;
            }
        }

        if (starCount == 0)
        {
            return;
        }

        if (totalWeight <= 0)
        {
            totalWeight = starCount;
        }

        var remainder = remaining;
        for (var i = 0; i < defs.Length; i++)
        {
            if (defs[i].Width.Type != GridUnitType.Star)
            {
                continue;
            }

            var w = (int)Math.Floor(remaining * (defs[i].Width.Value / totalWeight));
            w = Clamp(defs[i].MinWidth, w, defs[i].MaxWidth);
            sizes[i] = w;
            remainder -= w;
        }

        // Distribute leftover one cell at a time, stable by index.
        for (var pass = 0; remainder > 0 && pass < defs.Length; pass++)
        {
            for (var i = 0; i < defs.Length && remainder > 0; i++)
            {
                if (defs[i].Width.Type != GridUnitType.Star)
                {
                    continue;
                }

                if (sizes[i] < defs[i].MaxWidth)
                {
                    sizes[i]++;
                    remainder--;
                }
            }
        }
    }

    private static void AllocateStar(RowDefinition[] defs, int totalSize, int[] sizes)
    {
        var used = Sum(sizes);
        var remaining = Math.Max(0, totalSize - used);

        var totalWeight = 0.0;
        var starCount = 0;
        for (var i = 0; i < defs.Length; i++)
        {
            if (defs[i].Height.Type == GridUnitType.Star)
            {
                totalWeight += defs[i].Height.Value;
                starCount++;
            }
        }

        if (starCount == 0)
        {
            return;
        }

        if (totalWeight <= 0)
        {
            totalWeight = starCount;
        }

        var remainder = remaining;
        for (var i = 0; i < defs.Length; i++)
        {
            if (defs[i].Height.Type != GridUnitType.Star)
            {
                continue;
            }

            var h = (int)Math.Floor(remaining * (defs[i].Height.Value / totalWeight));
            h = Clamp(defs[i].MinHeight, h, defs[i].MaxHeight);
            sizes[i] = h;
            remainder -= h;
        }

        for (var pass = 0; remainder > 0 && pass < defs.Length; pass++)
        {
            for (var i = 0; i < defs.Length && remainder > 0; i++)
            {
                if (defs[i].Height.Type != GridUnitType.Star)
                {
                    continue;
                }

                if (sizes[i] < defs[i].MaxHeight)
                {
                    sizes[i]++;
                    remainder--;
                }
            }
        }
    }

    private static int GetSpanSize(int[] sizes, int start, int span, int gap)
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

    private static int Sum(int[] sizes)
    {
        var sum = 0;
        for (var i = 0; i < sizes.Length; i++)
        {
            sum += sizes[i];
        }
        return sum;
    }

    private static int Clamp(int min, int value, int max)
    {
        min = Math.Max(0, min);
        max = Math.Max(min, max == int.MaxValue ? int.MaxValue : Math.Max(0, max));
        return Math.Clamp(value, min, max);
    }
}

