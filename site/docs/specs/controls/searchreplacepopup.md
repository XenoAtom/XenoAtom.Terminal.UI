---
title: SearchReplacePopup Specs
---

# SearchReplacePopup Specs

This document captures the design and implementation details of `SearchReplacePopup` and its integration surface.

> [!NOTE]
> For end-user usage and examples, see [SearchReplacePopup](../../controls/searchreplacepopup.md).

## Overview

- **Status**: Implemented
- **Purpose**: Reusable find / find-and-replace popup UI used by host controls (for example `TextArea` and `LogControl`).
- **Hosting model**: A lightweight, non-focusable anchor `Visual` that opens a `Popup` window in the app window layer.
- **Target-driven**: All search, match navigation, and replace behavior is delegated to an `ISearchReplaceTarget` implementation.

## Goals

- Provide a consistent search UI that multiple controls can reuse without duplicating widget code.
- Keep the popup logic separate from the search engine:
  - the popup owns input fields, toggles, and buttons
  - the target owns query execution, match tracking, highlighting, and replace operations
- Preserve focus and restore it when the popup closes.
- Support moving the popup via mouse drag (header drag handle).

## Non-goals

- Automatic integration with every text-like control. Each host must opt in and implement a target.
- Implementing search algorithms inside the popup (regex, whole word, etc. are target responsibilities).
- Working in non-fullscreen apps (popups require a window layer).

## Public API

### SearchQuery

`SearchQuery` is a value type passed to targets:

- `Text : string?`
- `CaseSensitive : bool`
- `WholeWord : bool`
- `UseRegex : bool`

### SearchReplaceMode

- `Find`
- `Replace`

### ISearchReplaceTarget

Host controls implement `ISearchReplaceTarget`:

- `Title : string`
- `SupportsReplace : bool`
- `SetQuery(in SearchQuery query)`
- `NextMatch()` / `PreviousMatch()`
- `ReplaceCurrent(string replacement)`
- `ReplaceAll(string replacement)`
- `GetStatusText()` (for example "3/10" or "0 matches")
- `GetErrorText()` (for example invalid regex message)

The target is responsible for match computation, highlighting, and editor navigation.

### SearchReplacePopup

Bindable properties:

- `SearchText : string?`
- `ReplaceText : string?`
- `CaseSensitive : bool`
- `WholeWord : bool`
- `UseRegex : bool`
- `Mode : SearchReplaceMode`

Other properties and methods:

- `IsOpen : bool` (derived)
- `ClearQueryOnClose : bool` (clears the query sent to the target when closing)
- `Query : SearchQuery` (derived from the bindable fields)
- `OpenFind(string? initialSearchText = null) : bool`
- `OpenReplace(string? initialSearchText = null) : bool`
- `Close()`
- `ResetPosition()` (resets drag offsets)
- `ArrangeWithin(in Rectangle hostRect)` (host integration hook)

## Styling

`SearchReplacePopup` does not currently have a dedicated style type. It composes existing controls and styles:

- `TextBox` for search and replace inputs
- `Switch` for toggles (Case, Word, Regex)
- `Button` for navigation and replace actions
- `Group` for popup chrome
- `Popup` for window-layer hosting

The error message is a `TextBlock` styled with a theme-derived error foreground color.

## Tests & demos

Tests:

- `src/XenoAtom.Terminal.UI.Tests/TextAreaSearchReplaceTests.cs` (integration via `TextArea`, Tab behavior, navigation, replace all)
- `src/XenoAtom.Terminal.UI.Tests/SearchReplacePopupDragTests.cs` (drag repositioning)

Demo:

- `samples/ControlsDemo/Demos/SearchReplacePopupDemo.cs`

User documentation:

- `site/docs/controls/searchreplacepopup.md`

## Hosting and layout model

### Anchor visual

`SearchReplacePopup` itself is a non-focusable anchor visual. The host should:

1. Create a popup instance and attach it as a child.
2. Call `ArrangeWithin(hostRect)` during the host arrange pass.

`ArrangeWithin` places the popup anchor at:

- `x = hostRect.Right`
- `y = hostRect.Y`
- width/height = 0

This anchor point is chosen so that a `Popup` with `Placement = Left` uses the anchor X as the popup's right edge, which
typically keeps the popup inside the host area.

### Popup window

When opened, `SearchReplacePopup` creates a `Popup` window with:

- `Anchor = this`
- `Placement = PopupPlacement.Left`
- `MatchAnchorWidth = false`
- `CloseOnTab = false` (important so Tab stays inside the popup UI)
- `IsDraggable = true` and `DragHandleHeight = 1` (dragging the header row)
- `OffsetX` / `OffsetY` initialized from the last stored offsets so the popup remembers its position while open/close cycles.

Popups only work in fullscreen apps. If `Popup.Show()` throws, the search popup ignores it and reports failure from `OpenFind`/`OpenReplace`.

## Query application and rebuild behavior

### Applying query changes

When the bindable fields change (`SearchText`, `CaseSensitive`, `WholeWord`, `UseRegex`), the popup calls:

- `_target.SetQuery(Query)`

This call is suppressed while the popup is bulk-updating state (for example while opening).

### Mode switching and replace support

- If a target does not support replace, setting `Mode` to Replace is coerced back to Find.
- When the mode changes (Find <-> Replace), the popup rebuilds its UI if currently open (so the replace input row and buttons appear/disappear).

## Focus management

When opening, the popup stores the previously focused visual.
When closing (and not in a "rebuild" close), it restores focus to the original element.

## Input behavior

Popup-level keyboard handling (attached to the `Popup`):

- Enter or F3:
  - Shift pressed: `PreviousMatch()`
  - otherwise: `NextMatch()`
- Ctrl+H toggles Find <-> Replace (only when the target supports replace)

Mouse:

- The popup can be repositioned by dragging its header row (handled by `Popup` drag support).

## Known limitations

- There is no dedicated `SearchReplacePopupStyle` surface; customization is done by styling composed child controls or by wrapping the popup content.
- The demo may mention keyboard repositioning; the current implementation only supports repositioning by mouse drag (plus `ResetPosition()`).
- Targets own match status and errors; the popup just displays `GetStatusText()` and `GetErrorText()`.

## Future ideas

- Add a dedicated style for popup layout and button labels so hosts can theme it without replacing visuals.
- Add a richer status model (for example expose current match index and total as numbers) to avoid string parsing in hosts.
- Add optional "close on focus lost" behavior for hosts that want transient search UI.
- Add optional keyboard-based repositioning (if desired) via explicit commands.
