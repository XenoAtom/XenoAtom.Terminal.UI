// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// A simple in-memory <see cref="IDataGridDocument"/> backed by a list of row model instances.
/// </summary>
/// <remarks>
/// This adapter is intended for view-model scenarios where each row model exposes bindable properties and
/// columns are defined through <see cref="DataGridColumnInfo"/> accessors.
/// </remarks>
public sealed class DataGridListDocument : IDataGridDocument
{
    private readonly List<object> _rows;
    private readonly List<DataGridColumnInfo> _columns;

    private int _version;
    private int _updateDepth;
    private DataGridDocumentChangedEventArgs? _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridListDocument"/> class.
    /// </summary>
    public DataGridListDocument()
    {
        _rows = new List<object>();
        _columns = new List<DataGridColumnInfo>();
    }

    /// <summary>
    /// Gets the row model instances.
    /// </summary>
    public IReadOnlyList<object> Rows => _rows;

    /// <summary>
    /// Gets the column schema.
    /// </summary>
    public IReadOnlyList<DataGridColumnInfo> Columns => _columns;

    /// <inheritdoc />
    public IDataGridSnapshot CurrentSnapshot => new ListSnapshot(this);

    /// <inheritdoc />
    public int Version => _version;

    /// <inheritdoc />
    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    /// <inheritdoc />
    public void InsertRow(int rowIndex, object rowModel)
    {
        ArgumentNullException.ThrowIfNull(rowModel);
        rowIndex = Math.Clamp(rowIndex, 0, _rows.Count);
        _rows.Insert(rowIndex, rowModel);
        BumpVersion(DataGridChangeKind.Rows, rowIndex: rowIndex, rowCount: 1);
    }

    /// <inheritdoc />
    public void ReplaceRow(int rowIndex, object rowModel)
    {
        ArgumentNullException.ThrowIfNull(rowModel);
        if ((uint)rowIndex >= (uint)_rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        _rows[rowIndex] = rowModel;
        BumpVersion(DataGridChangeKind.Rows, rowIndex: rowIndex, rowCount: 1);
    }

    /// <inheritdoc />
    public void RemoveRows(int rowIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if ((uint)rowIndex >= (uint)_rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        count = Math.Min(count, _rows.Count - rowIndex);
        for (var i = 0; i < count; i++)
        {
            _rows.RemoveAt(rowIndex);
        }

        BumpVersion(DataGridChangeKind.Rows, rowIndex: rowIndex, rowCount: count);
    }

    /// <summary>
    /// Replaces the entire column schema.
    /// </summary>
    /// <param name="columns">The new columns.</param>
    public void SetColumns(ReadOnlySpan<DataGridColumnInfo> columns)
    {
        using var _ = BeginUpdate();
        _columns.Clear();
        for (var i = 0; i < columns.Length; i++)
        {
            _columns.Add(columns[i]);
        }
        BumpVersion(DataGridChangeKind.Schema, columnIndex: 0, columnCount: columns.Length);
    }

    /// <summary>
    /// Adds a row model to the end of the document.
    /// </summary>
    /// <param name="rowModel">The row model to add.</param>
    public void AddRow(object rowModel) => InsertRow(_rows.Count, rowModel);

    /// <inheritdoc />
    public event EventHandler<DataGridDocumentChangedEventArgs>? Changed;

    private void BumpVersion(DataGridChangeKind kind, int rowIndex = -1, int rowCount = 0, int columnIndex = -1, int columnCount = 0)
    {
        var oldVersion = _version;
        var newVersion = unchecked(oldVersion + 1);
        _version = newVersion;

        var args = new DataGridDocumentChangedEventArgs
        {
            OldVersion = oldVersion,
            NewVersion = newVersion,
            Kind = kind,
            RowIndex = rowIndex,
            RowCount = rowCount,
            ColumnIndex = columnIndex,
            ColumnCount = columnCount,
        };

        if (_updateDepth > 0)
        {
            _pending = _pending is null
                ? args
                : Coalesce(_pending, args);
            return;
        }

        Changed?.Invoke(this, args);
    }

    private static DataGridDocumentChangedEventArgs Coalesce(DataGridDocumentChangedEventArgs a, DataGridDocumentChangedEventArgs b)
        => new()
        {
            OldVersion = a.OldVersion,
            NewVersion = b.NewVersion,
            Kind = a.Kind | b.Kind,
            RowIndex = -1,
            RowCount = 0,
            ColumnIndex = -1,
            ColumnCount = 0,
        };

    private void EndUpdate()
    {
        if (_updateDepth <= 0)
        {
            return;
        }

        _updateDepth--;
        if (_updateDepth != 0)
        {
            return;
        }

        var pending = _pending;
        _pending = null;
        if (pending is not null)
        {
            Changed?.Invoke(this, pending);
        }
    }

    private sealed class UpdateScope : IDisposable
    {
        private DataGridListDocument? _doc;

        public UpdateScope(DataGridListDocument doc) => _doc = doc;

        public void Dispose()
        {
            _doc?.EndUpdate();
            _doc = null;
        }
    }

    private sealed class ListSnapshot : IDataGridSnapshot
    {
        private readonly DataGridListDocument _doc;
        private readonly int _version;

        public ListSnapshot(DataGridListDocument doc)
        {
            _doc = doc;
            _version = doc._version;
        }

        public int Version => _version;
        public int RowCount => _doc._rows.Count;
        public int ColumnCount => _doc._columns.Count;

        public DataGridColumnInfo GetColumn(int columnIndex) => _doc._columns[columnIndex];
        public object GetRowModel(int rowIndex) => _doc._rows[rowIndex];
    }
}
