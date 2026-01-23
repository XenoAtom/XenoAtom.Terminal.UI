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

var name = TerminalPrompts.Prompt(new TextPrompt("Name:")
{
    Placeholder = "Type your name…",
});

Terminal.WriteLine($"Hello {name}!");
```

## Number prompt

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var port = TerminalPrompts.Prompt(new NumberPrompt<int>("Port:")
{
    InitialValue = 8080,
    Validator = v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]",
});
```

## Selection prompt

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Prompts;

var color = TerminalPrompts.Prompt(new SelectionPrompt<string>("Pick a color:")
{
    Items = ["Red", "Green", "Blue"],
});
```

## Cancellation

Prompts can be canceled with `Esc`. The prompt methods throw `OperationCanceledException` when canceled.

