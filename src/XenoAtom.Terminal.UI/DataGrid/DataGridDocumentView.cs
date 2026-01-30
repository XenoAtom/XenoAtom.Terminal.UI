// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Provides an in-memory projection over a document, supporting sorting, filtering, and search query state.
/// </summary>
/// <remarks>
/// <para>
/// This implementation rebuilds its row mapping on changes and is intended as a default view for small/medium datasets.
/// For very large or remote datasets, applications should implement <see cref="IDataGridView"/> directly.
/// </para>
/// </remarks>
public sealed class DataGridDocumentView : ISortableDataGridView, IFilterableDataGridView, ISearchableDataGridView, IDisposable
{
    private readonly IDataGridDocument _document;
    private readonly List<DataGridSortDescription> _sorts;
    private readonly List<DataGridFilterDescription> _filters;
    private SearchQuery _searchQuery;

    private int _version;
    private int[] _rowMap;
    private DataGridColumnInfo[] _columns;

    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridDocumentView"/> class.
    /// </summary>
    /// <param name="document">The underlying document.</param>
    /// <param name="culture">The culture used for string comparisons and formatting.</param>
    public DataGridDocumentView(IDataGridDocument document, CultureInfo? culture = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _culture = culture ?? CultureInfo.InvariantCulture;
        _sorts = new List<DataGridSortDescription>();
        _filters = new List<DataGridFilterDescription>();
        _searchQuery = default;

        _rowMap = Array.Empty<int>();
        _columns = Array.Empty<DataGridColumnInfo>();

        _document.Changed += OnDocumentChanged;
        Rebuild(kind: DataGridChangeKind.Reset);
    }

    /// <inheritdoc />
    public IDataGridDocument Document => _document;

    /// <inheritdoc />
    public IDataGridViewSnapshot CurrentSnapshot => new ViewSnapshot(this);

    /// <inheritdoc />
    public event EventHandler<DataGridViewChangedEventArgs>? Changed;

    /// <inheritdoc />
    public IReadOnlyList<DataGridSortDescription> SortDescriptions => _sorts;

    /// <inheritdoc />
    public void SetSortDescriptions(IReadOnlyList<DataGridSortDescription> sortDescriptions)
    {
        ArgumentNullException.ThrowIfNull(sortDescriptions);
        _sorts.Clear();
        for (var i = 0; i < sortDescriptions.Count; i++)
        {
            _sorts.Add(sortDescriptions[i]);
        }
        Rebuild(kind: DataGridChangeKind.Projection);
    }

    /// <inheritdoc />
    public IReadOnlyList<DataGridFilterDescription> Filters => _filters;

    /// <inheritdoc />
    public void SetFilters(IReadOnlyList<DataGridFilterDescription> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        _filters.Clear();
        for (var i = 0; i < filters.Count; i++)
        {
            _filters.Add(filters[i]);
        }
        Rebuild(kind: DataGridChangeKind.Projection);
    }

    /// <inheritdoc />
    public SearchQuery SearchQuery => _searchQuery;

    /// <inheritdoc />
    public void SetSearchQuery(in SearchQuery query)
    {
        _searchQuery = query;
        RaiseChanged(DataGridChangeKind.Projection);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _document.Changed -= OnDocumentChanged;
    }

    private void OnDocumentChanged(object? sender, DataGridDocumentChangedEventArgs e)
    {
        _ = sender;
        _ = e;
        // Conservative approach: rebuild mapping on document structural/schema changes.
        Rebuild(kind: e.Kind);
    }

    private void Rebuild(DataGridChangeKind kind)
    {
        var snapshot = _document.CurrentSnapshot;

        // Cache columns by key for quick lookups in Sort/Filter.
        var cols = new DataGridColumnInfo[snapshot.ColumnCount];
        for (var c = 0; c < cols.Length; c++)
        {
            cols[c] = snapshot.GetColumn(c);
        }
        _columns = cols;

        var docRows = snapshot.RowCount;
        var rowMapList = new List<int>(docRows);

        for (var r = 0; r < docRows; r++)
        {
            if (PassesFilters(snapshot, r))
            {
                rowMapList.Add(r);
            }
        }

        if (_sorts.Count != 0)
        {
            rowMapList.Sort((a, b) => CompareRows(snapshot, a, b));
        }

        _rowMap = rowMapList.Count == 0 ? Array.Empty<int>() : rowMapList.ToArray();

        RaiseChanged(kind | DataGridChangeKind.Projection);
    }

