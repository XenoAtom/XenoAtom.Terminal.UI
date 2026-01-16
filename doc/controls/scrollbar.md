# ScrollBar

`ScrollBar` is a standalone scrollbar control (horizontal or vertical).

Screenshot placeholder:

![ScrollBar](../../img/screenshots/scrollbar.png)

## Basic usage

Scrollbars are typically used through `ScrollViewer`, but can be used directly:

```csharp
new ScrollBar()
    .Orientation(Orientation.Vertical)
    .Minimum(0)
    .Maximum(100)
    .Value(30);
```

## Styling

`ScrollBarStyle` controls track/thumb rendering and colors.

