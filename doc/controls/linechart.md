---
title: LineChart
---

# LineChart

`LineChart` renders a simple line chart.

## Basic usage

```csharp
new LineChart()
    .Values([1, 4, 2, 5, 3, 6]);
```

## Scaling

By default the chart auto-scales to the min/max values in `Values`. You can override:

- `Minimum`: fixed minimum
- `Maximum`: fixed maximum

## Defaults

- Default alignment: `HorizontalAlignment = Align.Start`, `VerticalAlignment = Align.Start`

## Styling

`LineChartStyle` controls the point glyph and colors.

## Related

- [Sparkline](./sparkline.md)
- [Styling](../styling.md)
