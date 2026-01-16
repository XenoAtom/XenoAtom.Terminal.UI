# Rendering & Performance

Rendering is done through an intermediate cell buffer:

- visuals render into a `CellBuffer`
- a diff renderer computes minimal terminal updates
- output is written in a single batched write per frame where possible

## Synchronized output (DEC private mode 2026)

Fullscreen and inline rendering use synchronized output to reduce tearing:

- “begin synchronized output” is emitted at the start of a frame
- “end synchronized output” is emitted at the end

## Cursor handling

The framework uses the terminal cursor as the caret for text controls:

- only one cursor is visible at a time
- controls report desired cursor position during rendering

## Avoiding allocations

The rendering path reuses internal buffers where possible to minimize per-frame allocations.

See also:

- `src/XenoAtom.Terminal.UI/Rendering`

