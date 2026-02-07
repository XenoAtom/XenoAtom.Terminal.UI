---
title: Brushes & Gradients (Internal Spec)
---

# Brushes & Gradients (Internal Spec)

This spec proposes an opt-in **brush** concept (solid colors and gradients) for terminal rendering in **XenoAtom.Terminal.UI**.

Motivations:
- Horizontal gradient text (foreground and/or background)
- Figlet text with diagonal gradients
- Highlight/sweep animations (e.g. left-to-right shine) using translucent overlays

The primary design goal is **low impact**: we do not want to refactor the entire framework to be brush-aware, and we do not want to bloat the per-cell `Style` representation.

## Goals

- Introduce a general brush model that can produce **per-cell colors** for foreground and background.
- Keep `Style` unchanged (still stores concrete per-cell colors, packed and allocation-free).
- Make the feature **opt-in** at the control level (V1 integrates only a small set of controls).
- Keep per-frame rendering allocation-free (brush sampling must not allocate per cell).
- Allow a **configurable default** for color interpolation space, with per-brush override.

## Non-goals (V1)

- Do not make every control accept brushes.
- Do not store brush references inside `Style` or `CellBuffer`.
- Do not require additional terminal features (truecolor looks best; indexed palettes will band).
- No radial gradients / patterns / noise brushes in V1 (can be added once the core model is stable).

## Current constraints (why we need an opt-in design)

Today, rendering is ultimately a `CellBuffer` of:
- a per-cell scalar (glyph / token), and
- a per-cell packed `Style` (foreground/background colors + flags).

`Style` is intentionally compact; it cannot reasonably store brush references, arrays of stops, or other rich styling objects.

Therefore, brushes must be applied by **controls during rendering** by sampling the brush and writing the resulting concrete colors into the `CellBuffer`.

> [!NOTE]
> Translucent effects should use RGBA colors. The `CellBuffer` already applies alpha blending when an overlay style
> contains explicit RGBA colors, which keeps "highlight overlays" consistent with existing style merging behavior.

## V1 scope (kept intentionally small)

Controls:
- `TextBlock`
- `TextFiglet`

Brush kinds:
- `Solid`
- `LinearGradient`

Multi-line TextBlock mapping:
- **per-line restart** by default (each rendered line samples the gradient from the beginning again)

Interpolation defaults:
- **configurable default** (theme-level default + per-brush override)

## Terminology

- **Brush**: a value that can be sampled to produce a `Color` for a given cell within a rectangular region.
- **Brush rect**: the rectangle defining the coordinate space of sampling (often `Bounds`, sometimes per-line).
- **Stop**: a color at an offset in [0..1] for gradients.
- **Tile mode**: behavior when sampling outside [0..1] (clamp/repeat/mirror).
- **Mix space**: which color space is used for interpolation (e.g. `Oklab`).

## Proposed public model (draft)

This section describes the intended public surface area. Exact API names can be adjusted during implementation.

### Brush kinds

V1:
- `Solid`: constant color
- `LinearGradient`: interpolate between multiple stops along a line

Future candidates:
- `RadialGradient`
- Patterns (terminal-friendly equivalents), noise, etc.

### Draft types (C# shape sketch)

```csharp
namespace XenoAtom.Terminal.UI.Styling;

public enum BrushKind
{
    Solid,
    LinearGradient,
}

public enum BrushTileMode
{
    Clamp,
    Repeat,
    Mirror,
}

public readonly record struct GradientStop(float Offset, Color Color);

public readonly record struct GradientPoint(float X, float Y);

public readonly record struct Brush
{
    public BrushKind Kind { get; }
    public BrushTileMode TileMode { get; }
    public ColorMixSpace? MixSpaceOverride { get; }

    // Solid
    public Color SolidColor { get; }

    // Linear gradient (relative points)
    public GradientPoint Start { get; }
    public GradientPoint End { get; }
    public ReadOnlyMemory<GradientStop> Stops { get; }

    public static Brush Solid(Color color, ColorMixSpace? mix = null);

    public static Brush LinearGradient(
        GradientPoint start,
        GradientPoint end,
        ReadOnlyMemory<GradientStop> stops,
        BrushTileMode tileMode = BrushTileMode.Clamp,
        ColorMixSpace? mix = null);

    public Color Sample(int cellX, int cellY, in Geometry.Rectangle brushRect, ColorMixSpace defaultMixSpace);
}
```

### Validation / invariants

- `Stops` must contain at least 2 stops.
- Stop offsets are in [0..1].
- Stops should be sorted by `Offset` (either required or enforced by construction).
- `Color.Default` is not allowed for stops in V1 (it is not a concrete color for interpolation).

Rationale for disallowing `Color.Default`:
- Gradients require concrete colors. "Inherit/default" can be modeled at the control/style level before constructing the brush.

### Default interpolation space (configurable)

Default resolution order:
1. `brush.MixSpaceOverride` if set
2. `Theme.GradientMixSpace` (new theme token; default recommended: `ColorMixSpace.Oklab`)

