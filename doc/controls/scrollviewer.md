# ScrollViewer

`ScrollViewer` provides a viewport with optional horizontal/vertical scrollbars for any content.

Screenshot placeholder:

![ScrollViewer](../../img/screenshots/scrollviewer.png)

## Basic usage

```csharp
new ScrollViewer(new VStack(
    "Line 1",
    "Line 2",
    "Line 3"
));
```

## Content implementing `IScrollable`

If `Content` implements `IScrollable`, ScrollViewer delegates scrolling to the content’s `ScrollModel`.
This enables controls like `TextArea` to own their own extent and viewport logic.

## Interaction

- Mouse wheel scrolls even when the content is not focused (unless the hosting scenario prevents mouse input).
- Scrollbars can be clicked/dragged; they participate in focus independently.

## Styling

`ScrollViewerStyle` controls scrollbar thickness and color palette.
`ScrollBarStyle` controls track/thumb colors and glyphs.

