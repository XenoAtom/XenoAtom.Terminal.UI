# Switch

`Switch` is a compact on/off toggle with a thumb and label.

## Basic usage

```csharp
var enabled = new State<bool>(true);
new Switch("Enabled").IsOn(enabled);
```

## Styling

`SwitchStyle` controls:

- left/right segment colors
- thumb glyph
- background/foreground for normal/hover/pressed/disabled

## Interaction

- `Space` / `Enter`: toggle when focused.
- Mouse click: toggle.

## Related

- `../binding.md`
- `../styling.md`
