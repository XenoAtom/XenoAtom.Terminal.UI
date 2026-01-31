---
title: DataGrid (High-Performance Data Table) Control Specs
---

# DataGrid (High-Performance Data Table) Control Specs

This document specifies a new **high-performance tabular data control** for **XenoAtom.Terminal.UI**.

The existing `Table` control is intentionally simple and geared toward *static display*. A separate control is needed for
large datasets and advanced interactions (scrolling, frozen rows/columns, selection, sorting, filtering, and bulk edit).

This control is named **`DataGridControl`** (and related types) to avoid collision/confusion with `System.Data.DataTable`.
The design is inspired by modern terminal UI patterns and spreadsheet/datagrid ecosystems (e.g. Textual’s DataTable).

---

## 1. Goals

- **High performance** with large datasets (10k+ rows) through virtualization and allocation-conscious rendering.
- **Scrollable** in both directions, with optional **frozen** header/rows/columns.
- **Versatile data model**:
  - bind to arbitrary row models (POCO/view-model objects),
  - adapt to `System.Data.DataTable`,
  - support custom “database-like” sources (paged/remote) without loading all rows.
- **Proper mutation API** (document-like) similar in spirit to `ITextDocument`:
  - explicit edit operations,
  - versioning,
  - change notifications,
  - batching.
- **Templating** for display and editing via `DataTemplate<T>` (see [Data Template Specs](data_template_specs.md)).
- **Bulk editing UX**: cell navigation + inline editor, with efficient commit/cancel and validation hooks.

## 2. Non-goals (V1)

- A full spreadsheet formula engine.
- Per-cell arbitrary nested interactive UI as the default rendering path (allowed, but not the fast path).
- Automatic column type inference that guesses wrong; favor explicit columns or adapters.
- Pixel-perfect parity with GUI DataGrids; this is a terminal-first control.

---

## 3. Terminology

- **Document**: the mutable underlying data structure (rows, columns, values) with change notifications.
- **Snapshot**: an immutable view of a document at a specific version (read-only; safe for rendering).
- **View**: a projection of the document (filtering/sorting/row mapping).
- **Cell**: intersection of a row and a column.
- **Row model**: the object instance backing a row; used as the binding owner.
- **Viewport**: the visible rectangle of the control (terminal cells).
- **Frozen**: always visible region (header row, optional top rows / left columns).

---

## 4. Architecture overview

`DataGridControl` is split into three layers:

1) **`IDataGridDocument`** — owns data and mutations (similar to `ITextDocument`).
2) **`IDataGridView`** — provides a sorted/filtered projection over a document (optional but recommended).
3) **`DataGridControl`** — the visual control; handles input, rendering, selection, and editing.

The control MUST be able to operate with:

- a direct document (`Document != null`, `View == null`), or
- an explicit view (`View != null`) that internally references a document.

---

## 5. Data model contracts

### 5.1 `IDataGridDocument` (mutation + versioning)

The document API SHOULD follow the same design principles as `ITextDocument`:

- a **version number** that increments on mutations,
- a **snapshot** for stable reads,
- a **batch update scope** (`BeginUpdate()`),
- a **single change event** carrying enough info to invalidate derived state efficiently.

```csharp
namespace XenoAtom.Terminal.UI.DataGrid;

/// <summary>
/// Represents a mutable tabular document that provides snapshots and change notifications.
/// </summary>
public interface IDataGridDocument
{
    /// <summary>Gets the current snapshot of the document.</summary>
    IDataGridSnapshot CurrentSnapshot { get; }

    /// <summary>Gets the current version number for the document.</summary>
    int Version { get; }

    /// <summary>Begins a batch update scope.</summary>
    IDisposable BeginUpdate();

    /// <summary>
    /// Inserts a row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index at which to insert the row model.</param>
    /// <param name="rowModel">The row model to insert.</param>
    void InsertRow(int rowIndex, object rowModel);

    /// <summary>
    /// Replaces the row model at the specified index.
    /// </summary>
    /// <param name="rowIndex">The index of the row model to replace.</param>
    /// <param name="rowModel">The new row model instance.</param>
    void ReplaceRow(int rowIndex, object rowModel);

    /// <summary>
    /// Removes a range of rows starting at the specified index.
    /// </summary>
    /// <param name="rowIndex">The starting row index.</param>
    /// <param name="count">The number of rows to remove.</param>
    void RemoveRows(int rowIndex, int count);

    /// <summary>
    /// Occurs when the document structure or schema changes (rows/columns/schema/reset).
    /// </summary>
    event EventHandler<DataGridDocumentChangedEventArgs> Changed;
}
```

