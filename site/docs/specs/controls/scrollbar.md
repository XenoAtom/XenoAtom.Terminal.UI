---
title: ScrollBar Specs
---

# ScrollBar Specs

This document captures design and implementation notes for `ScrollBar`.

> [!NOTE]
> For end-user usage and examples, see [ScrollBar](../../controls/scrollbar.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display a scroll “track” and “thumb” for a scrollable extent and viewport and let the user change an integer value.
- **Where it is used**:
  - standalone scroll bars
  - internal scroll bars inside `ScrollViewer`
  - controls that expose `ScrollModel` (via `IScrollable`) typically feed their model values into scroll bars

## Public API surface

### Types

- `ScrollBar : Visual` (abstract)
- `VScrollBar : ScrollBar` (`Orientation.Vertical`)
- `HScrollBar : ScrollBar` (`Orientation.Horizontal`)

### Constructors

- `ScrollBar(bool focusable = true)`:
  - sets `Focusable`
  - stretches along its main axis:
    - vertical bars default to `VerticalAlignment = Align.Stretch`
    - horizontal bars default to `HorizontalAlignment = Align.Stretch`
  - sets `SmallChange = 1`

### Bindable properties

- `Minimum : int`
- `Maximum : int`
- `Value : int`
  - clamped to `[min, max]` (swaps if `Maximum < Minimum`)
- `ViewportSize : int`
  - affects thumb length (can be `0`)
- `SmallChange : int` (wheel / arrow keys)
- `LargeChange : int` (page click / PageUp/PageDown)
  - when `<= 0`, defaults to `max(1, ViewportSize)`

### Routed events

- `ValueChanged` (bubble): raised when `Value` changes after clamping, with old/new values.

## Layout & rendering

### Measure

- Thickness is taken from `ScrollBarStyle.Thickness` (min 1, clamped to finite).
- Bars measure as:
  - **vertical**: fixed width = thickness, flexible height
  - **horizontal**: fixed height = thickness, flexible width

### Render

Rendering is glyph-based and uses `Theme.ScrollBars`:

- The bar renders the full track with `glyphs.Track` and a resolved “track style”.
- The thumb renders over the track with `glyphs.Thumb` and a resolved “thumb style”.
- The thumb style is **highlighted** when hovered, focused, or actively dragging.

### Thumb metrics (size & position)

Thumb is derived from `Minimum/Maximum/Value/ViewportSize` and available track length:

- If the range is `0` (or negative after normalization), thumb consumes the full track.
- Otherwise:
  - `contentSize = range + viewport`
  - `thumbLength ≈ trackLength * viewport / contentSize` (rounded)
  - clamped to `[ScrollBarStyle.MinThumbLength, trackLength]`
  - `thumbStart ≈ offset * (trackLength - thumbLength) / range` (rounded)

The algorithm is designed to be stable for small tracks (track length `<= 1` always yields a 1-cell thumb).

## Input behavior

### Pointer

- **Click on thumb**: begins drag capture and stores the starting UI coordinate + starting value.
- **Click on track outside thumb**: performs a “page” operation:
  - page size = `LargeChange` if `> 0`, else `max(1, ViewportSize)`
  - clicks before the thumb page up/left; after the thumb page down/right
- **Drag**: maps pointer delta to value delta using the usable track length (`trackLength - thumbLength`).
- **Wheel**: increments/decrements by `SmallChange` (min 1).

### Keyboard

- **Vertical**:
  - `Up/Down` by `SmallChange`
  - `PageUp/PageDown` by `LargeChange` (or `ViewportSize` fallback)
  - `Home/End` to `Minimum`/`Maximum`
- **Horizontal**:
  - `Left/Right` by `SmallChange`
  - `PageUp/PageDown`, `Home/End` behave similarly

## Styling

### ScrollBarStyle

`ScrollBarStyle` controls:

- `Thickness` (cells)
- `MinThumbLength` (cells)
- optional `TrackStyle` / `ThumbStyle`
- theme-based resolution:
  - track defaults to a dim style using `Theme.Muted`/`Theme.Border`
  - thumb defaults to bold, using a highlighted color when hovered/dragging/focused

Scroll bars use `Theme.ScrollBars.Track` and `Theme.ScrollBars.Thumb` glyphs for rendering.

## Tests & demos

- Scroll bars are exercised primarily through:
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewer*Tests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerTextAreaInteractionTests.cs` (hit testing + bar interaction)
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerRenderingTests.cs`
- Demos: Scroll bars are visible in ScrollViewer and other controls that overflow.

## Future / v2 ideas

- Optional “arrow buttons” at ends (classic scrollbar affordance) if desired.
- Support for click-to-jump mode (jump thumb to pointer position) as an alternative paging behavior.
