// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Data;

namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Adapts a <see cref="DataTable"/> into an <see cref="IDataGridDocument"/>.
/// </summary>
/// <remarks>
/// <para>
/// This adapter raises coarse-grained change notifications (typically <see cref="DataGridChangeKind.Reset"/>) because
/// <see cref="DataTable"/> cell updates do not participate in the binding dirty model.
/// </para>
/// <para>
/// Consumers that need binding-driven invalidation SHOULD project data into bindable row models.
/// </para>
/// </remarks>
public sealed class DataGridDataTableDocument : IDataGridDocument, IDisposable
{
    private readonly DataTable _table;
    private int _version;
    private int _updateDepth;
    private DataGridDocumentChangedEventArgs? _pending;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridDataTableDocument"/> class.
    /// </summary>
    /// <param name="table">The table to adapt.</param>
    public DataGridDataTableDocument(DataTable table)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        HookTableEvents();
    }

    /// <summary>
    /// Gets the adapted <see cref="DataTable"/>.
    /// </summary>
    public DataTable Table => _table;

    /// <inheritdoc />
    public IDataGridSnapshot CurrentSnapshot => new DataTableSnapshot(this);

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
        if (rowModel is not DataRow row)
        {
            throw new ArgumentException("Row model must be a DataRow for DataTable documents.", nameof(rowModel));
        }

        using var _ = BeginUpdate();

        rowIndex = Math.Clamp(rowIndex, 0, _table.Rows.Count);
        if (!ReferenceEquals(row.Table, _table))
        {
            throw new ArgumentException("Row belongs to a different DataTable.", nameof(rowModel));
        }

        _table.Rows.Remove(row);
        _table.Rows.InsertAt(row, rowIndex);
        BumpVersion(DataGridChangeKind.Rows);
    }

    /// <inheritdoc />
    public void ReplaceRow(int rowIndex, object rowModel)
    {
        if (rowModel is not DataRow row)
        {
            throw new ArgumentException("Row model must be a DataRow for DataTable documents.", nameof(rowModel));
        }

        if ((uint)rowIndex >= (uint)_table.Rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        using var _ = BeginUpdate();

        if (!ReferenceEquals(row.Table, _table))
        {
            throw new ArgumentException("Row belongs to a different DataTable.", nameof(rowModel));
        }

        _table.Rows.RemoveAt(rowIndex);
        _table.Rows.InsertAt(row, rowIndex);
        BumpVersion(DataGridChangeKind.Rows);
    }

    /// <inheritdoc />
    public void RemoveRows(int rowIndex, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if ((uint)rowIndex >= (uint)_table.Rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        using var _ = BeginUpdate();

        count = Math.Min(count, _table.Rows.Count - rowIndex);
        for (var i = 0; i < count; i++)
        {
            _table.Rows.RemoveAt(rowIndex);
        }

        BumpVersion(DataGridChangeKind.Rows);
    }

    /// <inheritdoc />
    public event EventHandler<DataGridDocumentChangedEventArgs>? Changed;

    /// <inheritdoc />
    public void Dispose() => UnhookTableEvents();

    private void HookTableEvents()
    {
        _table.TableNewRow += OnTableChanged;
        _table.RowChanged += OnTableChanged;
        _table.RowDeleted += OnTableChanged;
        _table.RowDeleting += OnTableChanged;
        _table.ColumnChanged += OnTableChanged;
        _table.ColumnChanging += OnTableChanged;
        _table.Columns.CollectionChanged += OnTableSchemaChanged;
    }

    private void UnhookTableEvents()
    {
        _table.TableNewRow -= OnTableChanged;
        _table.RowChanged -= OnTableChanged;
        _table.RowDeleted -= OnTableChanged;
        _table.RowDeleting -= OnTableChanged;
        _table.ColumnChanged -= OnTableChanged;
        _table.ColumnChanging -= OnTableChanged;
        _table.Columns.CollectionChanged -= OnTableSchemaChanged;
    }

    private void OnTableSchemaChanged(object? sender, System.ComponentModel.CollectionChangeEventArgs e)
    {
        _ = sender;
        _ = e;
        BumpVersion(DataGridChangeKind.Schema | DataGridChangeKind.Reset);
    }

    private void OnTableChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        // DataTable does not participate in the bindable dirty model; treat changes as a reset for consumers.
        BumpVersion(DataGridChangeKind.Reset);
    }

    private void BumpVersion(DataGridChangeKind kind)
    {
        var oldVersion = _version;
        var newVersion = unchecked(oldVersion + 1);
        _version = newVersion;

        var args = new DataGridDocumentChangedEventArgs
        {
            OldVersion = oldVersion,
            NewVersion = newVersion,
            Kind = kind,
        };

        if (_updateDepth > 0)
        {
            _pending = _pending is null
                ? args
                : new DataGridDocumentChangedEventArgs
                {
                    OldVersion = _pending.OldVersion,
                    NewVersion = args.NewVersion,
                    Kind = _pending.Kind | args.Kind,
                };
            return;
        }

        Changed?.Invoke(this, args);
    }

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
        private DataGridDataTableDocument? _doc;

        public UpdateScope(DataGridDataTableDocument doc) => _doc = doc;

        public void Dispose()
        {
            _doc?.EndUpdate();
            _doc = null;
        }
    }

    private sealed class DataRowAccessor : BindingAccessor
    {
        private readonly DataColumn _column;

        public DataRowAccessor(DataColumn column) : base(column.ColumnName)
        {
            _column = column;
        }

        public override object? GetValue(object instance)
        {
            var row = (DataRow)instance;
            var value = row[_column];
            return value == DBNull.Value ? null : value;
        }

        public override void SetValue(object instance, object? value)
        {
            var row = (DataRow)instance;
            row[_column] = value ?? DBNull.Value;
        }
    }

    private sealed class DataTableSnapshot : IDataGridSnapshot
    {
        private readonly DataGridDataTableDocument _doc;
        private readonly int _version;
        private readonly DataGridColumnInfo[] _columns;

        public DataTableSnapshot(DataGridDataTableDocument doc)
        {
            _doc = doc;
            _version = doc._version;

            var table = doc._table;
            var cols = new DataGridColumnInfo[table.Columns.Count];
            for (var i = 0; i < cols.Length; i++)
            {
                var c = table.Columns[i];
                cols[i] = new DataGridColumnInfo(
                    Key: c.ColumnName,
                    HeaderText: c.Caption ?? c.ColumnName,
                    ValueType: c.DataType,
                    ReadOnly: c.ReadOnly,
                    Accessor: new DataRowAccessor(c));
            }

            _columns = cols;
        }

        public int Version => _version;
        public int RowCount => _doc._table.Rows.Count;
        public int ColumnCount => _columns.Length;

        public DataGridColumnInfo GetColumn(int columnIndex) => _columns[columnIndex];
        public object GetRowModel(int rowIndex) => _doc._table.Rows[rowIndex];
    }
}
