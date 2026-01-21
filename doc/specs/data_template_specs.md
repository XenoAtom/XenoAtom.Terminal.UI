# Data Template Specifications

This document specifies a uniform, idiomatic, and extensible **data templating** model for `XenoAtom.Terminal.UI`.

The goal is to make it easy to:

- Render arbitrary data inside UI controls (lists, trees, grids, content panes, etc.).
- Define **defaults** once (per app / per subtree) so controls “just work” without per-instance boilerplate.
- Override templates locally when needed.
- Keep templates **reactive** and compatible with the binding/dependency tracking model.
- Support **virtualization and recycling** for very large data sets.

This spec borrows proven ideas from WPF/Avalonia/SwiftUI (DataTemplates, content presenters, item containers),
but adapts them to a retained-mode terminal UI with:

- Binding-driven invalidation (no diff engine).
- Environment-scoped configuration (`Visual.Get<T>()` / `Visual.Set<T>(...)`).
- Fluent API patterns.

---

## 1. Design goals

### 1.1 Primary goals

1. **Uniform API** across the codebase for "data -> `Visual`" mapping.
2. **Environment-scoped defaults**:
   - A template defined at the app/theme level becomes the default for all controls.
   - A template defined on a container affects only its descendants.
3. **Per-control overrides**:
   - A control can override the default template for its own items/content.
4. **Composability**:
   - Controls can combine a data template (content) with their own chrome (selection, focus, hover, disabled).
5. **Reactive correctness**:
   - Templates can safely bind to `State<T>`/`Binding<T>` and update without manual `RequestRender()`.
6. **Virtualization-ready**:
   - The templating contract must support recycling/reuse to avoid re-allocating visuals while scrolling.
7. **Performance**:
   - The core templating pipeline should avoid hot-path allocations; in particular, presenters should be generic
     (`DataPresenter<T>`) to avoid boxing.

### 1.2 Non-goals (V1)

- XAML-style triggers and named template parts.
- A full MVVM framework.

---

## 2. Terminology

- **Data template**: a contract that converts a data value into a `Visual` subtree (and optionally updates an existing subtree).
- **Template role**: why a template is being used. At minimum:
  - `Display`: render a value for viewing.
  - `Editor`: render a value for editing (typically requires a bindable source, e.g. `State<T>`/`Binding<T>`).
- **Template registry**: an environment-scoped collection of templates used for resolution.
- **Data presenter**: a control that hosts a single data value and renders it using the template registry
  (WPF “ContentPresenter” equivalent).
- **Recycling**: updating an existing visual instance to represent a different data item (typically used by virtualized lists).

---

## 3. Proposed public surface

### 3.1 `DataTemplateRole`

```csharp
public enum DataTemplateRole
{
    Display = 0,
    Editor = 1,
}
```

Notes:

- Most item controls use `Display` by default.
- Editor surfaces (future: forms/property grids) use `Editor`.

### 3.2 `DataTemplateContext`

`DataTemplateContext` provides metadata to templates without forcing every control to invent its own signature.

```csharp
public readonly record struct DataTemplateContext(
    Visual Owner,
    DataTemplateRole Role,
    int Index,
    DataTemplateItemState State);

[Flags]
public enum DataTemplateItemState
{
    None     = 0,
    Selected = 1 << 0,
    Hovered  = 1 << 1,
    Focused  = 1 << 2,
    Disabled = 1 << 3,
}
```

Guidance:

- Most templates should ignore `State`; selection/hover rendering should primarily be the responsibility of the owning control
  (similar to WPF’s `ItemContainerStyle`).
- `Index` MUST be `-1` when the template is not item-based (e.g. a single `DataPresenter<T>`).

### 3.3 `DataTemplate<T>` (recyclable contract)

The core template representation is a **struct** so template slots are not themselves delegates (avoids method-resolution conflicts
and keeps bindable properties simple). Internally it can still wrap delegates.

```csharp
public delegate Visual DataTemplateFactory<T>(Binding<T> binding, in DataTemplateContext context);

public delegate bool DataTemplateUpdater<T>(Visual visual, Binding<T> binding, in DataTemplateContext context);

public delegate void DataTemplateReleaser(Visual visual);

public readonly record struct DataTemplate<T>(
    DataTemplateFactory<T>? Create,
    DataTemplateUpdater<T>? TryUpdate = null,
    DataTemplateReleaser? Release = null)
{
    public bool IsEmpty => Create is null;
}
```

Semantics:

- `Create` builds a new `Visual` for a binding.
- `TryUpdate` enables recycling: update an existing visual instance to represent a different binding.
  - Returns `true` if the visual was updated successfully.
  - Returns `false` if the visual cannot be reused for that value (caller should fall back to `Create`).
