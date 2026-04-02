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

### Loop pacing

`Terminal.Live(...)` and `Terminal.Run(...)` now default to `LoopMode = Auto`.

In auto mode, the host loop is deadline/event-driven:

- idle apps block until input, resize, `Post(...)`, render invalidation, animation work, or async update completion wakes them;
- active update callbacks use an internal active cadence cap of about `15 ms` (`~66.7 Hz`) instead of a fixed `Sleep(1)` loop;
- work already spent inside `onUpdate`, layout, and rendering counts against that budget instead of being added on top of it;
- Windows uses a high-resolution waitable timer when supported, with a safe fallback on older systems.

This gives better responsiveness and more stable animation pacing than the old fixed 1ms polling loop while still
keeping UI work on the calling thread.

### Polling mode and `UpdateWaitDuration`

`UpdateWaitDuration` is no longer the default frame cadence control.
It is now the maximum coarse wait slice used by `LoopMode = Polling`, which preserves the older periodic re-evaluation
behavior when you explicitly opt into it:

```csharp
Terminal.Live(
    visual,
    onUpdate: () => TerminalLoopResult.Continue,
    options: new TerminalLiveOptions
    {
        LoopMode = TerminalLoopMode.Polling,
        UpdateWaitDuration = TimeSpan.FromMilliseconds(20)
    });

Terminal.Run(
    visual,
    onUpdate: () => TerminalLoopResult.Continue,
    options: new TerminalRunOptions
    {
        LoopMode = TerminalLoopMode.Polling,
        UpdateWaitDuration = TimeSpan.FromMilliseconds(20)
    });
```

Use polling mode only when you specifically want legacy periodic wake-ups.
Larger `UpdateWaitDuration` values reduce wake frequency, but they also make updates less responsive.

In `LoopMode = Auto`, changing `UpdateWaitDuration` does not change the active animation cadence.

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

## Async hosting and async updates

Terminal UI provides `LiveAsync` / `RunAsync` overloads that accept an **asynchronous update callback**.

Important behavioral notes:

- The app remains **single-threaded** (UI thread). There is no extra UI thread created by Terminal.UI.
- `LiveAsync` / `RunAsync` still **run the UI loop on the calling thread**; the async aspect is the update callback and the ability to `await` it naturally.
- Only the **hosting update callback** (`onUpdate`) is async. Routed event handlers remain synchronous (`OnKeyDown`, `OnPointer*`, etc.).
- The async update callback is executed **cooperatively**: if it awaits, Terminal.UI continues to tick input/animations/rendering and resumes the callback later on the UI thread.
- There is only one hosted async update callback in flight at a time.

### When to use an async update callback

Use async updates when you need to integrate with asynchronous APIs (I/O, timers, subprocesses) without blocking the UI loop.

Example: await a timer-like operation and then stop:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var progress = new State<double>(0);

await Terminal.LiveAsync(
    new ProgressBar().Value(progress),
    async _ =>
    {
        await Task.Delay(50);
        progress.Value = Math.Min(1.0, progress.Value + 0.05);
        return progress.Value >= 1.0
            ? TerminalLoopResult.StopAndKeepVisual
            : TerminalLoopResult.Continue;
    });
```

### Guidance (keep the UI responsive)

- Avoid long-running synchronous work directly on the UI thread.
- If you use `await` in `onUpdate`, prefer the default context-capturing behavior.
- Avoid `ConfigureAwait(false)` in the update callback unless you explicitly marshal back to the UI thread with `context.App.Dispatcher.InvokeAsync(...)`.

For a detailed explanation of the synchronization context, continuation behavior, thread affinity, limitations, and
recommended event-handler workarounds, see [Async & Await](async-await.md).

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
