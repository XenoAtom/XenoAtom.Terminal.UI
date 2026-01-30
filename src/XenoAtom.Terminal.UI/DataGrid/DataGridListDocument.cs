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
public sealed class DataGridListDocument<T> : IDataGridDocument where T : class
{
    private readonly List<T> _rows;
    private readonly List<DataGridColumnInfo> _columns;

    private int _version;
    private int _updateDepth;
    private DataGridDocumentChangedEventArgs? _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridListDocument{T}"/> class.
    /// </summary>
    public DataGridListDocument()
    {
        _rows = new List<T>();
        _columns = new List<DataGridColumnInfo>();
    }

    /// <summary>
    /// Gets the row model instances.
    /// </summary>
    public IReadOnlyList<T> Rows => _rows;

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
    void IDataGridDocument.InsertRow(int rowIndex, object rowModel)
    {
        if (rowModel is not T typedRow)
        {
            throw new ArgumentException($"Row model must be of type {typeof(T).FullName}.", nameof(rowModel));
        }

        InsertRow(rowIndex, typedRow);
    }

    /// <summary>
    /// Inserts a row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index at which to insert the row model.</param>
    /// <param name="rowModel">The row model to insert.</param>
    public void InsertRow(int rowIndex, T rowModel)
    {
        ArgumentNullException.ThrowIfNull(rowModel);
        rowIndex = Math.Clamp(rowIndex, 0, _rows.Count);
        _rows.Insert(rowIndex, rowModel);
        BumpVersion(DataGridChangeKind.Rows, rowIndex: rowIndex, rowCount: 1);
    }

    /// <inheritdoc />
    void IDataGridDocument.ReplaceRow(int rowIndex, object rowModel)
    {
        if (rowModel is not T typedRow)
        {
            throw new ArgumentException($"Row model must be of type {typeof(T).FullName}.", nameof(rowModel));
        }

        ReplaceRow(rowIndex, typedRow);
    }

    /// <summary>
    /// Replaces a row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index of the row model to replace.</param>
    /// <param name="rowModel">The row model to set.</param>
    public void ReplaceRow(int rowIndex, T rowModel)
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
    /// Adds a column to the end of the column schema.
    /// </summary>
    /// <param name="column">The column to add.</param>
    /// <returns>This instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="column"/> has an empty key.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="column"/> has a null accessor.</exception>
    public DataGridListDocument<T> AddColumn(DataGridColumnInfo column)
    {
        if (string.IsNullOrWhiteSpace(column.Key))
        {
            throw new ArgumentException("Column key must not be empty.", nameof(column));
        }

        ArgumentNullException.ThrowIfNull(column.Accessor);

        _columns.Add(column);
        BumpVersion(DataGridChangeKind.Schema, columnIndex: _columns.Count - 1, columnCount: 1);
        return this;
    }

    /// <summary>
    /// Adds a typed column to the end of the column schema.
    /// </summary>
    /// <typeparam name="TValue">The CLR type of values in the column.</typeparam>
    /// <param name="column">The typed column to add.</param>
    /// <returns>This instance.</returns>
    public DataGridListDocument<T> AddColumn<TValue>(DataGridColumnInfo<TValue> column) => AddColumn((DataGridColumnInfo)column);

    /// <summary>
    /// Adds a column using a typed accessor. The key and header text default to <see cref="BindingAccessor.Name"/>.
    /// </summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="accessor">The typed accessor.</param>
    /// <returns>This instance.</returns>
    public DataGridListDocument<T> AddColumn<TValue>(BindingAccessor<TValue> accessor) => AddColumn((DataGridColumnInfo<TValue>)accessor);

    /// <summary>
    /// Adds a row model to the end of the document.
    /// </summary>
    /// <param name="rowModel">The row model to add.</param>
    public void AddRow(T rowModel) => InsertRow(_rows.Count, rowModel);

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
        private DataGridListDocument<T>? _doc;

        public UpdateScope(DataGridListDocument<T> doc) => _doc = doc;

        public void Dispose()
        {
            _doc?.EndUpdate();
            _doc = null;
        }
    }

    private sealed class ListSnapshot : IDataGridSnapshot
    {
        private readonly DataGridListDocument<T> _doc;
        private readonly int _version;

        public ListSnapshot(DataGridListDocument<T> doc)
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
