# Prompts (Inline)

XenoAtom.Terminal.UI provides a small set of inline prompts built on top of the regular controls and the hosting
infrastructure (`Terminal.Live`).

Prompts are intended for *inline* scenarios (live regions). For fullscreen applications, prefer dialogs/popups inside
`Terminal.Run`.

> Screenshots: `docs/images/prompts/text.png` (placeholder)

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
