# SelectionList

`SelectionList<T>` is a multi-select list widget (checkbox-style selection in-layout).

Screenshot placeholder:

![SelectionList](../../img/screenshots/selectionlist.png)

## Basic usage

```csharp
new SelectionList<string>()
    .AddItem("First")
    .AddItem("Second", isChecked: true);
```

## Styling

`SelectionListStyle` controls glyphs, spacing, and selection visuals.
