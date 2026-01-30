// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Specifies the selection mode for <see cref="DataGridControl"/>.
/// </summary>
public enum DataGridSelectionMode
{
    /// <summary>
    /// Select individual cells.
    /// </summary>
    Cell,

    /// <summary>
    /// Select rows.
    /// </summary>
    Row,

    /// <summary>
    /// Select columns.
    /// </summary>
    Column,
}

/// <summary>
/// Represents a cell coordinate (row, column).
/// </summary>
public readonly record struct DataGridCell(int Row, int Column)
{
    /// <summary>
    /// Gets the "no cell" value.
    /// </summary>
    public static DataGridCell None => new(-1, -1);
}

/// <summary>
/// Specifies when editing starts in a <see cref="DataGridControl"/>.
/// </summary>
public enum DataGridEditMode
{
    /// <summary>
    /// Editing starts when the user presses Enter or F2 on the current cell.
    /// </summary>
    OnEnter,

    /// <summary>
    /// Editing starts when the current cell changes.
    /// </summary>
    OnCellChange,

    /// <summary>
    /// Editing starts when the user types.
    /// </summary>
    OnTyping,
}

/// <summary>
/// A high-performance, scrollable, virtualized, data-bound grid control.
/// </summary>
public sealed partial class DataGridControl : Visual, IScrollable
{
    private readonly ScrollModel _scroll;
    private readonly BindableList<DataGridColumn> _columns;

    private readonly VisualList<Visual> _headerVisuals;
    private readonly List<int> _headerVisualColumns;
    private readonly BindableList<Visual> _cellVisuals;
    private readonly List<Visual> _cellRecyclePool;

    private readonly VisualList<Visual> _filterVisuals;
    private readonly List<TextBox> _filterBoxes;

    private readonly SearchReplacePopup _searchPopup;
    private readonly DataGridSearchTarget _searchTarget;

    private IDataGridDocument? _appliedDocument;
    private IDataGridView? _appliedView;

    private IDataGridViewSnapshot? _lastSnapshot;
    private int _lastSnapshotVersion = -1;
    private int _lastSnapshotColumnCount = -1;
    private int _lastColumnsVersion = -1;

    private int[] _resolvedColumnWidths = Array.Empty<int>();
    private int[] _resolvedColumnStarts = Array.Empty<int>();
    private int[] _visibleColumnToSnapshotColumn = Array.Empty<int>();

    private bool _ensureCurrentCellVisible;

    private Visual? _activeEditor;
    private DataGridCell _activeEditorCell = DataGridCell.None;
    private bool _pendingStartEdit;

    private int _lastMatchesKey;
    private readonly List<DataGridCell> _matches;
    private int _activeMatchIndex;
    private string? _searchError;

    private bool _updatingFilterUi;
    private int _lastFilterHash;

    private readonly List<ResolvedColumn> _cachedResolvedColumns = new(32);

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridControl"/> class.
    /// </summary>
    public DataGridControl()
    {
        Focusable = true;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Stretch;

        _scroll = new ScrollModel(this);
        _columns = new BindableList<DataGridColumn>(
            owner: this,
            name: "DataGrid.Columns",
            onAdding: AttachColumn,
            onRemoving: DetachColumn);

        _headerVisuals = new VisualList<Visual>(this, "DataGrid.HeaderVisuals");
        _headerVisualColumns = new List<int>();
        _cellRecyclePool = new List<Visual>(64);
        _cellVisuals = new BindableList<Visual>(
            owner: this,
            name: "DataGrid.CellVisuals",
            onAdding: AttachCollectionChild,
            onRemoving: v =>
            {
                DetachCollectionChild(v);
                _cellRecyclePool.Add(v);
            });

        _filterVisuals = new VisualList<Visual>(this, "DataGrid.FilterVisuals");
        _filterBoxes = new List<TextBox>();

        _matches = new List<DataGridCell>();
        _activeMatchIndex = -1;

        _searchTarget = new DataGridSearchTarget(this);
        _searchPopup = new SearchReplacePopup(_searchTarget);
        AttachChild(_searchPopup);

        this.ShowHeader(true);
        this.SelectionMode(DataGridSelectionMode.Cell);
        this.EditMode(DataGridEditMode.OnEnter);
        this.FrozenRows(0);
        this.FrozenColumns(0);
        this.CurrentCell(DataGridCell.None);
    }

    /// <summary>
    /// Gets the scroll model for the grid.
    /// </summary>
    public ScrollModel Scroll => _scroll;

    /// <summary>
    /// Gets the columns collection.
    /// </summary>
    [Bindable]
    public BindableList<DataGridColumn> Columns => _columns;

    /// <summary>
    /// Gets or sets the document bound to this grid.
    /// </summary>
    [Bindable]
    public partial IDataGridDocument? Document { get; set; }

