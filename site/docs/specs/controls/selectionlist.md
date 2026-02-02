---
title: SelectionList Specs
---

# SelectionList Specs

This document captures design and implementation notes for `SelectionList<T>`.

> [!NOTE]
> For end-user usage and examples, see [SelectionList](../../controls/selectionlist.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: A list control that supports multi-selection via checkboxes, plus a focused “cursor row”.
- **Selection model**:
  - `SelectedIndex` controls the focused row (navigation)
  - `Checked` is a parallel list of booleans (multi-select state)
- **Scrolling**: Implements `IScrollable` with an internal `ScrollModel`.
- **Templating**: Uses `DataTemplate<T>` to render item content, with recycling via `TryUpdate` / `Release`.

## Public API surface

### Type

- `SelectionList<T> : Visual, IScrollable` (sealed)

### Bindable properties

- `Items : BindableList<T>`
- `Checked : BindableList<bool>`
  - kept aligned with `Items` count (missing entries default to `false`)
- `SelectedIndex : int`
  - clamped to `-1` when empty, otherwise `[0..Items.Count-1]`
- `ItemTemplate : DataTemplate<T>`

### Helpers

- `AddItem(T item, bool isChecked = false)` appends to both `Items` and `Checked`.

### Scrolling

- `Scroll : ScrollModel`

## Checked list alignment

The control maintains the invariant:

- `Checked.Count == Items.Count`

This is enforced via `EnsureCheckedCount()` and is called from:

- layout phases (`Measure`, `Render`)
- interaction handlers (toggle/select all/invert)
- item visual rebuild

When items are removed, trailing entries in `Checked` are removed accordingly.

## Layout & scrolling behavior

### Prepare

`PrepareChildren` snapshots scroll state:

- `ScrollVersion = _scroll.Version`

This is the standard pattern used to avoid “read then write the same bindable” violations when `Arrange` updates the scroll model.

### Measure

Measure computes a prefix area and then measures the item visuals:

- prefix width:
  - marker column: `runeWidth(SelectionListStyle.FocusMarkerGlyph)`
  - checkbox column: `max(runeWidth(CheckedGlyph), runeWidth(UncheckedGlyph))`
  - plus `SelectionListStyle.SpaceBetweenGlyphAndText`
- measures each item with `MaxWidth = Infinite, MaxHeight = 1` and takes the max width
- `MeasuredContentWidth = prefixWidth + maxItemWidth`
- desired height = `max(1, Items.Count)`

When height is bounded and vertical overflow is expected, it reserves extra width for a vertical scrollbar using `ScrollViewerStyle.ScrollBarThickness` to avoid horizontal overflow feedback loops.

### Arrange

- Reads `_ = ScrollVersion` and ensures item visuals exist.
- Updates scroll model:
  - viewport = `(innerWidth, innerHeight)`
  - extent = `(max(innerWidth, MeasuredContentWidth), itemCount)`
- Ensures the selected row is visible when selection changes.
- Arranges each item at:
  - `x = innerLeft + prefixWidth - OffsetX`
  - `y = innerTop + (itemIndex - OffsetY)`
  - `width = extentWidth - prefixWidth`
  - `height = 1`

### Render

The list paints its own chrome, then items render themselves:

- resolves per-row style via `SelectionListStyle.ResolveItemStyle(theme, enabled, selectedRow, focused)`
- draws:
  - focus marker glyph on the selected row (`FocusMarkerGlyph`)
  - checked/unchecked glyph from `Checked[itemIndex]`
  - gap spaces after the checkbox to keep style consistent

## Interaction

### Keyboard

- navigation: `Up/Down/Home/End/PageUp/PageDown` updates `SelectedIndex`
- toggle: `Space` / `Enter` flips `Checked[SelectedIndex]`
- select all: `Ctrl+A` sets all `Checked[i] = true`
- invert: `Ctrl+I` toggles all `Checked[i]`

### Pointer

- left click on a row:
  - selects it
  - toggles its checked state
- wheel: moves `SelectedIndex` by one row

## Styling

### SelectionListStyle

Key knobs:

- glyphs:
  - `FocusMarkerGlyph` (default `→`)
  - `CheckedGlyph` / `UncheckedGlyph` (defaults `☑` / `☐`)
- `SpaceBetweenGlyphAndText`
- styles: `Item`, `SelectedFocused`, `SelectedUnfocused`, `Disabled`

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/SelectionListTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs`
- Demo:
  - ControlsDemo includes SelectionList examples.

## Future / v2 ideas

- Expose a higher-level selection model (selected items, selected indices) instead of the parallel `Checked` list for ergonomics.
- Add range selection (Shift+Up/Down) when terminal key encoding supports it.
