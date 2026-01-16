# Hosting & Terminal Integration

XenoAtom.Terminal.UI is designed to feel integrated with `XenoAtom.Terminal`:

- Use `Terminal.Write(Visual)` to render a visual once.
- Use `Terminal.Live(Visual, Func<bool>)` for inline live regions.
- Use `Terminal.Run(Visual, Func<bool>)` for fullscreen applications.

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

The update callback returns `true` to keep running, `false` to stop.

### Writing during updates

During `onUpdate`, you can write regular output via `Terminal.WriteLine(...)` / `Terminal.Write(...)`.
That output is placed above the live region. The live region is then re-rendered below.

### Options

`TerminalLiveOptions` controls how the live region ends:

- `RemoveOnEnd = true`: removes the live region and restores the cursor to where it was before the region.
- `RemoveOnEnd = false`: leaves the final frame on screen and moves the cursor after it.

## Fullscreen: `Terminal.Run`

Fullscreen runs a UI loop on the main thread:

- the app owns the viewport
- input events are routed to focused controls
- dialogs/popups can be used

The `onUpdate` callback can be used to drive animations (e.g. spinner/progress) or background state updates.

See also:

- `doc/input.md`
- `doc/rendering.md`

