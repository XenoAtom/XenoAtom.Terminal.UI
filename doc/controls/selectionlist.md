# SelectionList

`SelectionList` is a multi-select list widget (checkbox-style selection in-layout).

Screenshot placeholder:

![SelectionList](../../img/screenshots/selectionlist.png)

## Basic usage

```csharp
var selected = new State<int>(0);

new SelectionList()
    .Items.Add(
        new SelectionListItem("First"),
        new SelectionListItem("Second")
    );
```

## Styling

`SelectionListStyle` controls glyphs, spacing, and selection visuals.

