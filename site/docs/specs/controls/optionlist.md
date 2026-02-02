---
title: OptionList Specs
---

# OptionList Specs

This document captures the design and implementation details of `OptionList<T>`.

> [!NOTE]
> For end-user usage and examples, see [OptionList](../../controls/optionlist.md).

## Overview

- **Status**: Implemented
- **Purpose**: Display a vertical list of options with single selection, hover state, and optional activation.
- **Scrolling**: Implements `IScrollable` via an owned `ScrollModel` (vertical and horizontal offsets, no built-in scrollbars).
- **Templating**: Uses `DataTemplate<T>` (display role) to build item visuals, with recycling support.
- **Rows**: Supports multi-line item visuals by using a uniform item height (the max height across items).

## Public API surface

### Type

- `public sealed partial class OptionList<T> : Visual, IScrollable`

### Bindable properties

- `Items : BindableList<T>`
- `SelectedIndex : int`
- `ActivateOnClick : bool`
- `ItemTemplate : DataTemplate<T>`
- `ItemIsEnabled : Delegator<Func<T, bool>>`
- `ItemSearchText : Delegator<Func<T, string?>>`

### Routed events

- `SelectionChanged` (bubble): raised when selection changes (`OldIndex`/`NewIndex`)
- `ItemActivated` (bubble): raised when an item is activated (click/keyboard depending on configuration)

### Scrolling

- `Scroll : ScrollModel` (owned scroll model)

## Item visuals

`OptionList<T>` maintains a child `Visual` for each item:

- If an item `T` is already a `Visual`, it is used directly.
- Otherwise, the list uses `ItemTemplate` (or the environment `DataTemplates`) in `DataTemplateRole.Display`.
- If no template is available, the list falls back to a `TextBlock` that calls `ToString()` on the item.

### Recycling

When the list rebuilds its item visuals (items version or resolved template change), removed visuals are added to a recycle pool.
During rebuild, the list may reuse pooled visuals when:

- `DataTemplate<T>.TryUpdate` is provided and returns `true`.
- Otherwise the pooled visual is released via `DataTemplate<T>.Release` (if provided) and a new visual is created.

## OptionListItem (convenience row)

The library provides `OptionListItem : Visual` as a convenient row visual for menus and command-like lists:

- `Content : Visual?` - main label/content (line 1)
- `Shortcut : Visual?` - optional shortcut area (line 1, right-aligned)
- `Description : Visual?` - optional second line, indented
- `SearchText : string?` - optional search text for type-to-jump

`OptionListItem` measures:

- width as the max of:
  - `Content` width (plus optional `Shortcut` + gap)
  - `DescriptionIndent + Description` width
- height is `1` when `Description` is null, otherwise `2`.

## Layout & scrolling behavior

### PrepareChildren and scroll version

`PrepareChildren` snapshots scroll state:

- `ScrollVersion = _scroll.Version`

`Arrange` and `Render` read `ScrollVersion` so scroll changes trigger layout/render updates without causing "read then write"
dependency loops (a common pattern for `IScrollable` controls in this codebase).

### Measure

Measure:

- Ensures item visuals exist (see above) and measures every child with unbounded constraints.
- Computes a prefix width:
  - `markerWidth = runeWidth(OptionListStyle.MarkerGlyph)` (minimum 1)
  - `prefixWidth = markerWidth + OptionListStyle.SpaceBetweenGlyphAndText`
- Stores:
  - `MeasuredItemHeight` = the maximum item `DesiredSize.Height` (minimum 1)
  - `MeasuredContentWidth` = `prefixWidth + maxItemWidth`
- Computes desired height as `items.Count * MeasuredItemHeight` (minimum 1).

Important: the list uses a uniform item height (max across items). If you mix 1-row and 2-row items, all rows will reserve
2 rows of height.

### ScrollViewer interaction (reserved width)

When the available height is bounded and the content is taller than the available height, measure reserves additional width
equal to `ScrollViewerStyle.ScrollBarThickness`. This avoids a common loop where adding a vertical scrollbar reduces width,
which would introduce horizontal overflow.

### Arrange

Arrange:

- Ensures item visuals exist.
- Chooses a viewport height that is an integer multiple of `MeasuredItemHeight` so scrolling stays row-aligned.
- Updates the scroll model:
  - `ViewportWidth = bounds.Width`
  - `ViewportHeight = viewportItems * itemHeight`
  - `ExtentHeight = itemCount * itemHeight`
  - `ExtentWidth = max(viewportWidth, MeasuredContentWidth)`
- Keeps `OffsetY` clamped and aligned to full item rows.
- Arranges every item visual (including offscreen ones). Offscreen items will be arranged outside the list bounds.

### EnsureSelectedVisible

When selection changes, the list sets a flag and scrolls in arrange so the selected row is visible:

- If the selection is above the viewport, it scrolls up to put it at the top.
- If the selection is below the viewport, it scrolls down to put it at the bottom.

## Interaction

### Keyboard

Keyboard navigation:

- Up/Down, PageUp/PageDown, Home/End: move selection
- Enter/Space: activate selected item (raises `ItemActivated`)
- Type-to-jump:
  - typing non-control characters appends to a 1-second buffer
  - the list selects the first item whose search text starts with the buffer (case-insensitive)

### Pointer

- Hover updates an internal `HoveredIndex` used for hover styling.
- Left click selects on press (if enabled).
- Left click can also activate on release when `ActivateOnClick` is true and the press/release happened on the selected row.
- Mouse wheel moves selection by 1 step (and selection scrolling keeps it visible).

## Styling

### OptionListStyle

OptionList uses `OptionListStyle` (`src/XenoAtom.Terminal.UI/Styling/OptionListStyle.cs`):

- `MarkerGlyph` and spacing:
  - `SpaceBetweenGlyphAndText`
  - `SpaceBetweenContentAndShortcut` (used by `OptionListItem`)
  - `DescriptionIndent` (used by `OptionListItem`)
- Item styles:
  - `Item`, `SelectedFocused`, `SelectedUnfocused`, `Hovered`, `Disabled`

The default style resolution uses theme colors (accent and hover fill) when available.

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/OptionListTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs` (OptionList inside ScrollViewer sizing rules)
- Demo:
  - `samples/ControlsDemo/Demos/OptionListDemo.cs`

## Future / v2 ideas

- Virtualize and/or lazily create item visuals for very large lists (today the list measures and arranges all items).
- Add multi-selection and/or checkable rows as a separate control or mode.
- Add richer search (wrap-around, next-match cycling, highlight).
