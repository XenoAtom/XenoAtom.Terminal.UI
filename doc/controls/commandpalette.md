# CommandPalette

`CommandPalette` is a searchable command launcher built on top of the unified `Command` system.

## Fullscreen-only

`CommandPalette` uses the popup/window system and is available only in fullscreen `Terminal.Run(...)` applications.

## What it shows

`CommandPalette` lists commands discovered from the current focus context:

- Commands registered on the focused visual and its ancestors
- Global commands registered on the running `TerminalApp`

Only commands marked with `CommandPresentation.CommandPalette` are shown.

## Typical usage

Create a palette instance and register a command that opens it:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

var palette = new CommandPalette();

root.AddCommand(new Command
{
    Id = "App.CommandPalette",
    LabelMarkup = "Command palette",
    Gesture = new KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl),
    Presentation = CommandPresentation.CommandBar | CommandPresentation.CommandPalette,
    Execute = _ => palette.Show(),
});
```

When open:

- Type to search
- Use Up/Down to select
- Press Enter to execute
- Press Esc to close

## Popup chrome

When displayed via `CommandPalette.Show()`, the palette can be wrapped with a template visual (border/chrome) using `CommandPaletteStyle`:

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

var palette = new CommandPalette();
palette.Style(CommandPaletteStyle.Default with
{
    PopupTemplateFactory = visual => new Border(visual),
});
```

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Related
- [Commands](../commands.md)
- [Input](../input.md)
- [CommandBar](./commandbar.md)
- [Popup](./popup.md)
