# Styling & Themes

XenoAtom.Terminal.UI uses a theme/style model built on ANSI colors and cell styling.

## Theme

`Theme` is a set of style values used as the default environment for visuals.

- Fullscreen apps typically use `Theme.Default` (RGB scheme).
- Inline/live widgets typically use `Theme.Terminal` (uses terminal default colors).

## Styles

Controls obtain their styles from the environment:

```csharp
new Button("OK").Style(new ButtonStyle { Tone = ControlTone.Primary })
```

Styles are records, so variations can be created with `with`:

```csharp
var danger = ButtonStyle.Default with { Tone = ControlTone.Error };
```

## Color schemes

`ColorScheme` represents a 16-color scheme.
Schemes can be:

- terminal-indexed (`Color.Basic16(...)`)
- RGB (`Color.Rgb(...)`)

## Glyphs

Rendering glyphs (borders, scrollbars, etc.) are stored in styles using `Rune` so controls can be re-themed without changing behavior.

See also:

- `doc/controls/button.md`
- `doc/controls/border.md`

