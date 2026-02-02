---
title: "Debugging & Performance Tools"
---

# Debugging & Performance Tools

XenoAtom.Terminal.UI includes built-in diagnostics to help you understand when and why the UI updates, and how much work
each frame performs.

## Debug overlay (F12)

In fullscreen apps (`Terminal.Run(...)`) and inline/live hosts (`Terminal.Live(...)`), press `F12` to toggle the debug
overlay.

The gesture is configurable via `TerminalAppOptions.ToggleDebugOverlayGesture` (default: `F12`).

![Debug Overlay](../img/debugging.png){.terminal}

> [!TIP]
> Keep the overlay enabled while developing dynamic UIs. It makes “silent” performance bugs obvious (for example: a
> control constantly re-measuring due to an unstable binding).

## What the overlay shows

### Frame and FPS

- `Frame`: monotonically increasing render frame index.
- `FPS`: frames per second (computed from the time between frames).

If FPS is low, use the timing lines below to see which stage is expensive.

### Tick and Update

- `Tick`: total time spent in the app loop for the tick.
- `Update`: time spent in the user `onUpdate` callback (your code).

If `Update` is high, you’re likely doing too much work in your update loop (I/O, allocations, heavy computations).

### Top timings

These are top-level timings for the render pipeline:

- `Measure`: total time spent measuring the visual tree.
- `Arrange`: total time spent arranging the visual tree.
- `Render`: total time spent rendering the visual tree into the `CellBuffer`.
- `Host`: time spent diffing + writing output to the terminal.
- `Total`: total render frame time.

### Calls and caches

These lines show how many times each stage ran across all visuals in the frame:

- `Calls: DynamicUpdate`: how often visuals rebuilt internal children/models.
- `Calls: Prepare`: how often `PrepareChildren` ran.
- `Calls: Measure`: how many visuals were measured.
- `Calls: Arrange`: how many visuals were arranged.
- `Calls: Render`: how many visuals executed `RenderOverride`.

Cache counters indicate how many times the framework reused a previous result:

- `Cache` (Measure/Arrange): cache hits for layout.
- `ClipSkips`: render calls skipped because the visual was fully clipped.

If `Measure`/`Arrange` calls are high every frame, it often means:

- you are changing bindable state every frame (even when values don’t really change), or
- a control is reading state in a way that prevents caching (unstable dependencies).

### Repaint and Dirty rectangles

- `Repaint`: the region that the app chose to repaint for this frame.
- `Dirty`: the union of dirty rectangles reported during the frame.

If the overlay says `(full repaint)`, the frame repainted the entire viewport.

> [!NOTE]
> `Dirty: <none>` combined with `(full repaint)` usually means the host forced a full repaint (common after resize or
> initial frames), not that the dirty system is broken.

### Diff output

- `Diff: N chars`: how many output characters the diff renderer wrote to the terminal.
- `Diff: N cells`: how many cells changed compared to the previous buffer.
- `full=yes/no`: whether the diff renderer forced a full redraw.

If `Diff` is consistently high, you are rewriting many cells each frame (large animated regions, blinking cursors, etc.).

### Focus and Hover

Shows the current focused and hovered visual type names. This is useful when debugging input routing and focus scopes.

## Using the overlay to optimize

### Symptom: constant Measure/Arrange every frame

Likely causes:

- some bindable property is changing each frame (even if it changes back), or
- a control reads and writes the same bindable state in one pass and triggers extra invalidations.

Start by:

1. Locate the region that animates/changes.
2. Check which stage is expensive (Measure/Arrange/Render).
3. Temporarily hide/disable parts of the tree to isolate the culprit.

### Symptom: high Render but low Diff

This usually means you are spending time rendering clipped or unchanged regions. Look at `ClipSkips`, and consider:

- reducing visual tree size,
- avoiding per-cell loops outside the clipped region,
- using virtualization (`DataGridControl`, `LogControl`) for large datasets.

## Related

- [Rendering](rendering.md)
- [Binding](binding.md)
- [Layout](layout.md)
