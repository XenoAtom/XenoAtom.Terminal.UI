---
title: "Binding & State"
---

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

> [!NOTE]
> `State<T>` is itself a bindable model: it is just a class with a single `[Bindable]` property named `Value`.
> Use `State<T>` for small, UI-only state. Use a custom bindable model when you want to group multiple related
> values together.

Conceptually, `State<T>` is similar to:

```csharp
public sealed partial class MyState<T>
{
    [Bindable] public partial T Value { get; set; }
}
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

## Bindable models (your own data types)

You can also use `[Bindable]` on your own model classes so controls can bind to them directly.
This is especially useful for data-driven controls like [DataGridControl](controls/datagrid.md).

Example:

```csharp
using XenoAtom.Terminal.UI;

public enum WorkItemKind { Feature, Bug, Chore }
public enum WorkItemPriority { Low, Medium, High, Critical }

public sealed partial class WorkItem
{
    [Bindable] public partial bool Done { get; set; }
    [Bindable] public partial WorkItemKind Kind { get; set; }
    [Bindable] public partial WorkItemPriority Priority { get; set; }
    [Bindable] public partial int EstimateHours { get; set; }
    [Bindable] public partial string Title { get; set; } = string.Empty;
    [Bindable] public partial string Notes { get; set; } = string.Empty;
}
```

The generator produces a `Bind` helper (for bindings) and strongly-typed `BindingAccessor<T>` instances (for reuse):

### Binding a model to controls (forms)

```csharp
var model = new WorkItem();

var ui = new VStack(
    new HStack(new Switch().IsOn(model.Bind.Done), "Done").Spacing(1),
    new EnumSelect<WorkItemKind>().Value(model.Bind.Kind),
    new EnumSelect<WorkItemPriority>().Value(model.Bind.Priority),
    new NumberBox<int>().Value(model.Bind.EstimateHours),
    new TextBox().Text(model.Bind.Title),
    new TextArea().Text(model.Bind.Notes).MinHeight(5).MaxHeight(5).Scrollable()
);
```

### Reusing accessors (DataGrid, documents, schema)

```csharp
var done = WorkItem.Accessor.Done;
var title = WorkItem.Accessor.Title;
```

You can plug these accessors into the data grid document model without writing manual getters/setters:

```csharp
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;

var doc = new DataGridListDocument<WorkItem>()
    .AddColumn(WorkItem.Accessor.Done)
    .AddColumn(WorkItem.Accessor.Title);

doc.AddRow(new WorkItem { Done = false, Title = "Write docs" });
doc.AddRow(new WorkItem { Done = true, Title = "Ship v1" });

using var view = new DataGridDocumentView(doc);
var grid = new DataGridControl { View = view };
```

> [!NOTE]
> `[Bindable]` requires source generation. In most projects this is enabled automatically by referencing
> `XenoAtom.Terminal.UI` (which brings the generator as an analyzer). If you use custom build settings, ensure the
> source generator package is included so your partial properties are generated.

## When to use `Func<T>`

Use `Func<T>` to compute a value on demand, while still being dependency-tracked:

```csharp
new TextBlock(() => $"Tick: {tick.Value}")
```

The same pattern applies to styles:

```csharp
new Button("Save")
    .Style(() => isDanger.Value
        ? (ButtonStyle.Default with { ShowBorder = true })
        : ButtonStyle.Default);
```

For style-specific guidance, see [Styling](styling.md).

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

- [Text Editing](text-editing.md)