- `Release` is called when a visual is removed from a recycling pool permanently (optional hook to dispose resources).

Normative rules:

- `Create` MUST return a non-null `Visual` (otherwise throw at call site).
- `TryUpdate` MUST NOT attach/detach visuals directly; it should only update bindable properties/state on the provided visual subtree.
  (The owning control manages parenting.)

### 3.4 `DataTemplates` (environment-scoped registry)

`DataTemplates` is an environment-scoped registry of templates, resolved via `Visual.Get<DataTemplates>()`.

#### 3.4.1 Immutability without duplication (overlay chaining)

`DataTemplates` SHOULD be immutable and *replaced* via `Visual.Set(...)` when changed.

To avoid "copy the world to override one entry", `DataTemplates` supports cheap overrides by chaining:

- Each `DataTemplates` instance stores only the templates registered in that layer.
- Resolution checks the current layer first, then walks `Parent` until a match is found.
- Overriding a single entry is done by creating a new layer whose `Parent` points to the existing registry.

```csharp
public sealed record DataTemplates : IStyle<DataTemplates>
{
    public static DataTemplates Default { get; }
    public static StyleKey<DataTemplates> Key { get; }

    // Optional: parent for overlay chaining.
    public DataTemplates? Parent { get; init; }

    // Register only adds/overrides in the current layer.
    public DataTemplates Register<T>(DataTemplateRole role, DataTemplate<T> template);

    // Resolve prefers the current layer and falls back to Parent.
    public bool TryResolve<T>(DataTemplateRole role, out DataTemplate<T> template);
}
```

Key points:

- Overriding a single template should allocate only the new layer + the new entry.
- Controls that read `Get<DataTemplates>()` are automatically re-evaluated when the subtree’s `DataTemplates` value changes via `Set`.

Why immutability works well with bindings:

- The binding system already tracks `Get<T>()` calls. When you do `container.Set(newRegistry)`, any descendants that read
  `Get<DataTemplates>()` are invalidated and re-evaluated.
- The registry itself does not need to be bindable or mutable to be reactive; *replacing* the environment value is enough.
  This keeps the templating model simple and allocation-free in hot paths.

#### 3.4.2 Registry construction (builder-style)

For usability, the API SHOULD include a builder-style entry point to avoid repeatedly copying internal tables when registering
multiple templates at once.

Example:

```csharp
var templates = DataTemplates.Default.Derive(builder => builder
    .Register<string>(DataTemplateRole.Display, new((Binding<string> b, in DataTemplateContext _) => new TextBlock(() => b.GetValue())))
    .Register<DateTime>(DataTemplateRole.Display, new((Binding<DateTime> b, in DataTemplateContext _) => new TextBlock(() => b.GetValue().ToString("u"))))
);

root.Set(templates);
```

Proposed API:

```csharp
public sealed record DataTemplates : IStyle<DataTemplates>
{
    public static DataTemplates Default { get; }
    public static StyleKey<DataTemplates> Key { get; }

    public DataTemplates? Parent { get; init; }

    // Convenience: single registration for simple cases.
    public DataTemplates Register<T>(DataTemplateRole role, DataTemplate<T> template);

    // Preferred for multiple registrations: the builder mutates a temporary table once.
    public DataTemplates Derive(Func<DataTemplatesBuilder, DataTemplatesBuilder> configure);
}

public sealed class DataTemplatesBuilder
{
    public DataTemplatesBuilder Register<T>(DataTemplateRole role, DataTemplate<T> template);
}
```

Notes:

- `Derive(...)` returns a new `DataTemplates` instance whose `Parent` defaults to the receiver, so you don't need to pass
  `Parent = ...` manually for common overlay cases.
- `Register(...)` can remain as a convenience method, but the documentation SHOULD recommend `Derive(...)` for any non-trivial
  set of registrations.

#### 3.4.3 Resolution matching

Resolution MUST support:

- Exact match for `TryResolve<T>(...)` (strongly typed; allocation-free).

### 3.5 `DataPresenter<T>`

`DataPresenter<T>` hosts a single value and resolves a template to render it.

It is generic to avoid boxing for value types and to keep binding paths type-safe.

```csharp
public sealed class DataPresenter<T> : Visual
{
    [Bindable] public partial T Value { get; set; }
    [Bindable] public partial DataTemplateRole Role { get; set; }

    // Optional override: if non-empty, bypasses registry resolution.
    [Bindable] public partial DataTemplate<T> Template { get; set; }
}
```

Behavior:

1. If `Template` is non-empty: use it.
2. Else resolve via `Get<DataTemplates>()`.
3. The produced visual becomes the presenter's single child visual.

Caching guidance:

- The presenter SHOULD keep the produced child visual until the *effective template* changes.
- The presenter MUST NOT rebuild solely because the value changes: templates receive a `Binding<T>` and should react via bindings.

