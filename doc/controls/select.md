# Select / Dropdown

`Select<T>` is a compact dropdown control. It opens a popup list and closes when:

- clicking outside
- pressing Tab or Escape

Screenshot placeholder:

![Select](../../img/screenshots/select.png)

## Basic usage

```csharp
var select = new Select<string>()
    .Items(["First", "Second", "Third"]);
```

## Custom item visuals

By default, `Select<T>` renders each item using `value.ToString()` in a `TextBlock`. To render richer content,
set `ItemTemplate`:

```csharp
using XenoAtom.Terminal.UI.Templating;

var placements = new Select<PopupPlacement>()
    .Items([PopupPlacement.Below, PopupPlacement.Above, PopupPlacement.Right, PopupPlacement.Left])
    .ItemTemplate(new DataTemplate<PopupPlacement>(
        Display: static (DataTemplateValue<PopupPlacement> value, in DataTemplateContext _) =>
            new HStack(Symbols.ArrowRight, new TextBlock(() => value.GetValue().ToString())).Spacing(1),
        Editor: null));
```
