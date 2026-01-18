# ProgressBar

`ProgressBar` renders a progress bar using different variants (thin, shaded, segmented, bracketed).

Screenshot placeholder:

![ProgressBar](../../img/screenshots/progressbar.png)

## Basic usage

```csharp
var progress = new State<double>(0.66);
new ProgressBar().Value(progress);
```

## Styling

`ProgressBarStyle` controls variants, glyphs, and color palette.

If you want to display a label, a percentage, or a spinner next to a progress bar, use `ProgressTaskGroup`.
