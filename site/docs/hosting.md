---
title: "Hosting & Terminal Integration"
---

# Hosting & Terminal Integration

XenoAtom.Terminal.UI is designed to feel integrated with `XenoAtom.Terminal`:

- Use `Terminal.Write(Visual)` to render a visual once.
- Use `Terminal.Live(Visual, Func<TerminalLoopResult>)` for inline live regions.
- Use `Terminal.Run(Visual, Func<TerminalLoopResult>)` for fullscreen applications.

All of these APIs are exposed via C# 14 extension members in `src/XenoAtom.Terminal.UI/TerminalExtensions.cs`.

## Inline: `Terminal.Write`

`Terminal.Write(visual)` measures, arranges, renders, and writes the final output once.

This is useful for:

- Tables
- One-shot widgets (progress snapshot, summaries)
- Rich markup blocks (see `Markup`)

## Inline live: `Terminal.Live`

`Terminal.Live(visual, onUpdate)` repeatedly:

1. Positions the cursor at the live region anchor.
2. Calls `onUpdate`.
3. Renders the visual and updates the live region.

The update callback returns a `TerminalLoopResult`:

- `Continue`: keep running.
- `Stop`: stop and remove the live region (cursor restored to where it was before the live region).
- `StopAndKeepVisual`: stop and keep the final frame (cursor placed after the live region).

You can also use the overload that receives a `TerminalRunningContext` to access the host kind and terminal instance.

### Mouse input (default: off)

By default, inline live regions do **not** enable terminal mouse reporting. This preserves the terminal emulator's
default mouse behavior (for example: selecting/copying text with the mouse).

If you want mouse interactions (hover, clicks, scroll wheel) for controls hosted in `Terminal.Live`, enable it:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

Terminal.Live(
    new VStack(new Button("Click me")).Padding(1),
    onUpdate: () => TerminalLoopResult.Continue,
    options: new TerminalLiveOptions { EnableMouse = true, MouseMode = TerminalMouseMode.Move });
```

### Filling the viewport height

Inline live regions measure with an "infinite" height by default, so a simple visual like a `VStack("Hello")` will
typically only reserve a single row.

If you want the live region to reserve the full visible terminal height (useful for backgrounds, canvases, and layouts
that need vertical space), set the root visual to `VerticalAlignment(Align.Stretch)`:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Layout;

Terminal.Live(
    new VStack("Hello").VerticalAlignment(Align.Stretch),
    onUpdate: () => TerminalLoopResult.Continue);
```

### Writing during updates

During `onUpdate`, you can write regular output via `Terminal.WriteLine(...)` / `Terminal.Write(...)`.
That output is placed above the live region. The live region is then re-rendered below.

## Fullscreen: `Terminal.Run`

Fullscreen runs a UI loop on the main thread:

- the app owns the viewport
- input events are routed to focused controls
- dialogs/popups can be used

The `onUpdate` callback can be used to drive animations (e.g. spinner/progress) or background state updates.

### Exit gesture

Fullscreen `Terminal.Run(...)` exits when the configured exit gesture is triggered.

- Default: `Ctrl+Q`
- Configurable via `TerminalRunOptions.ExitGesture`

See also:

- [Input](input.md)
- [Rendering](rendering.md)
