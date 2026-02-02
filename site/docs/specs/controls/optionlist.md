---
title: OptionList Specs
---

# OptionList Specs

This document captures design and implementation notes for `OptionList<T>`.

> [!NOTE]
> For end-user usage and examples, see [OptionList](../../controls/optionlist.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display a vertical list of options with selection, hover, and optional activation.
- **Scrolling**: Implements `IScrollable` with an internal `ScrollModel` (supports vertical + horizontal scrolling).
- **Templating**: Uses `DataTemplate<T>` for item visuals, with recycling (`TryUpdate` / `Release`) to reduce allocations.
- **Option metadata**:
  - per-item enabled state (`ItemIsEnabled`)
  - per-item search text (`ItemSearchText`) for type-to-jump
  - richer layout via `OptionListItem` (content + shortcut + description)

## Public API surface

### Type

- `OptionList<T> : Visual, IScrollable` (sealed)

### Bindable properties

- `Items : BindableList<T>`
- `SelectedIndex : int`
  - clamped to the nearest enabled item (see `ItemIsEnabled`)
- `ActivateOnClick : bool`
  - when enabled, clicking an item raises an activation event
- `ItemTemplate : DataTemplate<T>`
  - display template for items (local override; can fall back to `DataTemplates`)
- `ItemIsEnabled : Delegator<Func<T, bool>>`
  - optional callback for disabling specific items
- `ItemSearchText : Delegator<Func<T, string?>>`
  - optional callback used for type-to-jump search strings

### Routed events

- `SelectionChanged` (bubble): raised when selection changes (`OldIndex`/`NewIndex`)
- `ItemActivated` (bubble): raised when an item is activated (click/keyboard depending on configuration)

### Scrolling

- `Scroll : ScrollModel` (content-owned scroll model)

## Item visuals & OptionListItem

`OptionList<T>` creates one visual per item and attaches them as children.

The library also provides `OptionListItem` as a convenience visual for richer rows:

- `OptionListItem.Content : Visual?` (main label/content)
- `OptionListItem.Shortcut : Visual?` (right-aligned shortcut hint)
- `OptionListItem.Description : Visual?` (second line)
- `OptionListItem.SearchText : string?` (override for type-to-jump)

`OptionListItem` measures:

- width as the max of:
  - `Content` width (plus optional `Shortcut` + gap)
  - `DescriptionIndent + Description` width
- height:
  - `1` when no description
  - `2` when description is present

## Layout & scrolling behavior

### Prepare

`PrepareChildren` snapshots scroll state:

- `ScrollVersion = _scroll.Version`

`Arrange`/`Render` then read `ScrollVersion` as a dependency while still being able to update the underlying scroll model safely.

### Measure

Measure computes:

- prefix width:
  - `markerWidth = runeWidth(OptionListStyle.MarkerGlyph)`
  - `prefixWidth = markerWidth + OptionListStyle.SpaceBetweenGlyphAndText`
- item desired width/height:
  - items are measured unbounded to find max width and max height across items
  - `MeasuredItemHeight = max(1, maxItemHeight)`
- `MeasuredContentWidth = prefixWidth + maxItemWidth`
- desired height = `items.Count * MeasuredItemHeight` (at least 1)

When the height is bounded and vertical overflow is expected, measure reserves extra width for a vertical scrollbar using `ScrollViewerStyle.ScrollBarThickness` to avoid “bar introduces overflow” loops.

### Arrange

Arrange:

- reads `_ = ScrollVersion` and ensures item visuals exist
- computes `viewportHeight` as a multiple of `MeasuredItemHeight` so scrolling stays row-aligned
- sets scroll viewport and extent:
  - extent height = `count * itemHeight`
  - extent width = `max(innerWidth, MeasuredContentWidth)` for horizontal scrolling
- clamps/aligns offsets:
  - vertical offsets are aligned to full rows (`OffsetY` snapped to multiples of `itemHeight`)
- arranges each item at the correct translated position based on scroll offsets

Selection changes set a flag so `Arrange` can call `EnsureSelectedVisible(...)`.

## Interaction

### Keyboard

`OptionList<T>` supports typical list navigation and selection changes, and it also supports “type-to-jump”:

- typed characters build a short-lived buffer (`_typeBuffer`) used to find the next matching item based on `ItemSearchText`, `OptionListItem.SearchText`, visible text content, or `ToString()`.

### Pointer

- hover tracking sets an internal `HoveredIndex` (used for hover styling)
- click selects, and optionally activates (`ActivateOnClick`)

## Styling

### OptionListStyle

Key knobs:

- `MarkerGlyph`, spacing and indent:
  - `SpaceBetweenGlyphAndText`
  - `SpaceBetweenContentAndShortcut`
  - `DescriptionIndent`
- styles:
  - `Item`, `SelectedFocused`, `SelectedUnfocused`, `Hovered`, `Disabled`

Default resolution uses theme colors:

- hovered style uses `Theme.ControlFillHover` / `Theme.SurfaceAlt` when available
- focused selection is bold and uses `Theme.FocusBorder`/accent-like colors

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/OptionListTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs` (OptionList inside ScrollViewer sizing rules)
- Demo:
  - ControlsDemo includes OptionList examples (including shortcuts/description patterns).

## Future / v2 ideas

- Support multi-selection and/or checkable option lists.
- Add virtualization for very large lists.
