---
title: SearchReplacePopup
---

# SearchReplacePopup

`SearchReplacePopup` is a reusable Find / Find-and-Replace popup used by controls such as `TextArea` and `LogControl`.

## Keyboard

- `Ctrl+F`: open Find (control-specific)
- `Ctrl+H`: open Replace (control-specific; typically TextArea)
- `Enter` / `F3`: next match
- `Shift+Enter`: previous match
- `Esc`: close

The popup can be repositioned by dragging it with the mouse (header area).

## Integration model

`SearchReplacePopup` is hosted by another control and anchored within that control’s bounds.

To integrate it, implement `ISearchReplaceTarget` and update your host’s arrange pass:

```csharp
private readonly SearchReplacePopup _popup;

public MyControl()
{
    _popup = new SearchReplacePopup(new MySearchTarget(this));
    AttachChild(_popup);
}

protected override void ArrangeCore(in Rectangle finalRect)
{
    Bounds = finalRect;

    // ... arrange your main content ...

    _popup.ArrangeWithin(finalRect);
}
```

The popup UI is rendered using `Popup` in the app window layer, while `SearchReplacePopup` acts as a lightweight anchor visual.

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

## Related
- [Text Editing](../text-editing.md)
- [Input](../input.md)
- [Popup](./popup.md)
- [TextArea](./textarea.md)