### 5.2 `IDataGridSnapshot` (stable reads for rendering)

Snapshots MUST be:

- immutable (from the consumer point of view),
- cheap to obtain (`CurrentSnapshot`),
- safe to use without locks from UI rendering code.

```csharp
namespace XenoAtom.Terminal.UI.DataGrid;

public interface IDataGridSnapshot
{
    int Version { get; }

    int RowCount { get; }
    int ColumnCount { get; }

    DataGridColumnInfo GetColumn(int columnIndex);

    /// <summary>Gets the row model object for a row index.</summary>
    object GetRowModel(int rowIndex);
}

public readonly record struct DataGridColumnInfo(
    string Key,
    string HeaderText,
    Type? ValueType,
    bool ReadOnly,
    BindingAccessor Accessor);

public readonly record struct DataGridColumnInfo<T>(
    string Key,
    string HeaderText,
    bool ReadOnly,
    BindingAccessor<T> Accessor);
```

Notes:

- `Key` MUST be stable across versions for a “logical column” (even if the column order changes).
- `HeaderText` is a convenience; `DataGrid` can also accept a header template/visual.
- `ReadOnly` indicates that the underlying data source does not support edits for this column (e.g. no setter, computed
  value, read-only `DataColumn`).
- `Accessor` MUST be provided and is used by `DataGrid` to create cell bindings against the row model.

Ergonomics:

- For bindable row models, the `[Bindable]` source generator SHOULD generate a nested `Accessor` type that exposes
  `BindingAccessor<T>` instances for each bindable property (e.g. `MyRow.Accessor.Name`), including inheritance for
  derived row models.
- `BindingAccessor<T>` MAY convert to `DataGridColumnInfo<T>` (key/header default to `BindingAccessor.Name` and
  read-only defaults to `BindingAccessor.IsReadOnly`) so list-backed documents can be configured concisely.

Row model edits:

- Cell edits SHOULD flow through bindings created from the row model and the column accessor.
- Structural edits (row insertion/removal/replacement) are performed through `IDataGridDocument` methods and MUST raise
  `Changed` events with `Rows` (or `Reset`) so views can invalidate caches.

### 5.3 Change event args

Changes should be communicated as *coarse ranges* and *kinds* to keep invalidation fast.

```csharp
namespace XenoAtom.Terminal.UI.DataGrid;

[Flags]
public enum DataGridChangeKind
{
    None        = 0,
    Rows        = 1 << 0, // rows inserted/removed/replaced
    Columns     = 1 << 1, // columns inserted/removed/moved
    Schema      = 1 << 2, // column metadata changed (header, read-only, accessor, formatting hints)
    Projection  = 1 << 3, // sorting/filtering/search changed
    Reset       = 1 << 4, // large change; drop caches and re-measure
}

public sealed class DataGridDocumentChangedEventArgs : EventArgs
{
    public required int OldVersion { get; init; }
    public required int NewVersion { get; init; }

    public required DataGridChangeKind Kind { get; init; }

    /// <summary>Row range affected, if applicable.</summary>
    public int RowIndex { get; init; } = -1;
    public int RowCount { get; init; }

    /// <summary>Column range affected, if applicable.</summary>
    public int ColumnIndex { get; init; } = -1;
    public int ColumnCount { get; init; }
}
```

Normative guidance:

- Implementations SHOULD use `Reset` when precise ranges are expensive to compute.
- `BeginUpdate()` SHOULD coalesce multiple edits into a single `Changed` event where possible.
- Row model property changes are NOT required to raise `Changed` events. `DataGrid` is expected to read cell values via
  bindings so regular bindable invalidation covers the rendering path.

---

## 6. View model for sorting/filtering/search (V1)

To avoid forcing `DataGrid` to materialize or reorder large datasets itself, projection concerns (sorting, filtering,
searching) SHOULD be delegated to an `IDataGridView` abstraction.

