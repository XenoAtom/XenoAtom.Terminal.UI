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
public delegate Visual DataTemplateFactory<in T>(T value, in DataTemplateContext context);

public delegate bool DataTemplateUpdater<in T>(Visual visual, T value, in DataTemplateContext context);

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

- `Create` builds a new `Visual` for a value.
- `TryUpdate` enables recycling: update an existing visual instance to represent a different value.
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

To avoid “copy the world to override one entry”, `DataTemplates` supports cheap overrides by chaining:

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

    // Runtime-type resolution for heterogeneous values (reference types).
    public bool TryResolveForValue(
        object? value,
        DataTemplateRole role,
        out DataTemplate<object?> template,
        out Type resolvedDataType);
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

#### 3.4.2 Resolution matching

Resolution SHOULD support:

- Exact match
- Base type match
- Interface match
- Deterministic “most specific wins” ordering

### 3.5 `DataPresenter<T>`

`DataPresenter<T>` hosts a single value and resolves a template to render it.

It is generic to avoid boxing for value types and to keep binding paths type-safe.

```csharp
public sealed class DataPresenter<T> : ContentVisual
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
3. The produced visual becomes `Content` (via the `ContentVisual` pattern).

Caching guidance:

- The presenter SHOULD keep the produced `Content` until the *effective template* changes.
- For reference types, the presenter MAY also use runtime-type resolution so derived types can use their own templates.

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

When a control needs a visual for a value `value` and role `role`:

1. If the control has a non-empty per-instance template slot for that operation:
   - Use it.
2. Otherwise resolve via environment:
   - `var templates = Get<DataTemplates>();`
3. If no template is found, fall back:
   - If `value` is `Visual`, use it directly (identity).
   - Else render `new TextBlock(value?.ToString())`.

### 4.1 Typed vs runtime resolution

To keep `DataPresenter<T>` allocation-free:

- For value types: typed resolution (`TryResolve<T>`) is sufficient.
- For reference types: controls MAY use runtime resolution (`TryResolveForValue`) to support derived-type templates.

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

- `string` -> `new TextBlock(value)`
- `bool` -> `new TextBlock(value ? "True" : "False")`
- Numeric primitives -> `new TextBlock(value.ToString())`
- `Visual` -> identity (already a visual)

### 7.2 Reactive display defaults

Recommended defaults:

- `State<string>` / `Binding<string>` -> `new TextBlock(() => state.Value)`
- `State<int>` / `Binding<int>` -> `new TextBlock(() => state.Value.ToString())`

### 7.3 Editor defaults (reactive + bidirectional)

Editor templates should generally exist for bindable sources:

- `State<string>` / `Binding<string>` -> `new TextBox().Text(binding)`
- `State<int>` / `Binding<int>` -> `new NumberBox<int>().Value(binding)`
- `State<bool>` / `Binding<bool>` -> `new Switch().Value(binding)` (or `CheckBox`)

This enables a future property grid/forms experience without adding a new framework layer.

---

## 8. Examples

### 8.1 Per-instance item template

```csharp
new Select<MyModel>()
    .Items(models)
    .ItemTemplate(new DataTemplate<MyModel>(
        (m, ctx) => new HStack(
            new TextBlock(m.Name),
            new TextBlock(() => $"#{m.Id}").Style(TextBlockStyle.Muted)
        ).Spacing(2)));
```

### 8.2 Subtree-scoped defaults (overlay chaining)

```csharp
var templates = new DataTemplates { Parent = DataTemplates.Default }
    .Register<string>(DataTemplateRole.Display, new((s, _) => new TextBlock($"> {s}")));

new VStack(
    new Select<string>().Items(["One", "Two", "Three"]),
    new ListBox<string>().Items(["A", "B", "C"])
)
.Set(templates);
```

### 8.3 `DataPresenter<T>` for “just show this value”

```csharp
var name = new State<string>("Alex");

new VStack(
    new DataPresenter<State<string>> { Value = name, Role = DataTemplateRole.Display },
    new DataPresenter<State<string>> { Value = name, Role = DataTemplateRole.Editor }
).Spacing(1);
```

---

## 9. Control migration plan (proposal)

This section describes how existing controls can adopt data templating uniformly.

### 9.1 Controls already generic (`<T>`) that should adopt the model

#### `Select<T>`

Current:

- `Items : BindableList<T>`
- Per-instance `ContentFactory : Delegator<Func<T, Visual>>`

Proposed:

- Replace `ContentFactory` with `ItemTemplate : DataTemplate<T>`
- Default behavior:
  - If `ItemTemplate` is empty: resolve `DataTemplateRole.Display` from `DataTemplates`
  - Otherwise use `ItemTemplate`

Benefits:

- Environment defaults become possible (app-wide item rendering).
- Future virtualization can reuse the `TryUpdate` path.

### 9.2 Controls that are not generic but should become generic

#### `OptionList` -> `OptionList<T>`

Current:

- `Items : VisualList<OptionListItem>` where `OptionListItem` hosts visuals.

Proposed:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- `SelectedIndex` remains.
- The control owns the item chrome (marker, hover, selection highlight); template only produces content.

Migration strategy:

- Keep `OptionList` (non-generic) as a convenience wrapper: `OptionList : OptionList<Visual>` with identity template.

#### `SelectionList` -> `SelectionList<T>`

Current:

- `Items : VisualList<SelectionListItem>` (content-based).

Proposed:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- Add an explicit selection model:
  - `SelectedIndex` + `Checked` state (e.g. `BindableList<bool>` or `ISelectionModel`)

Migration strategy:

- Keep `SelectionList` as `SelectionList<Visual>` wrapper.

#### `ListBox` -> `ListBox<T>`

Current:

- `Items : VisualList<Visual>`

Proposed:

- `Items : BindableList<T>`
- `ItemTemplate : DataTemplate<T>`
- Provide wrappers for old usage where `T == Visual`.

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
