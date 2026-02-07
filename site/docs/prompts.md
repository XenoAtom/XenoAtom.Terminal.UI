---
title: Prompts (Inline)
---

# Prompts (Inline)

XenoAtom.Terminal.UI provides a small set of inline prompts built on top of the regular controls and the hosting
infrastructure (`Terminal.Live`).

Prompts are intended for *inline* scenarios (live regions). For fullscreen applications, prefer dialogs/popups inside
`Terminal.Run`.

## Basic usage

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var name = Terminal.Ask("Name:", p => p.Placeholder("Type your name…"));

Terminal.WriteLine($"Hello {name}!");
```

## Number prompt

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var port = Terminal.AskNumber<int>(
    "Port:",
    p => p
        .Default(8080)
        .Validate(v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]"));
```

## Selection prompt

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var color = Terminal.Prompt(new SelectionPrompt<string>("Pick a color:")
    .Items(["Red", "Green", "Blue"]));
```

## Cancellation

Prompts can be canceled with `Esc`. The prompt methods throw `OperationCanceledException` when canceled.

## Prompt templates

Each prompt composes a small visual tree from:
- `Message` (the question)
- the editor control (TextBox/NumberBox/Select/etc.)
- optional `Help`

This composition is controlled by `TerminalPrompt.PromptTemplate`.

### Built-in templates

`TerminalPromptTemplates` provides a few built-in options:
- `TerminalPromptTemplates.Compact` (default): message on the left, editor on the right, help below if present.
- `TerminalPromptTemplates.CompactRoundedGroup` / `CompactSquareGroup`: compact layout wrapped in a group.
- `TerminalPromptTemplates.VerboseRoundedGroup` / `VerboseSquareGroup`: a more verbose layout wrapped in a group.

You can switch templates via fluent helpers:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var name = Terminal.Ask("Name:", p => p.StyleCompact());

var port = Terminal.AskNumber<int>("Port:", p => p.StyleInGroup().Default(8080));
```

Or provide a custom template:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Prompts;

var answer = Terminal.Ask("Answer:", p =>
{
    p.PromptTemplate((prompt, editor) => new VStack(prompt.Message, editor).Spacing(1));
});
```

## Markup help

To set help text using ANSI markup, use `HelpMarkup(...)`:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var name = Terminal.Ask("Name:", p => p
    .Placeholder("Type your name...")
    .HelpMarkup("[dim]Press Enter to accept the default.[/]"));
```