    /// <summary>
    /// Gets or sets the view (sorted/filtered projection) bound to this grid.
    /// </summary>
    [Bindable]
    public partial IDataGridView? View { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header row is shown.
    /// </summary>
    [Bindable]
    public partial bool ShowHeader { get; set; }

    /// <summary>
    /// Gets or sets the number of frozen data rows (in addition to the header).
    /// </summary>
    [Bindable]
    public partial int FrozenRows { get; set; }

    /// <summary>
    /// Gets or sets the number of frozen columns.
    /// </summary>
    [Bindable]
    public partial int FrozenColumns { get; set; }

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    [Bindable]
    public partial DataGridSelectionMode SelectionMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the grid is read-only (disables editing).
    /// </summary>
    [Bindable]
    public partial bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the current cell (in view coordinates).
    /// </summary>
    [Bindable]
    public partial DataGridCell CurrentCell { get; set; }

    /// <summary>
    /// Gets or sets the edit mode.
    /// </summary>
    [Bindable]
    public partial DataGridEditMode EditMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the filter row is visible.
    /// </summary>
    [Bindable]
    public partial bool FilterRowVisible { get; set; }

    /// <summary>
    /// Gets or sets the current search query used for match highlighting and navigation.
    /// </summary>
    [Bindable]
    public partial SearchQuery SearchQuery { get; set; }

    [Bindable]
    private partial int ScrollVersion { get; set; }

    [Bindable]
    private partial int SourceVersion { get; set; }

    [Bindable]
    private partial int MeasuredContentWidth { get; set; }

    partial void OnFrozenRowsChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);
    partial void OnFrozenColumnsChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnCurrentCellChanging(ref DataGridCell value)
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            value = DataGridCell.None;
            return;
        }

        var rows = snapshot.RowCount;
        var cols = GetVisibleColumnCount(snapshot);
        if (rows <= 0 || cols <= 0)
        {
            value = DataGridCell.None;
            return;
        }

        var row = Math.Clamp(value.Row, 0, rows - 1);
        var col = Math.Clamp(value.Column, 0, cols - 1);
        value = new DataGridCell(row, col);
    }

    partial void OnCurrentCellChanged(DataGridCell value)
    {
        _ = value;
        _ensureCurrentCellVisible = true;
        if (EditMode == DataGridEditMode.OnCellChange)
        {
            _pendingStartEdit = true;
        }
    }

    partial void OnDocumentChanged(IDataGridDocument? value)
    {
        if (_appliedDocument is not null)
        {
            _appliedDocument.Changed -= OnSourceChanged;
        }

        _appliedDocument = value;
        if (_appliedDocument is not null)
        {
            _appliedDocument.Changed += OnSourceChanged;
            SourceVersion = _appliedDocument.Version;
        }
        else
        {
            SourceVersion = 0;
        }
    }

    partial void OnViewChanged(IDataGridView? value)
    {
        if (_appliedView is not null)
        {
            _appliedView.Changed -= OnViewChangedEvent;
        }

        _appliedView = value;
        if (_appliedView is not null)
        {
            _appliedView.Changed += OnViewChangedEvent;
            SourceVersion = _appliedView.CurrentSnapshot.Version;
        }
    }

    private void OnSourceChanged(object? sender, DataGridDocumentChangedEventArgs e)
    {
        _ = sender;
        SourceVersion = e.NewVersion;
    }

    private void OnViewChangedEvent(object? sender, DataGridViewChangedEventArgs e)
    {
        _ = sender;
        SourceVersion = e.NewVersion;
    }

    /// <inheritdoc />
    protected override int ChildrenCount
    {
        get
        {
            var count = _headerVisuals.Count + _filterVisuals.Count + _cellVisuals.Count;
            if (_activeEditor is not null)
            {
                count++;
            }

            return count + 1;
        }
    }

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        var i = index;
        if ((uint)i < (uint)_headerVisuals.Count)
        {
            return _headerVisuals[i];
        }

        i -= _headerVisuals.Count;
        if ((uint)i < (uint)_filterVisuals.Count)
        {
            return _filterVisuals[i];
        }

        i -= _filterVisuals.Count;
        if ((uint)i < (uint)_cellVisuals.Count)
        {
            return _cellVisuals[i];
        }

        i -= _cellVisuals.Count;
        if (_activeEditor is not null)
        {
            if (i == 0)
            {
                return _activeEditor;
            }

            i--;
        }

        if (i == 0)
        {
            return _searchPopup;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override void PrepareChildren()
    {
        ScrollVersion = _scroll.Version;

        _ = SourceVersion;

        EnsureHeaderVisuals();
        EnsureFilterVisuals();
        ApplyFilterToViewIfNeeded();

        if (!SearchQuery.Equals(default(SearchQuery)))
        {
            RebuildMatchesIfNeeded();
        }
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;

        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);

        var columnsWidth = ComputeNaturalColumnsWidth(snapshot, visibleColumns, constraints);

        var height = headerHeight + filterHeight + Math.Max(0, rowCount);
        height = Math.Max(1, height);

        MeasuredContentWidth = Math.Max(0, columnsWidth);

        var scrollBarThickness = Math.Max(1, GetStyle<ScrollViewerStyle>().ScrollBarThickness);
        var reserveVerticalBar = constraints.IsHeightBounded && height > constraints.MaxHeight;
        var reservedWidth = columnsWidth + (reserveVerticalBar ? scrollBarThickness : 0);

        var min = new Size(4, Math.Max(1, headerHeight + filterHeight + 1));
        var natural = new Size(
            constraints.IsWidthBounded ? Math.Min(reservedWidth, constraints.MaxWidth) : reservedWidth,
            constraints.IsHeightBounded ? Math.Min(height, constraints.MaxHeight) : height);
        var max = new Size(LayoutConstants.Infinite, LayoutConstants.Infinite);

        return SizeHints.Flex(
            min: constraints.Clamp(min),
            natural: constraints.Clamp(natural),
            max: max,
            growX: HorizontalAlignment == Align.Stretch ? 1 : 0,
            growY: VerticalAlignment == Align.Stretch ? 1 : 0,
            shrinkX: 1,
            shrinkY: 1);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var rect = finalRect;

        _ = ScrollVersion;
        _ = MeasuredContentWidth;

        var snapshot = GetSnapshot();
        if (snapshot is null || rect.Width <= 0 || rect.Height <= 0)
        {
            _scroll.SetViewport(0, 0);
            _scroll.SetExtent(0, 0);
            return;
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;

        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);

        ResolveColumnLayout(snapshot, visibleColumns, rect.Width);

        var frozenWidth = SumColumnsWidth(0, frozenColumns);
        var scrollViewportWidth = Math.Max(0, rect.Width - frozenWidth);
        var scrollViewportHeight = Math.Max(0, rect.Height - headerHeight - filterHeight - frozenRows);

        var scrollExtentWidth = Math.Max(0, SumColumnsWidth(frozenColumns, visibleColumns));
        var scrollExtentHeight = Math.Max(0, rowCount - frozenRows);

        _scroll.SetViewport(scrollViewportWidth, scrollViewportHeight);
        _scroll.SetExtent(scrollExtentWidth, scrollExtentHeight);

        if (_ensureCurrentCellVisible)
        {
            EnsureCurrentCellVisible(snapshot, frozenRows, frozenColumns);
            _ensureCurrentCellVisible = false;
        }

        ArrangeHeaderAndFilter(rect, headerHeight, filterHeight, frozenColumns);
        EnsureCellVisuals(snapshot, rect, headerHeight, filterHeight, frozenRows, frozenColumns);

        if (_pendingStartEdit)
        {
            _pendingStartEdit = false;
            _ = TryStartEdit(snapshot);
        }

        if (_activeEditor is not null)
        {
            var editorRect = TryGetCellRect(_activeEditorCell, rect, headerHeight, filterHeight, frozenRows, frozenColumns);
            if (editorRect is { } r)
            {
                _activeEditor.Arrange(r);
                App?.Focus(_activeEditor);
            }
            else
            {
                CloseEditor();
            }
        }

        _searchPopup.ArrangeWithin(rect);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        _ = ScrollVersion;
        _ = SourceVersion;

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        var style = GetStyle<DataGridStyle>();
        var theme = GetTheme();

        var cellStyle = style.ResolveCellStyle(theme);
        var headerStyle = style.ResolveHeaderStyle(theme);
        var selectionStyle = style.ResolveSelectionStyle(theme, focused: HasFocusWithin);
        var matchStyle = style.ResolveMatchHighlightStyle(theme);

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;
        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);

        FillRect(buffer, rect, cellStyle);

        if (headerHeight > 0)
        {
            FillRect(buffer, new Rectangle(rect.X, rect.Y, rect.Width, 1), headerStyle);
        }

        if (filterHeight > 0)
        {
            FillRect(buffer, new Rectangle(rect.X, rect.Y + headerHeight, rect.Width, 1), headerStyle);
        }

        if (headerHeight > 0)
        {
            var cols = EnsureResolvedColumns(snapshot, visibleColumns);
            for (var c = 0; c < cols.Count && c < _resolvedColumnWidths.Length; c++)
            {
                var col = cols[c];
                if (col.HeaderVisual is not null)
                {
                    continue;
                }

                var w = _resolvedColumnWidths[c];
                if (w <= 0)
                {
                    continue;
                }

                var x = rect.X + GetColumnX(c, rect, frozenColumns);
                if (x >= rect.Right || x + w <= rect.X)
                {
                    continue;
                }

                var align = col.Column?.HeaderAlignment ?? TextAlignment.Left;
                WriteAlignedText(buffer, new Rectangle(x, rect.Y, w, 1), col.HeaderText.AsSpan(), headerStyle, align);
            }
        }

        var searchText = SearchQuery.Text;
        var hasSearch = !string.IsNullOrEmpty(searchText);

        for (var viewRow = 0; viewRow < frozenRows; viewRow++)
        {
            var y = rect.Y + headerHeight + filterHeight + viewRow;
            if ((uint)(y - rect.Y) >= (uint)rect.Height)
            {
                break;
            }

            RenderRow(buffer, snapshot, viewRow, y, rect, visibleColumns, frozenColumns, cellStyle, selectionStyle, matchStyle, hasSearch ? searchText! : null);
        }

        var scrollRowsViewport = Math.Max(0, rect.Height - headerHeight - filterHeight - frozenRows);
        var scrollOffset = _scroll.OffsetY;
        for (var r = 0; r < scrollRowsViewport; r++)
        {
            var viewRow = frozenRows + scrollOffset + r;
            if ((uint)viewRow >= (uint)rowCount)
            {
                break;
            }

            var y = rect.Y + headerHeight + filterHeight + frozenRows + r;
            if ((uint)(y - rect.Y) >= (uint)rect.Height)
            {
                break;
            }

            RenderRow(buffer, snapshot, viewRow, y, rect, visibleColumns, frozenColumns, cellStyle, selectionStyle, matchStyle, hasSearch ? searchText! : null);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheel(PointerEventArgs e)
    {
        if (e.WheelDelta == 0)
        {
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Shift) != 0)
        {
            var maxOffset = Math.Max(0, _scroll.ExtentWidth - _scroll.ViewportWidth);
            if (maxOffset == 0)
            {
                return;
            }

            _scroll.SetOffset(
                e.WheelDelta > 0 ? Math.Max(0, _scroll.OffsetX - 1) : Math.Min(maxOffset, _scroll.OffsetX + 1),
                _scroll.OffsetY);
        }
        else
        {
            var maxOffset = Math.Max(0, _scroll.ExtentHeight - _scroll.ViewportHeight);
            if (maxOffset == 0)
            {
                return;
            }

            _scroll.SetOffset(
                _scroll.OffsetX,
                e.WheelDelta > 0 ? Math.Max(0, _scroll.OffsetY - 1) : Math.Min(maxOffset, _scroll.OffsetY + 1));
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Kind != TerminalMouseKind.Down || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        var hit = TryHitTestCell(snapshot, e.UiX, e.UiY);
        if (hit is { } cell)
        {
            CurrentCell = cell;
            App?.Focus(this);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Ctrl+Shift+F: toggle filter row.
        if ((e.Modifiers & (TerminalModifiers.Ctrl | TerminalModifiers.Shift)) == (TerminalModifiers.Ctrl | TerminalModifiers.Shift)
            && e.Char is TerminalChar.CtrlF)
        {
            if (CanFilter)
            {
                FilterRowVisible = !FilterRowVisible;
            }
            e.Handled = true;
            return;
        }

        // Ctrl+F: open search.
        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlF)
        {
            OpenSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == TerminalKey.F3)
        {
            if ((e.Modifiers & TerminalModifiers.Shift) != 0)
            {
                PreviousMatch();
            }
            else
            {
                NextMatch();
            }
            e.Handled = true;
            return;
        }

        if (_activeEditor is not null && e.Key is TerminalKey.Escape or TerminalKey.Enter or TerminalKey.Tab)
        {
            if (e.Key == TerminalKey.Escape)
            {
                CloseEditor();
            }
            else
            {
                var cell = _activeEditorCell;
                CloseEditor();
                if (e.Key == TerminalKey.Tab)
                {
                    MoveCurrentCell(deltaRow: 0, deltaCol: (e.Modifiers & TerminalModifiers.Shift) != 0 ? -1 : 1);
                }
                else
                {
                    CurrentCell = cell;
                }
            }

            e.Handled = true;
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                MoveCurrentCell(0, -1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                MoveCurrentCell(0, 1);
                e.Handled = true;
                return;
            case TerminalKey.Up:
                MoveCurrentCell(-1, 0);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                MoveCurrentCell(1, 0);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                MoveCurrentCell(-Math.Max(1, _scroll.ViewportHeight - 1), 0);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                MoveCurrentCell(Math.Max(1, _scroll.ViewportHeight - 1), 0);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                CurrentCell = new DataGridCell(CurrentCell.Row, 0);
                e.Handled = true;
                return;
            case TerminalKey.End:
                CurrentCell = new DataGridCell(CurrentCell.Row, Math.Max(0, GetVisibleColumnCount(snapshot) - 1));
                e.Handled = true;
                return;
            case TerminalKey.Enter:
            case TerminalKey.F2:
                if (!ReadOnly && TryStartEdit(snapshot))
                {
                    e.Handled = true;
                }
                return;
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (EditMode != DataGridEditMode.OnTyping || ReadOnly || _activeEditor is not null)
        {
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        _ = TryStartEdit(snapshot);
    }

    /// <summary>
    /// Opens the built-in search UI.
    /// </summary>
    public void OpenSearch()
    {
        VerifyAccess();
        _searchPopup.OpenFind(SearchQuery.Text);
    }

    /// <summary>
    /// Closes the built-in search UI.
    /// </summary>
    public void CloseSearch()
    {
        VerifyAccess();
        _searchPopup.Close();
    }

    private void NextMatch()
    {
        VerifyAccess();
        RebuildMatchesIfNeeded();
        if (_matches.Count == 0)
        {
            return;
        }

        _activeMatchIndex = _activeMatchIndex < 0 ? 0 : (_activeMatchIndex + 1) % _matches.Count;
        CurrentCell = _matches[_activeMatchIndex];
    }

    private void PreviousMatch()
    {
        VerifyAccess();
        RebuildMatchesIfNeeded();
        if (_matches.Count == 0)
        {
            return;
        }

        _activeMatchIndex = _activeMatchIndex < 0 ? _matches.Count - 1 : (_activeMatchIndex - 1 + _matches.Count) % _matches.Count;
        CurrentCell = _matches[_activeMatchIndex];
    }

    private string GetSearchStatusText()
    {
        // Ensure this participates in dependency tracking: query and source version affect matches.
        _ = SourceVersion;
        _ = SearchQuery;

        if (string.IsNullOrEmpty(SearchQuery.Text))
        {
            return "No search";
        }

        RebuildMatchesIfNeeded();
        if (_matches.Count == 0)
        {
            return "0 matches";
        }

        var active = _activeMatchIndex < 0 ? 0 : _activeMatchIndex + 1;
        return $"{active}/{_matches.Count}";
    }

    private string? GetSearchErrorText()
    {
        _ = SourceVersion;
        _ = SearchQuery;
        return _searchError;
    }

    private void AttachColumn(DataGridColumn column) => column.Attach(this);

    private void DetachColumn(DataGridColumn column) => column.Detach(this);

    private IDataGridViewSnapshot? GetSnapshot()
    {
        var view = View;
        if (view is not null)
        {
            return view.CurrentSnapshot;
        }

        var doc = Document;
        if (doc is null)
        {
            return null;
        }

        return new IdentitySnapshot(doc.CurrentSnapshot);
    }

    private int GetVisibleColumnCount(IDataGridViewSnapshot snapshot)
    {
        var cols = Columns;
        if (cols.Count == 0)
        {
            return snapshot.ColumnCount;
        }

        var count = 0;
        for (var i = 0; i < cols.Count; i++)
        {
            if (cols[i].Visible)
            {
                count++;
            }
        }

        return count;
    }

    private bool CanFilter => View is IFilterableDataGridView;

    private int ComputeNaturalColumnsWidth(IDataGridViewSnapshot snapshot, int visibleColumns, in LayoutConstraints constraints)
    {
        var style = GetStyle<DataGridStyle>();
        var spacing = Math.Max(0, style.ColumnSpacing);
        var showVerticalLines = style.ShowVerticalLines;

        var cols = EnsureResolvedColumns(snapshot, visibleColumns);
        var width = 0;

        for (var i = 0; i < cols.Count; i++)
        {
            width += cols[i].BaseWidth;
            if (i + 1 < cols.Count)
            {
                width += showVerticalLines ? 1 : spacing;
            }
        }

        width = Math.Max(1, width);

        if (constraints.IsWidthBounded)
        {
            width = Math.Min(width, constraints.MaxWidth);
        }

        return width;
    }

    private void ResolveColumnLayout(IDataGridViewSnapshot snapshot, int visibleColumns, int availableWidth)
    {
        var style = GetStyle<DataGridStyle>();
        var spacing = Math.Max(0, style.ColumnSpacing);
        var showVerticalLines = style.ShowVerticalLines;

        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

        if (_resolvedColumnWidths.Length != columns.Count)
        {
            _resolvedColumnWidths = new int[columns.Count];
            _resolvedColumnStarts = new int[columns.Count];
        }

        var totalFixed = 0;
        var totalStarWeight = 0.0;

        Span<double> starWeights = columns.Count <= 128 ? stackalloc double[columns.Count] : new double[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            var c = columns[i];
            _resolvedColumnWidths[i] = Math.Clamp(Math.Max(0, c.BaseWidth), c.MinWidth, c.MaxWidth);
            if (c.IsStar)
            {
                var weight = c.StarWeight <= 0 ? 1 : c.StarWeight;
                starWeights[i] = weight;
                totalStarWeight += weight;
            }
            else
            {
                totalFixed += _resolvedColumnWidths[i];
            }
        }

        var separators = columns.Count <= 1
            ? 0
            : (columns.Count - 1) * (showVerticalLines ? 1 : spacing);

        var availableForCells = Math.Max(0, availableWidth - separators);
        var currentTotal = totalFixed;
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].IsStar)
            {
                currentTotal += _resolvedColumnWidths[i];
            }
        }

        if (totalStarWeight > 0 && currentTotal < availableForCells)
        {
            var extra = availableForCells - currentTotal;
            for (var i = 0; i < columns.Count; i++)
            {
                if (!columns[i].IsStar)
                {
                    continue;
                }

                var share = (int)Math.Floor(extra * (starWeights[i] / totalStarWeight));
                _resolvedColumnWidths[i] = Math.Clamp(_resolvedColumnWidths[i] + share, columns[i].MinWidth, columns[i].MaxWidth);
            }
        }

        var x = 0;
        for (var i = 0; i < columns.Count; i++)
        {
            _resolvedColumnStarts[i] = x;
            x += _resolvedColumnWidths[i];
            if (i + 1 < columns.Count)
            {
                x += showVerticalLines ? 1 : spacing;
            }
        }
    }

    private int SumColumnsWidth(int start, int endExclusive)
    {
        if (_resolvedColumnWidths.Length == 0)
        {
            return 0;
        }

        var style = GetStyle<DataGridStyle>();
        var spacing = Math.Max(0, style.ColumnSpacing);
        var showVerticalLines = style.ShowVerticalLines;

        var x = 0;
        for (var i = start; i < endExclusive && (uint)i < (uint)_resolvedColumnWidths.Length; i++)
        {
            x += _resolvedColumnWidths[i];
            if (i + 1 < endExclusive)
            {
                x += showVerticalLines ? 1 : spacing;
            }
        }

        return x;
    }

    private void ArrangeHeaderAndFilter(Rectangle rect, int headerHeight, int filterHeight, int frozenColumns)
    {
        var yHeader = rect.Y;
        var yFilter = rect.Y + headerHeight;

        if (headerHeight > 0)
        {
            for (var i = 0; i < _headerVisuals.Count && i < _headerVisualColumns.Count; i++)
            {
                var visibleColumnIndex = _headerVisualColumns[i];
                if ((uint)visibleColumnIndex >= (uint)_resolvedColumnWidths.Length)
                {
                    continue;
                }

                var x = rect.X + GetColumnX(visibleColumnIndex, rect, frozenColumns);
                var w = _resolvedColumnWidths[visibleColumnIndex];
                _headerVisuals[i].Arrange(new Rectangle(x, yHeader, w, 1));
            }
        }

        if (filterHeight > 0)
        {
            for (var i = 0; i < _filterVisuals.Count && i < _resolvedColumnWidths.Length; i++)
            {
                var x = rect.X + GetColumnX(i, rect, frozenColumns);
                var w = _resolvedColumnWidths[i];
                _filterVisuals[i].Arrange(new Rectangle(x, yFilter, w, 1));
            }
        }
    }

    private int GetColumnX(int visibleColumnIndex, Rectangle rect, int frozenColumns)
    {
        var start = _resolvedColumnStarts[visibleColumnIndex];
        if (visibleColumnIndex < frozenColumns)
        {
            return start;
        }

        var frozenWidth = SumColumnsWidth(0, frozenColumns);
        return frozenWidth + (start - frozenWidth) - _scroll.OffsetX;
    }

    private void EnsureHeaderVisuals()
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            _headerVisuals.Clear();
            _headerVisualColumns.Clear();
            return;
        }

        _headerVisuals.Clear();
        _headerVisualColumns.Clear();
        if (!ShowHeader)
        {
            return;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

        // Materialize header visuals only when explicitly provided.
        // Otherwise the header text is rendered directly for performance.
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].HeaderVisual is { } hv)
            {
                _headerVisuals.Add(hv);
                _headerVisualColumns.Add(i);
            }
        }
    }

    private void EnsureFilterVisuals()
    {
        if (!FilterRowVisible || !CanFilter)
        {
            _filterVisuals.Clear();
            _filterBoxes.Clear();
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            _filterVisuals.Clear();
            _filterBoxes.Clear();
            return;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

        if (_filterBoxes.Count == columns.Count && _filterVisuals.Count == columns.Count)
        {
            return;
        }

        _filterVisuals.Clear();
        _filterBoxes.Clear();

        _updatingFilterUi = true;
        try
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var box = new TextBox().TextAlignment(TextAlignment.Left);
                _filterBoxes.Add(box);
                _filterVisuals.Add(box);
            }
        }
        finally
        {
            _updatingFilterUi = false;
        }
    }

    private void ApplyFilterToViewIfNeeded()
    {
        if (_updatingFilterUi || !FilterRowVisible || View is not IFilterableDataGridView filterable)
        {
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

        var hash = new HashCode();
        var filters = new List<DataGridFilterDescription>(columns.Count);
        for (var i = 0; i < columns.Count && i < _filterBoxes.Count; i++)
        {
            var text = _filterBoxes[i].Text;
            hash.Add(text, StringComparer.Ordinal);
            filters.Add(new DataGridFilterDescription(columns[i].Key, string.IsNullOrEmpty(text) ? null : text));
        }

        var next = hash.ToHashCode();
        if (next == _lastFilterHash)
        {
            return;
        }

        _lastFilterHash = next;
        filterable.SetFilters(filters);
    }

    private void EnsureCellVisuals(IDataGridViewSnapshot snapshot, Rectangle rect, int headerHeight, int filterHeight, int frozenRows, int frozenColumns)
    {
        _cellVisuals.Clear();

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var cols = EnsureResolvedColumns(snapshot, visibleColumns);

        var yTop = rect.Y + headerHeight + filterHeight;
        var height = rect.Height - headerHeight - filterHeight;
        if (height <= 0)
        {
            _cellRecyclePool.Clear();
            return;
        }

        var viewportScrollRows = Math.Max(0, height - frozenRows);
        var maxRow = Math.Min(snapshot.RowCount, frozenRows + _scroll.OffsetY + viewportScrollRows);

        var ctxBase = new DataTemplateContext(this, DataTemplateRole.Display, -1, DataTemplateItemState.None);

        for (var row = 0; row < maxRow; row++)
        {
            var y = row < frozenRows
                ? yTop + row
                : yTop + frozenRows + (row - frozenRows) - _scroll.OffsetY;

            if ((uint)(y - rect.Y) >= (uint)rect.Height)
            {
                continue;
            }

            var rowModel = snapshot.GetRowModel(row);

            for (var c = 0; c < cols.Count; c++)
            {
                var column = cols[c].Column;
                if (column is null)
                {
                    continue;
                }

                var effectiveReadOnly = ReadOnly || column.ReadOnly || cols[c].SchemaReadOnly;
                if (!column.HasDisplayTemplate(this, effectiveReadOnly))
                {
                    continue;
                }

                var x = rect.X + GetColumnX(c, rect, frozenColumns);
                var w = _resolvedColumnWidths[c];
                if (w <= 0)
                {
                    continue;
                }

                var cellRect = new Rectangle(x, y, w, 1);
                if (!Intersects(rect, cellRect))
                {
                    continue;
                }

                Visual? reused = null;
                if (_cellRecyclePool.Count != 0)
                {
                    var last = _cellRecyclePool.Count - 1;
                    reused = _cellRecyclePool[last];
                    _cellRecyclePool.RemoveAt(last);
                }

                var state = DataTemplateItemState.None;
                if (!IsEnabled)
                {
                    state |= DataTemplateItemState.Disabled;
                }

                if (HasFocusWithin)
                {
                    state |= DataTemplateItemState.Focused;
                }

                if (IsSelectedCell(row, c))
                {
                    state |= DataTemplateItemState.Selected;
                }

                var ctx = ctxBase with { Index = row, State = state };
                var v = column.CreateOrUpdateDisplayVisual(this, rowModel, ctx, effectiveReadOnly, reused, out _);
                _cellVisuals.Add(v);
                v.Arrange(cellRect);
            }
        }

        _cellRecyclePool.Clear();
    }

    private bool TryStartEdit(IDataGridViewSnapshot snapshot)
    {
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

        var cell = CurrentCell;
        if (cell == DataGridCell.None)
        {
            if (snapshot.RowCount <= 0 || columns.Count <= 0)
            {
                return false;
            }

            cell = new DataGridCell(0, 0);
            CurrentCell = cell;
        }

        var row = cell.Row;
        var col = cell.Column;

        if ((uint)row >= (uint)snapshot.RowCount || (uint)col >= (uint)columns.Count)
        {
            return false;
        }

        var column = columns[col].Column;
        if (column is null)
        {
            return false;
        }

        var effectiveReadOnly = ReadOnly || column.ReadOnly || columns[col].SchemaReadOnly;
        if (effectiveReadOnly)
        {
            return false;
        }

        var rowModel = snapshot.GetRowModel(row);
        var ctx = new DataTemplateContext(this, DataTemplateRole.Editor, row, DataTemplateItemState.Focused);
        if (!column.TryCreateEditorVisual(this, rowModel, ctx, out var editor) || editor is null)
        {
            return false;
        }

        OpenEditor(editor, cell);
        return true;
    }

    private void OpenEditor(Visual editor, DataGridCell cell)
    {
        CloseEditor();
        _activeEditor = editor;
        _activeEditorCell = cell;
        AttachChild(editor);
        App?.Focus(editor);
    }

    private void CloseEditor()
    {
        if (_activeEditor is null)
        {
            return;
        }

        DetachChild(_activeEditor);
        _activeEditor = null;
        _activeEditorCell = DataGridCell.None;
        App?.Focus(this);
    }

    private void MoveCurrentCell(int deltaRow, int deltaCol)
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        var rows = snapshot.RowCount;
        var cols = Math.Max(0, GetVisibleColumnCount(snapshot));
        if (rows <= 0 || cols <= 0)
        {
            CurrentCell = DataGridCell.None;
            return;
        }

        var current = CurrentCell == DataGridCell.None ? new DataGridCell(0, 0) : CurrentCell;
        var nextRow = Math.Clamp(current.Row + deltaRow, 0, rows - 1);
        var nextCol = Math.Clamp(current.Column + deltaCol, 0, cols - 1);
        CurrentCell = new DataGridCell(nextRow, nextCol);
    }

    private bool IsSelectedCell(int row, int visibleColumnIndex)
    {
        var current = CurrentCell;
        if (current == DataGridCell.None)
        {
            return false;
        }

        return SelectionMode switch
        {
            DataGridSelectionMode.Cell => current.Row == row && current.Column == visibleColumnIndex,
            DataGridSelectionMode.Row => current.Row == row,
            DataGridSelectionMode.Column => current.Column == visibleColumnIndex,
            _ => false,
        };
    }

    private void EnsureCurrentCellVisible(IDataGridViewSnapshot snapshot, int frozenRows, int frozenColumns)
    {
        _ = snapshot;

        var cell = CurrentCell;
        if (cell == DataGridCell.None)
        {
            return;
        }

        if (cell.Row >= frozenRows)
        {
            var y = cell.Row - frozenRows;
            _scroll.ScrollToMakeVisible(_scroll.OffsetX, y);
        }

        if (cell.Column >= frozenColumns && (uint)cell.Column < (uint)_resolvedColumnStarts.Length)
        {
            var frozenWidth = SumColumnsWidth(0, frozenColumns);
            var x = _resolvedColumnStarts[cell.Column] - frozenWidth;
            _scroll.ScrollToMakeVisible(x, _scroll.OffsetY);
        }
    }

    private DataGridCell? TryHitTestCell(IDataGridViewSnapshot snapshot, int x, int y)
    {
        var rect = Bounds;
        if (!rect.Contains(x, y))
        {
            return null;
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;
        var rowY = y - rect.Y - headerHeight - filterHeight;
        if (rowY < 0)
        {
            return null;
        }

        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);

        var viewRow = rowY < frozenRows ? rowY : frozenRows + _scroll.OffsetY + (rowY - frozenRows);
        if ((uint)viewRow >= (uint)rowCount)
        {
            return null;
        }

        var relX = x - rect.X;
        var frozenWidth = SumColumnsWidth(0, frozenColumns);
        var colX = relX;
        if (relX >= frozenWidth)
        {
            colX = frozenWidth + _scroll.OffsetX + (relX - frozenWidth);
        }

        for (var c = 0; c < _resolvedColumnStarts.Length; c++)
        {
            var start = _resolvedColumnStarts[c];
            var end = start + _resolvedColumnWidths[c];
            if (colX >= start && colX < end)
            {
                return new DataGridCell(viewRow, c);
            }
        }

        return null;
    }

    private Rectangle? TryGetCellRect(DataGridCell cell, Rectangle gridRect, int headerHeight, int filterHeight, int frozenRows, int frozenColumns)
    {
        if (cell == DataGridCell.None)
        {
            return null;
        }

        var row = cell.Row;
        var col = cell.Column;
        if ((uint)col >= (uint)_resolvedColumnWidths.Length)
        {
            return null;
        }

        var yTop = gridRect.Y + headerHeight + filterHeight;
        var y = row < frozenRows
            ? yTop + row
            : yTop + frozenRows + (row - frozenRows) - _scroll.OffsetY;

        if (y < gridRect.Y || y >= gridRect.Bottom)
        {
            return null;
        }

        var x = gridRect.X + GetColumnX(col, gridRect, frozenColumns);
        return new Rectangle(x, y, _resolvedColumnWidths[col], 1);
    }

    private void RenderRow(
        CellBuffer buffer,
        IDataGridViewSnapshot snapshot,
        int viewRow,
        int y,
        Rectangle rect,
        int visibleColumns,
        int frozenColumns,
        Style cellStyle,
        Style selectionStyle,
        Style matchStyle,
        string? searchText)
    {
        var cols = EnsureResolvedColumns(snapshot, visibleColumns);
        var rowModel = snapshot.GetRowModel(viewRow);
        var culture = GetCulture();

        for (var c = 0; c < cols.Count && c < _resolvedColumnWidths.Length; c++)
        {
            var w = _resolvedColumnWidths[c];
            if (w <= 0)
            {
                continue;
            }

            var x = rect.X + GetColumnX(c, rect, frozenColumns);
            if (x >= rect.Right || x + w <= rect.X)
            {
                continue;
            }

            var schema = cols[c];
            var column = schema.Column;
            var effectiveReadOnly = column is not null && (ReadOnly || column.ReadOnly || schema.SchemaReadOnly);

            var isSelected = IsSelectedCell(viewRow, c);
            var style = isSelected ? selectionStyle : cellStyle;

            var text = column is not null
                ? column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(schema.SchemaAccessor.GetValue(rowModel), culture);

            if (searchText is not null && searchText.Length != 0 && text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!isSelected)
                {
                    style = matchStyle;
                }
            }

            if (column is not null && column.HasDisplayTemplate(this, effectiveReadOnly))
            {
                FillRect(buffer, new Rectangle(x, y, w, 1), style);
                continue;
            }

            FillRect(buffer, new Rectangle(x, y, w, 1), style);
            WriteAlignedText(buffer, new Rectangle(x, y, w, 1), text.AsSpan(), style, column?.CellAlignment ?? TextAlignment.Left);
        }
    }

    private void RebuildMatchesIfNeeded()
    {
        var snapshot = GetSnapshot();
        var query = SearchQuery;
        var text = query.Text;
        if (snapshot is null || string.IsNullOrEmpty(text))
        {
            _matches.Clear();
            _activeMatchIndex = -1;
            _lastMatchesKey = 0;
            _searchError = null;
            return;
        }

        var key = new HashCode();
        key.Add(snapshot.Version);
        key.Add(text, StringComparer.Ordinal);
        key.Add(query.CaseSensitive);
        key.Add(query.WholeWord);
        key.Add(query.UseRegex);
        var nextKey = key.ToHashCode();
        if (nextKey == _lastMatchesKey)
        {
            return;
        }

        _lastMatchesKey = nextKey;
        _matches.Clear();
        _activeMatchIndex = -1;
        _searchError = null;

        var cols = GetVisibleColumnCount(snapshot);
        var resolvedCols = EnsureResolvedColumns(snapshot, cols);
        var culture = GetCulture();

        var comparison = query.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (var r = 0; r < snapshot.RowCount; r++)
        {
            var rowModel = snapshot.GetRowModel(r);
            for (var c = 0; c < resolvedCols.Count; c++)
            {
                var col = resolvedCols[c];
                var column = col.Column;
                var value = column is not null
                    ? column.FormatValue(this, rowModel, culture)
                    : ValueStringFormatter.ToString(col.SchemaAccessor.GetValue(rowModel), culture);

                if (value.IndexOf(text!, comparison) >= 0)
                {
                    _matches.Add(new DataGridCell(r, c));
                }
            }
        }
    }

    private List<ResolvedColumn> EnsureResolvedColumns(IDataGridViewSnapshot snapshot, int visibleColumns)
    {
        _ = visibleColumns;

        var columnsVersion = Columns.Version;
        if (snapshot.Version == _lastSnapshotVersion && snapshot.ColumnCount == _lastSnapshotColumnCount && columnsVersion == _lastColumnsVersion)
        {
            return _cachedResolvedColumns;
        }

        _lastSnapshot = snapshot;
        _lastSnapshotVersion = snapshot.Version;
        _lastSnapshotColumnCount = snapshot.ColumnCount;
        _lastColumnsVersion = columnsVersion;

        _cachedResolvedColumns.Clear();

        if (Columns.Count == 0)
        {
            for (var i = 0; i < snapshot.ColumnCount; i++)
            {
                var info = snapshot.GetColumn(i);
                _cachedResolvedColumns.Add(CreateResolvedFromSchema(info, uiColumn: null));
            }
        }
        else
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                var ui = Columns[i];
                if (!ui.Visible)
                {
                    continue;
                }

                var schemaIndex = FindSchemaColumnIndex(snapshot, ui.Key);
                if (schemaIndex < 0)
                {
                    _cachedResolvedColumns.Add(CreateResolvedMissingSchema(ui));
                    continue;
                }

                var info = snapshot.GetColumn(schemaIndex);
                _cachedResolvedColumns.Add(CreateResolvedFromSchema(info, ui));
            }
        }

        if (_visibleColumnToSnapshotColumn.Length != _cachedResolvedColumns.Count)
        {
            _visibleColumnToSnapshotColumn = new int[_cachedResolvedColumns.Count];
        }

        for (var i = 0; i < _cachedResolvedColumns.Count; i++)
        {
            _visibleColumnToSnapshotColumn[i] = FindSchemaColumnIndex(snapshot, _cachedResolvedColumns[i].Key);
        }

        return _cachedResolvedColumns;
    }

    private ResolvedColumn CreateResolvedMissingSchema(DataGridColumn ui)
    {
        var header = ui.Header;
        var headerWidth = header is null ? TerminalTextUtility.GetWidth(ui.Key.AsSpan()) : MeasureHeaderVisualWidth(header);
        var baseWidth = ResolveBaseWidth(ui, headerWidth);

        return new ResolvedColumn(
            Key: ui.Key,
            SchemaAccessor: ui.Accessor,
            SchemaReadOnly: true,
            HeaderText: ui.Key,
            HeaderVisual: header,
            Column: ui,
            BaseWidth: baseWidth,
            MinWidth: Math.Max(0, ui.MinWidth),
            MaxWidth: ui.MaxWidth <= 0 ? int.MaxValue : ui.MaxWidth,
            IsStar: ui.Width.Type == GridUnitType.Star,
            StarWeight: ui.Width.Type == GridUnitType.Star ? ui.Width.Value : 0);
    }

    private ResolvedColumn CreateResolvedFromSchema(DataGridColumnInfo info, DataGridColumn? uiColumn)
    {
        var ui = uiColumn;
        var headerVisual = ui?.Header;
        var headerText = info.HeaderText;
        var headerWidth = headerVisual is not null ? MeasureHeaderVisualWidth(headerVisual) : TerminalTextUtility.GetWidth(headerText.AsSpan());

        var minWidth = ui?.MinWidth ?? 0;
        var maxWidth = ui?.MaxWidth ?? int.MaxValue;
        maxWidth = maxWidth <= 0 ? int.MaxValue : maxWidth;

        var baseWidth = ui is null
            ? Math.Max(1, headerWidth)
            : ResolveBaseWidth(ui, headerWidth);

        var isStar = ui?.Width.Type == GridUnitType.Star;
        var starWeight = isStar == true ? ui!.Width.Value : 0;

        return new ResolvedColumn(
            Key: ui?.Key ?? info.Key,
            SchemaAccessor: info.Accessor,
            SchemaReadOnly: info.ReadOnly,
            HeaderText: headerText,
            HeaderVisual: headerVisual,
            Column: ui,
            BaseWidth: Math.Clamp(baseWidth, minWidth, maxWidth),
            MinWidth: Math.Max(0, minWidth),
            MaxWidth: maxWidth,
            IsStar: isStar == true,
            StarWeight: starWeight);
    }

    private static int ResolveBaseWidth(DataGridColumn ui, int headerWidth)
    {
        var w = ui.Width;
        var baseWidth = w.Type switch
        {
            GridUnitType.Fixed => (int)Math.Round(w.Value),
            GridUnitType.Auto => headerWidth,
            GridUnitType.Star => Math.Max(ui.MinWidth, headerWidth),
            _ => headerWidth,
        };
        baseWidth = Math.Max(baseWidth, ui.MinWidth);
        baseWidth = Math.Min(baseWidth, ui.MaxWidth <= 0 ? int.MaxValue : ui.MaxWidth);
        return Math.Max(1, baseWidth);
    }

    private int MeasureHeaderVisualWidth(Visual header)
    {
        header.Measure(LayoutConstraints.Unbounded);
        return Math.Max(1, header.DesiredSize.Width);
    }

    private static int FindSchemaColumnIndex(IDataGridViewSnapshot snapshot, string key)
    {
        for (var i = 0; i < snapshot.ColumnCount; i++)
        {
            var col = snapshot.GetColumn(i);
            if (string.Equals(col.Key, key, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }

    private static bool Intersects(in Rectangle a, in Rectangle b)
        => a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;

    private static void FillRect(CellBuffer buffer, Rectangle rect, Style style)
    {
        if (!buffer.ClipIntersects(rect))
        {
            return;
        }

        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), style);
            }
        }
    }

    private static void WriteAlignedText(CellBuffer buffer, Rectangle rect, ReadOnlySpan<char> text, Style style, TextAlignment alignment)
    {
        var width = rect.Width;
        if (width <= 0)
        {
            return;
        }

        var clipped = Clip(text, width);
        var cells = TerminalTextUtility.GetWidth(clipped);
        var x = AlignX(rect, alignment, width, cells);
        buffer.WriteText(x, rect.Y, clipped, style);
    }

    private static int AlignX(Rectangle rect, TextAlignment alignment, int availableWidth, int contentWidth)
    {
        if (availableWidth <= contentWidth)
        {
            return rect.X;
        }

        return alignment switch
        {
            TextAlignment.Center => rect.X + ((availableWidth - contentWidth) / 2),
            TextAlignment.Right => rect.X + (availableWidth - contentWidth),
            _ => rect.X,
        };
    }

    private static ReadOnlySpan<char> Clip(ReadOnlySpan<char> text, int maxCells)
    {
        if (maxCells <= 0 || text.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        if (!TerminalTextUtility.TryGetIndexAtCell(text, maxCells, out var endIndex))
        {
            endIndex = text.Length;
        }

        return text[..Math.Clamp(endIndex, 0, text.Length)];
    }

    private sealed record ResolvedColumn(
        string Key,
        BindingAccessor SchemaAccessor,
        bool SchemaReadOnly,
        string HeaderText,
        Visual? HeaderVisual,
        DataGridColumn? Column,
        int BaseWidth,
        int MinWidth,
        int MaxWidth,
        bool IsStar,
        double StarWeight);

    private sealed class IdentitySnapshot : IDataGridViewSnapshot
    {
        private readonly IDataGridSnapshot _snapshot;

        public IdentitySnapshot(IDataGridSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int Version => _snapshot.Version;
        public int RowCount => _snapshot.RowCount;
        public int ColumnCount => _snapshot.ColumnCount;
        public DataGridColumnInfo GetColumn(int columnIndex) => _snapshot.GetColumn(columnIndex);
        public int MapRowToDocument(int viewRowIndex) => viewRowIndex;
        public object GetRowModel(int viewRowIndex) => _snapshot.GetRowModel(viewRowIndex);
    }

    private sealed class DataGridSearchTarget : ISearchReplaceTarget
    {
        private readonly DataGridControl _owner;

        public DataGridSearchTarget(DataGridControl owner) => _owner = owner;

        public string Title => "DataGrid";
        public bool SupportsReplace => false;

        public void SetQuery(in SearchQuery query)
        {
            _owner.SearchQuery = query;
        }

        public void NextMatch() => _owner.NextMatch();
        public void PreviousMatch() => _owner.PreviousMatch();

        public int ReplaceCurrent(string replacement)
        {
            _ = replacement;
            return 0;
        }

        public int ReplaceAll(string replacement)
        {
            _ = replacement;
            return 0;
        }

        public string GetStatusText() => _owner.GetSearchStatusText();

        public string? GetErrorText() => _owner.GetSearchErrorText();
    }
}
