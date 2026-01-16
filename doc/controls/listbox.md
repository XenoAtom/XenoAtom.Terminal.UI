# ListBox

`ListBox` displays a scrollable list of visuals and supports selection/focus interaction.

Screenshot placeholder:

![ListBox](../../img/screenshots/listbox.png)

## Items

Items are visuals (not strings) to allow full composition.

```csharp
new ListBox().Items.Add(
    new HStack("•", "First"),
    new HStack("•", "Second")
);
```

## Styling

`ListBoxStyle` controls selection/hover colors and spacing.

