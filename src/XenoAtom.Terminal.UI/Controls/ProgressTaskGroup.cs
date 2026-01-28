// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a collection of progress tasks in a grid layout.
/// </summary>
/// <remarks>
/// This control is composed of existing visuals (grid, text, progress bars, spinners).
/// Use <see cref="Columns"/> to reorder, replace, or add custom columns.
/// </remarks>
public sealed partial class ProgressTaskGroup : Visual
{
    private readonly ComputedVisual _content;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressTaskGroup"/> class.
    /// </summary>
    public ProgressTaskGroup()
    {
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Start;

        Tasks = new BindableList<ProgressTask>(this, $"{nameof(ProgressTaskGroup)}.{nameof(Tasks)}");
        Columns = new BindableList<ProgressTaskColumn>(this, $"{nameof(ProgressTaskGroup)}.{nameof(Columns)}");

        _content = new ComputedVisual(Build);
        AttachChild(_content);
    }

    /// <summary>
    /// Gets the tasks displayed by this group.
    /// </summary>
    [Bindable]
    public BindableList<ProgressTask> Tasks { get; }

    /// <summary>
    /// Gets the column definitions used to display each task.
    /// </summary>
    /// <remarks>
    /// When empty, the group uses the columns defined by <see cref="ProgressTaskGroupStyle.DefaultColumns"/>.
    /// </remarks>
    [Bindable]
    public BindableList<ProgressTaskColumn> Columns { get; }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _content : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => _content.Measure(constraints);

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _content.Arrange(finalRect);
    }

    private Visual? Build()
    {
        var style = GetStyle<ProgressTaskGroupStyle>();

        var tasks = Tasks;
        var columns = Columns;
        var effectiveColumns = columns.Count > 0 ? (IReadOnlyList<ProgressTaskColumn>)columns : style.DefaultColumns;

        if (tasks.Count == 0 || effectiveColumns.Count == 0)
        {
            return null;
        }

        var rowSpacing = Math.Max(0, style.RowSpacing);
        var columnSpacing = Math.Max(0, style.ColumnSpacing);

        var includeRowGaps = rowSpacing > 0;
        var includeColumnGaps = columnSpacing > 0;

        var rowStride = includeRowGaps ? 2 : 1;
        var colStride = includeColumnGaps ? 2 : 1;

        var rowCount = (tasks.Count * rowStride) - (includeRowGaps ? 1 : 0);
        var colCount = (effectiveColumns.Count * colStride) - (includeColumnGaps ? 1 : 0);

        var grid = new Grid()
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Start);

        for (var c = 0; c < effectiveColumns.Count; c++)
        {
            var column = effectiveColumns[c];
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = column.Width,
                MinWidth = Math.Max(0, column.MinWidth),
                MaxWidth = Math.Max(0, column.MaxWidth),
            });

            if (includeColumnGaps && c + 1 < effectiveColumns.Count)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Fixed(columnSpacing) });
            }
        }

        for (var r = 0; r < tasks.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            if (includeRowGaps && r + 1 < tasks.Count)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Fixed(rowSpacing) });
            }
        }

        for (var r = 0; r < tasks.Count; r++)
        {
            var task = tasks[r];
            var rowIndex = r * rowStride;

            for (var c = 0; c < effectiveColumns.Count; c++)
            {
                var column = effectiveColumns[c];
                var colIndex = c * colStride;
                var cell = column.CreateCell(task);
                if (column.Id is { Length: > 0 } id)
                {
                    task.OnCellCreated(id, cell);
                }

                grid.Cell(cell, rowIndex, colIndex);
            }
        }

        return grid;
    }
}
