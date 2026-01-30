# CheckBox

`CheckBox` is a two-state toggle control (checked/unchecked) with a label/content.

## Basic usage

```csharp
var accepted = new State<bool>(false);

new CheckBox("Accept terms")
    .IsChecked(accepted);
```

## Content & spacing

The label can be a `Visual`, and spacing between glyph and label is controlled by `CheckBoxStyle`.

## Interaction

- `Space` / `Enter`: toggle when focused.
- Mouse click: toggle.

## Styling

`CheckBoxStyle` controls glyphs, spacing, and colors for normal/hover/focused/disabled states.

## Related

- `../binding.md`
- `../styling.md`
