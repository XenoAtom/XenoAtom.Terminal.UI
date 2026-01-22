# Breakdown

`Breakdown` renders a segmented proportional bar (a “breakdown”) with an optional legend. It is useful for showing how
a total value is distributed across categories (disk usage, budgets, KPIs, resource usage, etc.).

> Screenshots: TODO

## Basic usage

```csharp
var breakdown = new Breakdown()
    .Title("Disk usage")
    .ShowValues(true)
    .ShowPercentages(true)
    .Segment(42, "🗃️  Data", tooltip: new Markup("[primary]Data[/] files and databases."))
    .Segment(18, "📦  Packages", tooltip: new Markup("[success]Packages[/] in the cache."))
    .Segment(9,  "🧹  Temp", tooltip: new Markup("[warning]Temporary[/] files."))
    .Segment(3,  "🧯  Other", tooltip: new Markup("[error]Other[/] space usage."));
```

## Segments

Segments are stored in `Breakdown.Segments` as `BreakdownSegment` objects.

- `BreakdownSegment.Value`: numeric value used to compute proportions.
- `BreakdownSegment.Label`: a visual shown in the legend (use a `TextBlock`, `Markup`, an `HStack` with an icon, etc.).
- `BreakdownSegment.Color`: optional segment fill color; when not provided, the control cycles through theme tones.
- `BreakdownSegment.Tooltip`: optional tooltip content shown when hovering the segment in the bar.

For convenience, you can append segments fluently using `BreakdownExtensions.Segment(...)` as shown above.

## Interaction

`Breakdown` raises a routed `SegmentClicked` event when the user clicks a segment:

```csharp
breakdown.SegmentClicked((_, e) =>
{
    Terminal.WriteLine($"Clicked segment {e.Index}: {e.Segment.Value}");
});
```

## Layout

- `Breakdown.Title`: optional title shown above the bar.
- `Breakdown.LegendPlacement`: `Above` or `Below` (default: `Below`).
- `Breakdown.ShowValues` / `Breakdown.ShowPercentages`: controls legend value display.

## Styling

`Breakdown` is styled via `BreakdownStyle`:

- `FillRune`: rune used to fill the bar (default is a space with colored backgrounds).
- `SegmentGap`: number of empty cells between segments.
- `BarStyle`: optional base style applied to bar cells.
- `DefaultSegmentColors`: optional palette used when a segment does not provide an explicit `Color`.

Example:

```csharp
breakdown.Style(new BreakdownStyle
{
    SegmentGap = 0,
});
```

