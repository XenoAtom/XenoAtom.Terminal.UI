---
title: ScrollViewer Specs
---

# ScrollViewer Specs

This document captures design and implementation notes for `ScrollViewer`.

> [!NOTE]
> For end-user usage and examples, see [ScrollViewer](../../controls/scrollviewer.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Host a single content visual and provide horizontal/vertical scrolling with optional scroll bars.
- **Key responsibilities**:
  - coordinate scroll bars and offsets (`HorizontalOffset`/`VerticalOffset`)
  - clip/translate content via an internal viewport host
  - integrate with content-provided scrolling (`IScrollable` / `ScrollModel`) without fighting the content’s own layout

## Public API surface

### Type

- `ScrollViewer : Visual` (sealed)

### Constructors

- `ScrollViewer()` defaults to `Focusable = true` and `HorizontalAlignment/VerticalAlignment = Align.Stretch`.
- Overloads accept `Visual? content`, `bool focusable`, or a `Func<Visual?>` content factory.

### Bindable properties

- `Content : Visual?`
  - Template-like input: it is applied to an internal host in `PrepareChildren` (it does **not** auto-attach directly to the `ScrollViewer` visual tree).
- `HorizontalScrollEnabled : bool`
  - When `false`, horizontal bar is hidden and `HorizontalOffset` is forced to `0`.
- `VerticalScrollEnabled : bool`
  - When `false`, vertical bar is hidden and `VerticalOffset` is forced to `0`.
- `HorizontalOffset : int`, `VerticalOffset : int`
  - Scroll offsets for content **without** a content-owned `ScrollModel`.
  - When content provides a `ScrollModel`, offsets are kept in sync with it.

### Derived properties

- `ViewportWidth` / `ViewportHeight` report the internal content host viewport size (excluding scroll bars).
- `ContentScrollable` exposes the `IScrollable` interface when the content implements it.

## Internal composition

`ScrollViewer` attaches four internal children:

- `ContentViewportHost`: hosts and clips/translates the content by arranging it at `(x - HorizontalOffset, y - VerticalOffset)`.
- `VScrollBar` and `HScrollBar`: internal scroll bars (non-focusable) whose style is bridged from `ScrollViewerStyle`.
- `ScrollCornerVisual`: fills the bottom-right corner when both bars are visible.

## Scrolling modes

`ScrollViewer` supports two modes depending on whether the content implements `IScrollable`:

### Mode A: content provides a `ScrollModel` (preferred for rich editors)

- The content owns its `ScrollModel.ViewportWidth/ViewportHeight` and typically updates them from its own `Arrange`.
- `ScrollViewer` **does not** call `ScrollModel.SetViewport(...)` in this mode (to avoid oscillations and “caret pinning” issues).
- Bar visibility is derived from `Extent*` vs content-owned `Viewport*`.
- `ScrollViewer` keeps its `HorizontalOffset`/`VerticalOffset` synchronized with the model (even without a `TerminalApp`, e.g. unit tests).

### Mode B: content does not provide a scroll model

- `ScrollViewer` measures content with “infinite” constraints on the scrolling axes:
  - if horizontal scrolling is enabled, max width is effectively unbounded
  - if vertical scrolling is enabled, max height is effectively unbounded
- `ScrollViewer` owns the offsets and passes them into `ContentViewportHost.UpdateLayout(...)`.

## Layout & rendering

### Prepare

`PrepareChildren`:

- applies `Content` to the internal host and updates the cached `IScrollable` / `ScrollModel`
- snapshots offsets into internal bindables (`HorizontalOffsetFromPrepare`/`VerticalOffsetFromPrepare`)

The snapshot avoids reading and then writing the same bindable value during `Arrange` within the same tracking context.

### Measure

- If content provides a scroll model, `ScrollViewer` measures content with the incoming constraints (content is responsible for clamping).
- Otherwise, it measures with unbounded max size on enabled scroll axes.
- It caches:
  - `_extentHints` (the content `SizeHints`)
  - `_contentWidth` / `_contentHeight` from `hints.Natural`
- Returned hints are `SizeHints.Flex(...)` with:
  - `min = (1,1)` when content exists
  - natural sized to content (clamped to bounded constraints)
  - `growX/growY` when aligned `Align.Stretch`

### Arrange

Arrange computes:

- `thickness = max(1, ScrollViewerStyle.ScrollBarThickness)`
- bar visibility and content viewport size, with multi-pass stabilization:
  - **content scroll model mode** starts from previous `_showVerticalBar/_showHorizontalBar` to avoid oscillation and runs up to 3 passes
  - **no model mode** may re-measure content for the final viewport width when horizontal scrolling is not needed (so width-dependent layouts like wrapping report correct height)
- offset clamping is applied based on `extent - viewport`
- bar ranges are updated (`Minimum=0`, `Maximum=maxOffset`, `ViewportSize=viewport`)
- `ScrollViewerStyle` is bridged to `ScrollBarStyle` for the internal scroll bars (thickness, track style, thumb style)

### Render

`ScrollViewer` itself does not paint content directly; rendering is performed by its internal visuals:

- `ContentViewportHost` renders the content subtree
- scroll bars render their tracks/thumbs
- `ScrollCornerVisual` fills the corner using `Theme.ScrollBars.Track` glyph and the resolved track style

## Input

### Keyboard

When scroll is enabled, `OnKeyDown` supports:

- vertical: `Up/Down`, `PageUp/PageDown`, `Home/End`
- horizontal: `Left/Right`

Offsets are clamped to valid ranges derived from extent and viewport sizes (content model or internal cached values depending on scrolling mode).

### Pointer wheel

- Wheel scrolls vertically by default.
- Holding `Shift` scrolls horizontally.

## Styling

### ScrollViewerStyle

`ScrollViewerStyle` defines:

- `ScrollBarThickness`
- optional `TrackStyle` and `ThumbStyle`
- theme-based resolution helpers (`ResolveTrackStyle`, `ResolveThumbStyle`)

The internal scroll bars receive a bridged `ScrollBarStyle` during `Arrange` so changes to thickness/styling apply without requiring a custom scroll bar instance.

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerLayoutTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ScrollViewerTextAreaInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs`
- Demos:
  - ControlsDemo includes a ScrollViewer page and multiple controls embedded in scroll viewers.

## Future / v2 ideas

- Support explicit bar visibility modes (always/auto/hidden) and overlay scroll bars.
- Add optional “scroll by N lines” configuration for wheel scrolling.
- Add keyboard shortcuts for horizontal page scrolling (when the terminal key encoding supports it).

- Look for rendering/input tests in `src/XenoAtom.Terminal.UI.Tests`.
- See the ControlsDemo for interactive examples.

## Future / v2 ideas

- Consider documenting additional style knobs and adding more deterministic rendering tests as features grow.
