---
title: DataGridControl
---

# DataGridControl

`DataGridControl` is an interactive, virtualized, data-bound table control intended for large datasets and rich interaction:
scrolling, selection, searching/filtering, column resizing, and inline editing.

The lower-level contracts and data model live in [DataGrid Specs](../specs/datagrid_specs.md).

## Quick start

Typical usage is:

- create an `IDataGridDocument` (e.g. `DataGridListDocument<T>` or `DataGridDataTableDocument`),
- wrap it in a view (`DataGridDocumentView`) when you want sorting/filtering/search,
- bind it to `DataGridControl.View`,
- wrap the grid in a `ScrollViewer` to show scrollbars.

> [!TIP]
> You can bind `DataGridControl.Document` directly for the simplest scenario, but using a view is the recommended path
> when you want projection (sort/filter) and when the source can change shape.

## Example

```csharp
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;

public sealed partial class MyRow
{
    [Bindable] public partial int Id { get; set; }
    [Bindable] public partial string Name { get; set; } = string.Empty;
}

var doc = new DataGridListDocument<MyRow>()
    .AddColumn(MyRow.Accessor.Id)
    .AddColumn(MyRow.Accessor.Name);

using var view = new DataGridDocumentView(doc);

var grid = new DataGridControl { View = view, FrozenColumns = 1 };

// Optional: provide typed UI columns to enable typed templates/editors and per-column overrides.
grid.Columns.Add(new DataGridColumn<int>
{
    Key = MyRow.Accessor.Id.Name,
    TypedValueAccessor = MyRow.Accessor.Id,
    Width = GridLength.Auto,
    CellAlignment = TextAlignment.Right,
});

grid.Columns.Add(new DataGridColumn<string>
{
    Key = MyRow.Accessor.Name.Name,
    TypedValueAccessor = MyRow.Accessor.Name,
    Width = GridLength.Star(1),
});

var root = new ScrollViewer(grid);
```

## Documents, views, and schema-driven columns

`DataGridControl` renders columns from the *current snapshot*:

- A document produces a snapshot (`IDataGridDocumentSnapshot`) that describes columns + rows.
- A view (`IDataGridView`) projects a document (filtering/sorting), and exposes `CurrentSnapshot`.
- The control resolves visible columns either from:
  - the schema snapshot (`grid.Columns.Count == 0`), or
  - the UI column collection (`grid.Columns`) if you want per-column customization.

> [!IMPORTANT]
> Schema-only mode supports selection, scrolling, search, filtering, and column resizing.
> Add `grid.Columns` when you need typed templates/editors, custom header visuals, or per-column constraints.

## Column sizing and resizing

Sizing rules are intentionally simple:

- `Auto`: uses header width and a content sample (virtualized).
- `Fixed`: uses the given width.
- `Star`: participates in filling remaining space.

You can resize columns at runtime:

- Drag the separator between columns to set a fixed width.
- Double-click the separator to **auto-size** to the max content width (header + all rows).

> [!CAUTION]
> Auto-size scans the entire column. For very large datasets, prefer `AutoSizeSampleRowCount`-style sizing
> (the default auto sizing) and use auto-size on demand.

## Input

- `Ctrl+F`: open find UI (uses `SearchReplacePopup` in find mode)
- `F3` / `Shift+F3`: next / previous match
- `F4`: toggle filter row (when `View` is filterable)
- Arrow keys / PageUp / PageDown: navigate the current cell
- `F2` or `Enter`: edit current cell (when editable)

## Selection, copy, and clipboard

`DataGridControl` supports:

- cell selection (default) and row selection (via row anchor),
- `Ctrl+A` to select the entire table,
- `Ctrl+C` to copy the current selection.

The copied format is plain text designed to paste into editors/spreadsheets (tab-separated values).

## Editing

Editing is enabled when:

- `ReadOnly == false`, and
- the schema column is not read-only (or the UI column explicitly overrides).

When editing starts, the control chooses an editor:

- `TextBox` for strings (supports selection, scrolling inside the cell, copy/paste, undo/redo),
- `NumberBox` for numeric types,
- boolean and enum columns use type-appropriate editors when a typed UI column is provided.

> [!TIP]
> If you need a custom cell editor or display, provide a typed `DataGridColumn<T>` and use templates.
> See [Data Templating](../data-templating.md).

## Notes

- `DataGridControl` exposes a `ScrollModel` (via `IScrollable`) so `ScrollViewer` can render scrollbars and synchronize offsets.
- For UI columns, `DataGridColumn.Key` should match `DataGridColumnInfo.Key` from the snapshot.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch` 

## Related
- [DataGrid Specs](../specs/datagrid_specs.md)
- [Scrolling](../scrolling.md)
- [Binding](../binding.md)
- [Data Templating](../data-templating.md)
- [ScrollViewer](./scrollviewer.md)
