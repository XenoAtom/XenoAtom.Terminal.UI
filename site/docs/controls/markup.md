---
title: Markup
---

# Markup

`Markup` renders XenoAtom.Ansi markup text as a visual (with wrapping and styling).


![Markup](../../img/controls/markup.svg){.terminal}

## Basic usage

```csharp
new Markup("[bold]Hello[/] [gray]world[/]!");
```

## Notes

- Markup syntax and parsing are provided by **XenoAtom.Ansi** (`AnsiMarkup`). Terminal.UI converts markup into styled runs and renders them into a `CellBuffer`.
- Use `Markup` when you want inline color and text styles without building a full visual tree.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Related
- [Markup Parsing](../markup-parsing.md)
- [XenoAtom.Ansi](../ansi.md)
- [Styling](../styling.md)
- [Markup Specs](../specs/controls/markup.md)
