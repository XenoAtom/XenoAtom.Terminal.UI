---
title: Footer
---

# Footer

`Footer` is an app-chrome control (bottom area) for hints, status, and key gesture reminders.

`Footer` is typically used as the bottom bar of a `DockLayout`.

## Basic usage

```csharp
var root = new DockLayout()
    .Content(new TextArea("Main content"))
    .Bottom(new Footer()
        .Left("Tab focus")
        .Center("Ready")
        .Right("Ctrl+Q quit"));
```

## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Start`

## Key properties

- `Left`, `Center`, `Right`: slot visuals arranged on a single row

## Related

- [Header](header.md)
- [StatusBar](statusbar.md)
- [Layout](../layout.md)
