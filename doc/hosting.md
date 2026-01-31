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

- [Input](./input.md)
- [Rendering](./rendering.md)
