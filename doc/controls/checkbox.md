# CheckBox

`CheckBox` is a two-state toggle control (checked/unchecked) with a label/content.

Screenshot placeholder:

![CheckBox](../../img/screenshots/checkbox.png)

## Basic usage

```csharp
var accepted = new State<bool>(false);

new CheckBox("Accept terms")
    .IsChecked(accepted);
```

## Content & spacing

The label can be a `Visual`, and spacing between glyph and label is controlled by `CheckBoxStyle`.

## Styling

`CheckBoxStyle` controls glyphs, spacing, and colors for normal/hover/focused/disabled states.

