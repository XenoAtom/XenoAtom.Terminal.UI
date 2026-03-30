// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Scrolling;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;
using XenoAtom.Terminal.UI.Text;

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
/// Specifies how a cell reacts to keyboard and pointer activation.
/// </summary>
public enum DataGridCellActivationMode
{
    /// <summary>
    /// Choose the behavior automatically based on the active editor kind.
    /// </summary>
    Auto,

    /// <summary>
    /// Require the cell to enter edit mode before the editor can be used.
    /// </summary>
    ExplicitEdit,

    /// <summary>
    /// Activate the editor directly from the triggering gesture.
    /// </summary>
    DirectActivate,
}

/// <summary>
/// A high-performance, scrollable, virtualized, data-bound grid control.
/// </summary>
public sealed partial class DataGridControl : Visual, IScrollable, ISelectionOwner
{
    private const int AutoSizeSampleRowCount = 64;
    private const int SortButtonWidth = 2;

    private readonly ScrollModel _scroll;
    private readonly BindableList<DataGridColumn> _columns;
    private readonly Dictionary<string, int> _columnWidthOverrides = new(StringComparer.Ordinal);

    private readonly VisualList<Visual> _headerVisuals;
    private readonly List<int> _headerVisualColumns;
    private readonly BindableList<Visual> _cellVisuals;
    private readonly List<Visual> _cellRecyclePool;

    private readonly VisualList<Visual> _filterVisuals;
    private readonly List<TextBox> _filterBoxes;

    private readonly SearchReplacePopup _searchPopup;

    private sealed class FilterTextBox : TextBox
    {
        // Filter boxes should keep showing the placeholder when focused, otherwise the filter row can look "blank"
        // (the caret is rendered via the terminal cursor and may not be visible in screenshots).
        protected override bool ShowPlaceholderWhenUnfocusedOnly => false;
    }
    private readonly DataGridSearchTarget _searchTarget;

    private IDataGridDocument? _appliedDocument;
    private IDataGridView? _appliedView;

    private IDataGridViewSnapshot? _lastSnapshot;
    private int _lastSnapshotVersion = -1;
    private int _lastSnapshotColumnCount = -1;
    private int _lastColumnsKey;

    private int[] _resolvedColumnWidths = Array.Empty<int>();
    private int[] _resolvedColumnStarts = Array.Empty<int>();
    private int[] _visibleColumnToSnapshotColumn = Array.Empty<int>();

    private bool _ensureCurrentCellVisible;

    private Visual? _activeEditor;
    private DataGridCell _activeEditorCell = DataGridCell.None;
    private object? _activeEditorRowModel;
    private BindingAccessor? _activeEditorAccessor;
    private object? _activeEditorOriginalValue;
    private bool _activeEditorPooled;
    private bool _pendingDirectPointerActivation;
    private bool _routingSyntheticEditorInput;

    private readonly List<TextBox> _textBoxPool = new(8);

    private bool _resizingColumn;
    private int _resizingColumnIndex;
    private int _resizeStartUiX;
    private int _resizeStartWidth;

    private int _columnWidthVersionCounter;

    [Bindable]
    private partial int HoveredResizeColumnIndex { get; set; }

    [Bindable]
    private partial int HoveredSortColumnIndex { get; set; }

    [Bindable]
    private partial int PressedSortColumnIndex { get; set; }

    [Bindable]
    private partial bool IsSortPressedInside { get; set; }

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
        IsSelectable = true;

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
        _searchPopup = new SearchReplacePopup(_searchTarget)
        {
            ClearQueryOnClose = true,
        };
        AttachChild(_searchPopup);

        AddCommand(new Command
        {
            Id = "DataGrid.CommitEdit",
            LabelMarkup = string.Empty,
            Gesture = new KeyGesture(TerminalKey.Enter),
            Presentation = CommandPresentation.None,
            Execute = static v => ((DataGridControl)v).CommitEdit(),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is not null,
            ConsumesGestureWhenUnavailable = false,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.CancelEdit",
            LabelMarkup = string.Empty,
            Gesture = new KeyGesture(TerminalKey.Escape),
            Presentation = CommandPresentation.None,
            Execute = static v => ((DataGridControl)v).CancelEdit(),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is not null,
            ConsumesGestureWhenUnavailable = false,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.NextCell",
            LabelMarkup = string.Empty,
            Gesture = new KeyGesture(TerminalKey.Tab),
            Presentation = CommandPresentation.None,
            Execute = static v => ((DataGridControl)v).MoveEditorToAdjacentCell(deltaColumn: 1),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is not null,
            ConsumesGestureWhenUnavailable = false,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.PreviousCell",
            LabelMarkup = string.Empty,
            Gesture = new KeyGesture(TerminalKey.Tab, TerminalModifiers.Shift),
            Presentation = CommandPresentation.None,
            Execute = static v => ((DataGridControl)v).MoveEditorToAdjacentCell(deltaColumn: -1),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is not null,
            ConsumesGestureWhenUnavailable = false,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.Find",
            LabelMarkup = "Find",
            DescriptionMarkup = "Find text in the table.",
            Gesture = new KeyGesture(TerminalChar.CtrlF, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).OpenSearch(),
        });

