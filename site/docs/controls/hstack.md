---
title: HStack
---

# HStack

`HStack` stacks children horizontally.


![HStack](../../img/controls/hstack.svg){.terminal}

## Basic usage

```csharp
new HStack(
    new Button("Left"),
    new Button("Right")
).Spacing(2);
```

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Stretch` 

## Related
- [Layout](../layout.md)
- [VStack](vstack.md)
- [HStack Specs](../specs/controls/hstack.md)
