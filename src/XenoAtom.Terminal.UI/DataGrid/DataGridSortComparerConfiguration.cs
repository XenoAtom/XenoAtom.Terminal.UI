// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

internal readonly record struct DataGridSortComparerConfiguration(string ColumnKey, IComparer<object?> Comparer);

internal interface IConfigurableSortableDataGridView
{
    void ConfigureSortComparers(IReadOnlyList<DataGridSortComparerConfiguration> comparers);
}
