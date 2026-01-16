# Grid

`Grid` arranges children into rows and columns using explicit `GridCell` entries (no attached properties).

Screenshot placeholder:

![Grid](../../img/screenshots/grid.png)

## Basic usage

```csharp
new Grid()
    .Columns(new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star))
    .Rows(new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto))
    .Cell("Name:", row: 0, column: 0)
    .Cell(new TextBox("Alex"), row: 0, column: 1);
```

## Spans

Cells can span multiple rows/columns via `rowSpan` / `columnSpan`.