```csharp
namespace XenoAtom.Terminal.UI.DataGrid;

public interface IDataGridView
{
    IDataGridDocument Document { get; }

    /// <summary>Gets the current snapshot for the view projection.</summary>
    IDataGridViewSnapshot CurrentSnapshot { get; }

    event EventHandler<DataGridViewChangedEventArgs> Changed;
}

public interface IDataGridViewSnapshot
{
    int Version { get; }
    int RowCount { get; }
    int ColumnCount { get; }

    DataGridColumnInfo GetColumn(int columnIndex);

    /// <summary>Maps a view row index to a document row index.</summary>
    int MapRowToDocument(int viewRowIndex);

    /// <summary>Gets the row model object for a view row index.</summary>
    object GetRowModel(int viewRowIndex);
}

public sealed class DataGridViewChangedEventArgs : EventArgs
{
    public required int OldVersion { get; init; }
    public required int NewVersion { get; init; }
    public required DataGridChangeKind Kind { get; init; }
}
```

Sorting, filtering, and search API shape (V1):

- `IDataGridView` implementations MAY expose additional capabilities (interfaces) such as:
  - `ISortableDataGridView` (sort descriptions, multi-sort),
  - `IFilterableDataGridView` (column filters / predicate),
  - `ISearchableDataGridView` (find/next/previous, match counts),
  - `IPagedDataGridView` (incremental loading).

`DataGrid` MUST work even when the view does not implement these capabilities; in that case UI affordances are disabled
(or delegated to user-provided commands).

---

## 7. `DataGridControl` control public API

### 7.1 Control type

```csharp
using XenoAtom.Terminal.UI.Scrolling;

namespace XenoAtom.Terminal.UI.Controls;

public sealed partial class DataGridControl : Visual, IScrollable
{
    public DataGridControl();
}
```

### 7.2 Bindable properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Document` | `IDataGridDocument?` | `null` | Direct document binding (optional if `View` is set) |
| `View` | `IDataGridView?` | `null` | Sorted/filtered projection binding |
| `Columns` | `BindableList<DataGridColumn>` | empty | Column definitions (optional; see section 8) |
| `ShowHeader` | `bool` | `true` | Whether to display the header row |
| `FrozenRows` | `int` | `0` | Number of *data* rows frozen at top (in addition to header) |
| `FrozenColumns` | `int` | `0` | Number of columns frozen at left |
| `SelectionMode` | `DataGridSelectionMode` | `Cell` | Cell/row/column selection |
| `ReadOnly` | `bool` | `false` | Disables editing (still allows selection/sort) |
| `CurrentCell` | `DataGridCell` | `(-1,-1)` | Focused cell for navigation and editing |
| `EditMode` | `DataGridEditMode` | `OnEnter` | When editing starts |

The control MUST expose a `ScrollModel` for interoperability with `ScrollViewer`:

```csharp
public ScrollModel Scroll { get; }
```

### 7.3 Selection types

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public enum DataGridSelectionMode
{
    Cell,
    Row,
    Column,
}

public readonly record struct DataGridCell(int Row, int Column)
{
    public static DataGridCell None => new(-1, -1);
}

public enum DataGridEditMode
{
    /// <summary>Editing starts when the user presses Enter/F2 on the current cell.</summary>
    OnEnter,

    /// <summary>Editing starts as soon as the current cell changes.</summary>
    OnCellChange,

    /// <summary>Editing starts when the user types (spreadsheet-style).</summary>
    OnTyping,
}
```

Selection model rules:

- `CurrentCell` MUST always be within bounds when `RowCount/ColumnCount > 0`; otherwise it MUST be `None`.
- In `Row` mode, `CurrentCell.Column` represents the “current column” for horizontal navigation, but selection is row-based.
- In `Column` mode, `CurrentCell.Row` represents the “current row” for vertical navigation, but selection is column-based.

### 7.4 Routed events (suggested)

`DataGrid` SHOULD expose routed events for:

- `SelectionChanged`
- `CurrentCellChanged`
- `SortingChanged`
- `EditBeginning` / `EditCommitted` / `EditCanceled`

Event args should include (at minimum):

- previous/new selection (or deltas),
- current cell,
- sort descriptions,
- whether an edit was canceled by validation.

---

## 8. Columns and templating

### 8.1 `DataGridColumn`

The column object is responsible for:

- header content (text/visual),
- width settings,
- formatting/display,
- editing behavior for that column.

```csharp
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.Controls;

