# Binding & State

XenoAtom.Terminal.UI uses a binding model designed for terminal UIs:

- property access is tracked during dynamic updates, layout, and rendering
- when state changes, only the affected visuals are invalidated

## State

`State<T>` is a small observable container used to drive UI:

```csharp
var name = new State<string>("Alex");

var ui = new VStack(
    new TextBlock(() => $"Hello {name.Value}"),
    new TextBox().Text(name)
);
```

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

See also:

- `doc/text-editing.md`

