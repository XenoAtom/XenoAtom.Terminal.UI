# ProgressBar

`ProgressBar` renders progress using different variants (thin, shaded, segmented, bracketed…).

Screenshot placeholder:

![ProgressBar](../../img/screenshots/progressbar.png)

## Basic usage

```csharp
var progress = new State<double>(0.66);
new ProgressBar().Label("Work").Value(progress);
```

## Styling

`ProgressBarStyle` controls variants, glyphs, and color palette.