public abstract partial class DataGridColumn : IVisualElement
{
    [Bindable] public string Key { get; set; } = string.Empty;
    [Bindable] public Visual? Header { get; set; }
    [Bindable] public GridLength Width { get; set; } = GridLength.Star(1);
    [Bindable] public int MinWidth { get; set; }
    [Bindable] public int MaxWidth { get; set; } = int.MaxValue;
    [Bindable] public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the text alignment used when the header is rendered as text.
    /// </summary>
    [Bindable] public TextAlignment HeaderAlignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets the default text alignment for cells in this column.
    /// </summary>
    /// <remarks>
    /// This is used by the fast rendering path and as a default when templates render plain text.
    /// Typical defaults: strings = <see cref="TextAlignment.Left"/>, numbers = <see cref="TextAlignment.Right"/>.
    /// </remarks>
    [Bindable] public TextAlignment CellAlignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets a value indicating whether cells in this column are read-only (not editable).
    /// </summary>
    /// <remarks>
    /// This is a UI-level affordance that SHOULD be combined with schema/data-source read-only information
    /// (e.g. <see cref="DataGridColumnInfo.ReadOnly"/>) to compute an effective read-only state.
    /// </remarks>
    [Bindable] public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets the CLR type of the cell values in this column.
    /// </summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// Gets the accessor used to create bindings against a row model for this column.
    /// </summary>
    public abstract BindingAccessor ValueAccessor { get; }
}

/// <summary>
/// A typed column that carries typed templates and a typed accessor to avoid boxing for value types.
/// </summary>
/// <typeparam name="T">The cell value type.</typeparam>
public sealed partial class DataGridColumn<T> : DataGridColumn
{
    /// <summary>
    /// Gets or sets the accessor used to create <see cref="Binding{T}"/> instances for this column.
    /// </summary>
    [Bindable] public BindingAccessor<T> TypedValueAccessor { get; set; } = null!;

    /// <inheritdoc />
    public override Type ValueType => typeof(T);

    /// <inheritdoc />
    public override BindingAccessor ValueAccessor => TypedValueAccessor;

    /// <summary>
    /// Gets or sets the display template used for cells in this column.
    /// When empty (<see cref="DataTemplate{T}.IsEmpty"/>), templates are resolved from <see cref="DataTemplates"/>
    /// in the environment; if none is found, a default text renderer is used.
    /// </summary>
    [Bindable] public DataTemplate<T> CellTemplate { get; set; }

    /// <summary>
    /// Gets or sets the display template used for read-only cells in this column.
    /// </summary>
    /// <remarks>
    /// When empty, the column falls back to <see cref="CellTemplate"/>.
    /// This template is intended to render values differently when the underlying data is not editable (e.g. dim text,
    /// lock glyph, computed value styling) without requiring the control to enter edit mode.
    /// </remarks>
    [Bindable] public DataTemplate<T> ReadOnlyCellTemplate { get; set; }