### 3.6 Template properties on item controls

Controls that render items should expose a template slot:

```csharp
public sealed partial class Select<T> : Visual
{
    [Bindable] public partial DataTemplate<T> ItemTemplate { get; set; }
}
```

Rules:

- The default value is `default`/empty, meaning “use environment templates”.
- Controls may still keep “control templates” (e.g. popup wrappers) in styles; those are separate from **data templates**.

---

## 4. Resolution rules (normative)

When a control needs a visual for a bindable value `binding` and role `role`:

1. If the control has a non-empty per-instance template slot for that operation:
   - Use it.
2. Otherwise resolve via environment:
   - `var templates = Get<DataTemplates>();`
3. If no template is found, fall back:
   - If the current value is `Visual`, use it directly (identity).
   - Else render `new TextBlock(() => binding.GetValue()?.ToString())`.

### 4.1 Typed vs runtime resolution

To keep `DataPresenter<T>` allocation-free:

- Typed resolution (`TryResolve<T>`) is sufficient.

Notes:

- `TryResolve<T>` is intentionally **exact-match only**. Because `DataTemplate<T>` is strongly typed, a template registered for
  a base type or interface cannot be returned as `DataTemplate<T>` without introducing allocations (adapters).

If a consumer wants heterogeneous items without boxing, they should use a reference-type base/interface for `T`.

### 4.2 Null handling

`null` SHOULD be treated as a valid input:

- If a template is registered for `null` (role-specific), use it.
- Else fall back to `new TextBlock(string.Empty)`.

---

## 5. Virtualization and recycling

This section defines how templating supports large data sets with minimal allocations.

### 5.1 Recycling contract

Virtualizing controls SHOULD:

1. Create visuals via `template.Create`.
2. Reuse visuals by keeping a pool of detached visuals (removed from the visual tree).
3. When reusing a pooled visual:
   - If `template.TryUpdate` exists and returns `true`, reuse the visual.
   - Otherwise, discard it and create a new visual.
4. When permanently discarding a visual from the pool (e.g. the pool is over capacity, the control is disposed, or the template changes):
   - Call `template.Release` if provided.

Notes:

- “Detached” means: not parented to a live control and not participating in layout/render.
- `Release` is a finalizer-style hook for pooled visuals; it should be treated as “this instance will never be reused again”.

### 5.2 Important guidance for recyclable templates

For templates to be safely recyclable:

- Avoid capturing the item value in dynamic updates (`Update(...)`) that are registered once and never cleared.
- Prefer updating bindable properties on the existing visual, or use `State<T>`/`Binding<T>` inside the visual subtree.

Recommended pattern for maximum reuse:

- The owning control keeps a `State<T>` per realized item visual.
- The template builds a visual subtree bound to that `State<T>` (and possibly to selection/hover state).
- Recycling then becomes a cheap `state.Value = newItem`.

This pattern is a strong differentiator for a binding-driven terminal UI: you get React-like reuse without a diff engine.

### 5.3 Pool sizing

Pools SHOULD be bounded (configurable per control style/options) to avoid unbounded memory usage when scrolling through huge lists.

---

## 6. Interaction with the binding system

### 6.1 Tracked reads

Template resolution MUST be a tracked read:

- Controls MUST read template slots through bindable properties (e.g. `ItemTemplate`), not private fields.
- Controls MUST read the `DataTemplates` registry via `Get<DataTemplates>()` so changes to the registry can invalidate dependent visuals.

### 6.2 Rebuild vs update triggers

Controls should rebuild item visuals when:

- The effective template changes (instance slot or environment).
- Items are added/removed/reordered (or the realized viewport changes).

Controls should NOT rebuild visuals merely because a `State<T>` value changes; the visual subtree should update through bindings.

---

## 7. Defaults (recommended)

The default theme should ship with templates that make “drop data in UI” productive.

### 7.1 Display defaults

Recommended defaults for `DataTemplateRole.Display`:

- `string` -> `new TextBlock(() => binding.GetValue())`
- `string?` -> `new TextBlock(() => binding.GetValue() ?? string.Empty)`
- `bool` -> `new TextBlock(() => binding.GetValue() ? "True" : "False")`
- Numeric primitives -> `new TextBlock(() => binding.GetValue().ToString())`
- `Visual` -> identity (already a visual)

### 7.2 Reactive display defaults

Recommended guidance:

- Prefer building visuals that read `binding.GetValue()` inside a lambda so changes are tracked automatically.

### 7.3 Editor defaults (reactive + bidirectional)

Editor templates should generally exist for types that have built-in editor controls:

- `string?` -> `new TextBox().Text(binding)`
- `int` -> `new NumberBox<int>().Value(binding)`
- `bool` -> `new Switch().IsOn(binding)` (or `CheckBox`)

