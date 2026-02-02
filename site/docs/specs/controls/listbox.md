---
title: ListBox Specs
---

# ListBox Specs

This document captures design and implementation notes for `ListBox<T>`.

> [!NOTE]
> For end-user usage and examples, see [ListBox](../../controls/listbox.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display a vertical list of items with a single selection.
- **Scrolling**: Implements `IScrollable` with an internal `ScrollModel`, supporting both vertical and horizontal scrolling.
- **Templating**: Uses `DataTemplate<T>` to create item visuals, with recycling via `TryUpdate` / `Release` to reduce allocations.

## Public API surface

### Type

- `ListBox<T> : Visual, IScrollable` (sealed)

### Bindable properties

- `Items : BindableList<T>`
  - items collection; tracked via `Items.Version` for efficient rebuilds
- `SelectedIndex : int`
  - clamped to `-1` when empty, otherwise `[0..Items.Count-1]`
- `ItemTemplate : DataTemplate<T>`
  - when empty, a style-provided default template may be used (`DataTemplates` for role `Display`)

### Scrolling

- `Scroll : ScrollModel`
  - `ScrollModel` is owned by the list box and updated from `Arrange`
  - consumers (e.g. `ScrollViewer`) can read it to show scroll bars

## Item visual generation & recycling

`ListBox<T>` maintains:

- `_itemVisuals : BindableList<Visual>` (children)
- `_recyclePool : List<Visual>` (detached visuals eligible for reuse)
- `_lastItemsVersion` and `_lastResolvedTemplate` (to avoid unnecessary rebuilds)

Rebuild triggers:

- `Items.Version` changes
- resolved template changes (local `ItemTemplate` or `DataTemplates` fallback)

Build rules per item:

- if `T` is already a `Visual`, it is used directly (no templating)
- if no template is available, it falls back to `new TextBlock(() => ToStringObject(value))`
- otherwise:
  - wraps the item in `DataTemplateValue<T>`
  - tries to reuse a visual from the recycle pool using `DataTemplate.TryUpdate`
  - if reuse fails, optionally calls `DataTemplate.Release` and creates a new visual from `DataTemplate.Display`

## Layout & scrolling behavior

### Prepare

`PrepareChildren` snapshots the scroll model version:

- `ScrollVersion = _scroll.Version`

This is a standard pattern in the library: `Arrange`/`Render` can *write* to the scroll model, so they read a separate bindable (`ScrollVersion`) to participate in dependency tracking without violating the “read then write in the same context” rule.

### Measure

- Ensures item visuals exist.
- Measures each item with `MaxWidth = Infinite`, `MaxHeight = 1` (one row per item).
- Computes `MeasuredContentWidth = maxItemWidth + 2` (marker + space).
- Desired height is `max(1, Items.Count)`.

When the height is bounded and the list would overflow vertically, it reserves extra width for an expected vertical scroll bar:

- `reservedWidth = contentWidth + ScrollViewerStyle.ScrollBarThickness`

This prevents a common “bar causes overflow causes horizontal bar” feedback loop when embedded in a `ScrollViewer`.

### Arrange

Arrange updates the `ScrollModel` and positions each item visual:

- reads `_ = ScrollVersion` (dependency)
- sets viewport: `SetViewport(innerWidth, innerHeight)`
- sets extent:
  - height = item count
  - width = `max(innerWidth, MeasuredContentWidth)` so horizontal scroll can exist if needed
- ensures the selected item stays visible after selection changes
- clamps vertical offset to `max(0, extentHeight - viewportHeight)`
- arranges items at:
  - `x = innerLeft + 2 - Scroll.OffsetX` (marker column + spacing)
  - `y = innerTop + (itemIndex - Scroll.OffsetY)`
  - `width = extentWidth - 2`
  - `height = 1`

### Render

`RenderOverride` paints the list background and selection chrome (items render themselves):

- reads `_ = ScrollVersion` (dependency)
- fills the background row area with the per-item style so child visuals inheriting `Style.None` get a consistent foreground/background
- draws a marker glyph (`ListBoxStyle.MarkerGlyph`) in the marker column for the selected row

## Input behavior

### Keyboard

- `Up/Down`: move selection by 1
- `PageUp/PageDown`: move by viewport height
- `Home/End`: jump to first/last item

Selection changes set a flag so `Arrange` scrolls the selected row into view.

### Pointer

- left click selects the item at `OffsetY + (UiY - Bounds.Y)`
- wheel scroll adjusts selection by one row (and relies on selection-to-scroll behavior)

## Styling

### ListBoxStyle

Key knobs:

- `MarkerGlyph` (default `→`)
- style variants: `Item`, `SelectedFocused`, `SelectedUnfocused`, `Disabled`
- default resolution uses theme foreground, bold selection, and focus-accented color when focused

## Tests & demos

- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/ListBoxInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/ListControlHorizontalScrollViewerTests.cs` (ListBox inside ScrollViewer sizing rules)
- Demos:
  - ControlsDemo includes ListBox examples and shows templating + scrolling.

## Future / v2 ideas

- Add optional multi-selection (range selection) and expose selection model as a separate abstraction.
- Add virtualization for very large lists (only create visuals for visible rows).