    /// <summary>
    /// Gets or sets the editor template used when a cell enters edit mode.
    /// When empty (<see cref="DataTemplate{T}.IsEmpty"/>), templates are resolved from <see cref="DataTemplates"/>
    /// in the environment; if none is found, DataGrid uses built-in editors for common types.
    /// </summary>
    [Bindable] public DataTemplate<T> CellEditorTemplate { get; set; }
}
```

Notes:

- `Key` MUST match a column key coming from the snapshot when binding to schema-driven sources (e.g. `DataTable`).
- For POCO/view-model sources, `Key` MAY be arbitrary and the adapter maps it to getters/setters.
- Columns SHOULD be typed (`DataGridColumn<T>`) whenever possible to avoid boxing (especially for value types).
  `DataGridColumn<object?>` is the escape hatch for unknown types.
- `DataGrid` SHOULD create cell bindings using the row model from the snapshot and the column accessor (typically
  `DataGridColumn<T>.TypedValueAccessor` or <see cref="DataGridColumnInfo.ValueAccessor"/> when auto-generating columns), then pass:
  - a `DataTemplateValue<T>` to display templates, and
  - a `Binding<T>` to editor templates.
- Read-only behavior:
  - A cell is read-only when any of the following is true:
    - `DataGrid.ReadOnly` is `true`
    - `DataGridColumn.ReadOnly` is `true`
    - the underlying schema marks the column as read-only (`DataGridColumnInfo.ReadOnly == true`)
  - `CellEditorTemplate` MUST NOT be used for read-only cells.
  - For read-only cells, `DataGrid` MUST use `ReadOnlyCellTemplate` when it is not empty; otherwise it MUST use
    `CellTemplate` (and then environment/default fallbacks as usual).

### 8.2 Data template context for cells

To reuse the existing `DataTemplate<T>` contract (see [Data Template Specs](data_template_specs.md)), `DataGrid` SHOULD use
`DataTemplateContext` as follows when invoking `CellTemplate` / `CellEditorTemplate`:

- `Owner` = the `DataGrid` instance
- `Role` = `Display` or `Editor`
- `Index` = the **row index** (in view coordinates when `View` is set)
- `State` includes `Selected/Focused/Hovered/Disabled` as appropriate

The column identity is known by the `DataGridColumn` that owns the template, so templates can be column-specific without
needing a `(row, column)` pair in the context.

---

## 9. Scrolling + frozen panes

### 9.1 Scrolling model

`DataGrid` MUST be vertically and horizontally scrollable and MUST express its scroll state through its `ScrollModel`:

- `Scroll.OffsetY` = first visible data row index in the scrollable region
- `Scroll.OffsetX` = horizontal cell offset in columns *in cell units* (not column indices)

Implementation guidance (binding-driven invalidation):

- `DataGrid` MUST NOT rely on manual invalidation APIs.
- Because `ScrollModel` can be changed externally (e.g. `ScrollViewer` dragging scroll bars), `DataGrid` SHOULD bind its
  layout/rendering to `ScrollModel.Version` using the established `ScrollVersion` pattern used by other `IScrollable`
  controls (e.g. `TreeView`):

  - store a private bindable `ScrollVersion` on the control,
  - update it during `PrepareChildren()` (`ScrollVersion = Scroll.Version;`),
  - read it in `ArrangeCore` and `RenderOverride` (`_ = ScrollVersion;`).

This ensures arrange/render reruns when offsets/extents change, without reintroducing dependency loops.

Extent computation:

- `ExtentHeight` MUST be `HeaderHeight + FrozenRows + RowCount` (in terminal rows), plus optional separators.
- `ExtentWidth` MUST be the sum of resolved column widths plus optional separators/borders.

### 9.2 Frozen regions

`DataGrid` MUST support:

- header row frozen when `ShowHeader = true`,
- `FrozenRows` data rows frozen below the header,
- `FrozenColumns` columns frozen at the left.

Frozen regions MUST remain visible while scrolling the remaining region.

Implementation guidance:

- Treat the view as up to 4 regions (top-left, top, left, body) and render them with independent offsets.
- Borders/grid lines MUST align across regions.

---

## 10. Virtualization and recycling (performance-critical)

### 10.1 Row/column virtualization

The control MUST NOT allocate or measure visuals for off-screen rows/cells by default.

Minimum requirement:

- Only the **currently visible** rows (plus a small overscan, e.g. 1–2) may have realized cell visuals.

Stronger requirement (recommended):

- Virtualize both dimensions: only visible rows AND visible columns may have realized cell visuals.

### 10.2 Fast rendering path

To keep performance predictable for large datasets, the default display path SHOULD be “render text directly”:

- For common scalar values (`string`, numbers, dates, enums, bool), render via formatter into `CellBuffer`.
- Avoid allocating a `Visual` per cell when a column uses default formatting.
- Apply column alignment (`DataGridColumn.CellAlignment`) when writing formatted values.
- Even in this fast path, value reads SHOULD go through bindings (created from the row model + accessor) so updates to
  row model properties automatically invalidate render without requiring explicit “cell changed” events.

Cell templating (Visual-based) SHOULD still be supported, but it is not the universal fast path.

### 10.3 Recycling pools

For templated cells/headers/editors, `DataGrid` SHOULD maintain recycle pools keyed by:

- template identity,
- role (Display vs Editor),
- and (optionally) column key.

This mirrors the `DataTemplate<T>.TryUpdate(...)` pattern from [Data Template Specs](data_template_specs.md).

---

## 11. Selection and navigation

### 11.1 Keyboard navigation (default bindings)

Suggested bindings (exact gestures may evolve):

- Arrow keys: move `CurrentCell`
- `PageUp`/`PageDown`: move by viewport height
- `Home`/`End`: move within row (left/right) or within column (top/bottom)
- `Ctrl+Home`/`Ctrl+End`: first/last cell
- `Shift+Arrow` / `Shift+Page*`: extend selection (range)

`DataGrid` MUST keep the current cell visible by calling `Scroll.ScrollToMakeVisible(...)` as navigation occurs.

### 11.2 Selection ranges

Selection SHOULD be representable without materializing all selected indices (important for large ranges).

Recommended representation:

- a primary range (anchor + active),
- optional additional ranges (multi-select),
- support for row/column ranges.

### 11.3 Mouse support (when available)

- Click selects cell/row/column based on `SelectionMode`
- Drag selects a range (optional V1)
- Wheel scrolls vertically; `Shift+Wheel` scrolls horizontally (optional V1)
- Drag the **column resize handle** (the spacing between columns) to resize the left column.
  - Resize handles SHOULD be active across the full height of the grid (header, filter row, and body), not only the header.
  - The last column SHOULD also be resizable via a trailing resize handle after the last column.
  - Hovering a resize handle SHOULD show a distinct hover style to make the affordance discoverable.

### 11.4 Commands (discoverability)

`DataGridControl` SHOULD register UI commands for key gestures it supports so they are discoverable via `CommandBar`.
Suggested built-in commands:

- `DataGrid.Find` (`Ctrl+F`)
- `DataGrid.ToggleFilterRow` (`F4`)
- `DataGrid.NextMatch` (`F3`) / `DataGrid.PreviousMatch` (`Shift+F3`)
- `DataGrid.SelectAll` (`Ctrl+A`)
- `DataGrid.Copy` (`Ctrl+C`)
- `DataGrid.GoToStart` (`Ctrl+Home`) / `DataGrid.GoToEnd` (`Ctrl+End`)
- `DataGrid.EditCell` (`F2`)

---

## 12. Sorting, filtering, and search

Sorting, filtering, and searching are part of V1.

The projection state SHOULD live in the view layer so large datasets can be handled without `DataGrid` materializing or
reordering row models.

Suggested capability interfaces (exact API may evolve):

```csharp
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.DataGrid;

