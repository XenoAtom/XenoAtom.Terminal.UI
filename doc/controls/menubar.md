# MenuBar

`MenuBar` provides application chrome with menus and keyboard navigation.

Screenshot placeholder:

![MenuBar](../../img/screenshots/menubar.png)

## Usage

Use `MenuItem` to build menus and submenus. Menu interaction supports keyboard and mouse.

## Popup chrome

Menus are displayed in `Popup` windows. You can customize the chrome around the menu list (e.g. add/remove a border) by
overriding `MenuListStyle.PopupTemplateFactory` via the visual environment:

```csharp
using XenoAtom.Terminal.UI.Styling;

menuBar.Set(MenuListStyle.Key, MenuListStyle.Default with
{
    PopupTemplateFactory = null, // no wrapper
});
```
