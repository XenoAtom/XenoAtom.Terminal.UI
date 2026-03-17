---
title: TabControl Specs
---

# TabControl Specs

This document captures design and implementation notes for `TabControl`.

> [!NOTE]
> For end-user usage and examples, see [TabControl](../../controls/tabcontrol.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Display one tab page at a time, with a clickable header strip used for selection, closing, and overflow navigation.
- **Key goals**:
  - lightweight tab UI for terminal apps
  - tab headers are visuals (supports icons, counters, dynamic text, etc.)
  - tab pages are bindable model objects that can mutate in place
  - optional close buttons with state-aware styling
  - single-line overflow handling via left/right navigation buttons
  - optional content wrapper (e.g. border) via a template factory

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TabControl.cs`
  - `src/XenoAtom.Terminal.UI/Controls/TabPage.cs`
  - `src/XenoAtom.Terminal.UI/Styling/TabControlStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TabControlInteractionTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TabControlRenderingTests.cs`
  - `src/XenoAtom.Terminal.UI.Tests/TabControlFeatureTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TabControlDemo.cs`

## Public API surface

### Types

- `TabControl : Visual` (sealed)
- `TabPage : record class, IVisualElement`
- `TabCloseReason`
- `TabPageClosingEventArgs`
- `TabPageClosedEventArgs`

### Layout defaults (TabControl constructor)

- `Focusable = true`
- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Stretch`

### Properties

- `SelectedIndex : int` (bindable)
  - determines which tab content is shown
  - keyboard navigation skips disabled tabs
- `FirstVisibleIndex : int` (bindable)
  - preferred starting tab index for the visible header window when overflow buttons are active
- `Tabs : IReadOnlyList<TabPage>`
  - read-only view over the bindable internal list

### Methods

- `AddTab(Visual header, Visual content)`
- `AddTab(TabPage page)`
- `TryCloseTab(int index)`
- `TryCloseTab(TabPage page)`

### TabPage

`TabPage` is a bindable state container:

- `Header : Visual`
- `Content : Visual`
- `IsEnabled : bool`
- `ShowCloseButton : bool`
- `Data : object?`
- `RequestClosing` event
- `Closed` event

Because `TabPage` implements `IVisualElement`, page property changes participate in dependency tracking once the page is attached to a `TabControl`.

## Child and content model

`TabControl` attaches:

- all tab headers as children (always attached while their page is present)
- a single content root as the final child (always attached once initialized)

Only the selected content is hosted at any given time:

- an internal `ContentVisual` host (`TabContentHost`) contains the selected `TabPage.Content`
- that host is optionally wrapped by a template (`TabControlStyle.TabContentTemplateFactory`)

When a bound `TabPage.Header` changes while attached:

- the old header visual is detached
- the new header visual is attached

When a bound `TabPage.Content` changes while that page is selected:

- the content host switches to the new content visual immediately

## Layout and rendering

### PrepareChildren

`PrepareChildren`:

- resolves `TabControlStyle` and ensures a content template exists
- clears content when there are no tabs
- otherwise hosts the selected page content using a clamped `SelectedIndex`

### Measure

Measurement considers:

- header strip desired size (headers + tab padding + optional close button reserve)
- selected content desired size

Close button layout reserve:

- width = `GetRuneWidth(CloseButtonRune) + CloseButtonSpacing`
- only applied when `TabPage.ShowCloseButton` is true

### Arrange

Arrange computes:

- `_headerHeight`
- the visible header window
- hit ranges for:
  - tab selection
  - per-tab close buttons
  - overflow previous/next buttons

Overflow behavior:

- headers are kept on a single row
- when total header width exceeds the arranged width, overflow buttons are reserved at the far left and far right
- `FirstVisibleIndex` determines where the visible window starts
- selection changes can adjust `FirstVisibleIndex` to keep the selected tab visible
- manual overflow-button navigation updates `FirstVisibleIndex` directly and may hide the selected tab header while keeping the selected content visible

Non-visible headers are arranged to a zero rectangle so stale bounds do not render.

### Render

Render draws:

1. the header strip background
2. overflow button surfaces and glyphs when overflow is active
3. tab header surfaces for the currently visible tabs
4. close button surfaces and glyphs for visible closable tabs

Header/background text is still rendered by child visuals.

State inputs:

- tab enabled: `TabPage.IsEnabled && TabPage.Header.IsEnabled && TabPage.Content.IsEnabled`
- tab focused: `TabControl.HasFocus`
- tab selected: `index == SelectedIndex`
- tab hovered/pressed: tracked per header part
- close button hovered/pressed: tracked separately from the tab body
- overflow buttons have their own hover/pressed/disabled state

## Input behavior

### Keyboard

When focused:

- `Left`: select previous enabled tab
- `Right`: select next enabled tab

There is no wrap-around.

### Mouse

Mouse interaction targets the header strip only:

- tab body click selects the tab
- close button click requests closure via `TabPage.RequestClosing`
- overflow buttons move `FirstVisibleIndex` backward/forward by one tab

Close requests:

- `TabPage.RequestClosing` is raised first and may set `Cancel = true`
- if not cancelled, the page is removed and `TabPage.Closed` is raised
- `TabControl.TryCloseTab(...)` uses the same lifecycle

## Styling

### TabControlStyle

Existing properties remain:

- `TabPadding`
- `StripStyle`
- `TabStyle`, `TabHoveredStyle`, `TabPressedStyle`, `TabSelectedStyle`, `TabDisabledStyle`
- `TabContentTemplateFactory`

New styling surface:

- `CloseButtonRune`
- `CloseButtonSpacing`
- `CloseButtonStyle`, `CloseButtonHoveredStyle`, `CloseButtonPressedStyle`, `CloseButtonDisabledStyle`
- `OverflowPreviousRune`, `OverflowNextRune`
- `OverflowButtonStyle`, `OverflowButtonHoveredStyle`, `OverflowButtonPressedStyle`, `OverflowButtonDisabledStyle`

Default close button behavior:

- normal: inherits the resolved tab style
- hovered: error-toned background (falling back to hover/surface colors)
- pressed: error-toned pressed background
- disabled: dimmed/disabled foreground

Default overflow button behavior:

- normal: tab/button surface styling
- hovered: hover surface
- pressed: pressed surface
- disabled: dimmed/disabled foreground

## Tests

- `TabControlInteractionTests` covers:
  - keyboard selection
  - mouse-based tab switching
  - visual headers and arrange bounds
- `TabControlRenderingTests` covers:
  - tab pressed-state rendering
  - close-button hover rendering
  - content wrapper templating
- `TabControlFeatureTests` covers:
  - close-button lifecycle
  - cancellation
  - disabled-tab interaction
  - in-place page mutation
  - overflow scrolling

## Notes

- `TabPage` remains a record class for compatibility, but now behaves as an attached, bindable model object.
- `Tabs` stays read-only at the public API boundary; list mutation still flows through `AddTab(...)` / `TryCloseTab(...)`.