public enum DataGridSortDirection
{
    Ascending,
    Descending,
}

public readonly record struct DataGridSortDescription(string ColumnKey, DataGridSortDirection Direction);

public interface ISortableDataGridView : IDataGridView
{
    IReadOnlyList<DataGridSortDescription> SortDescriptions { get; }
    void SetSortDescriptions(IReadOnlyList<DataGridSortDescription> sortDescriptions);
}

public readonly record struct DataGridFilterDescription(string ColumnKey, string? Text);

public interface IFilterableDataGridView : IDataGridView
{
    IReadOnlyList<DataGridFilterDescription> Filters { get; }
    void SetFilters(IReadOnlyList<DataGridFilterDescription> filters);
}

public interface ISearchableDataGridView : IDataGridView
{
    SearchQuery SearchQuery { get; }
    void SetSearchQuery(in SearchQuery query);
}
```

Views MUST raise `Changed` with `Kind = DataGridChangeKind.Projection` when these settings change.

### 12.1 Sorting

Sorting MUST be expressed through the view layer when possible:

- If `View` implements sortable capabilities, `DataGrid` toggles sort on header interaction.
- Otherwise, sorting UI is disabled (or a user-provided command is invoked).

Suggested UX:

- click header toggles `None → Asc → Desc → None`
- `Shift+click` adds/removes secondary sort (multi-sort)

### 12.2 Filtering

Filtering is similarly delegated to the view layer.

`DataGrid` MUST provide:

- a built-in filter editing UI (at minimum: per-column text filters),
- rendering hints (filter icon/marker in header),
- keyboard shortcuts to open/close the filter UI.

Suggested UX (V1):

- `F4`: toggle filter row (one `TextBox` per visible column)
- typing in a filter box updates filters immediately (no explicit Apply button)
- `Escape`: clears the current filter box (or closes the filter row if empty)

Filtering semantics (default view implementation):

- a filter matches when the formatted cell text contains the filter text (case-insensitive),
- per-column filters are AND-ed together,
- a global “quick filter” MAY be supported (matches any visible column).

### 12.3 Searching (find)

Searching is distinct from filtering: it navigates to matching cells without changing the row set.

`DataGrid` SHOULD integrate with the existing `SearchReplacePopup` infrastructure:

- `Ctrl+F` opens a `SearchReplacePopup` in `Find` mode (replace disabled)
- `F3` / `Shift+F3` navigate next/previous match
- navigating to a match MUST update `CurrentCell` and call `Scroll.ScrollToMakeVisible(...)`
- matches MAY be highlighted in the viewport (style-driven)
- closing the search popup SHOULD clear match highlighting (while keeping the popup input fields for the next open)

For large datasets, `DataGrid` MAY delegate match discovery to a view implementing `ISearchableDataGridView`.

---

## 13. Editing model (bulk edit)

### 13.1 Edit lifecycle

`DataGrid` MUST support a single active editor at a time (spreadsheet-style) for performance:

- Enter edit mode on `Enter` / `F2` / typing (based on `EditMode`)
- Host an editor visual over the cell (or within the cell region)
- Commit on `Enter` (optionally `Tab` to next cell)
- Cancel on `Escape`

Read-only rule:

- `DataGrid` MUST NOT enter edit mode for a read-only cell (see section 8.1).

### 13.2 Validation hooks

Validation SHOULD be supported via:

- column-provided validation delegates, and/or
- integration with existing validation presenters if available.

On validation failure:

- the edit MUST remain active,
- the control SHOULD show an inline validation hint (style-driven).

### 13.3 Typed editing

For common types, the control SHOULD provide default editors:

- `string` → `TextBox`
- numeric → `NumberBox`
- `bool` → `Switch`
- enums → `Select<T>` / a simple `TextBox` editor with `Enum.TryParse` fallback

For custom types, `CellEditorTemplate` is used.

---

## 14. Adapters and integrations

### 14.1 `System.Data.DataTable`

Provide an adapter (name illustrative):

- `DataGridDataTableDocument : IDataGridDocument`

Requirements:

- columns map to `DataColumn` (key = `ColumnName` or stable identifier),
- rows map to `DataRow`,
- edits write back to the underlying `DataTable`,
- schema/row changes raised by `DataTable` MUST be translated to `Changed` events (coalesced when possible).
  Value changes SHOULD be reflected through the row model binding surface (preferred) or by raising `Reset` when
  fine-grained invalidation is not practical.

### 14.2 Items source / view-model lists

Provide an adapter for `IReadOnlyList<T>` / `BindableList<T>` (name illustrative):

- `DataGridListDocument<T> : IDataGridDocument`

Columns may be described by:

- `DataGridColumnInfo` / `DataGridColumnInfo<TValue>` in the document/view snapshot, backed by `BindingAccessor` / `BindingAccessor<TValue>`.
- Optional `DataGridColumn<TValue>` UI columns that map by `Key` to a `DataGridColumnInfo` from the snapshot (and provide templates/editors).

This supports “bulk edit a list of view models with bindable properties”.

Example (bindable row model + generated accessors):

```csharp
public sealed partial class SwimRow
{
    [Bindable] public partial int Lane { get; set; }
    [Bindable] public partial string Swimmer { get; set; } = string.Empty;
    [Bindable] public partial string Country { get; set; } = string.Empty;
    [Bindable] public partial double Time { get; set; }
}

