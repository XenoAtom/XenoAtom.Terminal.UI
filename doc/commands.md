# Commands and Key Hints

XenoAtom.Terminal.UI provides a lightweight **command system** to make keyboard shortcuts discoverable and reusable across UI surfaces (key hints, command palette, menus).

Commands are **retained-mode**, registered on visuals (local) or on the `TerminalApp` (global), and routed using the same focus → parent traversal as other keyboard handling.

> Screenshot placeholders will be added later.

## What is a command?

A `UiCommand` is an action with:

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
editor.AddCommand(new UiCommand
{
    Id = "App.Save",
    LabelMarkup = "[primary]Save[/]",
    Gesture = new XenoAtom.Terminal.UI.Input.TerminalKeyGesture(TerminalChar.CtrlS, TerminalModifiers.Ctrl),
    Execute = _ => Terminal.WriteLine("Saved"),
});
```

Notes:

- For Ctrl shortcuts, prefer `TerminalChar.CtrlX` + `TerminalModifiers.Ctrl` (terminals commonly emit control characters).
- `Gesture` and `Sequence` are mutually exclusive.

## Multi-stroke shortcuts (key sequences)

Use `TerminalKeySequence` to define sequences:

```csharp
var cmd = new UiCommand
{
    Id = "App.CommandPalette",
    LabelMarkup = "Command palette",
    Sequence = new TerminalKeySequence(
        new XenoAtom.Terminal.UI.Input.TerminalKeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
        new XenoAtom.Terminal.UI.Input.TerminalKeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl)),
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

Commands have `UiCommandPresentation` flags (for example `CommandBar`, `CommandPalette`, `Menu`, `ContextMenu`). In v1, the primary consumer is `CommandBar`.

## Related docs

- `doc/controls/commandbar.md`
- `doc/input.md`
- `doc/specs/command_specs.md`
