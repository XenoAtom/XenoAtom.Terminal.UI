---
title: TextFiglet
---

# TextFiglet

`TextFiglet` renders large banner text using a FIGlet font.


![TextFiglet](../../img/controls/textfiglet.svg){.terminal}

## Basic usage

```csharp
new TextFiglet("Hello")
{
    Font = FigletFont.Block,
};
```

## Fonts

FIGlet fonts are represented by `FigletFont` (namespace `XenoAtom.Terminal.UI.Figlet`).

- Use `FigletFont.Block` for a built-in demo font.
- Use embedded fonts from `FigletPredefinedFont` (e.g. `FigletPredefinedFont.Standard`, `FigletPredefinedFont.Slant`).
- Load a `.flf` font from a stream with `FigletFont.Load(...)`.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Styling
Use `TextFigletStyle` to change the foreground/background and decorations:

```csharp
new TextFiglet("XenoAtom")
    .Style(TextFigletStyle.Default with { TextStyle = CellStyle.None | TextStyle.Bold });
```

## Related

- [TextFiglet Specs](../specs/controls/textfiglet.md)
