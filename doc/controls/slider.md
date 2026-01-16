# Slider

`Slider` selects a value in a range and supports mouse/keyboard interaction.

Screenshot placeholder:

![Slider](../../img/screenshots/slider.png)

## Basic usage

```csharp
var value = new State<int>(25);
new Slider().Minimum(0).Maximum(100).Value(value);
```

## Styling

`SliderStyle` controls track/thumb glyphs and colors.

