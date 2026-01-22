# Data templating

Many controls in XenoAtom.Terminal.UI are **data-driven**: they take a list of values (`T`) and render each value using a **data template**.

This enables:

- Environment-scoped defaults (set once, apply everywhere in a subtree)
- Per-control overrides
- Allocation-friendly virtualization/recycling (via `DataTemplate<T>.TryUpdate` and `Release`)

Screenshot placeholder:

![Data templates](../img/screenshots/data-templates.png)

## DataTemplates registry

The active template registry is stored in the visual environment and resolved with:

- `Visual.Get<DataTemplates>()`
- `Visual.SetStyle(templates)` (or `visual.Style(templates)`)

`DataTemplates` is immutable and supports overlay chaining via `Derive(...)`:

```csharp
using XenoAtom.Terminal.UI.Templating;

var templates = DataTemplates.Default.Derive(builder => builder
    .Register<string>(DataTemplateRole.Display, new(
        (string value, in DataTemplateContext _) => new TextBlock($"> {value}"))));

var ui = new VStack(
        new ListBox<string>().Items(["One", "Two", "Three"]),
        new Select<string>().Items(["Alpha", "Beta"]))
    .Spacing(1)
    .Style(templates);
```

## DataPresenter<T>

`DataPresenter<T>` is the "content presenter" for data values: it hosts a single value and renders it using a resolved template.

```csharp
using XenoAtom.Terminal.UI.Templating;

var name = new State<string?>("Alex");

new VStack(
        name.PresentAs(DataTemplateRole.Display),
        name.PresentAs(DataTemplateRole.Editor))
    .Spacing(1);
```

`Editor` templates are intended for bindable sources (such as `State<T>` or `Binding<T>`), so that the editor can update the source.

## Per-control templates

Item controls expose template slots to override the environment:

- `Select<T>.ItemTemplate`
- `ListBox<T>.ItemTemplate`
- `OptionList<T>.ItemTemplate`
- `SelectionList<T>.ItemTemplate`

Example:

```csharp
using XenoAtom.Terminal.UI.Templating;

new Select<string>()
    .Items(["First", "Second", "Third"])
    .ItemTemplate(new DataTemplate<string>(
        (Binding<string> binding, in DataTemplateContext _) =>
            new HStack(Symbols.ArrowRight, new TextBlock(() => binding.GetValue())).Spacing(1)));
```

## Notes on performance and recycling

Controls may reuse visuals when items scroll in/out of view:

- `TryUpdate` allows a visual subtree to be reused for a different value.
- `Release` allows cleanup when a visual leaves the pool permanently.

For best results, templates should prefer updating bindable properties on an existing subtree rather than capturing item values in
dynamic updates that are registered once and never cleared.
