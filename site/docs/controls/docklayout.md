---
title: DockLayout
---

# DockLayout

`DockLayout` docks visuals to edges (Top/Bottom/Left/Right) and gives remaining space to the content area.


![DockLayout](../../img/controls/docklayout.svg){.terminal}

## Basic usage

```csharp
new DockLayout()
    .Top(new Header().Left("Title"))
    .Bottom(new Footer().Left("Hints"))
    .Content(new ScrollViewer(new TextArea(longText)));
```



## Defaults

- Default alignment: `HorizontalAlignment = Align.Stretch`, `VerticalAlignment = Align.Stretch` 
