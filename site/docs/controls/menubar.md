---
title: MenuBar
---

# MenuBar

`MenuBar` provides application chrome with menus and keyboard navigation.


![MenuBar](../../img/controls/menubar.png){.terminal}

## Fullscreen-only

Menus are implemented as popups and require a fullscreen `Terminal.Run(...)` app.

## Usage

Use `MenuItem` to build menus and submenus. Menu interaction supports keyboard and mouse. Hovering a top-level item moves the top-level keyboard selection to that item, and `Escape` closes the innermost open menu level first.

Menu items can be backed by a `Command`:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

var open = new Command
{
    Id = "App.Open",
    LabelMarkup = "[primary]Open[/]",
    Gesture = new KeyGesture(TerminalChar.CtrlO, TerminalModifiers.Ctrl),
    Presentation = CommandPresentation.Menu,
    Execute = _ => { /* ... */ },
};

var menuBar = new MenuBar();
menuBar.Items.Add(new MenuItem("File")
    .Items(
        new MenuItem("Open", open),
        MenuItem.Separator(),
        new MenuItem("Exit", () => { /* ... */ })));
```

When `MenuItem.Command` is set:

- enabled state is derived from `Command.CanExecuteFor(...)`
- the shortcut label is derived from `Command.Gesture` / `Command.Sequence` unless `MenuItem.Shortcut` is explicitly set

## Programmatic opening

`MenuBar` does not install a default global activation key. If your application wants a shortcut such as `F9`, add an
application command and call `OpenMenu()` (or `OpenMenu(index)`) from that command:

```csharp
app.AddGlobalCommand(new Command
{
    Id = "App.OpenMenu",
    LabelMarkup = "Open menu",
    Gesture = new KeyGesture(TerminalKey.F9),
    Execute = _ => menuBar.OpenMenu(),
});
```

`OpenMenu()` opens the currently selected top-level menu. `OpenMenu(index)` opens the specified top-level menu and clamps
the index to the available `Items` range.

## Popup chrome

Menus are displayed in `Popup` windows. You can customize the chrome around the menu list (e.g. add/remove a border) by
overriding `MenuListStyle.PopupTemplateFactory` via the visual environment:

```csharp
using XenoAtom.Terminal.UI.Styling;

menuBar.Style(MenuListStyle.Default with
{
    PopupTemplateFactory = null, // no wrapper
});
```


## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Related

- [MenuBar Specs](../specs/controls/menubar.md)
