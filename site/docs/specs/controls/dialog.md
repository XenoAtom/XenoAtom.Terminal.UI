---
title: Dialog Specs
---

# Dialog Specs

This document specifies the current behavior and design of the `Dialog` control as implemented.

> [!NOTE]
> For end-user usage and examples, see [Dialog](../../controls/dialog.md).

## Goals

- Provide a window-like overlay that can be shown in fullscreen apps using the window layer.
- Support an optional title area and a content area.
- Support modal behavior (block interaction behind the dialog) when hosted in the window layer.
- Support moving the dialog by dragging its title bar (top border row).
- Ensure the dialog surface/chrome does not inherit text attributes (underline, etc.) from whatever was rendered beneath it.

## Non-goals

- Inline dialogs (`Terminal.Live(...)`) are not a primary target; dialogs are designed to be hosted in fullscreen apps.
- Resizing (no resize handles; only explicit `Width`/`Height`).
- Built-in buttons/command model for “OK/Cancel” (compose these via `Content`).

## Public surface (v1)

- `Title : Visual?`
- `Content : Visual?`
- `Padding : Thickness` (padding between border and `Content`)
- `Left : int?` / `Top : int?` (position relative to the dialog’s layout slot)
- `Width : int?` / `Height : int?` (optional fixed size)
- `IsModal : bool` (implements `IModalVisual` for window-layer hit-testing)
- Methods:
  - `Show()` (fullscreen only; adds to the current `TerminalApp` window layer)
  - `Close()`

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`.
  - The dialog chooses its final size/position during arrange (see below).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/Dialog.cs`
- Window hosting and modal behavior:
  - `src/XenoAtom.Terminal.UI/Controls/WindowLayer.cs`
  - `src/XenoAtom.Terminal.UI/TerminalApp.cs` (`ShowWindow`, `CloseWindow`)
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/WindowLayerInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/OverlaySurfaceTextStyleLeakTests.cs`
- Demos:
  - `samples/ControlsDemo/Demos/DialogDemo.cs`
  - `samples/ControlsDemo/Demos/BackdropDemo.cs` (dialog + backdrop composition)

## Hosting

### Fullscreen window layer (recommended)

- `Show()` finds the current `TerminalApp` via `App ?? Dispatcher.AttachedApp` and calls `TerminalApp.ShowWindow(this)`.
- `Close()` calls `TerminalApp.CloseWindow(this)` when possible.
- When hosted in the window layer:
  - the dialog is treated as a “window” that can be brought to front by click,
  - `IsModal = true` causes hit-testing/input to be blocked behind the dialog (see `WindowLayerInteractionTests`).

### Manual hosting (possible)

`Dialog` is still a `Visual` and can be inserted directly into a tree (e.g. in a `ZStack`). In that case it renders and supports dragging,
but “modal” semantics are only meaningful when the host respects `IModalVisual` (the window layer does).

## Layout

### Measure

The measured size includes:

- a 1-cell border on each side (2 cells total width/height),
- `Padding` inside the border,
- the measured `Content` size,
- and the `Title` width (if present).

Key rules:

- `Width` / `Height` (when set) cap the available space for measuring content.
- `Content` is measured against:
  - `innerWidth = availableWidth - 2 - padding.Horizontal`
  - `innerHeight = availableHeight - 2 - padding.Vertical`
- Desired size:
  - `desiredWidth = Width ?? (2 + padding.Horizontal + content.DesiredSize.Width)`
  - `desiredHeight = Height ?? (2 + padding.Vertical + content.DesiredSize.Height)`
  - Both are clamped to at least `3x3`.
- When `Title` is present, it is measured as a single line (`maxHeight = 1`) and can increase the desired width to fit `titleWidth + 4`.

### Arrange bounds (positioning)

`Dialog` uses `PrepareArrangeBounds` to choose its final rect within the host slot:

- The “layout slot” is the rect passed by the parent/window layer for arranging.
- Final size is:
  - `width = min(slot.Width, Width ?? DesiredSize.Width)` (at least 3)
  - `height = min(slot.Height, Height ?? DesiredSize.Height)` (at least 3)
- Position is chosen relative to the slot:
  - If `Left`/`Top` are `null`, the dialog is centered in the slot.
  - Otherwise they are clamped to keep the dialog fully within the slot.

### Child arrangement

- `Title` (if present) is arranged on the **top border row**:
  - at `x + 2`, `y` with height `1`, and width clamped to `finalRect.Width - 4`.
- `Content` (if present) is arranged inside the inner rect:
  - `x + 1 + padding.Left`, `y + 1 + padding.Top`
  - width: `finalRect.Width - 2 - padding.Horizontal`
  - height: `finalRect.Height - 2 - padding.Vertical`

## Rendering

The dialog renders its own surface and chrome:

- Background fill: entire dialog rect is cleared with a “surface” style derived from the theme.
  - Background uses `Theme.PopupSurface ?? Theme.SurfaceAlt ?? Theme.Surface` when available.
- Border: drawn using `theme.Lines` glyphs and `theme.BorderStyle(focused)` where `focused = HasFocusWithin`.
- Title chrome: if `Title` is present, the title area is “cleared” to spaces and bolded on the top border row behind the title.

> [!IMPORTANT]
> The dialog explicitly preserves/normalizes text attributes when rendering the surface/chrome (`WithTextStyle(...)`) so that
> decorations like underline from content behind the dialog do not leak into the overlay. This is covered by tests.

## Input handling

### Dragging

- Left mouse button press on the top border row (`uiY == Bounds.Y`) starts a drag operation.
- While dragging, pointer movement updates `Left` and `Top` (clamped to the layout slot) so the dialog follows the cursor.
- Releasing the left mouse button ends the drag.

## Testing and demos

- `WindowLayerInteractionTests`:
  - dragging updates `Left`/`Top`
  - clicked window is brought to front
  - modal dialog blocks clicks behind it
- `OverlaySurfaceTextStyleLeakTests`:
  - dialog surface does not inherit underline from underlay

## Future ideas

- Add keyboard affordances:
  - close on `Escape` (optional)
  - move via arrow keys when focused
- Add optional resizing handles.
- Add built-in title bar buttons (close, maximize) as optional visuals.
- Add placement helpers (open centered, open at anchor, constrain to viewport edges).