    private int CompareRows(IDataGridSnapshot snapshot, int docRowA, int docRowB)
    {
        for (var i = 0; i < _sorts.Count; i++)
        {
            var s = _sorts[i];
            if (!TryGetColumnIndexByKey(s.ColumnKey, out var colIndex))
            {
                continue;
            }

            var column = _columns[colIndex];
            var rowA = snapshot.GetRowModel(docRowA);
            var rowB = snapshot.GetRowModel(docRowB);

            var a = column.Accessor.GetValue(rowA);
            var b = column.Accessor.GetValue(rowB);

            var cmp = CompareValues(a, b);
            if (cmp == 0)
            {
                continue;
            }

            if (s.Direction == DataGridSortDirection.Descending)
            {
                cmp = -cmp;
            }

            return cmp;
        }

        return docRowA.CompareTo(docRowB);
    }

    private bool PassesFilters(IDataGridSnapshot snapshot, int docRowIndex)
    {
        if (_filters.Count == 0)
        {
            return true;
        }

        var rowModel = snapshot.GetRowModel(docRowIndex);
        for (var i = 0; i < _filters.Count; i++)
        {
            var f = _filters[i];
            if (string.IsNullOrEmpty(f.Text))
            {
                continue;
            }

            if (!TryGetColumnIndexByKey(f.ColumnKey, out var colIndex))
            {
                continue;
            }

            var column = _columns[colIndex];
            var value = column.Accessor.GetValue(rowModel);
            var text = ValueStringFormatter.ToString(value, _culture);
            if (text.IndexOf(f.Text!, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetColumnIndexByKey(string key, out int columnIndex)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (string.Equals(_columns[i].Key, key, StringComparison.Ordinal))
            {
                columnIndex = i;
                return true;
            }
        }

        columnIndex = -1;
        return false;
    }

    private static int CompareValues(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a is null)
        {
            return -1;
        }

        if (b is null)
        {
            return 1;
        }

        if (a is IComparable comparable)
        {
            try
            {
                return comparable.CompareTo(b);
            }
            catch
            {
                // Fall back to string compare.
            }
        }

        var sa = a.ToString() ?? string.Empty;
        var sb = b.ToString() ?? string.Empty;
        return string.Compare(sa, sb, StringComparison.OrdinalIgnoreCase);
    }

    private void RaiseChanged(DataGridChangeKind kind)
    {
        var oldVersion = _version;
        var newVersion = unchecked(oldVersion + 1);
        _version = newVersion;
        Changed?.Invoke(this, new DataGridViewChangedEventArgs { OldVersion = oldVersion, NewVersion = newVersion, Kind = kind });
    }

    private sealed class ViewSnapshot : IDataGridViewSnapshot
    {
        private readonly DataGridDocumentView _view;
        private readonly int _version;
        private readonly IDataGridSnapshot _docSnapshot;

        public ViewSnapshot(DataGridDocumentView view)
        {
            _view = view;
            _version = view._version;
            _docSnapshot = view._document.CurrentSnapshot;
        }

        public int Version => _version;
        public int RowCount => _view._rowMap.Length;
        public int ColumnCount => _docSnapshot.ColumnCount;

        public DataGridColumnInfo GetColumn(int columnIndex) => _docSnapshot.GetColumn(columnIndex);

        public int MapRowToDocument(int viewRowIndex) => _view._rowMap[viewRowIndex];

        public object GetRowModel(int viewRowIndex)
        {
            var docRow = MapRowToDocument(viewRowIndex);
            return _docSnapshot.GetRowModel(docRow);
        }
    }
}

