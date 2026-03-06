---
title: Layout
---

# Layout

Terminal UI layout is **cell-based** (integer coordinates) and uses a simple two-pass protocol:

- **Measure**: compute intrinsic `SizeHints` under `LayoutConstraints`
- **Arrange**: receive a finite `Rectangle` and position children inside it

This page explains how the layout protocol works and how to use it when building custom controls.
The full specification is also available for deeper details:

- [Layout Protocol Specs](specs/layout_protocol_specs.md)

## Why a two-pass protocol?

In a terminal UI:

- everything is rendered on a discrete grid (cells)
- text wrapping depends on the available width
- many controls need a “preferred” size but must still adapt to the viewport

A two-pass protocol gives predictable results:

- **Measure** answers: “How big would you like to be (min / natural / max) under these constraints?”
- **Arrange** answers: “Here is your actual rectangle; place your children inside it.”

This separation is what enables:

- accurate wrapping (measure under a known width)
- virtualization (only arrange/render what is visible)
- scrollable viewports (extent vs viewport logic)

## Key types

### `LayoutConstraints`

`LayoutConstraints` describes the allowed range for a measure pass:

- `MinWidth`, `MaxWidth`
- `MinHeight`, `MaxHeight`
- `IsWidthBounded`, `IsHeightBounded`

Unbounded maxima are represented by `int.MaxValue` and surfaced as `IsWidthBounded == false` / `IsHeightBounded == false`.

You’ll often create constraints using:

```csharp
var constraints = LayoutConstraints.FromMaxSize(new Size(80, 25));
// or:
var constraints = LayoutConstraints.Unbounded;
```

### `SizeHints`

`MeasureCore(...)` returns `SizeHints`:

- `Min`: minimum size the visual needs to function
- `Natural`: preferred size (must be finite)
- `Max`: maximum size (can be `int.MaxValue` per axis)
- `FlexGrowX/Y`, `FlexShrinkX/Y`: how a container may distribute extra/deficit space

Most controls use the helpers:

```csharp
SizeHints.Fixed(new Size(w, h));
SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
SizeHints.FlexX(min, natural);
SizeHints.FlexY(min, natural);
```

> [!IMPORTANT]
> `Natural` must be finite. If you want “unbounded growth”, use `Max = int.MaxValue` (and flex factors) rather than an
> infinite natural size.

### `Rectangle`

`Arrange(Rectangle)` provides a **finite** rectangle in UI coordinates. The framework clamps negative sizes to `0`.

## What the framework already does for you

### Margin is handled on `Visual`

Margin is a `Visual` property. You usually do not need to handle it inside a custom control:

- during measure, the framework deflates constraints by `Margin`, then inflates the returned `SizeHints` back by `Margin`
- during arrange, the framework deflates the final rect by `Margin` before calling `ArrangeCore(...)`

This means containers can generally use `child.DesiredSize` / `child.MeasureHints` directly and margins “just work”.

### `IsVisible` collapses layout

`Visual.IsVisible` is a **collapsed** visibility flag:

- `IsVisible = true`: the visual participates in layout, rendering, and input (subject to `IsEnabled`)
- `IsVisible = false`: the visual measures as `(0,0)`, arranges to an empty rect, does not render, and does not receive hit testing/input

This means hidden visuals do not reserve space in `HStack`, `VStack`, `WrapStack`, or other containers unless a control
explicitly models a separate “hidden but keeps layout slot” concept.

### Alignment is self-alignment

Alignment (`Align.Start/Center/End/Stretch`) is applied **by the child** when it is arranged into a slot.
As a container author you typically:

1) choose a *slot rectangle* for the child (where it may live)
2) call `child.Arrange(slot)`
3) the child positions itself inside that slot based on its alignment and min/max

This design is why many containers are simpler: they don’t need to implement per-child alignment logic.

By default:

- `HorizontalAlignment` is `Align.Start`
- `VerticalAlignment` is `Align.Start`

Containers and content controls may choose defaults more appropriate for their role (e.g. `ScrollViewer` stretches).

> [!TIP]
> Containers often default to `Align.Stretch` so they fill the viewport (e.g. `ScrollViewer`, `DataGridControl`),
> while leaf/content controls often default to `Align.Start` and size to content (e.g. `Button`, `TextBlock`).

## Implementing `MeasureCore` (custom controls)

### Rules of thumb

- Measure must be **pure**: compute hints, do not mutate persistent state needed by other phases.
- Always clamp subtractions: `Math.Max(0, max - padding)` to avoid negative widths/heights.
- Prefer returning `SizeHints` that preserve the child’s flex factors when you are just “wrapping” a child.