var doc = new DataGridListDocument<SwimRow>();
using (doc.BeginUpdate())
{
    doc
        .AddColumn(SwimRow.Accessor.Lane)
        .AddColumn(SwimRow.Accessor.Swimmer)
        .AddColumn(SwimRow.Accessor.Country)
        .AddColumn(SwimRow.Accessor.Time);
}
```

---

## 15. Styling

Introduce a style record similar to `TableStyle`, `ListBoxStyle`, etc.:

- `DataGridStyle : IStyle<DataGridStyle>`

Suggested fields:

- grid lines on/off (vertical column separators, optional header separator),
- header background/foreground style,
- cell style,
- selection styles (focused/unfocused),
- match highlight style (for search),
- optional frozen region separator style,
- optional glyph set (`LineGlyphs`) when grid lines are enabled.

Default appearance (V1):

- **compact/dense** (no outer border)
- **no grid lines** (no vertical separators, no header underline)
- header is visually separated by **background color** and/or bold text, not by borders
- selection uses a strong background highlight and remains readable in both focused/unfocused states

This is closer to Textual’s DataTable than `TableStyle.Grid`.

---

## 16. Relationship to existing `Table`

- `Table` remains the lightweight “static display” control.
- `DataGrid` is the “interactive, virtualized, data-bound” control.
- A convenience helper MAY exist to render an `IDataGridSnapshot` into a `Table` for scenarios that do not need
  selection/editing (e.g. export/print).