        AddCommand(new Command
        {
            Id = "DataGrid.ToggleFilterRow",
            LabelMarkup = "Filter row",
            DescriptionMarkup = "Show or hide the filter row.",
            Gesture = new KeyGesture(TerminalKey.F4),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).ToggleFilterRow(),
            CanExecute = static v => ((DataGridControl)v).CanFilter,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.NextMatch",
            LabelMarkup = "Next match",
            DescriptionMarkup = "Jump to the next search match.",
            Gesture = new KeyGesture(TerminalKey.F3),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).NextMatch(),
        });

        AddCommand(new Command
        {
            Id = "DataGrid.PreviousMatch",
            LabelMarkup = "Prev match",
            DescriptionMarkup = "Jump to the previous search match.",
            Gesture = new KeyGesture(TerminalKey.F3, TerminalModifiers.Shift),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).PreviousMatch(),
        });

        AddCommand(new Command
        {
            Id = "DataGrid.SelectAll",
            LabelMarkup = "Select all",
            DescriptionMarkup = "Select the entire table.",
            Gesture = new KeyGesture(TerminalChar.CtrlA, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).SelectAll(),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is null,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.Copy",
            LabelMarkup = "Copy",
            DescriptionMarkup = "Copy the current selection to the clipboard.",
            Gesture = new KeyGesture(TerminalChar.CtrlC, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).CopySelection(),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is null,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.GoToStart",
            LabelMarkup = "Top",
            DescriptionMarkup = "Go to the first cell (Ctrl+Home).",
            Gesture = new KeyGesture(TerminalKey.Home, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).GoToTableEdge(first: true),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is null,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.GoToEnd",
            LabelMarkup = "Bottom",
            DescriptionMarkup = "Go to the last cell (Ctrl+End).",
            Gesture = new KeyGesture(TerminalKey.End, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).GoToTableEdge(first: false),
            CanExecute = static v => ((DataGridControl)v)._activeEditor is null,
        });

        AddCommand(new Command
        {
            Id = "DataGrid.EditCell",
            LabelMarkup = "Edit",
            DescriptionMarkup = "Edit the current cell.",
            Gesture = new KeyGesture(TerminalKey.F2),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = static v => ((DataGridControl)v).StartEdit(),
            CanExecute = static v => !((DataGridControl)v).ReadOnly && ((DataGridControl)v)._activeEditor is null,
        });

        this.ShowHeader(true);
        this.ShowRowAnchor(true);
        this.RowAnchorWidth(1);
        this.SelectionMode(DataGridSelectionMode.Cell);
        this.EditMode(DataGridEditMode.OnEnter);
        this.CellActivationMode(DataGridCellActivationMode.Auto);
        this.FrozenRows(0);
        this.FrozenColumns(0);
        this.CurrentCell(DataGridCell.None);
        this.SelectedRow(-1);
        HoveredResizeColumnIndex = -1;
        HoveredSortColumnIndex = -1;
        PressedSortColumnIndex = -1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridControl"/> class with a document.
    /// </summary>
    /// <param name="document">The data grid document.</param>
    public DataGridControl(IDataGridDocument document) : this()
    {
        this.Document(document);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridControl"/> class with a dynamic document.
    /// </summary>
    /// <param name="document">A delegate that supplies the data grid document.</param>
    public DataGridControl(Func<IDataGridDocument?> document) : this()
    {
        this.Document(document);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridControl"/> class with a bound document.
    /// </summary>
    /// <param name="document">A binding that supplies the data grid document.</param>
    public DataGridControl(Binding<IDataGridDocument?> document) : this()
    {
        this.Document(document);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridControl"/> class with a document and view.
    /// </summary>
    /// <param name="document">The data grid document.</param>
    /// <param name="view">The optional projected view.</param>
    public DataGridControl(IDataGridDocument document, IDataGridView? view) : this(document)
    {
        this.View(view);
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
    /// Gets or sets a value indicating whether a small row anchor column is shown at the start of each row.
    /// </summary>
    /// <remarks>
    /// The row anchor is a compact affordance used for row selection and for visually tracking the current row.
    /// </remarks>
    [Bindable]
    public partial bool ShowRowAnchor { get; set; }

    /// <summary>
    /// Gets or sets the width of the row anchor column, in cells.
    /// </summary>
    [Bindable]
    public partial int RowAnchorWidth { get; set; }

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
    /// Gets or sets a value indicating whether the grid participates in selection ownership.
    /// </summary>
    [Bindable]
    public partial bool IsSelectable { get; set; }

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
    /// Gets or sets the row selected via the row anchor, or -1 when no row is selected.
    /// </summary>
    [Bindable]
    public partial int SelectedRow { get; set; }

    /// <inheritdoc />
    public bool HasSelection => IsTableSelected || SelectedRow >= 0;

    void ISelectionOwner.ClearSelection()
    {
        VerifyAccess();

        if (!HasSelection)
        {
            return;
        }

        IsTableSelected = false;
        SelectedRow = -1;
        App?.RequestRender();
    }

    /// <inheritdoc />
    public bool TryCopySelection(out string text)
    {
        VerifyAccess();

        if (!HasSelection)
        {
            text = string.Empty;
            return false;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            text = string.Empty;
            return false;
        }

        return TryGetSelectionText(snapshot, out text);
    }

    /// <summary>
    /// Gets or sets the edit mode.
    /// </summary>
    [Bindable]
    public partial DataGridEditMode EditMode { get; set; }

    /// <summary>
    /// Gets or sets the default activation behavior used by editable cells.
    /// </summary>
    /// <remarks>
    /// <see cref="DataGridCellActivationMode.Auto"/> keeps text-style editors in explicit edit mode while allowing
    /// toggle/action-style editors to react directly. Columns can override this behavior individually.
    /// </remarks>
    [Bindable]
    public partial DataGridCellActivationMode CellActivationMode { get; set; }

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

    /// <summary>
    /// Gets the current sort descriptions when the view supports sorting.
    /// </summary>
    public IReadOnlyList<DataGridSortDescription> SortDescriptions
        => View is ISortableDataGridView sortable ? sortable.SortDescriptions : Array.Empty<DataGridSortDescription>();

    /// <summary>
    /// Gets the sort direction for the specified column, or <see langword="null"/> when that column is not sorted.
    /// </summary>
    /// <param name="columnKey">The column key.</param>
    /// <returns>The current sort direction, or <see langword="null"/>.</returns>
    public DataGridSortDirection? GetColumnSortDirection(string columnKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnKey);

        var sorts = SortDescriptions;
        for (var i = 0; i < sorts.Count; i++)
        {
            if (string.Equals(sorts[i].ColumnKey, columnKey, StringComparison.Ordinal))
            {
                return sorts[i].Direction;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets the sort direction for the specified column.
    /// </summary>
    /// <param name="columnKey">The column key.</param>
    /// <param name="direction">The new direction, or <see langword="null"/> to clear the column sort.</param>
    /// <param name="additive">
    /// <see langword="true"/> to preserve other active sort descriptions; otherwise the column becomes the only active sort.
    /// </param>
    /// <returns><see langword="true"/> if the sort was applied; otherwise <see langword="false"/>.</returns>
    public bool TrySetColumnSortDirection(string columnKey, DataGridSortDirection? direction, bool additive = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnKey);

        if (View is not ISortableDataGridView sortable || !TryGetSortableColumn(columnKey, out _))
        {
            return false;
        }

        ConfigureSortComparers();

        var current = sortable.SortDescriptions;
        var next = new List<DataGridSortDescription>(additive ? current.Count + 1 : 1);
        if (additive)
        {
            for (var i = 0; i < current.Count; i++)
            {
                if (!string.Equals(current[i].ColumnKey, columnKey, StringComparison.Ordinal))
                {
                    next.Add(current[i]);
                }
            }
        }

        if (direction is { } appliedDirection)
        {
            next.Add(new DataGridSortDescription(columnKey, appliedDirection));
        }

        sortable.SetSortDescriptions(next);
        return true;
    }

    /// <summary>
    /// Cycles the sort direction for the specified column.
    /// </summary>
    /// <param name="columnKey">The column key.</param>
    /// <param name="additive">
    /// <see langword="true"/> to preserve other active sort descriptions; otherwise the column becomes the only active sort.
    /// </param>
    /// <returns><see langword="true"/> if the sort was applied; otherwise <see langword="false"/>.</returns>
    public bool TryToggleColumnSortDirection(string columnKey, bool additive = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnKey);

        DataGridSortDirection? nextDirection = GetColumnSortDirection(columnKey) switch
        {
            null => DataGridSortDirection.Descending,
            DataGridSortDirection.Descending => DataGridSortDirection.Ascending,
            _ => null,
        };

        return TrySetColumnSortDirection(columnKey, nextDirection, additive);
    }

    [Bindable]
    private partial int ScrollVersion { get; set; }

    [Bindable]
    private partial int SourceVersion { get; set; }

    [Bindable]
    private partial int MeasuredContentWidth { get; set; }

    [Bindable]
    private partial bool IsTableSelected { get; set; }

    [Bindable]
    private partial int ActiveEditorScrollVersion { get; set; }

    [Bindable]
    private partial int ActiveMatchVersion { get; set; }

    [Bindable]
    private partial int ColumnWidthVersion { get; set; }

    partial void OnFrozenRowsChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);
    partial void OnFrozenColumnsChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);
    partial void OnRowAnchorWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

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
        if (EditMode == DataGridEditMode.OnCellChange && !ReadOnly && _activeEditor is null)
        {
            var snapshot = GetSnapshot();
            if (snapshot is not null)
            {
                _ = TryStartEdit(snapshot);
            }
        }
    }

    partial void OnSelectedRowChanging(ref int value)
    {
        if (value < 0)
        {
            value = -1;
            return;
        }

        var snapshot = GetSnapshot();
        if (snapshot is null || snapshot.RowCount <= 0)
        {
            value = -1;
            return;
        }

        value = Math.Clamp(value, 0, snapshot.RowCount - 1);
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

        HoveredSortColumnIndex = -1;
        PressedSortColumnIndex = -1;
        IsSortPressedInside = false;
        ConfigureSortComparers();
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
        ActiveEditorScrollVersion = _activeEditor is IScrollable scrollable ? scrollable.Scroll.Version : 0;

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
        _ = ColumnWidthVersion;

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return SizeHints.Fixed(Size.Zero);
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;
        var anchorWidth = GetEffectiveRowAnchorWidth();

        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);

        var columnsWidth = ComputeNaturalColumnsWidth(snapshot, visibleColumns, constraints);
        var requestedWidth = columnsWidth + anchorWidth;

        var height = headerHeight + filterHeight + Math.Max(0, rowCount);
        height = Math.Max(1, height);

        MeasuredContentWidth = Math.Max(0, requestedWidth);

        var scrollBarThickness = Math.Max(1, GetStyle<ScrollViewerStyle>().ScrollBarThickness);
        var reserveVerticalBar = constraints.IsHeightBounded && height > constraints.MaxHeight;
        var reservedWidth = requestedWidth + (reserveVerticalBar ? scrollBarThickness : 0);

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
        _ = ActiveEditorScrollVersion;
        _ = MeasuredContentWidth;
        _ = ColumnWidthVersion;
        _ = ShowRowAnchor;
        _ = RowAnchorWidth;
        _ = CurrentCell;
        _ = SelectedRow;
        _ = IsTableSelected;

        var snapshot = GetSnapshot();
        if (snapshot is null || rect.Width <= 0 || rect.Height <= 0)
        {
            _scroll.SetViewport(0, 0);
            _scroll.SetExtent(0, 0);
            return;
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;
        var anchorWidth = GetEffectiveRowAnchorWidth();

        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);

        ResolveColumnLayout(snapshot, visibleColumns, Math.Max(0, rect.Width - anchorWidth));

        var frozenWidth = anchorWidth + SumColumnsWidth(0, frozenColumns);
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

        if (_activeEditor is not null)
        {
            var editorRect = TryGetCellRect(_activeEditorCell, rect, headerHeight, filterHeight, frozenRows, frozenColumns);
            if (editorRect is { } r)
            {
                // DataGridControl does not measure its children in MeasureCore for performance reasons.
                // Ensure the active editor is measured so its arrange logic doesn't clamp to a 0-size desired height.
                _activeEditor.Measure(new LayoutConstraints(0, r.Width, 0, r.Height));
                _activeEditor.Arrange(r);
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
        _ = IsTableSelected;
        _ = HoveredResizeColumnIndex;
        _ = HoveredSortColumnIndex;
        _ = PressedSortColumnIndex;
        _ = IsSortPressedInside;

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
        var anchorWidth = GetEffectiveRowAnchorWidth();
        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);
        var hoveredResize = HoveredResizeColumnIndex;
        var hoveredSort = HoveredSortColumnIndex;
        var pressedSort = PressedSortColumnIndex;
        var sortPressedInside = IsSortPressedInside;

        FillRect(buffer, rect, cellStyle);

        if (headerHeight > 0)
        {
            FillRect(buffer, new Rectangle(rect.X, rect.Y, rect.Width, 1), headerStyle);
        }

        if (filterHeight > 0)
        {
            // Filter row should look like inputs, not like the header strip.
            FillRect(buffer, new Rectangle(rect.X, rect.Y + headerHeight, rect.Width, 1), cellStyle);
        }

        if (headerHeight > 0)
        {
            if (anchorWidth > 0)
            {
                var anchorRect = new Rectangle(rect.X, rect.Y, Math.Min(anchorWidth, rect.Width), 1);
                var anchorStyle = IsTableSelected ? selectionStyle : headerStyle;
                FillRect(buffer, anchorRect, anchorStyle);

                if (IsTableSelected && anchorRect.Width > 0)
                {
                    buffer.SetCell(anchorRect.X + anchorRect.Width - 1, rect.Y, new Rune('■'), anchorStyle);
                }
            }

            var cols = EnsureResolvedColumns(snapshot, visibleColumns);
            for (var c = 0; c < cols.Count && c < _resolvedColumnWidths.Length; c++)
            {
                var col = cols[c];
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

                var buttonRect = GetSortButtonRect(col, x, rect.Y, w);
                var contentWidth = buttonRect?.X - x ?? w;
                if (contentWidth > 0 && col.HeaderVisual is null)
                {
                    var align = col.Column?.HeaderAlignment ?? TextAlignment.Left;
                    WriteAlignedText(buffer, new Rectangle(x, rect.Y, contentWidth, 1), col.HeaderText.AsSpan(), headerStyle, align);
                }

                if (buttonRect is { } resolvedButtonRect)
                {
                    var isPressed = pressedSort == c && sortPressedInside;
                    var isHovered = pressedSort == c ? sortPressedInside : hoveredSort == c;
                    var buttonStyle = style.ResolveSortButtonStyle(theme, headerStyle, isHovered, isPressed);
                    FillRect(buffer, resolvedButtonRect, buttonStyle);

                    var glyph = style.ResolveSortButtonGlyph(GetColumnSortDirection(col.Key));
                    buffer.SetCell(
                        resolvedButtonRect.X + resolvedButtonRect.Width - 1,
                        resolvedButtonRect.Y,
                        glyph,
                        buttonStyle);
                }
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

            RenderRow(buffer, snapshot, viewRow, y, rect, visibleColumns, frozenColumns, anchorWidth, cellStyle, selectionStyle, matchStyle, hasSearch ? searchText! : null);
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

            RenderRow(buffer, snapshot, viewRow, y, rect, visibleColumns, frozenColumns, anchorWidth, cellStyle, selectionStyle, matchStyle, hasSearch ? searchText! : null);
        }

        if (!_resizingColumn && hoveredResize >= 0 && TryGetColumnResizeHandleRect(snapshot, rect, visibleColumns, frozenColumns, hoveredResize, out var resizeHandle))
        {
            // Hover highlight for resize handles. This makes the handle discoverable without changing layout.
            var hoverStyle = Style.None;
            if (theme.FocusBorder is { } c)
            {
                hoverStyle = hoverStyle.WithBackground(c.WithAlpha(0x30));
            }

            FillRect(buffer, resizeHandle, hoverStyle);
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
        if (e.Handled)
        {
            return;
        }

        if (_routingSyntheticEditorInput && _activeEditor is not null && IsDescendantOf(e.OriginalSource, _activeEditor))
        {
            return;
        }

        if (_pendingDirectPointerActivation &&
            _activeEditor is not null &&
            e.Kind == TerminalMouseKind.DoubleClick &&
            e.Button == TerminalMouseButton.Left)
        {
            var args = CreateSyntheticPointerEvent(_activeEditor, e, TerminalMouseKind.Up);
            _routingSyntheticEditorInput = true;
            try
            {
                _activeEditor.RaiseEvent(Visual.PointerReleasedEvent, args);
            }
            finally
            {
                _routingSyntheticEditorInput = false;
            }

            _pendingDirectPointerActivation = false;
            if (_activeEditor is not null)
            {
                CloseEditor();
            }

            e.Handled = true;
            return;
        }

        if (e.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.DoubleClick) || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        if (_activeEditor is not null && _activeEditor.Bounds.Contains(e.UiX, e.UiY))
        {
            return;
        }

        if (_activeEditor is not null)
        {
            // Clicking outside the active editor commits the edit and restores grid interactions.
            CloseEditor();
        }

        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        var rect = Bounds;
        var anchorWidth = GetEffectiveRowAnchorWidth();
        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;

        if (e.Kind == TerminalMouseKind.DoubleClick && TryHitColumnResizeHandle(snapshot, e.UiX, e.UiY, rect, out var autoSizeColumnIndex))
        {
            AutoSizeColumn(snapshot, autoSizeColumnIndex);
            e.Handled = true;
            return;
        }

        if (headerHeight > 0 && TryHitSortButton(snapshot, e.UiX, e.UiY, rect, out var sortColumnIndex))
        {
            HoveredResizeColumnIndex = -1;
            HoveredSortColumnIndex = sortColumnIndex;
            PressedSortColumnIndex = sortColumnIndex;
            IsSortPressedInside = true;
            e.Handled = true;
            return;
        }

        if (TryBeginColumnResize(snapshot, e, rect))
        {
            e.Handled = true;
            return;
        }

        if (anchorWidth > 0 && e.UiX - rect.X < anchorWidth)
        {
            if (headerHeight > 0 && e.UiY - rect.Y < headerHeight)
            {
                SelectEntireTable(snapshot);
                App?.Focus(this);
                e.Handled = true;
                return;
            }

            var rowY = e.UiY - rect.Y - headerHeight - filterHeight;
            if (rowY >= 0)
            {
                var rowCount = snapshot.RowCount;
                var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
                var viewRow = rowY < frozenRows ? rowY : frozenRows + _scroll.OffsetY + (rowY - frozenRows);
                if ((uint)viewRow < (uint)rowCount)
                {
                    IsTableSelected = false;
                    SelectedRow = viewRow;
                    if (CurrentCell == DataGridCell.None)
                    {
                        CurrentCell = new DataGridCell(viewRow, 0);
                    }
                    else
                    {
                        CurrentCell = new DataGridCell(viewRow, CurrentCell.Column);
                    }

                    App?.Focus(this);
                    e.Handled = true;
                    return;
                }
            }
        }

        var hit = TryHitTestCell(snapshot, e.UiX, e.UiY);
        if (hit is { } cell)
        {
            var wasCurrent = cell == CurrentCell && CurrentCell != DataGridCell.None;
            IsTableSelected = false;
            SelectedRow = -1;
            CurrentCell = cell;
            App?.Focus(this);
            e.Handled = true;

            if (!ReadOnly && _activeEditor is null && TryGetEditableCellContext(snapshot, cell, out var editableContext))
            {
                if (ResolveCellActivationMode(editableContext) == DataGridCellActivationMode.DirectActivate)
                {
                    if (e.Kind == TerminalMouseKind.Down)
                    {
                        _ = TryDirectActivateWithPointer(snapshot, cell, e);
                    }

                    return;
                }
            }

            if (!ReadOnly && (e.ClickCount >= 2 || wasCurrent) && _activeEditor is null)
            {
                _ = TryStartEdit(snapshot);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_routingSyntheticEditorInput && _activeEditor is not null && IsDescendantOf(e.OriginalSource, _activeEditor))
        {
            return;
        }

        if (_pendingDirectPointerActivation && e.Kind is TerminalMouseKind.Move or TerminalMouseKind.Drag)
        {
            ContinueDirectPointerActivation(e);
            return;
        }

        if (PressedSortColumnIndex >= 0)
        {
            var snapshotForSort = GetSnapshot();
            if (snapshotForSort is null)
            {
                PressedSortColumnIndex = -1;
                IsSortPressedInside = false;
            }
            else
            {
                var bounds = Bounds;
                var isPressedInside = TryHitSortButton(snapshotForSort, e.UiX, e.UiY, bounds, out var pressedSortColumnIndex) &&
                                      pressedSortColumnIndex == PressedSortColumnIndex;
                IsSortPressedInside = isPressedInside;
                HoveredSortColumnIndex = isPressedInside ? pressedSortColumnIndex : -1;
                HoveredResizeColumnIndex = -1;
            }

            return;
        }

        if (_resizingColumn && !e.Handled)
        {
            var snapshot = GetSnapshot();
            if (snapshot is null)
            {
                _resizingColumn = false;
                return;
            }

            var columns = EnsureResolvedColumns(snapshot, GetVisibleColumnCount(snapshot));
            if ((uint)_resizingColumnIndex >= (uint)columns.Count)
            {
                _resizingColumn = false;
                return;
            }

            var resolvedColumn = columns[_resizingColumnIndex];
            var uiColumn = resolvedColumn.Column;

            var delta = e.UiX - _resizeStartUiX;
            var nextWidth = Math.Max(1, _resizeStartWidth + delta);

            var minWidth = uiColumn is not null ? uiColumn.MinWidth : resolvedColumn.MinWidth;
            var maxWidth = uiColumn is not null
                ? (uiColumn.MaxWidth > 0 ? uiColumn.MaxWidth : int.MaxValue)
                : resolvedColumn.MaxWidth;

            nextWidth = Math.Max(nextWidth, Math.Max(1, minWidth));
            if (maxWidth > 0)
            {
                nextWidth = Math.Min(nextWidth, maxWidth);
            }

            if (uiColumn is not null)
            {
                uiColumn.Width = GridLength.Fixed(nextWidth);
            }
            else
            {
                SetColumnWidthOverride(resolvedColumn.Key, nextWidth);
            }

            e.Handled = true;
            return;
        }

        if (e.Handled)
        {
            return;
        }

        var snapshotForHover = GetSnapshot();
        if (snapshotForHover is null)
        {
            HoveredResizeColumnIndex = -1;
            return;
        }

        var rect = Bounds;
        if (!rect.Contains(e.UiX, e.UiY))
        {
            HoveredResizeColumnIndex = -1;
            HoveredSortColumnIndex = -1;
            return;
        }

        if (TryHitSortButton(snapshotForHover, e.UiX, e.UiY, rect, out var hoveredSortColumn))
        {
            HoveredSortColumnIndex = hoveredSortColumn;
            HoveredResizeColumnIndex = -1;
            return;
        }

        HoveredSortColumnIndex = -1;
        HoveredResizeColumnIndex = TryHitColumnResizeHandle(snapshotForHover, e.UiX, e.UiY, rect, out var columnIndex) ? columnIndex : -1;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (_routingSyntheticEditorInput && _activeEditor is not null && IsDescendantOf(e.OriginalSource, _activeEditor))
        {
            return;
        }

        if (_pendingDirectPointerActivation && e.Kind == TerminalMouseKind.Up && e.Button == TerminalMouseButton.Left)
        {
            ContinueDirectPointerActivation(e);
            return;
        }

        if (PressedSortColumnIndex >= 0 && !e.Handled && e.Kind == TerminalMouseKind.Up && e.Button == TerminalMouseButton.Left)
        {
            var sortColumnIndex = PressedSortColumnIndex;
            var activate = IsSortPressedInside;

            PressedSortColumnIndex = -1;
            IsSortPressedInside = false;

            var snapshot = GetSnapshot();
            var rect = Bounds;
            HoveredSortColumnIndex = snapshot is not null && TryHitSortButton(snapshot, e.UiX, e.UiY, rect, out var hoveredSortColumn)
                ? hoveredSortColumn
                : -1;

            if (activate && snapshot is not null)
            {
                var visibleColumns = GetVisibleColumnCount(snapshot);
                var columns = EnsureResolvedColumns(snapshot, visibleColumns);
                if ((uint)sortColumnIndex < (uint)columns.Count)
                {
                    _ = TryToggleColumnSortDirection(columns[sortColumnIndex].Key, additive: IsAdditiveSortModifier(e.Modifiers));
                }
            }

            e.Handled = true;
            return;
        }

        if (!_resizingColumn || e.Handled)
        {
            return;
        }

        if (e.Kind != TerminalMouseKind.Up || e.Button != TerminalMouseButton.Left)
        {
            return;
        }

        _resizingColumn = false;
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        if (_routingSyntheticEditorInput && _activeEditor is not null && IsDescendantOf(e.OriginalSource, _activeEditor))
        {
            return;
        }

        if (_activeEditor is not null)
        {
            if (e.Key is TerminalKey.Up or TerminalKey.Down or TerminalKey.PageUp or TerminalKey.PageDown)
            {
                // Single-line editors don't typically use vertical navigation; treat it as leaving edit mode.
                CloseEditor();

                var delta = e.Key switch
                {
                    TerminalKey.Up => -1,
                    TerminalKey.Down => 1,
                    TerminalKey.PageUp => -Math.Max(1, _scroll.ViewportHeight - 1),
                    TerminalKey.PageDown => Math.Max(1, _scroll.ViewportHeight - 1),
                    _ => 0,
                };

                MoveCurrentCell(deltaRow: delta, deltaCol: 0);
                e.Handled = true;
                return;
            }
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlA && _activeEditor is null)
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Char is TerminalChar.CtrlC && _activeEditor is null)
        {
            CopySelection();
            e.Handled = true;
            return;
        }

        // F4: toggle filter row.
        // Note: Some terminals do not encode Ctrl+Shift+<key> reliably, so we avoid such shortcuts.
        if (e.Key == TerminalKey.F4 && _activeEditor is null && CanFilter)
        {
            ToggleFilterRow();
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

        if ((e.Modifiers & TerminalModifiers.Ctrl) != 0 && e.Key is TerminalKey.Home or TerminalKey.End)
        {
            GoToTableEdge(first: e.Key == TerminalKey.Home);

            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case TerminalKey.Left:
                IsTableSelected = false;
                MoveCurrentCell(0, -1);
                e.Handled = true;
                return;
            case TerminalKey.Right:
                IsTableSelected = false;
                MoveCurrentCell(0, 1);
                e.Handled = true;
                return;
            case TerminalKey.Up:
                IsTableSelected = false;
                MoveCurrentCell(-1, 0);
                e.Handled = true;
                return;
            case TerminalKey.Down:
                IsTableSelected = false;
                MoveCurrentCell(1, 0);
                e.Handled = true;
                return;
            case TerminalKey.PageUp:
                IsTableSelected = false;
                MoveCurrentCell(-Math.Max(1, _scroll.ViewportHeight - 1), 0);
                e.Handled = true;
                return;
            case TerminalKey.PageDown:
                IsTableSelected = false;
                MoveCurrentCell(Math.Max(1, _scroll.ViewportHeight - 1), 0);
                e.Handled = true;
                return;
            case TerminalKey.Home:
                IsTableSelected = false;
                CurrentCell = new DataGridCell(CurrentCell.Row, 0);
                e.Handled = true;
                return;
            case TerminalKey.End:
                IsTableSelected = false;
                CurrentCell = new DataGridCell(CurrentCell.Row, Math.Max(0, GetVisibleColumnCount(snapshot) - 1));
                e.Handled = true;
                return;
            case TerminalKey.Space:
                if (!ReadOnly && TryDirectActivateWithKeyboard(snapshot, CurrentCell, e.Key, e.Modifiers))
                {
                    e.Handled = true;
                }
                return;
            case TerminalKey.Enter:
                if (!ReadOnly && TryDirectActivateWithKeyboard(snapshot, CurrentCell, e.Key, e.Modifiers))
                {
                    e.Handled = true;
                    return;
                }
                goto case TerminalKey.F2;
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
        SearchQuery = default;
    }

    private void ToggleFilterRow()
    {
        VerifyAccess();
        if (CanFilter)
        {
            FilterRowVisible = !FilterRowVisible;
        }
    }

    private void SelectAll()
    {
        VerifyAccess();
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        SelectEntireTable(snapshot);
    }

    private void CopySelection()
    {
        VerifyAccess();
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        CopySelectionToClipboard(snapshot);
    }

    private void GoToTableEdge(bool first)
    {
        VerifyAccess();
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        IsTableSelected = false;
        SelectedRow = -1;

        var rows = snapshot.RowCount;
        var cols = Math.Max(0, GetVisibleColumnCount(snapshot));
        if (rows <= 0 || cols <= 0)
        {
            CurrentCell = DataGridCell.None;
            return;
        }

        CurrentCell = first ? new DataGridCell(0, 0) : new DataGridCell(rows - 1, cols - 1);
    }

    private void StartEdit()
    {
        VerifyAccess();
        if (ReadOnly || _activeEditor is not null)
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
        ActiveMatchVersion++;
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
        ActiveMatchVersion++;
    }

    private string GetSearchStatusText()
    {
        // Ensure this participates in dependency tracking: query and source version affect matches.
        _ = SourceVersion;
        _ = SearchQuery;
        _ = ActiveMatchVersion;

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

    private void AttachColumn(DataGridColumn column)
    {
        column.Attach(this);
        ConfigureSortComparers();
    }

    private void DetachColumn(DataGridColumn column)
    {
        column.Detach(this);
        ConfigureSortComparers();
    }

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

    internal void OnColumnSortConfigurationChanged() => ConfigureSortComparers();

    private void ConfigureSortComparers()
    {
        if (View is not IConfigurableSortableDataGridView configurableSortableView)
        {
            return;
        }

        if (Columns.Count == 0)
        {
            configurableSortableView.ConfigureSortComparers(Array.Empty<DataGridSortComparerConfiguration>());
            return;
        }

        var sortComparers = new List<DataGridSortComparerConfiguration>(Columns.Count);
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            var comparer = column.CreateSortComparer();
            if (comparer is null || string.IsNullOrEmpty(column.Key))
            {
                continue;
            }

            sortComparers.Add(new DataGridSortComparerConfiguration(column.Key, comparer));
        }

        configurableSortableView.ConfigureSortComparers(sortComparers);
    }

    private bool CanFilter => View is IFilterableDataGridView;

    private bool CanSort => View is ISortableDataGridView;

    private static bool IsAdditiveSortModifier(TerminalModifiers modifiers)
        => (modifiers & (TerminalModifiers.Ctrl | TerminalModifiers.Alt)) != 0;

    private bool TryGetSortableColumn(string columnKey, out DataGridColumn? column)
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            var candidate = Columns[i];
            if (candidate.Sortable && string.Equals(candidate.Key, columnKey, StringComparison.Ordinal))
            {
                column = candidate;
                return true;
            }
        }

        column = null;
        return false;
    }

    private int GetEffectiveRowAnchorWidth()
        => ShowRowAnchor ? Math.Max(0, RowAnchorWidth) : 0;

    private int ComputeNaturalColumnsWidth(IDataGridViewSnapshot snapshot, int visibleColumns, in LayoutConstraints constraints)
    {
        var style = GetStyle<DataGridStyle>();
        var spacing = Math.Max(0, style.ColumnSpacing);
        var showVerticalLines = style.ShowVerticalLines;

        var cols = EnsureResolvedColumns(snapshot, visibleColumns);
        var width = 0;

        for (var i = 0; i < cols.Count; i++)
        {
            width += ResolveEffectiveColumnWidth(cols[i]);
            if (i + 1 < cols.Count)
            {
                width += showVerticalLines ? 1 : spacing;
            }
        }

        // Reserve a trailing gap for the last-column resize handle.
        if (cols.Count > 0)
        {
            width += Math.Max(1, showVerticalLines ? 1 : spacing);
        }

        width = Math.Max(1, width);

        if (constraints.IsWidthBounded)
        {
            width = Math.Min(width, constraints.MaxWidth);
        }

        return width;
    }

    private int ResolveEffectiveColumnWidth(in ResolvedColumn column)
    {
        if (column.Column is null && _columnWidthOverrides.TryGetValue(column.Key, out var overrideWidth))
        {
            return Math.Clamp(overrideWidth, column.MinWidth, column.MaxWidth);
        }

        return column.BaseWidth;
    }

    private void SetColumnWidthOverride(string key, int width)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (_columnWidthOverrides.TryGetValue(key, out var current) && current == width)
        {
            return;
        }

        _columnWidthOverrides[key] = width;
        _columnWidthVersionCounter++;
        ColumnWidthVersion = _columnWidthVersionCounter;
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
            _resolvedColumnWidths[i] = Math.Clamp(Math.Max(0, ResolveEffectiveColumnWidth(c)), c.MinWidth, c.MaxWidth);
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

        // Reserve a trailing gap for the last-column resize handle so it does not overlap the last column content.
        var trailingGap = columns.Count == 0 ? 0 : Math.Max(1, showVerticalLines ? 1 : spacing);
        trailingGap = Math.Clamp(trailingGap, 0, Math.Max(0, availableWidth - separators));

        var availableForCells = Math.Max(0, availableWidth - separators - trailingGap);
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

        // If fixed widths + min widths overflow, shrink the last column (if possible) to preserve the trailing gap.
        if (columns.Count > 0 && trailingGap > 0)
        {
            var used = separators;
            for (var i = 0; i < columns.Count; i++)
            {
                used += _resolvedColumnWidths[i];
            }

            var targetUsed = Math.Max(0, availableWidth - trailingGap);
            var overflow = used - targetUsed;
            if (overflow > 0)
            {
                var last = columns.Count - 1;
                var shrinkable = Math.Max(0, _resolvedColumnWidths[last] - columns[last].MinWidth);
                var shrink = Math.Min(overflow, shrinkable);
                if (shrink > 0)
                {
                    _resolvedColumnWidths[last] -= shrink;
                }
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

    private int GetHeaderContentWidth(in ResolvedColumn column, int columnWidth)
    {
        if (columnWidth <= 0)
        {
            return 0;
        }

        return GetSortButtonWidth(column, columnWidth) is { } buttonWidth && buttonWidth > 0
            ? Math.Max(0, columnWidth - buttonWidth)
            : columnWidth;
    }

    private int GetSortButtonWidth(in ResolvedColumn column, int columnWidth)
    {
        if (!CanSort || column.Column?.Sortable != true || columnWidth <= 0)
        {
            return 0;
        }

        return Math.Min(SortButtonWidth, columnWidth);
    }

    private Rectangle? GetSortButtonRect(in ResolvedColumn column, int x, int y, int columnWidth)
    {
        var buttonWidth = GetSortButtonWidth(column, columnWidth);
        if (buttonWidth <= 0)
        {
            return null;
        }

        return new Rectangle(x + columnWidth - buttonWidth, y, buttonWidth, 1);
    }

    private void ArrangeHeaderAndFilter(Rectangle rect, int headerHeight, int filterHeight, int frozenColumns)
    {
        var yHeader = rect.Y;
        var yFilter = rect.Y + headerHeight;

        if (headerHeight > 0)
        {
            var snapshot = GetSnapshot();
            var cols = snapshot is null ? null : EnsureResolvedColumns(snapshot, GetVisibleColumnCount(snapshot));
            for (var i = 0; i < _headerVisuals.Count && i < _headerVisualColumns.Count; i++)
            {
                var visibleColumnIndex = _headerVisualColumns[i];
                if ((uint)visibleColumnIndex >= (uint)_resolvedColumnWidths.Length)
                {
                    continue;
                }

                var x = rect.X + GetColumnX(visibleColumnIndex, rect, frozenColumns);
                var w = _resolvedColumnWidths[visibleColumnIndex];
                if (cols is not null && (uint)visibleColumnIndex < (uint)cols.Count)
                {
                    w = GetHeaderContentWidth(cols[visibleColumnIndex], w);
                }

                if (w <= 0)
                {
                    continue;
                }

                // DataGridControl does not measure children in MeasureCore; ensure visuals have a non-zero desired height.
                _headerVisuals[i].Measure(new LayoutConstraints(0, w, 0, 1));
                _headerVisuals[i].Arrange(new Rectangle(x, yHeader, w, 1));
            }
        }

        if (filterHeight > 0)
        {
            for (var i = 0; i < _filterVisuals.Count && i < _resolvedColumnWidths.Length; i++)
            {
                var x = rect.X + GetColumnX(i, rect, frozenColumns);
                var w = _resolvedColumnWidths[i];
                // DataGridControl does not measure children in MeasureCore; ensure visuals have a non-zero desired height.
                _filterVisuals[i].Measure(new LayoutConstraints(0, w, 0, 1));
                _filterVisuals[i].Arrange(new Rectangle(x, yFilter, w, 1));
            }
        }
    }

    private int GetColumnX(int visibleColumnIndex, Rectangle rect, int frozenColumns)
    {
        _ = rect;
        var anchorWidth = GetEffectiveRowAnchorWidth();
        var start = _resolvedColumnStarts[visibleColumnIndex];
        if (visibleColumnIndex < frozenColumns)
        {
            return anchorWidth + start;
        }

        var frozenWidth = SumColumnsWidth(0, frozenColumns);
        return anchorWidth + frozenWidth + (start - frozenWidth) - _scroll.OffsetX;
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
                var box = new FilterTextBox()
                    .TextAlignment(TextAlignment.Left)
                    .Placeholder("Filter…")
                    .Style(TextBoxStyle.Default with { Padding = new Thickness(0) });
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

        // Apply filters while suppressing bindable notifications:
        // SetFilters triggers a view rebuild which raises a view-changed event synchronously, updating SourceVersion.
        // PrepareChildren reads SourceVersion for dependency tracking; suppressing notifications avoids a read->write
        // loop exception in the same tracking context, while still letting this render pass use the updated snapshot.
        using (BindingManager.Current.SuppressWriteTracking())
        {
            filterable.SetFilters(filters);
        }

        _lastFilterHash = next;
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
        var frozenRowCount = Math.Min(snapshot.RowCount, frozenRows);
        var scrollStartRow = Math.Min(snapshot.RowCount, frozenRows + _scroll.OffsetY);
        var maxRow = Math.Min(snapshot.RowCount, scrollStartRow + viewportScrollRows);

        var ctxBase = new DataTemplateContext(this, DataTemplateRole.Display, -1, DataTemplateItemState.None);

        for (var row = 0; row < frozenRowCount; row++)
        {
            EnsureCellRowVisuals(snapshot, rect, cols, row, yTop + row, ctxBase, frozenColumns);
        }

        for (var row = scrollStartRow; row < maxRow; row++)
        {
            var y = yTop + frozenRows + (row - scrollStartRow);
            EnsureCellRowVisuals(snapshot, rect, cols, row, y, ctxBase, frozenColumns);
        }

        _cellRecyclePool.Clear();
    }

    private void EnsureCellRowVisuals(
        IDataGridViewSnapshot snapshot,
        Rectangle rect,
        IReadOnlyList<ResolvedColumn> cols,
        int row,
        int y,
        DataTemplateContext ctxBase,
        int frozenColumns)
    {
        if ((uint)(y - rect.Y) >= (uint)rect.Height)
        {
            return;
        }

        var rowModel = snapshot.GetRowModel(row);

        for (var c = 0; c < cols.Count; c++)
        {
            if (IsEditingCell(row, c))
            {
                continue;
            }

            var column = cols[c].Column;
            var schemaAccessor = cols[c].SchemaAccessor;
            var schemaValueType = cols[c].SchemaValueType;
            var effectiveReadOnly = ReadOnly || cols[c].SchemaReadOnly || (column is not null && column.ReadOnly);
            var hasDisplayVisual = column is not null
                ? column.HasDisplayTemplate(this, effectiveReadOnly)
                : HasSchemaDisplayTemplate(schemaValueType);

            if (!hasDisplayVisual)
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
            if (!rect.Intersects(cellRect))
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
            Visual? v;
            if (column is not null)
            {
                v = column.CreateOrUpdateDisplayVisual(this, rowModel, ctx, effectiveReadOnly, reused, out _);
            }
            else if (!TryCreateSchemaDisplayVisual(schemaValueType, rowModel, schemaAccessor, ctx, reused, out v) || v is null)
            {
                continue;
            }

            _cellVisuals.Add(v);
            v.Measure(new LayoutConstraints(0, cellRect.Width, 0, cellRect.Height));
            v.Arrange(cellRect);
        }
    }

    private bool TryStartEdit(IDataGridViewSnapshot snapshot) => TryStartEdit(snapshot, CurrentCell, out _);

    private bool TryStartEdit(IDataGridViewSnapshot snapshot, DataGridCell cell, out Visual? editor)
    {
        editor = null;
        if (!TryGetEditableCellContext(snapshot, cell, out var context))
        {
            return false;
        }

        if (!TryCreateEditorForCell(context, out editor, out var pooled, out var accessor) || editor is null)
        {
            return false;
        }

        _activeEditorRowModel = context.RowModel;
        _activeEditorAccessor = accessor;
        _activeEditorOriginalValue = accessor.GetValueAsObject(context.RowModel);

        PrepareEditorForCell(editor);
        OpenEditor(editor, context.Cell, pooled);
        return true;
    }

    private bool TryGetEditableCellContext(IDataGridViewSnapshot snapshot, DataGridCell cell, out EditableCellContext context)
    {
        context = null!;

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);

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

        var resolvedColumn = columns[col];
        var column = resolvedColumn.Column;
        var effectiveReadOnly = ReadOnly || resolvedColumn.SchemaReadOnly || (column is not null && column.ReadOnly);
        if (effectiveReadOnly)
        {
            return false;
        }

        context = new EditableCellContext(cell, snapshot.GetRowModel(row), resolvedColumn);
        return true;
    }

    private bool TryCreateEditorForCell(EditableCellContext context, out Visual? editor, out bool pooled, out BindingAccessor accessor)
    {
        pooled = false;
        editor = null;

        var cell = context.Cell;
        var rowModel = context.RowModel;
        var column = context.ResolvedColumn.Column;
        var schemaAccessor = context.ResolvedColumn.SchemaAccessor;
        var schemaValueType = context.ResolvedColumn.SchemaValueType;
        var ctx = new DataTemplateContext(this, DataTemplateRole.Editor, cell.Row, DataTemplateItemState.Focused);

        if (column is not null && column.TryCreateEditorVisual(this, rowModel, ctx, out editor) && editor is not null)
        {
            accessor = column.ValueAccessor;
            return true;
        }

        if (column is null && TryCreateDefaultEditorFromSchema(schemaValueType, rowModel, schemaAccessor, ctx, out editor) && editor is not null)
        {
            accessor = schemaAccessor;
            return true;
        }

        var isStringCell = schemaValueType == typeof(string) || column is DataGridColumn<string>;
        if (isStringCell && TryCreatePooledTextBoxEditor(rowModel, schemaAccessor, out var textBox))
        {
            pooled = true;
            editor = textBox;
            accessor = schemaAccessor;
            return true;
        }

        accessor = schemaAccessor;
        return false;
    }

    private DataGridCellActivationMode ResolveCellActivationMode(EditableCellContext context)
    {
        if (context.ResolvedColumn.Column?.CellActivationMode is { } columnMode)
        {
            return columnMode;
        }

        if (CellActivationMode != DataGridCellActivationMode.Auto)
        {
            return CellActivationMode;
        }

        var valueType = context.ResolvedColumn.Column?.ValueType ?? context.ResolvedColumn.SchemaValueType;
        return valueType == typeof(bool)
            ? DataGridCellActivationMode.DirectActivate
            : DataGridCellActivationMode.ExplicitEdit;
    }

    private bool TryDirectActivateWithKeyboard(IDataGridViewSnapshot snapshot, DataGridCell cell, TerminalKey key, TerminalModifiers modifiers)
    {
        if (!TryGetEditableCellContext(snapshot, cell, out var context) ||
            ResolveCellActivationMode(context) != DataGridCellActivationMode.DirectActivate)
        {
            return false;
        }

        if (!TryStartEdit(snapshot, context.Cell, out var editor) || editor is null)
        {
            return false;
        }

        ArrangeActiveEditorNow(snapshot);

        var args = new KeyEventArgs
        {
            RawEvent = new TerminalKeyEvent
            {
                Key = key,
                Modifiers = modifiers,
            }
        };

        _routingSyntheticEditorInput = true;
        try
        {
            editor.RaiseEvent(Visual.KeyDownEvent, args);
        }
        finally
        {
            _routingSyntheticEditorInput = false;
        }

        if (args.Handled && ReferenceEquals(editor, _activeEditor))
        {
            CloseEditor();
        }

        return true;
    }

    private bool TryDirectActivateWithPointer(IDataGridViewSnapshot snapshot, DataGridCell cell, PointerEventArgs e)
    {
        if (!TryGetEditableCellContext(snapshot, cell, out var context) ||
            ResolveCellActivationMode(context) != DataGridCellActivationMode.DirectActivate)
        {
            return false;
        }

        if (!TryStartEdit(snapshot, context.Cell, out var editor) || editor is null)
        {
            return false;
        }

        ArrangeActiveEditorNow(snapshot);

        var pressedArgs = CreateSyntheticPointerEvent(editor, e, e.Kind);
        _routingSyntheticEditorInput = true;
        try
        {
            editor.RaiseEvent(Visual.PointerPressedEvent, pressedArgs);
        }
        finally
        {
            _routingSyntheticEditorInput = false;
        }

        if (!ReferenceEquals(editor, _activeEditor))
        {
            _pendingDirectPointerActivation = false;
            return true;
        }

        if (editor is CheckBox)
        {
            CloseEditor();
            return true;
        }

        _pendingDirectPointerActivation = editor is Button or Switch;
        if (!_pendingDirectPointerActivation && pressedArgs.Handled)
        {
            CloseEditor();
        }

        return true;
    }

    private void ContinueDirectPointerActivation(PointerEventArgs e)
    {
        if (!_pendingDirectPointerActivation || _activeEditor is null)
        {
            _pendingDirectPointerActivation = false;
            return;
        }

        var args = CreateSyntheticPointerEvent(_activeEditor, e, e.Kind);
        if (e.Kind == TerminalMouseKind.Up)
        {
            _routingSyntheticEditorInput = true;
            try
            {
                _activeEditor.RaiseEvent(Visual.PointerReleasedEvent, args);
            }
            finally
            {
                _routingSyntheticEditorInput = false;
            }

            _pendingDirectPointerActivation = false;
            if (_activeEditor is not null)
            {
                CloseEditor();
            }
        }
        else
        {
            _routingSyntheticEditorInput = true;
            try
            {
                _activeEditor.RaiseEvent(Visual.PointerMovedEvent, args);
            }
            finally
            {
                _routingSyntheticEditorInput = false;
            }
        }

        e.Handled = true;
    }

    private static PointerEventArgs CreateSyntheticPointerEvent(Visual target, PointerEventArgs source, TerminalMouseKind kind)
    {
        return new PointerEventArgs
        {
            RawEvent = new TerminalMouseEvent
            {
                Kind = kind,
                Button = source.Button,
                Modifiers = source.Modifiers,
                WheelDelta = source.WheelDelta,
                X = source.X,
                Y = source.Y,
            },
            UiX = source.UiX,
            UiY = source.UiY,
            ClickCount = source.ClickCount,
            LocalX = source.UiX - target.Bounds.X,
            LocalY = source.UiY - target.Bounds.Y,
        };
    }

    private void ArrangeActiveEditorNow(IDataGridViewSnapshot snapshot)
    {
        if (_activeEditor is null)
        {
            return;
        }

        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var headerHeight = ShowHeader ? 1 : 0;
        var filterHeight = FilterRowVisible && CanFilter ? 1 : 0;
        var anchorWidth = GetEffectiveRowAnchorWidth();
        var rowCount = snapshot.RowCount;
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var frozenRows = Math.Clamp(FrozenRows, 0, rowCount);
        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);

        ResolveColumnLayout(snapshot, visibleColumns, Math.Max(0, rect.Width - anchorWidth));

        var editorRect = TryGetCellRect(_activeEditorCell, rect, headerHeight, filterHeight, frozenRows, frozenColumns);
        if (editorRect is not { } r)
        {
            return;
        }

        _activeEditor.Measure(new LayoutConstraints(0, r.Width, 0, r.Height));
        _activeEditor.Arrange(r);
    }

    private static bool IsDescendantOf(Visual? visual, Visual ancestor)
    {
        for (var current = visual; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryCreatePooledTextBoxEditor(object rowModel, BindingAccessor accessor, out TextBox editor)
    {
        editor = null!;

        editor = _textBoxPool.Count == 0 ? new TextBox() : PopTextBox();
        editor.SetStyle(TextBoxStyle.Key, CreateCellEditorTextBoxStyle());
        editor.TextDocument = new DynamicTextDocument(
            getter: () => (string?)accessor.GetValueAsObject(rowModel) ?? string.Empty,
            setter: s => accessor.SetValueAsObject(rowModel, s));
        InitializeTextEditorForCell(editor);
        return true;
    }

    private void InitializeTextEditorForCell(TextEditorBase editor)
    {
        // Editors can carry caret/selection/scroll state from a previous cell. Reset to a predictable state:
        // - place the caret at the end (F2/edit-mode behavior)
        // - reset horizontal scroll so initial render isn't shifted
        var length = editor.TextDocument.CurrentSnapshot.Length;
        editor.CaretIndex = length;
        editor.Scroll.SetOffset(0, 0);
    }

    private TextBoxStyle CreateCellEditorTextBoxStyle()
    {
        var theme = GetTheme();

        // Make text selection clearly visible against the (also selection-colored) active cell background.
        var selection = (theme.Accent ?? theme.FocusBorder ?? theme.Selection)?.WithAlpha(0xA0);

        return TextBoxStyle.Default with
        {
            Padding = new Thickness(0, 0, 0, 0),
            Selection = selection,
        };
    }

    private bool TryCreateDefaultEditorFromSchema(Type? schemaValueType, object rowModel, BindingAccessor accessor, in DataTemplateContext context, out Visual? editor)
    {
        editor = null;
        if (schemaValueType is null)
        {
            return false;
        }

        if (schemaValueType == typeof(bool))
        {
            var boolBinding = new Binding<bool>(rowModel, WrapAccessor<bool>(accessor));
            var templates = GetStyle<DataTemplates>();
            if (templates.TryResolve<bool>(DataTemplateRole.Editor, out var template) && !template.IsEmpty && template.Editor is not null)
            {
                editor = template.Editor(boolBinding, context);
                return true;
            }

            editor = new CheckBox(boolBinding);
            return true;
        }

        if (schemaValueType == typeof(sbyte))
        {
            editor = CreateCellNumberBox<sbyte>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(byte))
        {
            editor = CreateCellNumberBox<byte>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(short))
        {
            editor = CreateCellNumberBox<short>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(ushort))
        {
            editor = CreateCellNumberBox<ushort>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(int))
        {
            editor = CreateCellNumberBox<int>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(uint))
        {
            editor = CreateCellNumberBox<uint>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(long))
        {
            editor = CreateCellNumberBox<long>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(ulong))
        {
            editor = CreateCellNumberBox<ulong>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(float))
        {
            editor = CreateCellNumberBox<float>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(double))
        {
            editor = CreateCellNumberBox<double>(rowModel, accessor);
            return true;
        }

        if (schemaValueType == typeof(decimal))
        {
            editor = CreateCellNumberBox<decimal>(rowModel, accessor);
            return true;
        }

        if (schemaValueType.IsEnum)
        {
            editor = CreateEnumEditor(rowModel, accessor, schemaValueType);
            return true;
        }

        return false;
    }

    private NumberBox<T> CreateCellNumberBox<T>(object rowModel, BindingAccessor accessor) where T : struct, System.Numerics.INumber<T>
    {
        var box = new NumberBox<T>();
        box.Value(new Binding<T>(rowModel, WrapAccessor<T>(accessor)));

        // Force an initial pull from the binding so the editor text reflects the current value.
        // Bound properties update their local cached value when read.
        _ = box.Value;

        box.SetStyle(TextBoxStyle.Key, CreateCellEditorTextBoxStyle());
        InitializeTextEditorForCell(box);
        return box;
    }

    private TextBox CreateEnumEditor(object rowModel, BindingAccessor accessor, Type enumType)
    {
        var culture = GetCulture();
        var localText = ValueStringFormatter.ToString(accessor.GetValueAsObject(rowModel), culture);

        var box = new TextBox();
        box.SetStyle(TextBoxStyle.Key, CreateCellEditorTextBoxStyle());

        box.TextDocument = new DynamicTextDocument(
            getter: () => localText,
            setter: text =>
            {
                localText = text ?? string.Empty;
                if (Enum.TryParse(enumType, localText, ignoreCase: true, out var parsed) && parsed is not null)
                {
                    accessor.SetValueAsObject(rowModel, parsed);
                }
            });

        InitializeTextEditorForCell(box);
        return box;
    }

    private static BindingAccessor<T> WrapAccessor<T>(BindingAccessor accessor)
    {
        if (accessor is BindingAccessor<T> typed)
        {
            return typed;
        }

        return new BindingAccessor<T>(
            accessor.Name,
            owner => (T)accessor.GetValueAsObject(owner)!,
            (owner, value) => accessor.SetValueAsObject(owner, value));
    }

    private TextBox PopTextBox()
    {
        var last = _textBoxPool.Count - 1;
        var editor = _textBoxPool[last];
        _textBoxPool.RemoveAt(last);
        return editor;
    }

    private void OpenEditor(Visual editor, DataGridCell cell, bool pooled)
    {
        CloseEditor();
        _activeEditor = editor;
        _activeEditorCell = cell;
        _activeEditorPooled = pooled;
        AttachChild(editor);
        App?.Focus(editor);
    }

    private void PrepareEditorForCell(Visual editor)
    {
        if (editor is not TextEditorBase textEditor)
        {
            return;
        }

        if (!editor.HasLocalStyle(TextBoxStyle.Key))
        {
            editor.SetStyle(TextBoxStyle.Key, CreateCellEditorTextBoxStyle());
        }

        InitializeTextEditorForCell(textEditor);
    }

    private void CloseEditor()
    {
        if (_activeEditor is null)
        {
            return;
        }

        var pooled = _activeEditorPooled;
        var pooledTextBox = pooled ? _activeEditor as TextBox : null;

        DetachChild(_activeEditor);
        _activeEditor = null;
        _activeEditorCell = DataGridCell.None;
        _activeEditorRowModel = null;
        _activeEditorAccessor = null;
        _activeEditorOriginalValue = null;
        _activeEditorPooled = false;
        _pendingDirectPointerActivation = false;

        if (pooledTextBox is not null)
        {
            pooledTextBox.TextDocument = new TextDocument();
            InitializeTextEditorForCell(pooledTextBox);
            _textBoxPool.Add(pooledTextBox);
        }
        App?.Focus(this);
    }

    private void CommitEdit()
    {
        VerifyAccess();
        if (_activeEditor is null)
        {
            return;
        }

        CloseEditor();
    }

    private void CancelEdit()
    {
        VerifyAccess();
        if (_activeEditor is null)
        {
            return;
        }

        if (_activeEditorRowModel is not null && _activeEditorAccessor is not null)
        {
            _activeEditorAccessor.SetValueAsObject(_activeEditorRowModel, _activeEditorOriginalValue);
        }

        CloseEditor();
    }

    private void MoveEditorToAdjacentCell(int deltaColumn)
    {
        VerifyAccess();
        if (_activeEditor is null)
        {
            return;
        }

        CloseEditor();
        MoveCurrentCell(deltaRow: 0, deltaCol: deltaColumn);

        var snapshot = GetSnapshot();
        if (snapshot is not null && !ReadOnly)
        {
            _ = TryStartEdit(snapshot);
        }
    }

    private void MoveCurrentCell(int deltaRow, int deltaCol)
    {
        var snapshot = GetSnapshot();
        if (snapshot is null)
        {
            return;
        }

        IsTableSelected = false;
        SelectedRow = -1;

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
        if (IsTableSelected)
        {
            return true;
        }

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

    private bool IsRowSelected(int row)
    {
        if (SelectionMode == DataGridSelectionMode.Row)
        {
            var current = CurrentCell;
            return current != DataGridCell.None && current.Row == row;
        }

        return SelectedRow >= 0 && SelectedRow == row;
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

        var anchorWidth = GetEffectiveRowAnchorWidth();
        if (anchorWidth > 0 && x - rect.X < anchorWidth)
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

        var relX = x - rect.X - anchorWidth;
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
        int rowAnchorWidth,
        Style cellStyle,
        Style selectionStyle,
        Style matchStyle,
        string? searchText)
    {
        var cols = EnsureResolvedColumns(snapshot, visibleColumns);
        var rowModel = snapshot.GetRowModel(viewRow);
        var culture = GetCulture();
        var isRowSelected = IsRowSelected(viewRow);

        if (rowAnchorWidth > 0)
        {
            var anchorRect = new Rectangle(rect.X, y, Math.Min(rowAnchorWidth, rect.Width), 1);
            var anchorStyle = isRowSelected ? selectionStyle : cellStyle;
            FillRect(buffer, anchorRect, anchorStyle);

            if (anchorRect.Width > 0)
            {
                var marker = isRowSelected
                    ? new Rune('■')
                    : (HasFocusWithin && CurrentCell != DataGridCell.None && CurrentCell.Row == viewRow ? new Rune('>') : new Rune(' '));
                buffer.SetCell(anchorRect.X + anchorRect.Width - 1, y, marker, anchorStyle);
            }
        }

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
            var effectiveReadOnly = ReadOnly || schema.SchemaReadOnly || (column is not null && column.ReadOnly);
            var hasDisplayVisual = column is not null
                ? column.HasDisplayTemplate(this, effectiveReadOnly)
                : HasSchemaDisplayTemplate(schema.SchemaValueType);

            var isSelected = IsSelectedCell(viewRow, c);
            var style = isSelected || isRowSelected ? selectionStyle : cellStyle;
            var cellRect = new Rectangle(x, y, w, 1);

            if (IsEditingCell(viewRow, c))
            {
                FillRect(buffer, cellRect, style);
                continue;
            }

            var text = column is not null
                ? column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(schema.SchemaAccessor.GetValueAsObject(rowModel), culture);

            if (searchText is not null && searchText.Length != 0 && text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (!isSelected)
                {
                    style = matchStyle;
                }
            }

            if (hasDisplayVisual)
            {
                FillRect(buffer, cellRect, style);
                continue;
            }

            FillRect(buffer, cellRect, style);
            WriteAlignedText(buffer, cellRect, text.AsSpan(), style, column?.CellAlignment ?? TextAlignment.Left);
        }
    }

    private bool IsEditingCell(int row, int visibleColumnIndex)
        => _activeEditor is not null
           && _activeEditorCell.Row == row
           && _activeEditorCell.Column == visibleColumnIndex;

    private bool HasSchemaDisplayTemplate(Type? schemaValueType)
        => schemaValueType == typeof(bool) && TryResolveSchemaBoolDisplayTemplate(out _);

    private bool TryCreateSchemaDisplayVisual(Type? schemaValueType, object rowModel, BindingAccessor accessor, in DataTemplateContext context, Visual? reused, out Visual? visual)
    {
        visual = null;
        if (schemaValueType != typeof(bool) || !TryResolveSchemaBoolDisplayTemplate(out var template))
        {
            return false;
        }

        var binding = new Binding<bool>(rowModel, WrapAccessor<bool>(accessor));
        var value = new DataTemplateValue<bool>(binding);

        if (reused is not null && template.TryUpdate is { } updater && updater(reused, value, context))
        {
            visual = reused;
            return true;
        }

        if (reused is not null && template.Release is { } release)
        {
            release(reused);
        }

        visual = template.Display!(value, context);
        return true;
    }

    private bool TryResolveSchemaBoolDisplayTemplate(out DataTemplate<bool> template)
    {
        var templates = GetStyle<DataTemplates>();
        return templates.TryResolve<bool>(DataTemplateRole.Display, out template) && !template.IsEmpty && template.Display is not null;
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
                    : ValueStringFormatter.ToString(col.SchemaAccessor.GetValueAsObject(rowModel), culture);

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

        var columnsKey = ComputeColumnsKey(snapshot);
        if (snapshot.Version == _lastSnapshotVersion && snapshot.ColumnCount == _lastSnapshotColumnCount && columnsKey == _lastColumnsKey)
        {
            return _cachedResolvedColumns;
        }

        _lastSnapshot = snapshot;
        _lastSnapshotVersion = snapshot.Version;
        _lastSnapshotColumnCount = snapshot.ColumnCount;
        _lastColumnsKey = columnsKey;

        _cachedResolvedColumns.Clear();
        var culture = GetCulture();

        if (Columns.Count == 0)
        {
            for (var i = 0; i < snapshot.ColumnCount; i++)
            {
                var info = snapshot.GetColumn(i);
                _cachedResolvedColumns.Add(CreateResolvedFromSchema(snapshot, info, uiColumn: null, culture));
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
                    _cachedResolvedColumns.Add(CreateResolvedMissingSchema(snapshot, ui, culture));
                    continue;
                }

                var info = snapshot.GetColumn(schemaIndex);
                _cachedResolvedColumns.Add(CreateResolvedFromSchema(snapshot, info, ui, culture));
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

    private int ComputeColumnsKey(IDataGridViewSnapshot snapshot)
    {
        // Make sure resolved columns react to column property changes even when the list itself doesn't change.
        // This is intentionally conservative: any column layout-affecting property participates in the cache key.
        var hc = new HashCode();
        hc.Add(snapshot.Version);
        hc.Add(snapshot.ColumnCount);
        hc.Add(CanSort);

        var cols = Columns;
        hc.Add(cols.Count);
        for (var i = 0; i < cols.Count; i++)
        {
            var c = cols[i];
            hc.Add(c.Visible);
            hc.Add(c.Key, StringComparer.Ordinal);
            hc.Add(c.MinWidth);
            hc.Add(c.MaxWidth);
            hc.Add(c.Width.Type);
            hc.Add(c.Width.Value);
            hc.Add(c.HeaderAlignment);
            hc.Add(c.CellAlignment);
            hc.Add(c.ReadOnly);
            hc.Add(c.Sortable);
            hc.Add(c.Header is null ? 0 : RuntimeHelpers.GetHashCode(c.Header));
        }

        return hc.ToHashCode();
    }

    private ResolvedColumn CreateResolvedMissingSchema(IDataGridViewSnapshot snapshot, DataGridColumn ui, CultureInfo culture)
    {
        var header = ui.Header;
        var headerWidth = header is null ? TerminalTextUtility.GetWidth(ui.Key.AsSpan()) : MeasureHeaderVisualWidth(header);
        headerWidth += ui.Sortable && CanSort ? SortButtonWidth : 0;
        var baseWidth = ResolveBaseWidth(snapshot, ui, ui.ValueAccessor, ui.ValueType, headerWidth, culture);

        return new ResolvedColumn(
            Key: ui.Key,
            SchemaAccessor: ui.ValueAccessor,
            SchemaValueType: ui.ValueType,
            SchemaReadOnly: true,
            HeaderText: ui.Key,
            HeaderVisual: header,
            Column: ui,
            BaseWidth: baseWidth,
            MinWidth: Math.Max(0, ui.MinWidth),
            MaxWidth: ui.MaxWidth <= 0 ? int.MaxValue : ui.MaxWidth,
            IsStar: ui.Width.Type is GridUnitType.Star or GridUnitType.FlexStar,
            StarWeight: ui.Width.Type is GridUnitType.Star or GridUnitType.FlexStar ? ui.Width.Value : 0);
    }

    private ResolvedColumn CreateResolvedFromSchema(IDataGridViewSnapshot snapshot, DataGridColumnInfo info, DataGridColumn? uiColumn, CultureInfo culture)
    {
        var ui = uiColumn;
        var headerVisual = ui?.Header;
        var headerText = info.HeaderText;
        var headerWidth = headerVisual is not null ? MeasureHeaderVisualWidth(headerVisual) : TerminalTextUtility.GetWidth(headerText.AsSpan());
        headerWidth += ui?.Sortable == true && CanSort ? SortButtonWidth : 0;

        var minWidth = ui?.MinWidth ?? 0;
        var maxWidth = ui?.MaxWidth ?? int.MaxValue;
        maxWidth = maxWidth <= 0 ? int.MaxValue : maxWidth;

        var baseWidth = ResolveBaseWidth(snapshot, ui, info.Accessor, info.ValueType, headerWidth, culture);

        var isStar = ui?.Width.Type is GridUnitType.Star or GridUnitType.FlexStar;
        var starWeight = isStar == true ? ui!.Width.Value : 0;

        return new ResolvedColumn(
            Key: ui?.Key ?? info.Key,
            SchemaAccessor: info.Accessor,
            SchemaValueType: info.ValueType,
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

    private int ResolveBaseWidth(IDataGridViewSnapshot snapshot, DataGridColumn? ui, BindingAccessor accessor, Type? valueType, int headerWidth, CultureInfo culture)
    {
        var minWidth = ui?.MinWidth ?? 0;
        var maxWidth = ui?.MaxWidth ?? int.MaxValue;
        maxWidth = maxWidth <= 0 ? int.MaxValue : maxWidth;

        var w = ui?.Width ?? GridLength.Auto;
        var sampleWidth = w.Type is GridUnitType.Auto or GridUnitType.Star or GridUnitType.FlexStar
            ? ComputeSampleContentWidth(snapshot, ui, accessor, valueType, culture)
            : 0;

        var baseWidth = w.Type switch
        {
            GridUnitType.Fixed => (int)Math.Round(w.Value),
            GridUnitType.Auto => Math.Max(headerWidth, sampleWidth),
            GridUnitType.Star => Math.Max(Math.Max(minWidth, headerWidth), sampleWidth),
            GridUnitType.FlexStar => Math.Max(Math.Max(minWidth, headerWidth), sampleWidth),
            _ => Math.Max(headerWidth, sampleWidth),
        };

        baseWidth = Math.Max(baseWidth, minWidth);
        baseWidth = Math.Min(baseWidth, maxWidth);
        return Math.Max(1, baseWidth);
    }

    private int ComputeSampleContentWidth(IDataGridViewSnapshot snapshot, DataGridColumn? column, BindingAccessor accessor, Type? valueType, CultureInfo culture)
    {
        _ = valueType;

        var rows = snapshot.RowCount;
        if (rows <= 0)
        {
            return 0;
        }

        var max = 0;
        var limit = Math.Min(rows, AutoSizeSampleRowCount);
        for (var r = 0; r < limit; r++)
        {
            var rowModel = snapshot.GetRowModel(r);
            var text = column is not null
                ? column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(accessor.GetValueAsObject(rowModel), culture);
            max = Math.Max(max, TerminalTextUtility.GetWidth(text.AsSpan()));
        }

        var current = CurrentCell;
        if (current != DataGridCell.None && (uint)current.Row < (uint)rows && current.Row >= limit)
        {
            var rowModel = snapshot.GetRowModel(current.Row);
            var text = column is not null
                ? column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(accessor.GetValueAsObject(rowModel), culture);
            max = Math.Max(max, TerminalTextUtility.GetWidth(text.AsSpan()));
        }

        return max;
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

        var textCells = TerminalTextUtility.GetWidth(text);
        if (textCells <= width)
        {
            var x = AlignX(rect, alignment, width, textCells);
            buffer.WriteText(x, rect.Y, text, style);
            return;
        }

        // Truncated: show an ellipsis to make clipping obvious.
        var ellipsis = new Rune('…');
        if (TerminalTextUtility.GetRuneWidth(ellipsis) != 1)
        {
            ellipsis = new Rune('.');
        }

        if (width == 1)
        {
            buffer.SetCell(rect.X, rect.Y, ellipsis, style);
            return;
        }

        var availableTextCells = width - 1;
        if (alignment == TextAlignment.Right)
        {
            var tail = ClipFromEnd(text, availableTextCells);
            var tailCells = TerminalTextUtility.GetWidth(tail);
            buffer.SetCell(rect.X, rect.Y, ellipsis, style);
            buffer.WriteText(rect.X + width - tailCells, rect.Y, tail, style);
            return;
        }

        var head = Clip(text, availableTextCells);
        buffer.WriteText(rect.X, rect.Y, head, style);
        buffer.SetCell(rect.X + width - 1, rect.Y, ellipsis, style);
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

    private static ReadOnlySpan<char> ClipFromEnd(ReadOnlySpan<char> text, int maxCells)
    {
        if (maxCells <= 0 || text.IsEmpty)
        {
            return ReadOnlySpan<char>.Empty;
        }

        var totalCells = TerminalTextUtility.GetWidth(text);
        if (totalCells <= maxCells)
        {
            return text;
        }

        var skipCells = totalCells - maxCells;
        if (!TerminalTextUtility.TryGetIndexAtCell(text, skipCells, out var startIndex))
        {
            startIndex = 0;
        }

        var slice = text[Math.Clamp(startIndex, 0, text.Length)..];
        if (TerminalTextUtility.GetWidth(slice) > maxCells)
        {
            slice = Clip(slice, maxCells);
        }

        return slice;
    }

    private void AutoSizeColumn(IDataGridViewSnapshot snapshot, int visibleColumnIndex)
    {
        var visibleColumns = GetVisibleColumnCount(snapshot);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);
        if ((uint)visibleColumnIndex >= (uint)columns.Count)
        {
            return;
        }

        var col = columns[visibleColumnIndex];
        var culture = GetCulture();

        var headerWidth = col.HeaderVisual is not null
            ? MeasureHeaderVisualWidth(col.HeaderVisual)
            : TerminalTextUtility.GetWidth(col.HeaderText.AsSpan());

        var maxWidth = Math.Max(1, headerWidth);
        var limit = snapshot.RowCount;
        for (var r = 0; r < limit; r++)
        {
            var rowModel = snapshot.GetRowModel(r);
            var text = col.Column is not null
                ? col.Column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(col.SchemaAccessor.GetValueAsObject(rowModel), culture);

            maxWidth = Math.Max(maxWidth, TerminalTextUtility.GetWidth(text.AsSpan()));

            if (maxWidth >= col.MaxWidth)
            {
                maxWidth = col.MaxWidth;
                break;
            }
        }

        maxWidth = Math.Clamp(maxWidth, Math.Max(1, col.MinWidth), col.MaxWidth);

        if (col.Column is not null)
        {
            col.Column.Width = GridLength.Fixed(maxWidth);
        }
        else
        {
            SetColumnWidthOverride(col.Key, maxWidth);
        }

        HoveredResizeColumnIndex = -1;
    }

    private bool TryBeginColumnResize(IDataGridViewSnapshot snapshot, PointerEventArgs e, Rectangle rect)
    {
        if (!TryHitColumnResizeHandle(snapshot, e.UiX, e.UiY, rect, out var columnIndex))
        {
            return false;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        var cols = EnsureResolvedColumns(snapshot, visibleColumns);
        if ((uint)columnIndex >= (uint)cols.Count || (uint)columnIndex >= (uint)_resolvedColumnWidths.Length)
        {
            return false;
        }

        _resizingColumn = true;
        _resizingColumnIndex = columnIndex;
        _resizeStartUiX = e.UiX;
        _resizeStartWidth = _resolvedColumnWidths[columnIndex];
        HoveredResizeColumnIndex = -1;
        return true;
    }

    private bool TryHitColumnResizeHandle(IDataGridViewSnapshot snapshot, int uiX, int uiY, Rectangle rect, out int columnIndex)
    {
        columnIndex = -1;

        if (!rect.Contains(uiX, uiY))
        {
            return false;
        }

        var anchorWidth = GetEffectiveRowAnchorWidth();
        if (uiX - rect.X < anchorWidth)
        {
            return false;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        if (visibleColumns <= 0)
        {
            return false;
        }

        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);
        var cols = EnsureResolvedColumns(snapshot, visibleColumns);

        for (var c = 0; c < cols.Count && c < _resolvedColumnWidths.Length; c++)
        {
            if (!TryGetColumnResizeHandleRect(snapshot, rect, visibleColumns, frozenColumns, c, out var handleRect))
            {
                continue;
            }

            if (handleRect.Contains(uiX, uiY))
            {
                columnIndex = c;
                return true;
            }
        }

        return false;
    }

    private bool TryHitSortButton(IDataGridViewSnapshot snapshot, int uiX, int uiY, Rectangle rect, out int columnIndex)
    {
        columnIndex = -1;
        if (!CanSort || !ShowHeader || !rect.Contains(uiX, uiY) || uiY != rect.Y)
        {
            return false;
        }

        var visibleColumns = GetVisibleColumnCount(snapshot);
        if (visibleColumns <= 0)
        {
            return false;
        }

        var frozenColumns = Math.Clamp(FrozenColumns, 0, visibleColumns);
        var columns = EnsureResolvedColumns(snapshot, visibleColumns);
        for (var c = 0; c < columns.Count && c < _resolvedColumnWidths.Length; c++)
        {
            var w = _resolvedColumnWidths[c];
            if (w <= 0)
            {
                continue;
            }

            var x = rect.X + GetColumnX(c, rect, frozenColumns);
            var buttonRect = GetSortButtonRect(columns[c], x, rect.Y, w);
            if (buttonRect is { } resolvedButtonRect && resolvedButtonRect.Contains(uiX, uiY))
            {
                columnIndex = c;
                return true;
            }
        }

        return false;
    }

    private bool TryGetColumnResizeHandleRect(IDataGridViewSnapshot snapshot, Rectangle rect, int visibleColumns, int frozenColumns, int visibleColumnIndex, out Rectangle rectHandle)
    {
        _ = snapshot;

        rectHandle = default;
        if ((uint)visibleColumnIndex >= (uint)_resolvedColumnWidths.Length || (uint)visibleColumnIndex >= (uint)_resolvedColumnStarts.Length)
        {
            return false;
        }

        var w = _resolvedColumnWidths[visibleColumnIndex];
        if (w <= 0)
        {
            return false;
        }

        var startUiX = rect.X + GetColumnX(visibleColumnIndex, rect, frozenColumns);
        var boundaryUiX = startUiX + w;

        if (visibleColumnIndex + 1 < visibleColumns && visibleColumnIndex + 1 < _resolvedColumnStarts.Length)
        {
            var nextStartUiX = rect.X + GetColumnX(visibleColumnIndex + 1, rect, frozenColumns);
            var handleWidth = Math.Max(1, nextStartUiX - boundaryUiX);
            rectHandle = new Rectangle(boundaryUiX, rect.Y, handleWidth, rect.Height);
            return true;
        }

        // Trailing handle after the last column: use any extra available space, and fall back to the last visible cell.
        var style = GetStyle<DataGridStyle>();
        var preferred = Math.Max(1, style.ShowVerticalLines ? 1 : style.ColumnSpacing);
        var available = rect.Right - boundaryUiX;
        if (available > 0)
        {
            rectHandle = new Rectangle(boundaryUiX, rect.Y, Math.Min(preferred, available), rect.Height);
            return true;
        }

        if (rect.Width <= 0)
        {
            return false;
        }

        rectHandle = new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height);
        return true;
    }

    private void SelectEntireTable(IDataGridViewSnapshot snapshot)
    {
        if (snapshot.RowCount <= 0 || GetVisibleColumnCount(snapshot) <= 0)
        {
            IsTableSelected = false;
            CurrentCell = DataGridCell.None;
            SelectedRow = -1;
            return;
        }

        IsTableSelected = true;
        SelectedRow = -1;
        if (CurrentCell == DataGridCell.None)
        {
            CurrentCell = new DataGridCell(0, 0);
        }
    }

    private void CopySelectionToClipboard(IDataGridViewSnapshot snapshot)
    {
        if (TryGetSelectionText(snapshot, out var text))
        {
            App?.Terminal.Clipboard.TrySetText(text);
        }
    }

    private bool TryGetSelectionText(IDataGridViewSnapshot snapshot, out string text)
    {
        var cols = EnsureResolvedColumns(snapshot, GetVisibleColumnCount(snapshot));
        if (cols.Count == 0 || snapshot.RowCount <= 0)
        {
            text = string.Empty;
            return false;
        }

        var culture = GetCulture();
        var sb = new StringBuilder();

        if (IsTableSelected)
        {
            AppendRow(cols, rowModel: null, culture, sb, isHeader: true);
            sb.Append('\n');

            for (var r = 0; r < snapshot.RowCount; r++)
            {
                var rowModel = snapshot.GetRowModel(r);
                AppendRow(cols, rowModel, culture, sb, isHeader: false);
                if (r + 1 < snapshot.RowCount)
                {
                    sb.Append('\n');
                }
            }
        }
        else if (SelectedRow >= 0 || SelectionMode == DataGridSelectionMode.Row)
        {
            var rowIndex = SelectedRow >= 0 ? SelectedRow : CurrentCell.Row;
            if ((uint)rowIndex >= (uint)snapshot.RowCount)
            {
                text = string.Empty;
                return false;
            }

            var rowModel = snapshot.GetRowModel(rowIndex);
            AppendRow(cols, rowModel, culture, sb, isHeader: false);
        }
        else
        {
            var cell = CurrentCell;
            if (cell == DataGridCell.None || (uint)cell.Row >= (uint)snapshot.RowCount || (uint)cell.Column >= (uint)cols.Count)
            {
                text = string.Empty;
                return false;
            }

            var rowModel = snapshot.GetRowModel(cell.Row);
            var c = cols[cell.Column];
            var cellText = c.Column is not null
                ? c.Column.FormatValue(this, rowModel, culture)
                : ValueStringFormatter.ToString(c.SchemaAccessor.GetValueAsObject(rowModel), culture);
            sb.Append(cellText);
        }

        if (sb.Length == 0)
        {
            text = string.Empty;
            return false;
        }

        text = sb.ToString();
        return true;
    }

    private void AppendRow(List<ResolvedColumn> cols, object? rowModel, CultureInfo culture, StringBuilder sb, bool isHeader)
    {
        for (var c = 0; c < cols.Count; c++)
        {
            if (c != 0)
            {
                sb.Append('\t');
            }

            if (isHeader)
            {
                sb.Append(cols[c].HeaderText);
                continue;
            }

            var col = cols[c];
            var text = col.Column is not null
                ? col.Column.FormatValue(this, rowModel!, culture)
                : ValueStringFormatter.ToString(col.SchemaAccessor.GetValueAsObject(rowModel!), culture);
            sb.Append(text);
        }
    }

    private sealed record ResolvedColumn(
        string Key,
        BindingAccessor SchemaAccessor,
        Type? SchemaValueType,
        bool SchemaReadOnly,
        string HeaderText,
        Visual? HeaderVisual,
        DataGridColumn? Column,
        int BaseWidth,
        int MinWidth,
        int MaxWidth,
        bool IsStar,
        double StarWeight);

    private sealed record EditableCellContext(
        DataGridCell Cell,
        object RowModel,
        ResolvedColumn ResolvedColumn);

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
