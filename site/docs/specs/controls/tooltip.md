---
title: Tooltip Specs
---

# Tooltip Specs

This document captures design and implementation notes for `TooltipHost` and the tooltip overlay system.

> [!NOTE]
> For end-user usage and examples, see [Tooltip](../../controls/tooltip.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Show non-interactive hover hints as an overlay window in fullscreen apps.
- **Key goals**:
  - tooltips are non-modal and do not steal focus
  - tooltips do not intercept pointer events (hover/click go to the underlying control)
  - show after a delay while hovered; hide immediately on hover leave
  - only one tooltip is visible per app at a time
  - reuse popup-like placement logic (above/below/left/right with basic flipping)
- **Non-goals**:
  - tooltips in inline/live hosting (no window layer)
  - interactive tooltips (no buttons, no focus, no input capture)
  - anchored tooltip repositioning while the pointer moves (TooltipHost anchors to the content bounds; other controls can
    use an explicit anchor rect)

## Implementation notes

- Primary implementation:
  - `src/XenoAtom.Terminal.UI/Controls/TooltipHost.cs`
  - `src/XenoAtom.Terminal.UI/Controls/TooltipWindow.cs` (internal overlay window)
  - `src/XenoAtom.Terminal.UI/Styling/TooltipStyle.cs`
  - `src/XenoAtom.Terminal.UI/VisualExtensions.cs` (`.Tooltip(...)` convenience)
- Integration:
  - `src/XenoAtom.Terminal.UI/TerminalApp.cs` owns a single active tooltip window and routes it through the window layer.
  - `src/XenoAtom.Terminal.UI/Controls/BreakdownChart.cs` uses `TooltipWindow` directly for segment hover tooltips (with
    a pointer-based anchor rectangle).
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/TooltipTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/TooltipDemo.cs`

## Public API surface

### TooltipHost

`TooltipHost` is the public wrapper control:
- `TooltipHost : ContentVisual, IAnimatedVisual` (sealed)

Properties:
- `Content : Visual?` (inherited from `ContentVisual`)
- `TooltipContent : Visual?`
- `ShowDelayMilliseconds : int` (default 500)
- `Placement : PopupPlacement` (default Below)
- `OffsetX : int` (default 0)
- `OffsetY : int` (default 1)

Convenience API:
- `VisualExtensions.Tooltip(this Visual visual, Visual tooltip) -> TooltipHost`
  - wraps the target visual in a TooltipHost and sets `TooltipContent`.

### TooltipWindow (internal)

`TooltipWindow` is an internal `ContentVisual` that renders a tooltip surface in window-layer coordinates.
It is created and managed by TooltipHost (and some other controls, e.g. BreakdownChart).

## Hosting constraints

Tooltips require the window layer, therefore:
- tooltips are only supported in fullscreen apps
- attempting to show tooltips in non-fullscreen hosting throws from `TerminalApp.ShowTooltipWindow`

Tooltips are implemented as "windows" via the window layer, but with non-interactive characteristics:
- `TooltipWindow.IsHitTestVisible = false` (so it is excluded from pointer hit testing)
- `TooltipWindow.IsEnabled = false` (so even if hit-tested, it does not receive input)
- `TooltipWindow.Focusable = false` (so it does not steal focus when shown)

## Show/hide and animation behavior

### Animation integration

TooltipHost implements `IAnimatedVisual` and is automatically registered/unregistered when attached/detached because
`Visual.AttachToApp` registers `IAnimatedVisual` instances.

Implementation detail:
- `TooltipHost.NextAnimationTick` currently returns `0`, which means the app considers it eligible to run on every
  animation pass. The actual logic is lightweight and exits early when no tooltip work is needed.

### Show/hide rules (TooltipHost.AdvanceAnimation)

On each animation pass:
- if not attached to an app, not visible, or not enabled: close tooltip
- if `TooltipContent` is null: close tooltip and clear any scheduled show tick
- if not hovered: close tooltip and clear scheduled show tick
- if already open and still hovered: do nothing
- if hovered and not open:
  - when entering hover, schedule a show tick: `now + delay`
  - once `now >= scheduled`, open the tooltip window and clear the schedule

When `TooltipContent` changes:
- TooltipHost closes any open tooltip and clears the schedule.

On detach from app:
- TooltipHost closes the tooltip (ensures windows do not outlive the anchor tree).

### One tooltip per app

`TerminalApp` keeps a single `_activeTooltipWindow`.
When a new tooltip window is shown:
- if another tooltip is active, it is removed from the window layer first

Additionally, when other windows (dialogs/popups/context menus) are shown, the app closes the active tooltip window so
tooltips do not remain visible behind/over modal UI.

## Placement and layout (TooltipWindow)

### Measure

TooltipWindow measures the content to determine the tooltip size, but it always returns flexible hints so the window can
be arranged within the fullscreen root rectangle.

Content measurement rules:
- `TooltipStyle.Padding` is applied inside the border
- a border thickness of 1 cell on each side is reserved (2 cells total in each dimension)
- `TooltipStyle.MaxWidth` optionally caps the max width used for measuring content

Inner content constraints:
- `innerMaxWidth = maxWidth - padding.Horizontal - 2`
- `innerMaxHeight = maxHeight - padding.Vertical - 2` (when finite)

### Arrange

Arrange computes a popup rectangle:
- `desiredWidth = desiredContentWidth + padding.Horizontal + 2`
- `desiredHeight = desiredContentHeight + padding.Vertical + 2`
- clamped to the available final rect

Default position (no anchor): centered in the final rect.

Anchoring:
- TooltipWindow can be anchored either by:
  - `AnchorRect` (explicit rectangle in UI coordinates), or
  - `Anchor.Bounds` (when AnchorRect is null)

Placement is based on `PopupPlacement`:
- Below/Above/Left/Right
- with simple flip logic when the preferred side would go out of bounds and the opposite side has room

Offsets:
- `OffsetX` and `OffsetY` are applied after placement selection.

Final clamping:
- the popup rect is clamped so it remains fully within the final rect bounds.

Tooltip content is arranged inside:
- `popupRect` minus a 1-cell border and minus `TooltipStyle.Padding`.

### Render

TooltipWindow renders:
- the surface as a filled rectangle of spaces (`TooltipStyle.ResolveSurfaceStyle(theme)`)
- a 1-cell border using `TooltipStyle.Glyphs` and `TooltipStyle.ResolveBorderStyle(theme)`

The tooltip window does not render the tooltip content itself; child rendering draws content on top.

## Styling

### TooltipStyle

TooltipStyle controls:
- `Padding`
- `MaxWidth` (defaults to 60)
- `SurfaceStyle` and `BorderStyle` overrides
- `Glyphs : LineGlyphs` (default: Rounded; glyphs are non-ASCII in code)

Default style resolution is theme-driven:
- surface prefers `theme.PopupSurface`, then `theme.SurfaceAlt`, then `theme.Surface`
- border uses `theme.BorderStyle(focused: false)` and matches the chosen surface background

Important: to prevent style leakage from the underlay (e.g. underline), both `ResolveSurfaceStyle` and
`ResolveBorderStyle` explicitly return styles with an explicit text style (via `WithTextStyle(style.TextStyle)`).
This ensures the tooltip surface/border establishes a stable baseline for any nested content that renders with
`Style.None`.

## Tests

`TooltipTests` validate:
- tooltips show after a delay when hovered and hide when hover leaves
- tooltips do not intercept clicks on the underlying control (hit testing/input routing bypass the tooltip window)

## Future / v2 ideas

- Improve `IAnimatedVisual.NextAnimationTick` usage for TooltipHost so the app can schedule show ticks more efficiently.
- Optional positioning modes for TooltipHost (e.g., anchor to pointer instead of anchor bounds).
- Optional multi-tooltip policy (currently one per app by design).
