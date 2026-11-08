# Testing

XenoAtom.Terminal.UI apps can be tested without a terminal: build the app over an in-memory terminal, drive it frame by frame from the test thread, inject input through the same terminal events a real terminal produces, control time, and read what was rendered cell by cell.

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

var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
using var terminal = new TerminalInstance(backend);
var app = new TerminalApp(root, terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
using var driver = new TerminalAppDriver(app);
```

The driver does not own the app or the terminal. One driver per test thread: each running app has its own dispatcher and ambient terminal, so test classes can run in parallel. All driver calls must happen on the thread that started or runs the app; other threads get an `InvalidOperationException`.

Apps driven by an update callback (the `TerminalExtensions` run helpers that take an `onUpdate` loop callback) are not covered by the driver yet: build a `TerminalApp` from a root visual as above.

## Stepped mode

`Start()` begins a run without blocking. Each `Tick()` runs one frame: pending actions, input, animations, update, layout and render. `Stop()` (or disposing the driver) ends the run; a stopped driver can be started again.

```csharp
driver.Start();
driver.Tick();                                  // first frame
Assert.AreEqual("Name:", driver.Screen.GetLine(0).TrimEnd());

driver.SendText("Ada");                         // input methods run a frame themselves
Assert.AreEqual("Ada", text.Value);
StringAssert.Contains(driver.Screen.GetText(), "Ada");
```

`TickUntil(condition, maxFrames)` runs frames until a condition holds and throws `TimeoutException` otherwise; `Render()` forces a full layout and render pass.

`driver.IsStopRequested` tells whether the app was asked to stop (exit gesture, `app.Stop()`, a quit command). Stepped mode keeps running frames after a stop request, so assert on this property:

```csharp
driver.SendKey('q', TerminalModifiers.Ctrl);    // default fullscreen exit gesture
Assert.IsTrue(driver.IsStopRequested);
```

## Input

Input methods push events into the terminal backend, and the app reads them through its `TerminalInstance` like events from a real terminal: the terminal's mouse-mode filtering and double-click synthesis, the app's focus scoping and the exit gesture all apply. In stepped mode the app drains the events on the test thread during the frame (no input relay thread is involved), and each input method runs one frame, so the input has been processed when it returns. Calling an input method when the driver is neither started nor running throws `InvalidOperationException`.

| Method | Sends |
|---|---|
| `SendKey(TerminalKey[, modifiers])` | A key such as `Enter`, `Tab`, `Up`, with the `Char` a terminal produces for it; `Space` is sent as the character `' '` |
| `SendKey(char[, modifiers])` | A character key; with `Ctrl` and a letter, the control character (`Ctrl+Q` is `TerminalChar.CtrlQ`); a text event follows when the character is printable and only `Shift` is held |
| `SendText(string)` | Typed text: one key event per character and one text event per run of printable characters; `'\r'`/`'\n'`, `'\t'`, `'\b'` and `ESC` are sent as `Enter`, `Tab`, `Backspace` and `Escape` |
| `SendPaste(string)` | A bracketed paste |
| `MoveMouse(x, y)` | A mouse move (hover) |
| `Click(x, y[, button])`, `DoubleClick(x, y)` | Button down and up at a cell; `DoubleClick` sends two clicks, which the terminal turns into a double click |
| `Wheel(x, y, delta)` | A wheel event; a positive delta scrolls up, a negative one down |
| `Resize(columns, rows)` | A terminal resize |

Cells are zero-based; `visual.GetAbsoluteBounds()` gives a visual's position after a frame:

```csharp
var bounds = okButton.GetAbsoluteBounds();
driver.Click(bounds.X, bounds.Y);
```

Double clicks are detected by the terminal, as with a real one: two button-down events of the same button within 500 ms (real time) and within one cell of each other (adjacent cells included) become a double click. Two quick `Click` calls at the same or adjacent cells therefore produce one; use `DoubleClick` when that is what you want, and cells at least two apart otherwise.

## Reading the screen

`driver.Screen` reads the app's cell buffer after the last frame, so styles are exact and nothing is re-parsed from ANSI output:

```csharp
var screen = driver.Screen;
var width = screen.Width;                // cells
var height = screen.Height;
var text = screen.GetText();             // rows joined by '\n', trailing spaces kept
var line = screen.GetLine(2);            // one row
var cell = screen[0, 2];                 // TerminalCell
var glyph = cell.Text;                   // one grapheme, " " for blank
var hasForeground = cell.Style.TryGetForeground(out var foreground);
var isContinuation = cell.IsWideContinuation; // second cell of a two-cell glyph
```

A glyph two cells wide (emoji and other wide runes) occupies two cells; `GetLine` and `GetText` include its text once, and a grapheme can be several UTF-16 characters, so a line's length is not its number of cells: use the indexer when the exact column matters.

The screen holds the text and style of each cell only: the cursor and the debug overlay are not part of it, and hyperlinks are not exposed. In inline mode (`TerminalHostKind.Inline`) it covers the app's live region, not the whole terminal.

When a test is about the output protocol itself, read the raw ANSI output from the `InMemoryTerminalBackend` kept from the setup: `backend.GetOutText()`.

## Time

The app reads time from `TerminalAppOptions.TimeProvider` (default `TimeProvider.System`; `null` is rejected by the `TerminalApp` constructor with an `ArgumentException`): the update loop, animations, spinners, tooltip delays, toast durations and multi-stroke command sequence timeouts. Only timestamps are used, never wall-clock time.

`driver.Tick(TimeSpan)` moves the app's clock forward before running the frame, whatever the provider:

```csharp
var tooltip = okButton.Tooltip("Saves the form").ShowDelayMilliseconds(500);
// ...
driver.MoveMouse(bounds.X, bounds.Y);
driver.Tick(TimeSpan.FromMilliseconds(600));
StringAssert.Contains(driver.Screen.GetText(), "Saves the form");
```

How the pieces fit:

- The moved time belongs to the app, not to the provider: frames, controls and `TerminalRunningContext.Timestamp` see it (as does `driver.Timestamp`), but `app.TimeProvider.GetTimestamp()` does not. It is kept when the driver is stopped, by a second driver on the same app, and in running mode, so time never goes backwards.
- For full control, pass a controllable provider. `FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package works as is; advancing it and `driver.Tick(TimeSpan)` add up:

```csharp
var time = new FakeTimeProvider();
var app = new TerminalApp(root, terminal, new TerminalAppOptions
{
    HostKind = TerminalHostKind.Fullscreen,
    TimeProvider = time,
});
```

- With the default `TimeProvider.System`, real time also passes between frames; use a provider that only moves when told to (such as `FakeTimeProvider`) for tests that must not depend on machine speed.
- Running mode waits between frames in real time whatever the provider, so a frozen provider still produces frames (about 60 per second) and the test advances time itself, for example from the frame callback.

Not on the app clock (they use the system clock):

- `OptionList` type-ahead (`Environment.TickCount64`) and `TextBox`/`TextArea` undo coalescing (`Environment.TickCount`).
- The terminal's double-click window (in XenoAtom.Terminal).
- Debug overlay metrics, which measure real durations.

## Running mode

`Run(onFrame, cancellationToken)` runs the app's real loop on the calling thread and invokes the callback on that thread after each frame. Return `false` to end the run; the app itself is not stopped, so it can be run or started again. Input methods called from the callback only queue their events; the loop's input relay processes them on a next frame. An exception thrown by the callback ends the run and is rethrown by `Run`.

Always cap a running-mode test, with a frame limit and a token, so a regression cannot hang the test run:

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var frames = 0;
driver.Run(d =>
{
    frames++;
    if (frames == 1) d.SendText("Ada");
    return text.Value != "Ada" && frames < 100;  // stop once the text arrived, or after 100 frames
}, timeout.Token);
Assert.AreEqual("Ada", text.Value);
```

While the callback is attached, the loop produces frames at its active frame rate even when the app is idle. Prefer stepped mode for deterministic tests; running mode exercises the real loop pacing and the input relay thread.
