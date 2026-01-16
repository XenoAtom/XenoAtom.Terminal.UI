# TabControl

`TabControl` hosts multiple tab pages with clickable headers.
Tab headers are visuals, enabling rich header content (icons, counters, etc.).

Screenshot placeholder:

![TabControl](../../img/screenshots/tabcontrol.png)

## Basic usage

```csharp
new TabControl()
    .Pages.Add(
        new TabPage("Tab1", new TextBlock("ContentTab1")),
        new TabPage("Tab2", new TextBlock("ContentTab2"))
    );
```

## Styling

`TabControlStyle` controls header rendering (button-like appearance, hover/pressed/selected states).

