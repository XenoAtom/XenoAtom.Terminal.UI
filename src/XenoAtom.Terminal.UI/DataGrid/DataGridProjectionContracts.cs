// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Specifies sort direction for a data grid view.
/// </summary>
public enum DataGridSortDirection
{
    /// <summary>
    /// Ascending sort.
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending sort.
    /// </summary>
    Descending,
}

/// <summary>
/// Describes a sort operation for a view.
/// </summary>
/// <param name="ColumnKey">The column key to sort by.</param>
/// <param name="Direction">The sort direction.</param>
public readonly record struct DataGridSortDescription(string ColumnKey, DataGridSortDirection Direction);

/// <summary>
/// Describes a filter applied to a column.
/// </summary>
/// <param name="ColumnKey">The column key to filter on.</param>
/// <param name="Text">The filter text.</param>
public readonly record struct DataGridFilterDescription(string ColumnKey, string? Text);

/// <summary>
/// Adds sorting capabilities to an <see cref="IDataGridView"/>.
/// </summary>
public interface ISortableDataGridView : IDataGridView
{
    /// <summary>
    /// Gets the current sort descriptions.
    /// </summary>
    IReadOnlyList<DataGridSortDescription> SortDescriptions { get; }

    /// <summary>
    /// Sets the sort descriptions for the view.
    /// </summary>
    /// <param name="sortDescriptions">The sort descriptions.</param>
    void SetSortDescriptions(IReadOnlyList<DataGridSortDescription> sortDescriptions);
}

/// <summary>
/// Adds filtering capabilities to an <see cref="IDataGridView"/>.
/// </summary>
public interface IFilterableDataGridView : IDataGridView
{
    /// <summary>
    /// Gets the current filters.
    /// </summary>
    IReadOnlyList<DataGridFilterDescription> Filters { get; }

    /// <summary>
    /// Sets the filters for the view.
    /// </summary>
    /// <param name="filters">The filters.</param>
    void SetFilters(IReadOnlyList<DataGridFilterDescription> filters);
}

/// <summary>
/// Adds searching capabilities to an <see cref="IDataGridView"/>.
/// </summary>
public interface ISearchableDataGridView : IDataGridView
{
    /// <summary>
    /// Gets the current search query.
    /// </summary>
    SearchQuery SearchQuery { get; }

    /// <summary>
    /// Sets the search query.
    /// </summary>
    /// <param name="query">The query.</param>
    void SetSearchQuery(in SearchQuery query);
}

