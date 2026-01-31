---
title: Padder
---

# Padder

`Padder` adds padding around a single content visual. Use it when you want spacing without drawing any border.

Screenshot placeholder:

![Padder](../../img/screenshots/padder.png)

## Basic usage

```csharp
new Padder("Padded content").Padding(1);
```

## Fluent `Pad(...)` helper

Any `Visual` can be wrapped in a `Padder` using `VisualExtensions.Pad(...)`:

```csharp
new TextBlock("Hello").Pad(2);
```



## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start` 

