---
title: ContextMenu Specs
---

# ContextMenu Specs

This document specifies the current behavior of context menus in XenoAtom.Terminal.UI as implemented.

> [!NOTE]
> For end-user usage and examples, see [Context menus](../../controls/contextmenu.md).

## Related specifications

- Command integration and discovery:
  - [Command System & Key Hints](../command_specs.md)
- Menu model and shared menu styling:
  - [Menu System & Context Menus](menu-system.md)

This spec focuses on the **concrete behavior** of context menus (the fullscreen popup menu opened by right-click).

## Goals

- Provide an easy way to show a context menu in **fullscreen** apps.
- Support two sources of menu content:
  - explicit `Visual.ContextMenuFactory`
  - command-based discovery (`CommandPresentation.ContextMenu`)
- Support submenus, separators, keyboard navigation, and mouse selection.
- Keep the menu implementation allocation-conscious and compatible with the retained-mode pipeline.

## Non-goals

- Context menus in inline/live scenarios (`Terminal.Live(...)`) (not supported).
- A separate public `ContextMenu` control type (current implementation is a service + popup host).
- Fully dynamic menus that continuously react while the menu is open (items are mostly snapshotted at open time).

## Public surface (v1)

### `Visual.ContextMenuFactory`

- `Func<Visual, IEnumerable<MenuItem>>? ContextMenuFactory { get; set; }`
- When a context menu is requested, the framework searches the hovered visual chain for the nearest non-null factory.

### `ContextMenuService`

- `Popup Show(Visual target, IEnumerable<MenuItem> items, int uiX, int uiY)`
  - Creates and shows a context menu popup at the requested UI coordinate.
  - Throws when no fullscreen `TerminalApp` is running.

### Implicit right-click behavior (fullscreen apps)

- In fullscreen apps, a right-click (`TerminalMouseButton.Right`) triggers the context menu behavior.
- If a menu is already open, right-click closes it (no nested root context menus).

## Implementation map

- Service + list implementation:
  - `src/XenoAtom.Terminal.UI/Controls/ContextMenuService.cs`
- Right-click integration and command fallback:
  - `src/XenoAtom.Terminal.UI/TerminalApp.cs` (`TryShowContextMenu` / `ShowContextMenu`)
- Menu model:
  - `src/XenoAtom.Terminal.UI/Controls/MenuItem.cs`
- Styling:
  - `src/XenoAtom.Terminal.UI/Styling/MenuListStyle.cs`
  - `src/XenoAtom.Terminal.UI/Styling/PopupStyle.cs`
- Tests:
  - `src/XenoAtom.Terminal.UI.Tests/ContextMenuTests.cs`
- Demo:
  - `samples/ControlsDemo/Demos/ContextMenuDemo.cs`

## How items are chosen

When opening a context menu for a hovered target `hitTarget`:

1. **Factory-based (preferred):**
   - The app walks `hitTarget → Parent → …` and uses the first visual with a non-null `ContextMenuFactory`.
   - The factory is invoked with the visual that declared the factory.
2. **Command-based fallback:**
   - If no factory is found, the app collects commands with `CommandPresentation.ContextMenu` via `CommandQuery`.
   - Each discovered command becomes a `MenuItem` whose header is `new Markup(cmd.LabelMarkup)` and whose `CommandTarget` is the visual that registered the command.

## Targeting rules (commands)

Menu item execution uses a “target” visual:

- `MenuItem.CommandTarget` (when set) wins.
- Otherwise, the menu host uses the original hovered `hitTarget`.

The effective target is passed to:

- `Command.IsVisibleFor(target)`
- `Command.CanExecuteFor(target)`
- `Command.Execute(target)`

## Layout

Context menus are rendered by an internal `ContextMenuList` visual hosted inside a `Popup`.

### Measure

- The list uses `MenuListStyle.Padding` around the content.
- Each row is measured at infinite width and height `1`.
- Width is computed as:
  - `padding.Horizontal + max(rowWidth) + submenuColumnWidth`
