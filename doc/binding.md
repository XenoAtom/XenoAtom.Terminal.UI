# Binding & State

XenoAtom.Terminal.UI uses a binding model designed for terminal UIs:

- property access is tracked during dynamic updates, layout, and rendering
- when state changes, only the affected visuals are invalidated

The goal is to let you build “live” UIs without manual invalidation calls (`RequestRender`, `MarkMeasureDirty`, …).

## State

`State<T>` is a small observable container used to drive UI:

```csharp
var name = new State<string>("Alex");

var ui = new VStack(
    new TextBlock(() => $"Hello {name.Value}"),
    new TextBox().Text(name)
);
```

## Tracking contexts (what gets invalidated)

Bindable values are tracked when they are *read* during a “tracking context”, including:

- dynamic updates / composition (building children, reacting to changes)
- `PrepareChildren`
- layout (`Measure` / `Arrange`)
- `Render`
- input handlers that read bindables to decide behavior

When a tracked value changes, the framework re-runs only the relevant passes for the affected visuals.

> [!IMPORTANT]
> For tracking to work, always read bindable state through the **property**, not a private backing field.

## Bindable properties

Most public control properties are `[Bindable]` and participate in dependency tracking.
The source generator emits:

- property accessors wired into the binding hub
- fluent extension methods for `T`, `Func<T>`, and `State<T>` overloads

## When to use `Func<T>`

Use `Func<T>` to compute a value on demand, while still being dependency-tracked:

```csharp
new TextBlock(() => $"Tick: {tick.Value}")
```

## Two-way binding

Some controls (TextBox/TextArea) can bind their value to a `State<string>` by providing a document wrapper that reads/writes the bound value.

## The “read then write” rule (and how to work with it)

To prevent accidental dependency loops, the binding system disallows **reading** and then **writing** the same bindable
property within a single tracking context (e.g. within one `Arrange` pass).

If you hit an exception like:

> Cannot read and then write `SomeControl.SomeProperty` within a same tracking context

it usually means a method both:

- read a bindable property (to react to it), then
- wrote to that same bindable property (to “fix up” state) in the same pass.

### Workaround pattern: split read/write across phases

The most common pattern is to mirror external state into an *internal bindable version* in `PrepareChildren`,
then read that mirrored value in `Arrange` / `Render`.

Example (scrollable controls):

```csharp
[Bindable] private partial int ScrollVersion { get; set; }

protected override void PrepareChildren()
{
    // Write in PrepareChildren…
    ScrollVersion = Scroll.Version;
}

protected override void ArrangeCore(in Rectangle rect)
{
    // …read in ArrangeCore (different tracking context).
    _ = ScrollVersion;

    // Now it is safe to update the scroll model without creating a read/write loop.
    Scroll.SetViewport(rect.Width, rect.Height);
}
```

This pattern is also useful for “measured values” that are computed in `Measure` but consumed in `Arrange`.

> [!TIP]
> If you need a derived/computed value for layout, prefer an internal bindable property like `MeasuredContentWidth`
> and make the dependency explicit (`_ = MeasuredContentWidth;`) in the pass that consumes it.

## Custom controls (user code)

When building your own control:

1. Use `[Bindable]` for any state that affects layout or rendering.
2. Avoid mutating bindable properties during `Render`.
3. If you must derive state during layout, prefer the “split read/write” pattern above.

See also:

- [Text Editing](./text-editing.md)
