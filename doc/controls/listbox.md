# ListBox

`ListBox<T>` displays a scrollable list of items and supports selection/focus interaction.

Screenshot placeholder:

![ListBox](../../img/screenshots/listbox.png)

## Items

Items are data values. By default, the list resolves a `DataTemplate<T>` from the environment (`DataTemplates`) to render each item.
You can override this per instance via `ItemTemplate`.

```csharp
new ListBox<string>()
    .Items(["First", "Second"]);
```

## Custom item visuals

```csharp
using XenoAtom.Terminal.UI.Templating;

new ListBox<string>()
    .Items(["First", "Second"])
    .ItemTemplate(new DataTemplate<string>(
        (string value, in DataTemplateContext _) => new HStack(Symbols.ArrowRight, new TextBlock(value)).Spacing(1)));
```

## Styling

`ListBoxStyle` controls selection/hover colors and spacing.

