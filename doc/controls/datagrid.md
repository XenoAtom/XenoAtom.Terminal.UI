# DataGrid

`DataGrid` is an interactive, virtualized, data-bound table control intended for large datasets and rich interaction
(scrolling, selection, filtering/search, and inline editing).

The lower-level contracts live in `doc/specs/datagrid_specs.md`.

## Quick start

Typical usage is:

- create an `IDataGridDocument` (e.g. `DataGridListDocument` or `DataGridDataTableDocument`),
- wrap it in a view (`DataGridDocumentView`) when you want sorting/filtering/search,
- bind it to `DataGrid.View`,
- wrap the grid in a `ScrollViewer` to show scrollbars.

## Example

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using DataGridControl = XenoAtom.Terminal.UI.Controls.DataGrid;

var doc = new DataGridListDocument();
// doc.SetColumns(...) and doc.AddRow(...) omitted for brevity.

using var view = new DataGridDocumentView(doc);

var grid = new DataGridControl { View = view, FrozenColumns = 1 };

// Provide typed UI columns to enable typed templates/editors.
// grid.Columns.Add(new DataGridColumn<string> { Key = "...", TypedAccessor = ... });

var root = new ScrollViewer(grid);
```

## Input

- `Ctrl+F`: open find UI (uses `SearchReplacePopup` in find mode)
- `F3` / `Shift+F3`: next / previous match
- `Ctrl+Shift+F`: toggle filter row (when `View` is filterable)
- Arrow keys / PageUp / PageDown: navigate the current cell
- `F2` or `Enter`: edit current cell (when editable)

## Notes

- `DataGrid` exposes a `ScrollModel` (via `IScrollable`) so `ScrollViewer` can render scrollbars and synchronize offsets.
- For schema-driven sources, `DataGridColumn.Key` should match `DataGridColumnInfo.Key` from the snapshot.
