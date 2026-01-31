# Commands and Key Hints

XenoAtom.Terminal.UI provides a lightweight **command system** to make keyboard shortcuts discoverable and reusable across UI surfaces (key hints, command palette, menus).

Commands are **retained-mode**, registered on visuals (local) or on the `TerminalApp` (global), and routed using the same focus → parent traversal as other keyboard handling.

> Screenshot placeholders will be added later.

## What is a command?

A `Command` is an action with:

- A stable `Id`
- A user-facing `LabelMarkup` (ANSI markup supported, including theme tokens like `[primary]`)
- An optional `Gesture` (single-stroke shortcut)
- An optional `Sequence` (`Ctrl+K Ctrl+P` style multi-stroke shortcuts)
- Optional `CanExecute` / `IsVisible` predicates

## Registering commands on visuals

Register commands directly on a control instance:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

var editor = new TextArea("Hello");
editor.AddCommand(new Command
{
    Id = "App.Save",
    LabelMarkup = "[primary]Save[/]",
    Gesture = new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlS, TerminalModifiers.Ctrl),
    Execute = _ => Terminal.WriteLine("Saved"),
});
```

Notes:

- For Ctrl shortcuts, prefer `TerminalChar.CtrlX` + `TerminalModifiers.Ctrl` (terminals commonly emit control characters).
- `Gesture` and `Sequence` are mutually exclusive.

## Multi-stroke shortcuts (key sequences)

Use `KeySequence` to define sequences:

```csharp
var cmd = new Command
{
    Id = "App.CommandPalette",
    LabelMarkup = "Command palette",
    Sequence = new KeySequence(
        new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
        new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl)),
    Execute = _ => Terminal.WriteLine("Open palette"),
};
```

By design, a gesture used as a sequence prefix should not also be used as a standalone command in the same scope.

## Showing key hints with CommandBar

Use `CommandBar` as part of your app chrome:

```csharp
using XenoAtom.Terminal.UI.Controls;

var root = new DockLayout()
    .Content(new TextArea())
    .Bottom(new VStack(
        new CommandBar(),
        new Footer().Left("Tab focus | Mouse").Right("Ctrl+Q quit"))
        .Spacing(0));
```

The command bar shows commands for the current focus context and clips when the line is full.

## Where commands appear

Commands have `CommandPresentation` flags (for example `CommandBar`, `CommandPalette`, `Menu`, `ContextMenu`).

- `CommandBar` shows context-aware key hints for the focused visual.
- `CommandPalette` lists commands from the current focus context and global commands.
- `MenuBar` supports menu items backed by commands.
- Context menus can be built from commands via `CommandPresentation.ContextMenu`.

While a modal popup/menu is active, global shortcuts are not executed “behind” it.

## Related docs

- [CommandBar](./controls/commandbar.md)
- [Input](./input.md)
- [CommandPalette](./controls/commandpalette.md)
- [MenuBar](./controls/menubar.md)
- [ContextMenu](./controls/contextmenu.md)
- [Command Specs](./specs/command_specs.md)