> [!IMPORTANT]
> Layout runs under a binding tracking context. If you read bindables during measure/arrange, those reads become
> dependencies for invalidation. Read state through properties (bindables), not private backing fields.

### Example: a padded single-child control

This is the classic pattern used by `Padder`, `Border`, and many content controls:

```csharp
public sealed class PaddedContent : ContentVisual
{
    [Bindable] public Thickness Padding { get; set; } = new(1);

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var pad = Padding;
        var padH = pad.Horizontal;
        var padV = pad.Vertical;

        var maxW = constraints.IsWidthBounded ? Math.Max(0, constraints.MaxWidth - padH) : int.MaxValue;
        var maxH = constraints.IsHeightBounded ? Math.Max(0, constraints.MaxHeight - padV) : int.MaxValue;
        var inner = new LayoutConstraints(0, maxW, 0, maxH);

        var child = Content;
        var childHints = child is null ? SizeHints.Fixed(Size.Zero) : child.Measure(inner);

        var min = new Size(childHints.Min.Width + padH, childHints.Min.Height + padV);
        var nat = new Size(childHints.Natural.Width + padH, childHints.Natural.Height + padV);
        var max = new Size(
            childHints.Max.Width == int.MaxValue ? int.MaxValue : childHints.Max.Width + padH,
            childHints.Max.Height == int.MaxValue ? int.MaxValue : childHints.Max.Height + padV);

        return SizeHints.Flex(min, nat, max,
            childHints.FlexGrowX, childHints.FlexGrowY,
            childHints.FlexShrinkX, childHints.FlexShrinkY).Normalize();
    }

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var child = Content;
        if (child is null)
        {
            return;
        }

        var pad = Padding;
        var inner = new Rectangle(
            finalRect.X + pad.Left,
            finalRect.Y + pad.Top,
            Math.Max(0, finalRect.Width - pad.Horizontal),
            Math.Max(0, finalRect.Height - pad.Vertical));

        child.Arrange(inner);
    }
}
```

Notes:

- The example uses `int.MaxValue` for “unbounded” because the sentinel is `int.MaxValue` in this library.
- The container doesn’t implement alignment; the child does.

## Implementing `ArrangeCore` (custom controls)

### Slot-based arrangement

When arranging children, think in **slots**:

- compute a rectangle where a child is allowed to live
- call `child.Arrange(slot)`

The child will:

- apply its `Margin`
- apply `Align.Start/Center/End/Stretch`
- respect its own `MinWidth/MaxWidth/MinHeight/MaxHeight`

### When you might re-measure in arrange

Some controls need to re-measure children when the final main-axis size becomes known.
For example, a wrapping text visual might measure differently under width `∞` vs width `40`.

This is a valid pattern (used by `WrapStackBase`), but it should be used carefully:

- re-measuring in arrange can be expensive if done frequently
- prefer doing the “real” measure under a bounded width whenever possible

## Flex sizing (Grow/Shrink)

Containers like `HStack`, `VStack`, and `WrapStackBase` use flex factors to distribute space:

- when there is extra room, children with `FlexGrowX/Y > 0` get a share of it (up to `Max`)
- when space is tight, children with `FlexShrinkX/Y > 0` give up space (down to `Min`)

You don’t need to use flex to write custom controls, but it’s the mechanism that makes “fill remaining space” work
predictably.

## Troubleshooting

### “My control takes infinite size”

- Ensure `Natural` is finite (never `int.MaxValue`).
- Use `Max = int.MaxValue` when you want unbounded growth.
- Use `Align.Stretch` (and flex grow) when you want the control to fill its container.

### “Layout doesn’t update when my state changes”

- Make sure state that affects layout is **bindable** and is read during `MeasureCore` or `ArrangeCore`.
- Don’t read private fields; read bindable properties so the framework can track dependencies.
- `IsVisible` participates in layout dependency tracking, so toggling it invalidates parent layout automatically.

### “Cannot read then write within the same tracking context”

Layout participates in binding tracking. If you compute and also “fix up” a bindable during `Measure`/`Arrange`,
you may hit the binding loop guard.

See [Binding & State](binding.md) for the recommended “split read/write across phases” pattern.

## Related

- [Layout Protocol Specs](specs/layout_protocol_specs.md)
- [Control Development](control-development.md)
- [Binding & State](binding.md)
- [Controls Reference](controls/readme.md)
