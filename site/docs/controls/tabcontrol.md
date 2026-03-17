---
title: TabControl
---

# TabControl

`TabControl` hosts multiple tab pages with clickable headers.
Tab headers are visuals, enabling rich header content (icons, counters, dynamic text, etc.).
`TabPage` is now a bindable model, so you can update a page's `Header`, `Content`, `IsEnabled`, `ShowCloseButton`, or `Data`
without removing and recreating the tab.

![TabControl](../../img/controls/tabcontrol.svg){.terminal}

## Basic usage

```csharp
var logs = new TabPage("Logs", "Tail -f output")
{
    ShowCloseButton = true,
    Data = "logs",
};

logs.RequestClosing += (_, e) =>
{
    if (HasPendingSave())
    {
        e.Cancel = true;
    }
};

var tabs = new TabControl(
    new TabPage("Status", "Ready"),
    logs,
    new TabPage("Metrics", "42 req/s") { ShowCloseButton = true });
```

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch`
- Tab headers stay on a single line. When they do not fit, overflow buttons appear at the far left and far right.

## Tab pages

`TabPage` exposes bindable state:

- `Header : Visual`
- `Content : Visual`
- `IsEnabled : bool`
- `ShowCloseButton : bool`
- `Data : object?`

Close lifecycle:

- `RequestClosing` lets a page cancel a close request.
- `Closed` is raised after the page has been removed.
- `TabControl.TryCloseTab(...)` closes a page programmatically and uses the same lifecycle as the close button.

## Styling

`TabControlStyle` controls header rendering, close buttons, overflow buttons, and the content wrapper.

By default:

- selected tabs use the accent/focus styling
- close buttons inherit the tab style, then switch to an error-toned hover/pressed state
- overflow buttons use the tab/button surface styling
- the selected tab content is wrapped in a border

```csharp
new TabControl(
    new TabPage("Status", "Ready"),
    new TabPage("Logs", "…") { ShowCloseButton = true })
    .Style(TabControlStyle.Rounded with
    {
        CloseButtonRune = new Rune('x'),
        OverflowPreviousRune = new Rune('<'),
        OverflowNextRune = new Rune('>'),
    });
```

## Interaction

- Mouse click on a tab header activates the tab.
- Clicking a close button requests tab closure and may be cancelled by the page.
- When tabs overflow, left/right overflow buttons scroll the visible header window.
- `Left` / `Right` arrow keys switch between enabled tabs when the control is focused.

## Related

- [Styling](../styling.md)
- [Layout](../layout.md)
- [TabControl Specs](../specs/controls/tabcontrol.md)
