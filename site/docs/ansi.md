---
title: XenoAtom.Ansi
---

# XenoAtom.Ansi

XenoAtom.Ansi is the low-level ANSI/VT library that powers markup and styling in XenoAtom.Terminal and
XenoAtom.Terminal.UI.

It provides:

- ANSI/VT sequence emission (SGR styles, some cursor/screen operations)
- Lightweight **markup** rendering (Spectre.Console-like tags)
- ANSI parsing/tokenization and styled runs
- ANSI-aware text utilities (strip/measure/wrap/truncate)

## Markup (the syntax used by Terminal.UI)

Terminal.UI uses XenoAtom.Ansi markup syntax in multiple places:

- the [`Markup`](controls/markup.md) control
- prompt help strings (e.g. `HelpMarkup(...)`)
- internal markup parsing utilities (`MarkupTextParser`)

Example:

```csharp
new Markup("[bold yellow]Warning:[/] disk is almost full");
```

Markup is designed to be safe with interpolated values: user input is escaped so it can’t inject tags when you use the
interpolated handler APIs. When you build markup manually, you can escape with:

```csharp
using XenoAtom.Ansi;

var safe = AnsiMarkup.Escape(userInput);
```

## How Terminal.UI turns markup into UI styles

Terminal/UI styling is not “raw ANSI”. Terminal.UI parses markup into:

- plain text
- a list of styled runs (`StyledRun[]`) that reference `Terminal.UI.Style`

See:

- [Markup Parsing](markup-parsing.md) (`MarkupTextParser`)

## ANSI emission (AnsiWriter)

If you are using XenoAtom.Terminal directly (without Terminal.UI), XenoAtom.Ansi provides `AnsiWriter` for producing
escape sequences efficiently:

```csharp
using XenoAtom.Ansi;

using var b = new AnsiBuilder();
var w = new AnsiWriter(b);
w.Foreground(AnsiColors.BrightYellow).Decorate(AnsiDecorations.Bold).Write("Warning").ResetStyle();
```

> [!NOTE]
> Terminal.UI does not render “raw ANSI”. It renders a `CellBuffer` and then writes ANSI via the host.
> `AnsiWriter` is most useful when you are building a CLI/TUI directly on XenoAtom.Terminal.

## Relationship to Terminal and Terminal.UI

- XenoAtom.Terminal uses XenoAtom.Ansi for styles, markup, and output helpers.
- XenoAtom.Terminal.UI uses XenoAtom.Ansi for markup parsing and styled runs, and then renders to a `CellBuffer`.

See:

- [Ecosystem & Foundations](foundations.md)
- [XenoAtom.Terminal](terminal.md)
