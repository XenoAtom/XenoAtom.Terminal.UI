# Switch

`Switch` is a compact on/off toggle with a thumb and label.

Screenshot placeholder:

![Switch](../../img/screenshots/switch.png)

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