This enables a future property grid/forms experience without adding a new framework layer.

---

## 8. Examples

### 8.1 Per-instance item template

```csharp
new Select<MyModel>()
    .Items(models)
    .ItemTemplate(new DataTemplate<MyModel>(
        (Binding<MyModel> binding, in DataTemplateContext _) =>
        {
            var m = binding.GetValue();
            return new HStack(
                    new TextBlock(m.Name),
                    new TextBlock(() => $"#{m.Id}").Style(TextBlockStyle.Muted))
                .Spacing(2);
        }));
```

### 8.2 Subtree-scoped defaults (overlay chaining)

```csharp
var templates = new DataTemplates { Parent = DataTemplates.Default }
    .Register<string>(DataTemplateRole.Display, new((Binding<string> b, in DataTemplateContext _) => new TextBlock(() => $"> {b.GetValue()}")));

new VStack(
    new Select<string>().Items(["One", "Two", "Three"]),
    new ListBox<string>().Items(["A", "B", "C"])
)
.Set(templates);
```

### 8.3 `DataPresenter<T>` for “just show this value”

```csharp
var name = new State<string?>("Alex");

new VStack(
    new DataPresenter<string?>().Value(name).Role(DataTemplateRole.Display),
    new DataPresenter<string?>().Value(name).Role(DataTemplateRole.Editor)
).Spacing(1);
```

---

## 9. Control adoption (status)

This repository is pre-1.0; breaking changes are acceptable if they improve usability and consistency. The focus is on simplifying
user code and making the framework more coherent, while keeping performance excellent. Tests, samples, and documentation MUST be
updated accordingly.

### 9.1 Controls already generic (`<T>`) that adopt the model

#### `Select<T>`

Current implementation:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- If `ItemTemplate` is empty: resolve `DataTemplateRole.Display` from `DataTemplates`.

Benefits:

- Environment defaults become possible (app-wide item rendering).
- Future virtualization can reuse the `TryUpdate` path.

### 9.2 Controls that became generic

#### `OptionList<T>`

Current implementation:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- `SelectedIndex` remains.
- The control owns the item chrome (marker, hover, selection highlight); template only produces content.

#### `SelectionList<T>`

Current implementation:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- `SelectedIndex` + `Checked : BindableList<bool>` (kept in sync with `Items`)

#### `ListBox<T>`

Current implementation:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- When a visual list is desired, use `ListBox<Visual>` (the default registry includes `Visual` identity).

#### `TreeView` -> `TreeView<TNode>` (or `TreeView<T>`)

Current:

- `Roots : BindableList<TreeNode>` where `TreeNode` contains `Header : Visual` and `Data : object?`.

Proposed:

- Separate **data model** from **visual model**:
  - Either:
    - `TreeView<T>` with `Roots : BindableList<T>` + `ChildrenSelector : Func<T, IEnumerable<T>>`
  - Or:
    - `TreeView<TNode>` where `TNode` is a node model interface/class with `Children`.
- Templates:
  - `NodeTemplate : DataTemplate<TNode>` for the header/content.
  - Optional icon template or icon resolver.

Migration strategy:

- Keep the current `TreeNode` API initially; introduce the generic version as additive.

---

## 10. Future extensions (V2+)

The V1 primitives above are chosen to unlock:

1. **Template selectors**:
   - `IDataTemplateSelector` similar to WPF `DataTemplateSelector`.
2. **Item container templates**:
   - Customize chrome (selection/focus) separately from item content.
3. **Auto-form generation**:
   - Reflect over a model and use `Editor` templates for `Binding<T>` properties.
4. **Source generator helpers**:
   - Generate strongly typed `.ItemTemplate(...)` extensions and diagnostics for missing templates.

---

## 11. Namespaces and code organization (proposal)

Data templating types SHOULD live in a dedicated namespace to keep the public API discoverable:

- `XenoAtom.Terminal.UI.Templating`
  - `DataTemplateRole`
  - `DataTemplateContext`
  - `DataTemplateItemState`
  - `DataTemplate<T>` and related delegates
  - `DataTemplates` and `DataTemplatesBuilder`
- `XenoAtom.Terminal.UI.Controls`
  - `DataPresenter<T>` (as a visual/control)
  - Control-specific `ItemTemplate` properties (e.g. `Select<T>.ItemTemplate`)

File layout (suggested):

- `src/XenoAtom.Terminal.UI/Templating/`
  - `DataTemplateRole.cs`
  - `DataTemplateContext.cs`
  - `DataTemplate{T}.cs`
  - `DataTemplates.cs`
  - `DataTemplatesBuilder.cs`
- `src/XenoAtom.Terminal.UI/Controls/`
  - `DataPresenter{T}.cs`
