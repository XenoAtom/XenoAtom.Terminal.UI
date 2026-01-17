# ScrollBar

`ScrollBar` is the abstract base for standalone scroll bars.

Use `VScrollBar` (vertical) or `HScrollBar` (horizontal).

Screenshot placeholder:

![ScrollBar](../../img/screenshots/scrollbar.png)

## Basic usage

Scrollbars are typically used through `ScrollViewer`, but can be used directly:

```csharp
new VScrollBar()
    .Minimum(0)
    .Maximum(100)
    .Value(30);
```

## Styling

`ScrollBarStyle` controls track/thumb rendering and colors.
