# CommandPalette

`CommandPalette` is a searchable command launcher used in ControlsDemo.

Screenshot placeholder:

![CommandPalette](../../img/screenshots/commandpalette.png)

## Popup chrome

When displayed via `CommandPalette.Show()`, the palette can be wrapped with a template visual (border/chrome) using `CommandPaletteStyle`:

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

var palette = new CommandPalette();
palette.SetStyle(CommandPaletteStyle.Default with
{
    PopupTemplateFactory = visual => new Border(visual),
});
```
