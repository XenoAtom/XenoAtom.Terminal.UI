# Select / Dropdown

`Select<T>` is a compact dropdown control. It opens a popup list and closes when:

- clicking outside
- pressing Tab or Escape

Screenshot placeholder:

![Select](../../img/screenshots/select.png)

## Basic usage

```csharp
var select = new Select<string>();
select.Items.AddRange("First", "Second", "Third");
```

## Custom item visuals

By default, `Select<T>` renders each item using `value.ToString()` in a `TextBlock`. To render richer content,
set `ContentFactory`:

```csharp
var placements = new Select<PopupPlacement>()
    .ContentFactory(p => new HStack(Symbols.ArrowRight, new TextBlock(p.ToString())).Spacing(1));
placements.Items.AddRange(PopupPlacement.Below, PopupPlacement.Above, PopupPlacement.Right, PopupPlacement.Left);
```
