# RadioButton

`RadioButton` is a single-choice toggle used in groups.

Screenshot placeholder:

![RadioButton](../../img/screenshots/radiobutton.png)

## Basic usage

Use a shared `State<int>` (or any state) to model a selected option.

```csharp
var choice = new State<int>(0);

new VStack(
    new RadioButton("First").IsChecked(() => choice.Value == 0).Click(() => choice.Value = 0),
    new RadioButton("Second").IsChecked(() => choice.Value == 1).Click(() => choice.Value = 1)
);
```

## Styling

`RadioButtonStyle` controls glyphs and colors.

