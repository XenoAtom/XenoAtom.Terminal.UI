---
title: Select / Dropdown Specs
---

# Select / Dropdown Specs

This document captures design and implementation notes for `Select<T>` (a compact dropdown).

> [!NOTE]
> For end-user usage and examples, see [Select / Dropdown](../../controls/select.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Allow selecting a single item from a list by opening a popup list.
- **Key goals**:
  - small and fast control suitable for forms
  - predictable layout: selected content plus a trailing "arrow" indicator
  - low allocation templating: reuse the selected visual when possible
  - integrate with `Popup` for dismissal behaviors (outside click, `Esc`, `Tab`, etc.)
- **Non-goals**:
  - multi-select (use `SelectionList<T>` / other list controls)
  - built-in search UI in the dropdown itself (this control delegates list interaction to `ListBox<T>`)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/Select.cs`
  - `src/XenoAtom.Terminal.UI/Styling/SelectStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SelectTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/SelectDemo.cs`

## Public API surface

### Type

- `Select<T> : ContentVisual`

### Properties

- `Items : BindableList<T>`
  - Created in the constructor and owned by the control.
  - Changes to the list affect selection clamping and selected content rebuilding.
- `SelectedIndex : int` (bindable)
  - The single selection state.
  - Clamped to `[0 .. Items.Count - 1]` when `Items.Count > 0`.
  - When `Items.Count == 0`, the internal selected content is cleared and the effective selected index is `-1`.
- `ItemTemplate : DataTemplate<T>` (bindable)
  - Used for both:
    - the selected value (collapsed state)
    - the items inside the popup list (expanded state)
  - When empty, the control resolves a display template from environment `DataTemplates` (role: `Display`).
  - If no template is available, the control falls back to rendering `value.ToString()` in a `TextBlock`.

### Events

- `SelectionChanged` routed event (bubble).
  - The event is raised after clamping so that the event reports the effective selection.

## Data templating behavior

### Selected content creation and reuse

`Select<T>` uses the `ContentVisual.Content` slot to render the selected value.

The selected content is updated on:
- `SelectedIndex` changes
- `ItemTemplate` changes (forced rebuild)
- measure, as a safety net (ensures the selected visual exists before measuring)

Template resolution for the selected content is:
1) `ItemTemplate` if non-empty
2) `DataTemplates.TryResolve(DataTemplateRole.Display, out template)` from the environment
3) default fallback (empty template)

When the selected value is not a `Visual`, the control creates the visual via `DataTemplate<T>.Display(...)`.

To avoid rebuilding visuals on every selection update, it uses `DataTemplate<T>.TryUpdate` when all of the following
conditions are true:
- the resolved template equals the previously resolved template
- there is an existing `Content` visual
- `TryUpdate` returns `true`

When the selected value is already a `Visual`, the control uses it directly as `Content` (no template is applied).

## Layout and rendering

### Measure

`Select<T>` measures as a single-line control with a content area and a trailing arrow indicator.

Key details:
- The arrow width is computed from `SelectStyle.ArrowGlyph` using `TerminalTextUtility.GetWidth(...)`.
- The content is measured with a max width of:
  - `constraints.MaxWidth - style.Padding.Horizontal - arrowWidth` (when finite)
- The returned size adds back:
  - `style.Padding.Horizontal + arrowWidth` to width
  - `style.Padding.Vertical` to height
- The minimum width and natural width are clamped to at least `3` cells.
- The minimum height and natural height are clamped to at least `1` cell.

### Arrange

Arrange places the selected `Content` inside an inner rectangle:
- `inner = finalRect` minus `SelectStyle.Padding`
- the content rectangle reserves space on the right for the arrow by subtracting `arrowWidth`

### Render

Render performs two operations:
- fills the entire `Bounds` rectangle with spaces using the resolved style (a stable background baseline)
- writes the arrow glyph at the right edge using `TextStyle.Dim`

Implementation note: the arrow is currently written on the first row (`y = Bounds.Y`), even if the control is taller
than 1 cell.

## Input behavior

### Mouse

- Left click opens the popup.

### Keyboard

- `Enter` / `Space`: open the popup.
- `Up` / `Down` (when the popup is closed): quick selection changes by +/- 1 within `[0 .. Items.Count - 1]`.

`Select<T>` currently implements these behaviors directly in `OnKeyDown` / `OnPointerPressed` (it does not register
commands for them yet).

## Popup behavior

### Popup creation

When opened, `Select<T>` creates a `Popup` with:
- `Anchor = this`
- `Placement = Below`
- `MatchAnchorWidth = true` (dropdown matches the collapsed control width)
- `AdditionalWidth = 2` (extra space for borders/padding in the popup template)

The popup content is a `ListBox<T>` configured with:
- `.Items(Items)` (same list instance as the owner)
- `.ItemTemplate(resolvedTemplate)`
- `SelectedIndex` initialized to the clamped owner selection

The list is optionally wrapped via `SelectStyle.PopupTemplateFactory` (default: wraps with a `Border` that stretches).

### Selecting and closing

- Clicking inside the list (left button) selects `owner.SelectedIndex = list.SelectedIndex` and closes the popup.
- Pressing `Enter` / `Space` in the list selects and closes the popup.
- When the popup closes (for any reason), the owner clears its internal popup reference and requests focus back to the
  `Select<T>`.

Dismissal mechanisms like "click outside" / `Esc` / `Tab` are handled by the `Popup` control.

## Styling

### SelectStyle

`SelectStyle` controls:
- `Padding`
- `ArrowGlyph`
- state styles: `NormalStyle`, `HoverStyle`, `FocusedStyle`, `DisabledStyle`
- `PopupTemplateFactory` for wrapping the dropdown list surface

Default behavior:
- when no explicit state style is provided, the control uses `Theme.Foreground` and applies `TextStyle.Bold` on focus
  and `TextStyle.Dim` when disabled.

## Future / v2 ideas

- Commands for key actions (`Open`, `SelectNext`, `SelectPrevious`) to improve CommandBar discoverability.
- Optional arrow placement improvements (center vertically when height > 1).
- Optional integrated search UI (if `ListBox<T>` does not provide the desired experience for dropdown use cases).