Draft theme token:

```csharp
public ColorMixSpace GradientMixSpace { get; init; } = ColorMixSpace.Oklab;
```

Rationale:
- `Oklab` tends to look perceptually better for gradients.
- Theme-level default avoids static global state and fits the existing styling approach.

## Sampling semantics

### Coordinate mapping

Sampling uses cell coordinates and a provided `brushRect`.

For a cell `(x, y)` inside `brushRect`, compute normalized coordinates (center-of-cell sampling):
- `u = (x - brushRect.X + 0.5f) / brushRect.Width`
- `v = (y - brushRect.Y + 0.5f) / brushRect.Height`

If `brushRect.Width <= 0` or `brushRect.Height <= 0`, sampling should return the first stop (or solid color) to keep behavior defined.

### Linear gradient parameter

For a linear gradient from `Start` to `End` (both expressed in relative brush coordinates):
- Convert cell `(u, v)` to parameter `t` using projection onto the gradient axis.
- Apply tile mode to `t`.
- Find the enclosing stops and mix colors:
  - `Color.Mix(a, b, t, mixSpace)`

### Tile modes

- `Clamp`: clamp `t` to [0..1]
- `Repeat`: `t = t - floor(t)`
- `Mirror`: triangle-wave mirroring across integer boundaries

## Rendering integration (low impact)

### Key decision: do not change `Style`

`Style` remains per-cell and contains concrete colors only.

Brushes are applied at render time by:
1. sampling per cell, and
2. writing the resulting `Color` into the `Style` used for that cell.

### CellBuffer helper APIs (opt-in)

To avoid duplicating grapheme and wide-glyph handling, V1 should add opt-in helpers (likely as `CellBuffer` extension methods) such as:
- `FillRectWithBrush(...)` (for background/foreground fill)
- `WriteTextWithBrush(...)` (for per-cell foreground/background sampling when writing text)

Important details:
- Wide glyphs occupy multiple cells and use continuation cells. Sampling should produce one style per glyph and apply the same style to continuation cells (preserves current invariants).
- No per-cell allocations (no per-cell `string` creation, no per-cell collections).

## V1 control integration

V1 integrates only `TextBlock` and `TextFiglet`.

### TextBlock

Extend `TextBlockStyle`:
- `Brush? ForegroundBrush`
- `Brush? BackgroundBrush`

Precedence:
- If `ForegroundBrush` is set, it overrides `Foreground` for glyph cells.
- If `BackgroundBrush` is set, it overrides `Background` where background is applied.

Brush rect mapping:
- Single-line: `brushRect = Bounds`
- Multi-line: **per-line restart** (default):
  - for each rendered line at y = `lineY`, use:
    - `brushRect = new Rectangle(Bounds.X, lineY, Bounds.Width, 1)`
  - this makes a horizontal gradient restart on each line.

Background behavior:
- If `FillBackground == true` and `BackgroundBrush` is set, fill the entire bounds using the brush (sample per cell).
- If `FillBackground == false`, apply background brush only to the cells written by the text (consistent with current background application rules).

### TextFiglet

Extend `TextFigletStyle`:
- `Brush? ForegroundBrush`
- `Brush? BackgroundBrush`

Brush rect mapping:
- Use the full `Bounds` by default. This naturally supports diagonal gradients across the figlet area.

Behavior:
- If `ForegroundBrush` is set, sample per glyph cell and set the foreground.
- If `BackgroundBrush` is set, apply it to glyph cells (V1 keeps background "glyph-only" unless a clear `FillBackground` use case appears).

## Examples (intended usage)

- Horizontal gradient text:
  - `ForegroundBrush = LinearGradient((0,0), (1,0), [red -> yellow -> green])`
- Diagonal figlet:
  - `ForegroundBrush = LinearGradient((0,0), (1,1), [accent -> white])`
- Highlight sweep:
  - Use RGBA stops and animate `Start/End` positions or use `Repeat/Mirror` tile modes to move a band.

## Test plan (for implementation)

Unit tests:
- Construction validation:
  - rejects 0/1 stop
  - rejects out-of-range offsets
  - rejects `Color.Default` (V1)
- Sampling correctness:
  - 2-stop gradient sampled at known points yields expected colors for `ColorMixSpace` modes
  - tile mode behavior (clamp/repeat/mirror)

Integration (render-level) tests:
- `TextBlock`:
  - multi-line per-line restart: both line 1 and line 2 start at stop[0] on their first cell
  - `FillBackground` + `BackgroundBrush` fills the entire bounds with sampled colors
- `TextFiglet`:
  - diagonal gradient produces different colors at top-left vs bottom-right cells

## Future extensions (post-V1)

- Radial gradients and additional brush kinds.
- Markup integration (gradient per span) once the model is stable.
- Micro-optimizations if needed after profiling (row-wise incremental stepping, caching sampled rows for stable rects, etc.).
