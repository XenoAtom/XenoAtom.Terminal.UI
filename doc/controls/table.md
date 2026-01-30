# Table

`Table` displays a grid of cells. Cells and headers are visuals for full composability.

## Basic usage

```csharp
new Table()
    .Headers("Task", "Status")
    .AddRow("Download", "Running")
    .AddRow("Render", "OK");
```

## Styling

`TableStyle` controls border glyphs, header style, separators, and line visibility.

## Notes

- `Table` is best for relatively small datasets where you want rich per-cell visuals.
- For very large datasets, prefer `DataGridControl` (virtualized, scrollable, selection/search/editing).

## Related

- `./datagrid.md`
- `../layout.md`
- `../styling.md`
