# Testing

XenoAtom.Terminal.UI apps can be tested without a terminal: build the app over an in-memory terminal, drive it frame by frame from the test thread, inject input through the same path a real terminal uses, control time, and read what was rendered cell by cell.

The pieces, all in the `XenoAtom.Terminal.UI.Testing` namespace of the main package:

- `TerminalAppDriver` starts, ticks and stops a `TerminalApp` on the calling thread, or runs its real loop with a callback between frames.
- `TerminalScreen` is a read-only view of the last rendered frame: text per line, and a `TerminalCell` (text, style, wide-glyph continuation) per cell.
- `TerminalAppOptions.TimeProvider` makes the app read time from a `TimeProvider` you control.

## Setup

Build the app as usual, over a standalone `TerminalInstance` with an `InMemoryTerminalBackend` sized like the terminal you want to simulate. The driver needs a `VirtualTerminalBackend` (which `InMemoryTerminalBackend` is) to inject input:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Testing;

var text = new State<string?>("");
var root = new VStack(new TextBlock("Name:"), new TextBox().Text(text));

using var terminal = new TerminalInstance(new InMemoryTerminalBackend(new TerminalSize(40, 10)));
var app = new TerminalApp(root, terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
using var driver = new TerminalAppDriver(app);
```

The driver does not own the app or the terminal. One driver per test thread: each running app has its own dispatcher and ambient terminal, so test classes can run in parallel.

## Stepped mode

`Start()` begins a run without blocking. Each `Tick()` runs one frame: pending actions, input, animations, update, layout and render. `Stop()` (or disposing the driver) ends the run.

```csharp
driver.Start();
driver.Tick();                                  // first frame
Assert.AreEqual("Name:", driver.Screen.GetLine(0).TrimEnd());

driver.SendText("Ada");                         // input methods run a frame themselves
Assert.AreEqual("Ada", text.Value);
StringAssert.Contains(driver.Screen.GetText(), "Ada");
```

`TickUntil(condition, maxFrames)` runs frames until a condition holds and throws `TimeoutException` otherwise; `Render()` forces a full layout and render pass.

## Input

Input methods push events into the terminal backend, so they go through the input relay, the terminal's mouse-mode filtering, focus scoping and the exit gesture, like events from a real terminal. In stepped mode each method also runs one frame, so the input has been processed when it returns.

| Method | Sends |
|---|---|
| `SendKey(TerminalKey[, modifiers])` | A key such as `Enter`, `Tab`, `Up`, with the `Char` a terminal produces for it |
| `SendKey(char[, modifiers])` | A character key; with `Ctrl` and a letter, the control character (`Ctrl+Q` is `TerminalChar.CtrlQ`); a text event follows when the character is printable and only `Shift` is held |
| `SendText(string)` | Typed text: one key event per character, then one text event |
| `SendPaste(string)` | A bracketed paste |
| `MoveMouse(x, y)` | A mouse move (hover) |
| `Click(x, y[, button])`, `DoubleClick(x, y)`, `Wheel(x, y, delta)` | Mouse buttons and wheel at a cell |
| `Resize(columns, rows)` | A terminal resize |

Cells are zero-based; `visual.GetAbsoluteBounds()` gives a visual's position after a frame:

```csharp
var bounds = okButton.GetAbsoluteBounds();
driver.Click(bounds.X, bounds.Y);
```

Two `Click` calls at the same cell within 500 ms are turned into a double click by the terminal, as with a real terminal; use `DoubleClick` when that is what you want, and different cells otherwise.

## Reading the screen

`driver.Screen` reads the app's cell buffer after the last frame, so styles are exact and nothing is re-parsed from ANSI output:

```csharp
var screen = driver.Screen;
screen.Width; screen.Height;             // cells
screen.GetText();                        // rows joined by '\n', trailing spaces kept
screen.GetLine(2);                       // one row
var cell = screen[0, 2];                 // TerminalCell
cell.Text;                               // one grapheme, " " for blank
cell.Style.TryGetForeground(out var fg); // Style: colors and TextStyle
cell.IsWideContinuation;                 // second cell of a two-cell glyph
```

A glyph two cells wide (emoji and other wide runes) occupies two cells; `GetLine` and `GetText` include its text once, so use the indexer when the exact column matters.

The raw ANSI output is still available through `driver.Backend.GetOutText()` when a test is about the output protocol itself.

## Time

The app reads time from `TerminalAppOptions.TimeProvider` (default `TimeProvider.System`): the update loop, animations, spinners, tooltip delays, toast durations and multi-stroke command sequence timeouts. Only timestamps are used, never wall-clock time.

`driver.Tick(TimeSpan)` moves the app's time forward before running the frame, whatever the provider:

```csharp
var tooltip = okButton.Tooltip("Saves the form").ShowDelayMilliseconds(500);
// ...
driver.MoveMouse(bounds.X, bounds.Y);
driver.Tick(TimeSpan.FromMilliseconds(600));
StringAssert.Contains(driver.Screen.GetText(), "Saves the form");
```

For full control, pass a controllable provider. `FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package works as is; advancing it and `driver.Tick(TimeSpan)` add up:

```csharp
var time = new FakeTimeProvider();
var app = new TerminalApp(root, terminal, new TerminalAppOptions
{
    HostKind = TerminalHostKind.Fullscreen,
    TimeProvider = time,
});
```

## Running mode

`Run(onFrame)` runs the app's real loop on the calling thread and invokes the callback on that thread after each frame. Return `false` to stop the app. Input methods called from the callback only queue their events; the loop processes them on the next frame.

```csharp
var frames = 0;
driver.Run(d =>
{
    frames++;
    if (frames == 1) d.SendText("Ada");
    return text.Value != "Ada";             // stop once the text arrived
});
```

Prefer stepped mode for deterministic tests; running mode exercises the real loop pacing and the input relay thread.
