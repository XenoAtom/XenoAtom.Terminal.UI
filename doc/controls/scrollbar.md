---
title: ScrollBar
---

# ScrollBar

`ScrollBar` is the abstract base for standalone scroll bars.

Use `VScrollBar` (vertical) or `HScrollBar` (horizontal).

## Basic usage

Scrollbars are typically used through `ScrollViewer`, but can be used directly:

```csharp
new VScrollBar()
    .Minimum(0)
    .Maximum(100)
    .Value(30);
```

In most apps you should prefer `ScrollViewer` + `IScrollable`, because it keeps viewport/extent synchronized and avoids
manual offset math.

## Behavior and interaction

- Clicking the track pages up/down (or left/right).
- Dragging the thumb sets the value proportionally.
- Scrollbars can be focused to enable keyboard interaction (arrow keys/page keys).

## Binding to a scroll model

When used inside `ScrollViewer`, scrollbars are automatically bound to the target `ScrollModel`:

- `ScrollModel.OffsetX/OffsetY` maps to scrollbar value,
- `ScrollModel.ViewportWidth/ViewportHeight` maps to thumb size,
- `ScrollModel.ExtentWidth/ExtentHeight` maps to range.

> [!NOTE]
> If you host a scrollbar directly, you are responsible for keeping its range/value consistent with the content.

## Defaults

- `VScrollBar`: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Stretch`
- `HScrollBar`: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Start`

## Styling

`ScrollBarStyle` controls track/thumb rendering and colors.

## Related

- [Scrolling](../scrolling.md)
- [ScrollViewer](./scrollviewer.md)
