---
title: TabControl Specs
---

# TabControl Specs

This document captures design and implementation notes for `TabControl`.

> [!NOTE]
> For end-user usage and examples, see [TabControl](../../controls/tabcontrol.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display one tab page at a time, with a clickable header strip to switch pages.
- **Key goals**:
  - lightweight tab UI for terminal apps
  - tab headers are visuals (supports icons, counters, dynamic text, etc.)
  - only the selected tab content is attached/measured/rendered
  - styleable header strip and "button-like" tab header states
  - optional content wrapper (e.g., border) via a template factory
- **Non-goals**:
  - removing/reordering tabs in v1 (tabs are added imperatively)
  - header scrolling / overflow management (no horizontal scrolling in v1)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TabControl.cs`
  - `src/XenoAtom.Terminal.UI/Controls/TabPage.cs`
  - `src/XenoAtom.Terminal.UI/Styling/TabControlStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TabControlInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TabControlRenderingTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TabControlDemo.cs`

## Public API surface

### Types

- `TabControl : Visual` (sealed)
- `TabPage : record class` (header/content pair)

### Layout defaults (TabControl constructor)

- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`

### Properties

- `SelectedIndex : int` (bindable)
  - Determines which tab is selected (0-based index).
  - `OnKeyDown` clamps changes to `[0 .. Tabs.Count - 1]`.
  - Important: the implementation does not always coerce the property itself. During `PrepareChildren`, tab content
    selection uses a clamped index even if `SelectedIndex` is out of range.
- `Tabs : IReadOnlyList<TabPage>`
  - Read-only view of the internal tab list.

### Methods

- `AddTab(Visual header, Visual content)`
  - Validates that `header.Parent` and `content.Parent` are null (tab visuals must not already be attached elsewhere).
  - Attaches the header to the tab control's visual tree.
- `AddTab(TabPage page)`
  - Convenience overload.

### TabPage

`TabPage` is a simple pair:
- `Header : Visual`
- `Content : Visual`

Strings are commonly used as headers/contents via implicit conversion to `Visual` (a string becomes a `TextBlock`).

## Child and content model

`TabControl` attaches:
- all tab headers as children (always attached)
- a single content root as the final child (always attached once initialized)

Only the selected content is hosted at any given time:
- an internal `ContentVisual` host (`TabContentHost`) contains the selected `TabPage.Content`
- that host is optionally wrapped by a template (`TabControlStyle.TabContentTemplateFactory`)

Changing the content template factory replaces the content root visual:
- old content root is detached
- a new wrapper is created and attached
- the wrapper's `Content` is forced to be the internal content host

## Layout and rendering

### PrepareChildren

`PrepareChildren`:
- resolves `TabControlStyle` and ensures a content template exists (`EnsureContentTemplate(style)`)
- if no tabs exist, sets host content to null
- otherwise sets host content to the selected tab content using `Math.Clamp(SelectedIndex, 0, Tabs.Count - 1)`

### Measure

Measurement considers:
- header strip desired size (based on headers and padding)
- selected tab content desired size (via the content root)

Header measurement:
- each header is measured with infinite max width
- header height is the max of all header natural heights (minimum 1)
- tab header width is `header.Natural.Width + TabPadding.Horizontal`
- the measured header strip width is the sum of tab widths plus a 1-cell gap between tabs

Content measurement:
- content is measured with:
  - max width: `constraints.MaxWidth` (or infinite)
  - max height: `constraints.MaxHeight - headerHeight` (or infinite)

The returned hints are:
- `min` and `natural` are set to the computed (clamped) size `(max(headerWidth, contentWidth), headerHeight + contentHeight)`
- `max` is infinite in both dimensions
- grow/shrink are enabled (grow 1, shrink 1)

### Arrange

Arrange computes:
- `_headerHeight` as max header desired height, clamped to `[1 .. finalRect.Height]`
- `_hitRanges` for each arranged tab area

Headers are arranged left-to-right:
- each tab width is `header.DesiredSize.Width + TabPadding.Horizontal`, clamped to remaining width
- the header itself is arranged inside that tab width using left/right padding
- a 1-cell gap is reserved after every tab (including the last tab, as implemented)

Content is arranged below the header strip:
- content rectangle starts at `Y + _headerHeight`
- height is `finalRect.Height - _headerHeight`
- the content root is arranged into that rectangle

### Render

Render draws only the header strip backgrounds:
1) fills the header strip rectangle with spaces using `ResolveStripStyle(theme)`
2) for each hit range, fills that tab rectangle with spaces using `ResolveTabStyle(...)`

The actual header text and tab content are rendered by child visuals.

Tab style state inputs:
- enabled: `TabPage.Content.IsEnabled`
- focused: `TabControl.HasFocus`
- selected: `range.Index == SelectedIndex`
- hovered: `range.Index == HoveredIndex`
- pressed: `range.Index == PressedIndex && IsPressedInside`

## Input behavior

TabControl currently implements interactions directly in pointer/key handlers (it does not register commands yet).

### Keyboard

When focused:
- `Left`: `SelectedIndex = max(0, SelectedIndex - 1)`
- `Right`: `SelectedIndex = min(Tabs.Count - 1, SelectedIndex + 1)`

There is no wrap-around in v1.

### Mouse

Mouse interaction targets the header strip only (the first `_headerHeight` rows).

- Move:
  - updates `HoveredIndex` based on hit testing
  - updates `IsPressedInside` when dragging within the same pressed tab range
- Left press on a tab range:
  - sets `PressedIndex` and `IsPressedInside = true`
- Left release:
  - activates the tab only when released over the same pressed tab range while `IsPressedInside` is true
  - always clears pressed state afterwards

## Styling

### TabControlStyle

Key properties:
- `TabPadding : Thickness` (horizontal padding affects layout; vertical padding is currently not applied in arrange)
- `StripStyle : Style?` (tab strip background)
- `TabStyle`, `TabHoveredStyle`, `TabPressedStyle`, `TabSelectedStyle`, `TabDisabledStyle`
- `TabContentTemplateFactory : Func<Visual, ContentVisual?>?`
  - wraps the selected content host (default wraps in a `Border`)

Default style resolution:
- strip: `StripStyle ?? theme.BaseTextStyle()`
- normal tab: `TabStyle ?? theme.SurfaceStyle()`
- hovered: uses `theme.ControlFillHover ?? theme.SurfaceAlt` (background) and bold
- pressed: uses `theme.Selection` (background) and bold
- selected: bold and `theme.Accent` foreground
- focused + selected: underline and `theme.FocusBorder` foreground when available

There are several predefined `TabControlStyle` variants that swap the content wrapper border style (e.g. rounded/single/double/ascii).

## Tests

- `TabControlInteractionTests` covers:
  - left/right arrow selection changes
  - mouse click selection switching content
  - visual headers and bounds arrange
- `TabControlRenderingTests` covers:
  - header strip/tab background rendering and pressed state blending
  - application of `TabContentTemplateFactory` (verifies the rounded border template renders below the strip)

## Future / v2 ideas

- Commands for key actions (select next/previous) for CommandBar discoverability.
- Tab strip overflow behavior (scrolling or multi-row strip) for many tabs.
- Optional "close tab" affordances and tab removal APIs.
