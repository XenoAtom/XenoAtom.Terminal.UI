# Scrolling

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

## ScrollBar

`ScrollBar` is an abstract base for scroll bars. Use `VScrollBar` (vertical) or `HScrollBar` (horizontal) directly, or let `ScrollViewer` create and manage them for you.

See also:

- `doc/controls/scrollviewer.md`
- `doc/controls/scrollbar.md`
