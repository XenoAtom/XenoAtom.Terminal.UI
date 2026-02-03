---
title: XenoAtom.Terminal
---

# XenoAtom.Terminal

XenoAtom.Terminal is the foundation underneath XenoAtom.Terminal.UI. It is a modern replacement for `System.Console`
designed for TUI/CLI apps: safe output, terminal-native capabilities, unified input events, and deterministic testing.

> [!NOTE]
> XenoAtom.Terminal is a terminal API, not a widget framework. Terminal.UI provides the widget/layout layer.

## Why use it (vs `System.Console`)?

XenoAtom.Terminal keeps a familiar console-like surface (`Write`, `ReadKey`, cursor, colors), while adding the
capabilities that are hard to do correctly and portably with `System.Console`:

- Atomic ANSI-safe output (no interleaved styling/cursor ops)
- Markup/styling built on `XenoAtom.Ansi`
- Unified input event stream (keyboard, mouse, resize, bracketed paste when available)
- Scopes that restore terminal state on dispose (cursor, raw mode, alternate screen, etc.)
- Backends for deterministic testing (in-memory backend)

## Getting started

### Initialization

Terminal is initialized lazily on first use, or explicitly via `Terminal.Open()`:

```csharp
using XenoAtom.Terminal;

using var session = Terminal.Open();
Terminal.WriteLine("Hello from XenoAtom.Terminal");
```

### Output: plain, atomic, and markup

Basic output:

```csharp
using XenoAtom.Terminal;

Terminal.WriteLine("Hello");
```

Atomic output (prevents interleaving with concurrent writes):

```csharp
Terminal.WriteAtomic(w =>
{
    w.Foreground(ConsoleColor.DarkGray);
    w.Write("[");
    w.Write(DateTimeOffset.UtcNow.ToString("HH:mm:ss.fff"));
    w.Write("] ");
    w.ResetStyle();
    w.Write("message\n");
});
```

Markup output (powered by `XenoAtom.Ansi`):

```csharp
Terminal.WriteMarkup("[bold green]Hello[/] [gray]world[/]!");
```

See:

- [Markup](markup.md) (syntax reference and Terminal.UI semantic tokens)

### Capabilities and environment differences

Terminal behavior depends on the host (Windows Console vs terminal emulator vs CI logs). You can inspect capabilities:

```csharp
Terminal.WriteLine($"Ansi={Terminal.Capabilities.AnsiEnabled}, Color={Terminal.Capabilities.ColorLevel}");
```

> [!TIP]
> If you need deterministic behavior (e.g. tests), initialize Terminal with an explicit backend.

## Input model (events, not polling)

XenoAtom.Terminal supports a unified event stream (keyboard, resize, mouse/paste when available). This is a better fit
for TUIs than mixing multiple platform-specific input APIs.

> [!IMPORTANT]
> Avoid mixing `System.Console` input APIs with XenoAtom.Terminal input APIs in the same app.

Example: consuming the event stream (for custom TUIs or advanced integrations):

```csharp
using XenoAtom.Terminal;

await foreach (var ev in Terminal.ReadEventsAsync())
{
    Terminal.WriteLine(ev.ToString());
}
```

## ReadLine editor

XenoAtom.Terminal includes a rich `ReadLine` editor (history, completion, clipboard, rendering), with both sync and async APIs:

```csharp
using XenoAtom.Terminal;

var line = Terminal.ReadLine(new TerminalReadLineOptions
{
    PromptMarkup = () => "[bold]Name:[/] ",
});
```

Highlights:

- history (scoped, not global), completion, undo/redo
- fixed-width view and optional ellipsis
- bracketed paste and optional mouse editing (when supported)

Terminal.UI builds higher-level prompt controls on top of Terminal's live hosting; see:

- [Prompts](prompts.md) (inline prompts built on `Terminal.Live`)

## Testing and backends

Terminal can run against a virtual/in-memory backend, which makes it suitable for deterministic tests. Terminal.UI tests
use the same idea at the UI layer (render to buffers and diff).

## How Terminal.UI uses XenoAtom.Terminal

Terminal.UI integrates as extension members on `Terminal`:

- `Terminal.Write(Visual)` - render once
- `Terminal.Live(Visual, onUpdate)` - inline live region (mouse reporting is disabled by default)
- `Terminal.Run(Visual, onUpdate)` - fullscreen app (alternate screen)

See:

- [Hosting](hosting.md)
- [Rendering](rendering.md)
- [Ecosystem and Foundations](foundations.md)
