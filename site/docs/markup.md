---
title: Markup
---

# Markup

XenoAtom.Terminal and XenoAtom.Terminal.UI support a lightweight markup syntax for styling text.
It is implemented by XenoAtom.Ansi (`AnsiMarkup`) and is used for:

- Terminal output (for example `Terminal.WriteMarkup(...)`)
- The `Markup` control in Terminal.UI
- Parsing markup into styled runs for custom controls (`MarkupTextParser`)

Markup is intentionally small and terminal-friendly: it is closer to "styled text" than to Markdown.

## Quick examples

Basic styling:

```csharp
new Markup("[bold]Hello[/] [gray]world[/]!");
```

Foreground + background:

```csharp
new Markup("[black on yellow]Warning[/] disk is almost full");
```

Nested tags:

```csharp
new Markup("[bold]Title: [red]Error[/][/] details");
```

## Tag syntax

- Tags are written as `[ ... ]` and can be nested.
- Close the most recent tag with `[/]`.
- Multiple tokens can appear in a single tag (space-separated):
  - `[bold yellow on blue]...[/]`

### Escaping

Escape literal brackets by doubling toggle characters:

- `[[` renders a literal `[`
- `]]` renders a literal `]`

If you build markup strings manually, you can also escape user input:

```csharp
using XenoAtom.Ansi;

var safe = AnsiMarkup.Escape(userInput);
```

## Safe interpolation (avoid markup injection)

If you embed user input into markup, you usually want escaping to be automatic.
XenoAtom.Ansi provides interpolated-string handler APIs that escape inserted values.

Terminal.UI exposes safe overloads on `Markup` so this works:

```csharp
var userInput = "[red]not actually red[/]";

// Safe: userInput is escaped automatically by the interpolated handler.
var visual = new Markup($"[primary]{userInput}[/]");
```

> [!IMPORTANT]
> Prefer handler-based interpolation (as above) over manual string concatenation.
> If you build markup strings manually, call `AnsiMarkup.Escape(...)` on user-provided text.

## Supported styles

### Decorations

The following decoration tokens are supported:

- `bold`
- `dim`
- `italic`
- `underline`
- `blink`
- `invert`
- `hidden`
- `strikethrough`

Tokens can be combined in a single tag:

```csharp
new Markup("[bold underline]Emphasis[/]");
```

### Foreground colors

Basic color names:

- `black`, `red`, `green`, `yellow`, `blue`, `magenta`, `cyan`, `white`
- `gray` / `grey`

Bright variants:

- `brightred`, `bright-red`, `bright_red` (variants are accepted)

Web (CSS/SVG/X11) named colors are also supported:

- `cornflowerblue`, `rebeccapurple`, `darkslategray`, ...

Use `web:` to disambiguate when needed:

- `web:gray`, `web:red`
- `web:lightblue` (so it is treated as a web color name instead of a "bright" variant)

### Background colors

Background can be specified using:

- `on <color>` (recommended for readability)
- `bg:<color>`
- `bg=<color>`

Examples:

```csharp
new Markup("[black on yellow]Warning[/]");
new Markup("[bg:blue white]Info[/]");
```

### 256-color indexed palette

Indexed palette entries are supported:

- Foreground: `0..255`
- Background: `bg:0..255` or `bg=0..255`

Example:

```csharp
new Markup("[208]orange-ish[/]");
```

### Truecolor

Truecolor is supported with:

- `#RRGGBB`
- `rgb(r,g,b)`

And background forms:

- `bg:#RRGGBB`
- `bg:rgb(r,g,b)`

Example:

```csharp
new Markup("[#ff8800]orange[/] and [bg:#002244 white]contrast[/]");
```

## Terminal.UI semantic color tags

Terminal.UI themes can provide semantic markup tokens derived from `Theme`:

- `[primary]`, `[success]`, `[warning]`, `[error]`
- `[accent]`, `[muted]`, `[disabled]`

These tokens are created by `Theme.GetMarkupStyles()` and are available to the `Markup` control and to internal markup parsing.

> [!NOTE]
> In Terminal.UI, themes may use RGBA colors and alpha blending. Markup tokens are still colors, but the final appearance can depend on what is behind the text.

## Parsing markup into styled runs (`MarkupTextParser`)

Terminal.UI includes `XenoAtom.Terminal.UI.Text.MarkupTextParser`, which parses markup into:

- plain text (markup stripped)
- `StyledRun[]` spans describing which segments use which `Terminal.UI.Style`

This is useful when you want markup styling without creating a `Markup` visual (for example, inside a custom control that renders many rows efficiently).

Example:

```csharp
using XenoAtom.Terminal.UI.Text;

var parser = new MarkupTextParser();
var text = parser.Parse("[red]Error:[/] Something happened", out var runs);

// text == "Error: Something happened"
// runs contains spans for "Error:" (red) and the rest (default style).
```

Notes:

- `MarkupTextParser` instances are reusable and keep internal buffers to minimize allocations.
- Instances are not thread-safe.

## What markup is not

- It is not Markdown.
- It does not implement hyperlinks as markup tags.
  - Hyperlinks are an ANSI/VT feature (OSC 8) and are emitted via writers, not via `AnsiMarkup` tags.

## Related

- [XenoAtom.Ansi](ansi.md)
- [XenoAtom.Terminal](terminal.md)
- [Markup control](controls/markup.md)
- [Markup specs](specs/controls/markup.md)
