---
title: Markup
---

# Markup

`Markup` renders XenoAtom.Ansi markup text as a visual (with wrapping and styling).

![Markup](../../img/controls/markup.png){.terminal}

## Basic usage

```csharp
new Markup("[bold]Hello[/] [gray]world[/]!");
```

## Markup syntax

See [Markup](../markup.md) for the full markup syntax reference (tags, colors, escaping, semantic tokens).

## Notes

- Markup syntax and parsing are provided by **XenoAtom.Ansi** (`AnsiMarkup`). Terminal.UI converts markup into styled runs and renders them into a `CellBuffer`.
- Use `Markup` when you want inline color and text styles without building a full visual tree.
- Terminal.UI themes also provide semantic tokens such as `[primary]`, `[success]`, `[warning]`, and `[error]`.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Selection and copy

`Markup` supports mouse selection over the rendered plain text:

- drag with the left mouse button to select a range
- double-click to select a word
- `Ctrl+C` copies the active selection

Selection is enabled by default. Set `IsSelectable = false` to opt out.

## Related
- [Markup](../markup.md)
- [XenoAtom.Ansi](../ansi.md)
- [Styling](../styling.md)
- [Markup Specs](../specs/controls/markup.md)
