# Table

`Table` displays a grid of cells. Cells and headers are visuals for full composability.

Screenshot placeholder:

![Table](../../img/screenshots/table.png)

## Basic usage

```csharp
new Table()
    .Headers("Task", "Status")
    .AddRow("Download", "Running")
    .AddRow("Render", "OK");
```

## Styling

`TableStyle` controls border glyphs, header style, separators, and line visibility.

