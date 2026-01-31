---
title: Markup Parsing (MarkupTextParser)
---

# Markup Parsing (MarkupTextParser)

XenoAtom.Terminal.UI includes a lightweight parser for ANSI markup:
`XenoAtom.Terminal.UI.Text.MarkupTextParser`.

This is useful when you want to:

- Accept markup input.
- Convert it to plain text.
- Keep the styling information as spans (runs) that can be rendered efficiently.

Unlike the `Markup` control, `MarkupTextParser` is intended for reuse by custom controls that want to avoid creating
one visual per line or per span.

## API overview

`MarkupTextParser.Parse()` returns:

- The plain text (markup stripped).
- A `StyledRun[]` describing which parts of the plain text use which `Style`.

```csharp
using XenoAtom.Terminal.UI.Text;

var parser = new MarkupTextParser();
var text = parser.Parse("[red]Error:[/] Something happened", out var runs);

// text == "Error: Something happened"
// runs contains spans for "Error:" (red) and the rest (default style).
```

## Rendering strategy (custom controls)

The typical rendering pattern is:

1. Parse the markup into plain text + runs.
2. Render the plain text into a `CellBuffer`.
3. Apply the `Style` from each `StyledRun` while drawing the corresponding range.

> Screenshot: `site/img/markuptextparser.png` (placeholder)

## Notes

- `MarkupTextParser` instances are reusable and keep internal buffers to minimize allocations.
- Instances are not thread-safe.

