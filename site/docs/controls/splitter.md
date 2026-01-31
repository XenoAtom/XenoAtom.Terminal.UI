---
title: Splitter (HSplitter / VSplitter)
---

# Splitter (HSplitter / VSplitter)

Splitters resize adjacent content panes using mouse drag.

Screenshot placeholder:

![Splitter](../../img/screenshots/splitter.png)

## Basic usage

```csharp
new HStack(
    leftPane,
    new VSplitter(),
    rightPane
);
```

`SplitterStyle` controls handle glyphs and colors.



## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

