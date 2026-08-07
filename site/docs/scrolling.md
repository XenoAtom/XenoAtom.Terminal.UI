---
title: Scrolling
---

# Scrolling

XenoAtom.Terminal.UI uses a single scrolling model across controls: `IScrollable` + `ScrollModel`.
This keeps scrollbars, mouse wheel behavior, and nested scrolling consistent.

## ScrollViewer

`ScrollViewer` provides clipped scrolling for any content visual:

```csharp
new ScrollViewer(new VStack("Line 1", "Line 2"))
```

### Content that implements `IScrollable`

If `ScrollViewer.Content` implements `IScrollable`, the scroll viewer uses the content’s scroll model:

- scrollbars and mouse wheel update the content scroll offsets
- the content is responsible for exposing its extent and viewport via `ScrollModel`

This is how `TextArea` integrates with `ScrollViewer`.

### Content that does not implement `IScrollable`

If the content is not scrollable, the scroll viewer owns its scroll offsets and scrolls by translating the content viewport.

### Scroll bar visibility

Use `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` to configure each bar as
`ScrollBarVisibility.Auto`, `Hidden`, or `Always`. `Hidden` preserves scrolling without displaying a bar, while
`Always` reserves the bar's space before laying out content. Automatic bars continue to use layout convergence when one
bar changes the available space on the other axis.

## IScrollable and ScrollModel

`IScrollable` is a simple contract:

```csharp
public interface IScrollable
{
    ScrollModel Scroll { get; }
}
```

`ScrollModel` represents the state of a scrollable surface:

- `ViewportWidth` / `ViewportHeight`: visible region size (in cells/rows)
- `ExtentWidth` / `ExtentHeight`: total scrollable size (in cells/rows)
- `OffsetX` / `OffsetY`: current scroll position
- `Version`: a bindable “snapshot” of the current scroll state

### How it works in practice

- The scrollable control updates `ScrollModel` during `Arrange`:
  - `Scroll.SetViewport(viewportWidth, viewportHeight)`
  - `Scroll.SetExtent(extentWidth, extentHeight)`
- `ScrollViewer` reads the model to decide:
  - whether scrollbars should be visible,
  - the thumb size and range,
  - how to clamp offsets.

### Version and dependency tracking

`ScrollModel.Version` is a bindable property updated whenever viewport/extent/offset changes.
Controls often mirror it into an internal bindable property in `PrepareChildren` to avoid “read then write” tracking loops.

See [Binding](binding.md) for the pattern.

## Implementing a custom scrollable control

At a high level:

1. Add a `ScrollModel` field: `private readonly ScrollModel _scroll;`
2. Implement `IScrollable.Scroll => _scroll;`
3. In `Arrange`, update viewport/extent and render content offset by `Scroll.OffsetX/OffsetY`.

> [!TIP]
> Prefer `Scroll.ScrollToMakeVisible(...)` when you need “ensure selection visible” behavior.

## ScrollBar

`ScrollBar` is an abstract base for scroll bars. Use `VScrollBar` (vertical) or `HScrollBar` (horizontal) directly, or let `ScrollViewer` create and manage them for you.

See also:

- [ScrollViewer](controls/scrollviewer.md)
- [ScrollBar](controls/scrollbar.md)
