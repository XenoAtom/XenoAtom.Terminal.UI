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
- Support optional border labels on the top-right, bottom-left, and bottom-right edges.
- Support modal behavior (block interaction behind the dialog) when hosted in the window layer.
- Support moving the dialog by dragging its title bar (top border row).
- Support pointer resizing from the left, right, bottom, and bottom-right handles.
- Ensure the dialog surface/chrome does not inherit text attributes (underline, etc.) from whatever was rendered beneath it.

## Non-goals

- Inline dialogs (`Terminal.Live(...)`) are not a primary target; dialogs are designed to be hosted in fullscreen apps.
- Built-in buttons/command model for “OK/Cancel” (compose these via `Content`).

## Public surface (v1)

- `Title : Visual?`
- `Content : Visual?`
- `TopRightText : Visual?`
- `BottomLeftText : Visual?`
- `BottomRightText : Visual?`
- `Padding : Thickness` (padding between border and `Content`)
- `Left : int?` / `Top : int?` (position relative to the dialog’s layout slot)
- `Width : int?` / `Height : int?` (optional fixed size)
- `IsResizable : bool` (defaults to `true`)
- `IsModal : bool` (implements `IModalVisual` for window-layer hit-testing)
- Style:
  - `DialogStyle`
    - `Glyphs`
    - `SurfaceStyle`
    - `BorderCellStyle` / `FocusedBorderCellStyle`
    - `LabelBackgroundStyle`
    - `ResizeHandleHoverStyle`
- Methods:
  - `Show()` (fullscreen only; adds to the current `TerminalApp` window layer)
  - `Close()`

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`.
  - The dialog chooses its final size/position during arrange (see below).

## Implementation map

- Control: `src/XenoAtom.Terminal.UI/Controls/Dialog.cs`
- Style: `src/XenoAtom.Terminal.UI/Styling/DialogStyle.cs`
- Window hosting and modal behavior:
  - `src/XenoAtom.Terminal.UI/Controls/WindowLayer.cs`
  - `src/XenoAtom.Terminal.UI/TerminalApp.cs` (`ShowWindow`, `CloseWindow`)
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/DialogTests.cs`
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

`Dialog` is still a `Visual` and can be inserted directly into a tree (e.g. in a `ZStack`). In that case it renders and supports dragging/resizing,
but “modal” semantics are only meaningful when the host respects `IModalVisual` (the window layer does).

## Layout

### Measure

The measured size includes:

- a 1-cell border on each side (2 cells total width/height),
- `Padding` inside the border,
- the measured `Content` size,
- and the widest border-label row (`Title + TopRightText`, `BottomLeftText + BottomRightText`).

Key rules:

- `Width` / `Height` (when set) cap the available space for measuring content.
- `Content` is measured against:
  - `innerWidth = availableWidth - 2 - padding.Horizontal`
  - `innerHeight = availableHeight - 2 - padding.Vertical`
- Desired size:
  - `desiredWidth = Width ?? (2 + padding.Horizontal + content.DesiredSize.Width)`
  - `desiredHeight = Height ?? (2 + padding.Vertical + content.DesiredSize.Height)`
  - Both are clamped to at least `3x3` and respect inherited `MinWidth` / `MinHeight`.
- Border labels are measured as single-line visuals (`maxHeight = 1`) and can increase the desired width to fit the left/right label pairs.

### Arrange bounds (positioning)

`Dialog` uses `PrepareArrangeBounds` to choose its final rect within the host slot:

- The “layout slot” is the rect passed by the parent/window layer for arranging.
- Final size is:
  - `width = min(slot.Width, clamp(Width ?? DesiredSize.Width, MinWidth, MaxWidth))`
  - `height = min(slot.Height, clamp(Height ?? DesiredSize.Height, MinHeight, MaxHeight))`
- Position is chosen relative to the slot:
  - If `Left`/`Top` are `null`, the dialog is centered in the slot.
  - Otherwise they are clamped to keep the dialog fully within the slot.

### Child arrangement

- `Title` is treated as the top-left border label.
- `TopRightText`, `BottomLeftText`, and `BottomRightText` are arranged like `Group` labels:
  - left labels start at `x + 2`
  - right labels end at `x + width - 3`
  - all border labels use height `1`
- `Content` (if present) is arranged inside the inner rect:
  - `x + 1 + padding.Left`, `y + 1 + padding.Top`
  - width: `finalRect.Width - 2 - padding.Horizontal`
  - height: `finalRect.Height - 2 - padding.Vertical`

## Rendering

The dialog renders its own surface and chrome:

- Background fill: entire dialog rect is cleared with a “surface” style derived from the theme.
  - Background uses `Theme.PopupSurface ?? Theme.SurfaceAlt ?? Theme.Surface` when available.
- Border: drawn using `DialogStyle.Glyphs ?? theme.Lines` and `DialogStyle.ResolveBorderStyle(...)`.
- Label cutouts: border-label spans are cleared with `DialogStyle.ResolveLabelBackgroundStyle()`.
- Resize hover/drag affordances: the active resize handle and hovered/active move bar are redrawn with `DialogStyle.ResolveResizeHandleHoverStyle(...)`.

> [!IMPORTANT]
> The dialog explicitly preserves/normalizes text attributes when rendering the surface/chrome (`WithTextStyle(...)`) so that
> decorations like underline from content behind the dialog do not leak into the overlay. This is covered by tests.

## Input handling

### Dragging

- Left mouse button press on the top border row (`uiY == Bounds.Y`) starts a drag operation.
- Hovering the top border row highlights the move bar using the same hover style as the resize handles.
- While dragging, pointer movement updates `Left` and `Top` (clamped to the layout slot) so the dialog follows the cursor.
- Releasing the left mouse button ends the drag.

### Resizing

- When `IsResizable = true`, the dialog exposes pointer handles on:
  - the left border
  - the right border
  - the bottom border
  - the bottom-right corner
- Resize hover is tracked independently so the handle can be highlighted before drag starts.
- Resizing writes explicit `Left` / `Top` / `Width` / `Height` values so the resized placement persists.
- Left resize keeps the right edge fixed and updates both `Left` and `Width`.
- Right resize updates `Width`.
- Bottom resize updates `Height`.
- Bottom-right resize updates both `Width` and `Height`.
- Minimum resize constraints are taken from inherited `MinWidth` / `MinHeight` (with an absolute floor of `3x3`).

## Testing and demos

- `WindowLayerInteractionTests`:
  - dragging updates `Left`/`Top`
  - clicked window is brought to front
  - modal dialog blocks clicks behind it
- `DialogTests`:
  - border labels arrange on each edge
  - each resize handle updates the expected dimension(s)
  - min-size clamping works during resize
  - custom resize hover style is rendered on resize edges and the move bar
- `OverlaySurfaceTextStyleLeakTests`:
  - dialog surface does not inherit underline from underlay

## Future ideas

- Add keyboard affordances:
  - close on `Escape` (optional)
  - move via arrow keys when focused
- Add built-in title bar buttons (close, maximize) as optional visuals.
- Add placement helpers (open centered, open at anchor, constrain to viewport edges).
