---
title: MenuBar Specs
---

# MenuBar Specs

This document captures the design and implementation details of `MenuBar`.

> [!NOTE]
> For end-user usage and examples, see [MenuBar](../../controls/menubar.md).

## Overview

- **Status**: Implemented
- **Purpose**: Top-level application menu bar with popup menus and submenus.
- **Surface area**:
  - `MenuBar` control (top-level bar)
  - `MenuItem` model (menu tree)
  - Popup menu lists (internal visuals hosted in `Popup`)

## Goals

- Provide an app-style menu experience suitable for terminal apps:
  - Keyboard navigation (arrow keys, Enter/Space, Escape).
  - Mouse interaction (click to open, hover to switch/open submenus).
- Integrate with the command system:
  - Use `Command.CanExecuteFor(...)` and `Command.IsVisibleFor(...)` when a menu item is backed by a `Command`.
  - Derive shortcut display from `Command.Gesture` / `Command.Sequence` when not explicitly provided.
- Keep rendering lightweight (menus are rendered directly to a `CellBuffer` and use simple layout).

## Non-goals

- "Alt to focus menu bar" semantics (not currently implemented; focus is handled by the app focus system).
- Type-to-select within menus.
- Async command execution semantics in the menu layer (commands may execute async internally, but the menu just invokes).

## Public API

### MenuBar

`MenuBar` is a `Visual` with:

- `Items : BindableList<MenuItem>` (bindable getter) - top-level menu items.

Notes:

- `MenuBar` is focusable and defaults to `HorizontalAlignment = Align.Stretch`.
- Menus are shown in `Popup` windows, so this control is intended for fullscreen apps.

### MenuItem

`MenuItem` is the menu node model and implements `IVisualElement`:

- `Header : Visual` - required header visual (often a `TextBlock`).
- `Items : BindableList<MenuItem>` (bindable getter) - submenu items.
- `Command : Command?` (bindable) - optional backing command.
- `CommandTarget : Visual?` - optional explicit target used for command visibility/enabled and execution.
- `Action : Delegator<Action>` (bindable) - optional action used when `Command` is null.
- `Icon : Visual?` (bindable) - optional icon visual.
- `Shortcut : Visual?` (bindable) - optional shortcut visual (overrides derived shortcut).
- `IsSeparator : bool` (bindable) - separator row flag.
- `IsVisible : bool` (bindable) - visibility flag (combined with `Command.IsVisibleFor`).
- `IsEnabled : bool` (bindable) - enabled flag (combined with `Command.CanExecuteFor`).

Helpers:

- `MenuItem.Separator()` / `MenuItem.CreateSeparator()` creates a disabled separator item.

## Behavior model

### Visibility and enabled evaluation

When the menu is opened, items are filtered by visibility:

- `MenuItem.IsVisible` must be true.
- If `MenuItem.Command` is set, it must also be visible for the effective target (`CommandTarget ?? menuTarget`).

Enabled state for selection and invocation:

- Separators are never selectable.
- `MenuItem.IsEnabled` must be true.
- If `MenuItem.Command` is set, it must also be executable for the effective target.

The menu target is resolved as:

- `TerminalApp.FocusedElement ?? TerminalApp.Root` (when available), otherwise the `MenuBar` itself.

### Invocation

When a selectable item is activated:

- If `Command` is set, the menu invokes `Command.Execute(effectiveTarget)` (after re-checking visibility and enabled state).
- Otherwise it invokes `Action.Invoke` (if any).

After invocation, all menus are closed.

## Layout

### MenuBar

`MenuBar` lays out top-level items horizontally:

- `MenuBarStyle.Padding` applies around the bar.
- `MenuBarStyle.ItemSpacing` applies between items.
- Each top-level item is presented by an internal `MenuBarItem` that applies `MenuBarStyle.ItemPadding` around the header.

### Menu lists

Opening a menu creates an internal `MenuList` visual hosted inside a `Popup`:

- Root menu popup placement: `Below` the active top-level `MenuBarItem`.
- Submenu popup placement: `Right` of the active menu row.
- Popup padding is forced to zero (`PopupStyle.Default with { Padding = Thickness.Zero }`).
- `MenuListStyle.PopupTemplateFactory` can wrap the list (the default uses a `Group` wrapper to provide chrome).

