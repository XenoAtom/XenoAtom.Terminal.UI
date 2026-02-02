---
title: Popup Specs
---

# Popup Specs

This document captures the design and implementation details of `Popup`.

> [!NOTE]
> For end-user usage and examples, see [Popup](../../controls/popup.md).

## Overview

- **Status**: Implemented
- **Purpose**: Display transient content in an overlay layer, positioned relative to an anchor (or to the viewport).
- **Modal**: `Popup` implements `IModalVisual` and is treated as a modal overlay by the focus/event system.
- **Content scrolling**: `Popup` hosts its content inside an internal, non-focusable `ScrollViewer` so large content can scroll.
- **Dismissal**: Outside click, Escape, and optionally Tab.

## Goals

- Provide a single, reusable overlay primitive used by many controls:
  - dropdowns (`Select<T>`)
  - menus (`MenuBar`, context menus)
  - command palette, search popups, and other transient panels
- Make popups safe by default:
  - they close on outside click
  - they are modal and block underlying command routing while open
- Keep positioning deterministic and terminal-friendly (integer rectangles, clamping to viewport).

## Non-goals

- Inline popups in non-fullscreen hosts. Popups require a `TerminalApp` window layer (fullscreen apps).
- Built-in border/chrome rendering. Popups draw a surface; callers typically wrap content in `Border`/`Group` for chrome.

## Public API

### PopupPlacement

`PopupPlacement` controls how a popup is positioned relative to an anchor:

- `Below`, `Above`, `Right`, `Left`

### Popup

Key properties:

- `Content : Visual?` (bindable) - popup content.
- `Anchor : Visual?` - anchor visual used for positioning.
- `AnchorRect : Rectangle?` - explicit anchor rectangle (takes precedence over `Anchor`).
- `Placement : PopupPlacement` (bindable) - relative placement when anchored.
- `MatchAnchorWidth : bool` (bindable) - ensures popup width is at least the anchor width.
- `AdditionalWidth : int` (bindable) - extra width added after width computation.
- `HorizontalPopupAlignment : Align` (bindable) - alignment when not anchored.
- `VerticalPopupAlignment : Align` (bindable) - alignment when not anchored.
- `OffsetX : int` / `OffsetY : int` (bindable) - manual offsets applied after positioning.
- `CloseOnTab : bool` (bindable) - whether Tab closes the popup before focus traversal.
- `IsDraggable : bool` (bindable) - enables drag repositioning.
- `DragHandleHeight : int` (bindable) - height of the draggable region at the top of the popup.

Key methods:

- `Show()` - adds the popup to the active `TerminalApp` window layer.
- `Close()` - removes the popup and raises the `Closed` routed event.

## Hosting model

`Popup.Show()` calls `TerminalApp.ShowWindow(this)`:

- This requires a fullscreen app (window layer must exist).
- If the popup itself is not focusable, the app will focus the first focusable child in the popup subtree.

Popups are modal and participate in the "active modal root" logic used by `TerminalApp` to route keyboard input and
tab traversal.

## Layout and positioning

### Layout slot

`Popup` itself stretches to fill the available UI slot. This is intentional:

- It allows the popup to detect outside clicks (the popup receives pointer events everywhere).
- It allows clamping the popup rectangle to the viewport.

The actual visible popup surface is tracked as an internal `_popupRect`.

### Desired popup size

`ArrangeCore` computes a desired popup size from:

- `PopupStyle.Padding`
- `Content.DesiredSize`
- `MatchAnchorWidth` and `AdditionalWidth`

Desired size is then clamped to the layout slot size.

### Anchored positioning

When `AnchorRect` is set, or when `Anchor` is set (uses `Anchor.Bounds`), the popup is positioned relative to the anchor:

- `Below` / `Above` prefer the requested side but may flip to the other side if the requested side does not fit and the other does.
- `Right` / `Left` prefer the requested side but:
  - shrink width to the available space on that side (and remeasure content for the new width)
  - may flip to the other side if the requested side does not fit but the other does

This "shrink on the requested side" behavior avoids the common UX issue where a right/left placement appears to not work
because the popup is clamped to the opposite edge.

### Unanchored positioning

When there is no anchor, `HorizontalPopupAlignment` and `VerticalPopupAlignment` are used within the slot.

If horizontal alignment is `Align.Stretch`, the popup width becomes the slot width and the content is remeasured for that width.
If vertical alignment is `Align.Stretch`, the popup height becomes the slot height.

### Manual offsets and clamping

After computing the position:

- `OffsetX` and `OffsetY` are applied.
- The popup rectangle is clamped to remain within the slot.

## Content scrolling

`Popup` wraps its content in an internal `ScrollViewer`:

- The internal scroll viewer is not focusable, so focus stays on the content (if focusable) rather than the scroll host.
- Mouse wheel scroll works for tall content.
- Shift + mouse wheel scroll works horizontally for wide content (via the scroll viewer input behavior).

## Rendering

`Popup.RenderOverride` fills `_popupRect` with spaces using `PopupStyle.ResolveSurfaceStyle(theme)`.

Important: popups render their surface using blank glyphs. The resolved surface style explicitly specifies the text style
so that decorations (for example underline) do not leak from underlay content into the popup surface. This is validated by tests.

Popups do not draw a border by default; border/chrome is usually provided by wrapping the content (for example `Border` or `Group`).

## Input handling

- Outside click closes the popup.
- Escape closes the popup.
- When `IsDraggable` is enabled, dragging within the top `DragHandleHeight` rows moves the popup by updating `OffsetX`/`OffsetY`.

Tab handling is implemented at the app level:

- When Tab is pressed, `TerminalApp` checks the active modal root and closes it if it is a `Popup` and `CloseOnTab` is true.

## Styling

Popups use:

- `PopupStyle` (`src/XenoAtom.Terminal.UI/Styling/PopupStyle.cs`):
  - `Padding`
  - `SurfaceStyle` (or theme-derived popup surface fill)

Notes:

- `PopupStyle.BorderStyle` exists but is not currently used by `Popup` rendering.
  Border/chrome is provided by wrapper visuals such as `Border`/`Group` or a custom popup template in the calling control.

## Tests, demos, and docs

Tests:

- `src/XenoAtom.Terminal.UI.Tests/PopupTests.cs` (placement, flipping/shrinking, outside click, tab close, dragging, scrolling)
- `src/XenoAtom.Terminal.UI.Tests/OverlaySurfaceTextStyleLeakTests.cs` (popup surface should not inherit text decorations)

Demo:

- `samples/ControlsDemo/Demos/PopupDemo.cs`

User documentation:

- `site/docs/controls/popup.md`

## Future ideas

- Add an optional built-in border/chrome mode (or remove unused `PopupStyle.BorderStyle` if wrappers remain the intended pattern).
- Add an optional "close on focus lost" mode for transient popups that should dismiss when focus moves away.
- Add explicit constraints for max popup size (separate from viewport clamping) for consistent UX across apps.
