# Slider

`Slider` selects a value in a range and supports mouse/keyboard interaction.

## Basic usage

```csharp
var value = new State<int>(25);
new Slider().Minimum(0).Maximum(100).Value(value);
```

## Interaction

- Arrow keys adjust the value by a small step.
- PageUp/PageDown adjust by a larger step.
- Mouse click/drag moves the thumb.

## Styling

`SliderStyle` controls track/thumb glyphs and colors.

## Related

- `../binding.md`
- `../styling.md`