Menu list sizing:

- Height is `Padding.Vertical + itemCount` (each row is 1 cell high).
- Width is `Padding.Horizontal + maxRowWidth + submenuColumnWidth`.
  - `submenuColumnWidth` is 0 when there are no visible submenus.

## Rendering

### MenuBar

`MenuBar.RenderOverride` fills its bounds with spaces using `MenuBarStyle.ResolveBarStyle(theme)`.

Each `MenuBarItem` fills its own bounds with spaces using `MenuBarStyle.ResolveItemStyle(...)` based on:

- enabled
- open (root popup open for this item)
- selected (only when the menu bar has focus)
- hovered

### Menu lists

`MenuList.RenderOverride` renders row backgrounds and indicators:

- Each row area is filled with spaces using either:
  - `MenuListStyle.ResolveSeparatorStyle(...)` for separators, or
  - `MenuListStyle.ResolveItemStyle(...)` for normal rows (enabled/selected/hovered).
- Separators draw a horizontal line across the row using `theme.Lines.Horizontal`.
- Items with visible submenus draw a submenu indicator glyph (default is U+203A) in a dedicated right-side column.

Rendering is simple: there is no clipping or virtualization; the list is sized to its content.

## Input handling

### MenuBar keyboard

When the menu bar has focus:

- Left/Right: move the selected top-level item (skips disabled items).
- Enter/Space/Down: open the selected menu (or invoke directly if the item has no submenu).

### MenuBar mouse

- Left click on a top-level item toggles open/close.
- When any menu is open, hovering a different top-level item switches the open menu to that item.

### Menu list keyboard

When a menu list has focus:

- Up/Down: move selection within the list (skips disabled and separators); closes any open submenu.
- Enter/Space: invoke selected item, or open its submenu if it has one.
- Right:
  - if the selected item has a visible submenu: opens it
  - otherwise (root menu only): opens the next enabled top-level menu
- Left:
  - root menu: opens the previous enabled top-level menu
  - submenu: closes the submenu popup and returns focus to the parent list
- Escape: closes all menus.

### Menu list mouse

- Hover moves selection and will open a submenu when the hovered item has one.
- Left click selects and invokes (or opens submenu). Clicking a separator consumes the click (no action).

## Command routing interaction

Menus are hosted in popups. While a menu popup is open, global commands should not execute "behind" the popup.
This is covered by tests (see below).

## Styling

MenuBar uses:

- `MenuBarStyle` (`src/XenoAtom.Terminal.UI/Styling/MenuBarStyle.cs`):
  - bar and item background styles
  - item padding and spacing
  - item styles for normal/hovered/selected/open/disabled states
- `MenuListStyle` (`src/XenoAtom.Terminal.UI/Styling/MenuListStyle.cs`):
  - menu list padding
  - spacing between icon/header/shortcut
  - submenu indicator glyph
  - row styles (normal/selected/hovered/disabled/separator)
  - popup wrapper (`PopupTemplateFactory`)

## Tests, demos, and docs

Tests:

- `src/XenoAtom.Terminal.UI.Tests/MenuTests.cs` (open/invoke, submenu navigation, popup close, global command blocking)

Demo:

- `samples/ControlsDemo/Demos/MenuBarDemo.cs`

User documentation:

- `site/docs/controls/menubar.md`

## Known limitations

- No Alt key menu activation semantics.
- No type-to-jump in open menus.
- Menu rows are always 1 line high; multi-line headers are not supported in menus.

## Future ideas

- Add explicit `MenuBar` commands for discoverability (e.g. open menu, close menu, next/previous top-level).
- Add type-to-jump and/or incremental search within a menu list.
- Support richer row layout (multi-line, additional columns) if needed.

## Styling

- Styling is controlled via the theme and `MenuBarStyle` (where applicable).

## Tests & demos

- Look for rendering/input tests in `src/XenoAtom.Terminal.UI.Tests`.
- See the ControlsDemo for interactive examples.

## Future / v2 ideas

- Consider documenting additional style knobs and adding more deterministic rendering tests as features grow.
