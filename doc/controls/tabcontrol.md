# TabControl

`TabControl` hosts multiple tab pages with clickable headers.
Tab headers are visuals, enabling rich header content (icons, counters, etc.).

## Basic usage

```csharp
new TabControl(
    new TabPage("Tab1", "ContentTab1"),
    new TabPage("Tab2", "ContentTab2"));
```

## Styling

`TabControlStyle` controls header rendering (button-like appearance, hover/pressed/selected states) and the content wrapper.

By default, the selected tab content is wrapped in a border.

```csharp
new TabControl(
    new TabPage("Status", "Ready"),
    new TabPage("Logs", "…"))
    .Style(TabControlStyle.Rounded);
```

## Interaction

- Mouse click on a tab header activates the tab.
- `Left` / `Right` arrow keys switch between tabs when the control is focused.

## Related

- `../styling.md`
- `../layout.md`