- Height is computed as:
  - `padding.Vertical + items.Count`
- `submenuColumnWidth` is `0` unless at least one item has a **visible submenu**, in which case the list reserves a right-side column wide enough to draw `MenuListStyle.SubmenuGlyph` plus spacing.

### Arrange

- Rows are arranged top-to-bottom in the inner rect (padding removed).
- Each row is arranged with width:
  - `innerRect.Width - submenuColumnWidth`
  (so submenu arrows never overlap the item content).

## Rendering

### Row background

For each row, the list fills the row area with:

- `MenuListStyle.ResolveSeparatorStyle(theme)` for separators
- `MenuListStyle.ResolveItemStyle(theme, enabled, selected, hovered)` for regular items

### Separators

- Separator items draw a full-width horizontal line using `theme.Lines.Horizontal` (dimmed via the separator style).

### Submenu indicator

- When an item has at least one visible child item (`IsVisibleFor(target)`), the submenu glyph (`›` by default) is drawn in the reserved submenu column and rendered with `TextStyle.Dim`.

## Input handling

The list is focusable and supports both keyboard and mouse interaction.

### Keyboard

- `Up` / `Down`: move selection to the previous/next selectable item; closes any open submenu.
- `Enter` / `Space`: invoke selected item (or open submenu if it has one).
- `Right`: open submenu for selected item (when present).
- `Left`:
  - closes the current submenu (when in a submenu),
  - or closes the root popup (when in the root menu).
- `Escape`: closes the root popup.

### Mouse

- Move: updates hover, selects the hovered row, and ensures the submenu for that selection is opened.
- Left click:
  - on an enabled item: invokes (or opens submenu)
  - on a disabled item or separator: handled, but does not invoke

## Submenus

- Submenus are hosted as additional `Popup` instances with:
  - `Placement = PopupPlacement.Right`
  - `Anchor = rowVisual`
- Only one submenu popup is kept open per list; opening a new submenu closes the previous one.
- Submenu items are filtered to those visible for the effective target (`MenuItem.IsVisibleFor(target)`).
- The root list tracks all open submenu popups and closes them when the root popup closes (no orphan popups).

## Popup behavior and focus restoration

- The context menu is hosted in a fullscreen window layer (`Popup`).
- Root popup defaults:
  - `AnchorRect = (uiX, uiY, 1, 1)`
  - `Placement = Below`
  - `MatchAnchorWidth = false`
  - `CloseOnTab = true`
  - `PopupStyle.Padding = 0` (chrome is provided by the `MenuListStyle.PopupTemplateFactory` wrapper)
- When the root popup closes, the app restores focus to the element that was focused before opening the context menu (if still attached).

## Styling

Context menus reuse the menu list styling:

- `MenuListStyle` controls spacing, padding, glyphs, and per-state styles (normal/selected/hovered/disabled/separator).
- `MenuListStyle.PopupTemplateFactory` controls the “chrome” around the list when shown in a popup.
  - Default wraps the list in a `Group`.
  - `MenuListStyle.NoBorder` disables the wrapper.

## Testing and demos

- `ContextMenuTests` verifies:
  - right-click uses `ContextMenuFactory` when provided
  - right-click falls back to command discovery (`CommandPresentation.ContextMenu`) when no factory is present
- `ContextMenuDemo` shows both command-based and factory-based menus, and uses `ContextMenuService.Show(...)` for screenshots.

## Current limitations

- Root menu items are not automatically filtered by `MenuItem.IsVisibleFor(target)`; factories should return only the items they want to display.
- Command-based items are built from a snapshot of discovered commands; while open, enabled/visible state is not fully dynamic.

## Future ideas

- Filter root items by `IsVisibleFor(target)` for consistency with submenus.
- Optional keyboard gesture to open the context menu (e.g. `Shift+F10`) when supported by the terminal input layer.
- Optional “placement flip” (open above when near the bottom of the viewport).
