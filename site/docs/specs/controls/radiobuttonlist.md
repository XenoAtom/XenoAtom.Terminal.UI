---
title: RadioButtonList Specs
---

# RadioButtonList Specs

This document captures design and implementation notes for `RadioButtonList<T>`.

> [!NOTE]
> End-user documentation is currently covered under [RadioButton](../../controls/radiobutton.md) (list variants are demonstrated in the ControlsDemo).

## Overview

- **Status**: Implemented
- **Primary purpose**: A single-selection list that renders selection state using radio button glyphs.
- **Scrolling**: Implements `IScrollable` with an internal `ScrollModel`.
- **Templating**: Uses `DataTemplate<T>` and a simple recycle pool to reduce allocations when the item list changes.

## Public API surface

### Type

- `RadioButtonList<T> : Visual, IScrollable` (sealed)

### Bindable properties

- `Items : BindableList<T>`
- `SelectedIndex : int`
  - clamped to `-1` when empty, otherwise `[0..Items.Count-1]`
- `ItemTemplate : DataTemplate<T>`
  - optional; can fall back to style-provided `DataTemplates` for role `Display`

### Scrolling

- `Scroll : ScrollModel`

## Item visuals & recycling

The list maintains:

- `_itemVisuals : BindableList<Visual>` (children)
- `_recyclePool : List<Visual>` (detached visuals eligible for reuse)
- `_lastItemsVersion` + `_lastResolvedTemplate` (to avoid rebuilds when nothing changed)

Build rules per item:

- if `T` is already a `Visual`, it is used directly
- if no template is available, it falls back to `TextBlock(() => ToStringObject(value))`
- otherwise:
  - creates `DataTemplateValue<T>(value)`
  - tries to reuse a visual via `DataTemplate.TryUpdate`
  - otherwise releases and recreates via `DataTemplate.Display`

## Layout & scrolling behavior

### Prepare

`PrepareChildren` snapshots the scroll model version into a bindable:

- `ScrollVersion = _scroll.Version`

`Arrange` and `Render` then read `ScrollVersion` as a dependency while still being able to update the underlying `ScrollModel` safely.

### Measure

- Computes a glyph “prefix width”:
  - `checkWidth = max(runeWidth(CheckedGlyph), runeWidth(UncheckedGlyph))`
  - `prefixWidth = checkWidth + SpaceBetweenGlyphAndText`
- Measures item visuals with `MaxWidth = Infinite, MaxHeight = 1` and takes the max width.
- `MeasuredContentWidth = prefixWidth + maxItemWidth`
- desired height = `max(1, itemCount)`

When height is bounded and vertical overflow is expected, it reserves extra width for a vertical scrollbar using `ScrollViewerStyle.ScrollBarThickness` to avoid “vertical bar reduces viewport ⇒ horizontal bar appears” feedback loops.

### Arrange

- Reads `_ = ScrollVersion` and ensures item visuals exist.
- Updates scroll model:
  - viewport = `(innerWidth, innerHeight)`
  - extent = `(max(innerWidth, MeasuredContentWidth), itemCount)`
- Ensures selected item visibility after selection changes.
- Arranges items at:
  - `x = innerLeft + prefixWidth - OffsetX`
  - `y = innerTop + (itemIndex - OffsetY)`
  - `width = extentWidth - prefixWidth`
  - `height = 1`

### Render

The list renders its chrome; items render themselves:

- Fills the viewport background with `Style.None` then per-row style.
- For each visible row:
  - resolves row style via `RadioButtonListStyle.ResolveItemStyle(...)`
  - draws either `CheckedGlyph` or `UncheckedGlyph` at the glyph column
  - draws the gap area as spaces to keep styles consistent

## Interaction

- Keyboard navigation: `Up/Down/Home/End/PageUp/PageDown` updates `SelectedIndex`.
- Pointer:
  - left click selects the row under the pointer
  - mouse wheel moves selection by one row

Selection changes set a flag so `Arrange` scrolls the selection into view.

## Styling

### RadioButtonListStyle

Key knobs:

- `CheckedGlyph` / `UncheckedGlyph` (defaults match `RadioButtonStyle`)
- `SpaceBetweenGlyphAndText`
- `Item`, `SelectedFocused`, `SelectedUnfocused`, `Disabled`

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/RadioButtonListTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs`
- Demo:
  - ControlsDemo includes RadioButtonList examples.

## Future / v2 ideas

- Support disabled items / skipping disabled items (like `OptionList<T>` does) if needed.
- Add multi-line row support (variable item height) if a richer row model is required.
