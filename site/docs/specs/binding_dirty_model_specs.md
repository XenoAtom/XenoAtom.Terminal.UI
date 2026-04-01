---
title: Binding + Dirty Model (Internal Spec)
---

# Binding + Dirty Model (Internal Spec)

This document describes how **XenoAtom.Terminal.UI** keeps the visual tree up-to-date **without manual invalidation**.

It is an **internal** specification intended for framework contributors and control authors.

## Goals

- A visual tree updates automatically from **bindable property writes**.
- Controls do not call ad-hoc invalidation methods (`Invalidate()`, `MarkMeasureDirty()`, …).
- The framework can decide the minimal work to redo per frame:
  - Dynamic update (optional, explicit)
  - Prepare children
  - Measure
  - Arrange
  - Render

## Core concept: dependency tracking

All bindable property getters use `BindingManager.Current.RegisterRead(...)`.

Each of the following phases is executed under a `BindingManager.StartTracking()` session:

- Dynamic updates (`Visual.Update(...)` and dynamic list updates)
- `Visual.PrepareChildren()`
- `Visual.Measure(in LayoutConstraints)`
- `Visual.Arrange(in Rectangle)`
- `Visual.RenderTree(CellBuffer)`

At the end of the phase, the visual reports the set of bindings it read to the owning `TerminalApp` via
`TerminalApp.UpdateDependencies(visual, kind, dependencies)`.

`TerminalApp` maintains indices (binding → visuals) per phase.

When a bindable property is written (via generated setter or `BindingManager.NotifyValueChanged`), the binding manager
raises `BindingManager.ValueChanged`. The `TerminalApp` records these bindings, and on the next tick it:

1. Looks up impacted visuals in each dependency index.
2. Marks those visuals out-of-date for the appropriate phase(s).
3. Performs a single render if anything was invalidated.

## The five phases

### Dynamic update (optional)

Dynamic updates exist for **explicit** “re-evaluate later” behavior (e.g. dynamic list rebuild, animation-driven
properties, etc.).

- User code triggers this via fluent `.Update(...)`.
- Bindable list implementations used for dynamic content use this mechanism to reset/rebuild themselves safely.

Controls should **not** use dynamic updates to wire internal children or templates. Use `PrepareChildren()` for that.

### PrepareChildren()

`PrepareChildren()` is the canonical place to synchronize *public API state* into *internal child visuals*.

Typical uses:

- Apply a template factory result to an internal host.
- Bridge a public bindable property into an internal visual property.
- Attach/detach internal children based on current state.

Properties read during `PrepareChildren()` are tracked. When any of them changes, the framework will re-run
`PrepareChildren()` before layout/rendering.

**Important**: `PrepareChildren()` must be:

- Idempotent (safe to call multiple times)
- Fast (it runs on the UI thread)
- Allocation-light (avoid rebuilding trees unless needed)

### Measure(constraints)

Measure returns **finite** `SizeHints` and must never return infinity for `Natural` or `Min`.

Measure reads must go through bindable properties so dependencies are tracked. Avoid using private fields directly if
those fields correspond to user-configurable state.

### Arrange(finalRect)

Arrange receives a **finite** rectangle and lays out children.

Arrange can depend on state (alignment, scroll offsets, etc.) and those reads are tracked to invalidate arrangement
when necessary.

### RenderTree(buffer)

Render reads are tracked separately from measure/arrange. Rendering should:

- Use the `Bounds` previously computed by arrange.
- Prefer theme/style access via `GetTheme()` and `GetStyle<T>()` so style reads are tracked.

Rendering can be triggered even if layout does not change (e.g. caret blink). Bindable computations used during render
should therefore avoid expensive work on every render tick. Prefer cached state or computed visual updates that only
recompute on dependency changes.

## Manual invalidation is not supported

Controls must not call:

- `Visual.Invalidate()`
- `Visual.MarkMeasureDirty()`, `MarkArrangeDirty()`, `MarkRenderDirty()`, …

These APIs exist as framework internals and are driven by `TerminalApp` when bindings change.

If a control needs to react to a state change, make that state bindable and read it during the appropriate phase(s).

## Visual-typed bindables and attachment

For bindable properties of type `Visual` (or nullable `Visual?`), the source generator can generate “attach child”
logic automatically.

### Default behavior

`[Bindable] public partial Visual? Content { get; set; }`

- When set, the generated setter attaches the child to `this` (and detaches the previous value).
- This is appropriate when the property represents a **direct child** in the logical tree.

### NoVisualAttach

Some controls store visuals for templating/internal composition and attach them elsewhere (e.g. to an internal host).
In that case, auto-attachment is incorrect.

Use:

`[Bindable(NoVisualAttach = true)] public partial Visual? X { get; set; }`

The generator will:

- Still generate binding accessors and binding notifications.
- **Skip** auto-attach/detach logic in the property setter.

Then attach/detach should happen in `PrepareChildren()`.

## Source generator rules (framework convention)

- Bindable properties always get binding accessors and binding notifications.
- Fluent extension methods are generated **only** for:
  - Public bindable properties
  - Declared on publicly visible types
- Non-public bindables (private/protected/internal) still work for binding and dependency tracking, but do not generate
  fluent APIs.

## ComputedVisual (pattern)

`ComputedVisual` exists to build a child visual from a factory:

- It exposes a bindable `Func<Visual> DynamicVisual`.
- `PrepareChildren()` evaluates the factory and attaches the resulting `Child`.

This avoids using dynamic updates as “initializers” for dynamic children.

## Control author checklist

When implementing or refactoring a control:

1. Use `[Bindable]` for all state that affects layout/rendering.
2. Read state through the bindable property in `PrepareChildren()`, `MeasureCore`, `ArrangeCore`, and `RenderOverride`.
3. Bridge public state to internal visuals inside `PrepareChildren()`.
4. For `Visual` properties that are not direct children, use `[Bindable(NoVisualAttach = true)]` and attach in
   `PrepareChildren()`.
5. Do not call manual invalidation APIs.

