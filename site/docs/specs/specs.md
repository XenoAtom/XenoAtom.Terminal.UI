---
title: XenoAtom.Terminal.UI Specifications
---

# XenoAtom.Terminal.UI Specifications

This document contains specifications for the `XenoAtom.Terminal.UI` project.

Status: Draft (v0.2 - first round)

All names used in this document are preliminary and subject to change.

## Resources

The following libraries and resources are relevant to help specify this project:

- `XenoAtom.Ansi` library: `C:\code\XenoAtom\XenoAtom.Ansi`
- `XenoAtom.Terminal` library: `C:\code\XenoAtom\XenoAtom.Terminal`
  - Input events, cursor/window operations, markup/styling, scopes, clipboard, deterministic backends.
- `XenoAtom.Collections` library: `C:\code\XenoAtom\XenoAtom.Collections`
  - Internal collections (`UnsafeDictionary`, `UnsafeList`) for hot paths.

The NuGet packages of these libraries will be used for the `XenoAtom.Terminal.UI` project.

For inspiration:

- `XenoUI` prototype library: `C:\code\StackRift\XenoUI`
  - Component model inspiration (but terminal UI will not copy XenoUI one-to-one).

## Vision

`XenoAtom.Terminal.UI` aims to be the best .NET terminal UI framework by combining:

- A retained-mode component tree (predictable, debuggable).
- A modern dependency tracking/binding model (targeted invalidation, minimal recomputation).
- First-class support for both:
  - Fullscreen TUIs (alternate screen).
  - Inline interactive/document flow (live regions, progress, prompts, mixed output).
- Strong correctness guarantees (terminal restore, resize handling, Unicode width).
- High performance (minimal allocations, optimized redraw).

### Goals

- Enable building complex terminal applications with rich layout, styling, and interactivity.
- Provide an ergonomic API that feels lightweight to use.
- Provide built-in controls sufficient for real apps (not just demos).
- Provide deterministic testing capabilities (render-to-virtual-terminal + input injection).

### Non-goals (for MVP)

- No XAML.
- No global “diff engine” over the full UI tree.
  - The UI is retained; selective recomputation is enabled via bindings and computed subtrees.
- No full accessibility story in MVP (but keep space for it).
- No full-blown animation system in MVP (but keep a simple timing/tick model).
- No multi-window terminal compositor; keep one active app session at a time.

### Guiding principles

- Correctness first, then performance.
- Avoid hidden global state (except the terminal instance if using `Terminal.Instance`).
- Avoid unnecessary allocations; prefer `Span<T>` / `ReadOnlySpan<T>` and pooling.
- Always restore terminal state on exit/crash (best effort).
- Clearly separate:
  - “Document flow” output (append-only, not interactive).
  - “Live/interactive” regions (updated in place).

## Terminology

- **Cell**: one terminal column in a row.
- **Viewport**: the terminal window size (`TerminalSize`).
- **Frame**: the visual state rendered for a viewport/region (including cursor state).
- **Region**: a rectangular area in the viewport (used by inline live rendering).
- **Flow**: append-only document output that scrolls (tables, paragraphs, logs).
- **Live region**: an area kept at the bottom of current output and updated in place (progress bars, interactive widgets).

## Architecture overview

At runtime a `TerminalApp` orchestrates:

1) Read input events from `XenoAtom.Terminal` (`TerminalEvent` stream)
2) Route events to visuals / commands
3) Apply state updates (bindable properties)
4) Invalidate only what is necessary (build/layout/render)
5) Render and flush via a host

The key implementation pillars are:

- A single-threaded UI runtime (`Dispatcher`)
- A retained visual tree (`Visual`)
- A binding/dependency tracker (`BindingManager`)
- A layout protocol (measure/arrange in terminal cells)
- Rendering targets:
  - Fullscreen rendering (viewport)
  - Inline rendering (document flow + live region)

## Runtime & Hosting

### `TerminalApp`

`TerminalApp` is the main entry point for running a terminal UI.

Responsibilities:

- Own the `Dispatcher` and enforce UI-thread affinity.
- Own the root `Visual` for the session.
- Own the host (`FullscreenHost` or `InlineInteractiveHost`).
- Run the event loop and schedule render passes.
- Restore terminal state on exit (best effort).
- Manage terminal input for the session (best effort):
  - Ensure input is running while the app is interactive.
  - If the app started the input loop, it stops it on exit; otherwise it leaves it running.

Output ownership rule:

- While a `TerminalApp` session is active, application code must not write directly to the terminal output stream.
  - Doing so can corrupt cursor-relative rendering and is considered undefined behavior.
  - Output must be produced via the app model (document flow blocks, or live UI).

### Hosts

A host is responsible for:

- Setting/maintaining terminal modes (scopes).
- Providing a render surface (viewport or region).
- Flushing rendered output to `XenoAtom.Terminal` efficiently.

Two hosts are required:

- `FullscreenHost`: full viewport ownership (alternate screen).
- `InlineInteractiveHost`: document flow + live region ownership (inline, interactive).

The same `Visual` system is used for both hosts.

### Fullscreen host (alternate screen)

Behavior:

- Uses terminal scopes (best effort):
  - `UseAlternateScreen()`
  - `UseRawMode(TerminalRawModeKind.CBreak)`
  - `HideCursor()`
  - `EnableMouseInput(...)` when supported
  - `EnableBracketedPasteInput()` when supported
- Owns the whole viewport; draws from `(0,0)` to `(Columns-1, Rows-1)`.
- On `TerminalResizeEvent`: re-measure, re-arrange, repaint.

Flush strategy:

- MVP: repaint whole viewport each frame (acceptable in alternate screen).
- Later: maintain a cell buffer and compute diffs for minimal ANSI updates.

### InlineInteractiveHost (document flow + live region)

`InlineInteractiveHost` is used for:

- Rendering “document flow” output (non-interactive) such as paragraphs/tables that scroll.
- Rendering and updating a “live region” (interactive or live-updating widgets) at the end of output.

#### Document model

The inline host manages two conceptual areas:

- **Flow area (append-only)**
  - Content is appended over time (like a terminal document).
  - Once rendered, it is not re-rendered (it becomes part of terminal scrollback).
  - Flow elements can be any renderable UI (including markup text).

- **Live region (updated in place)**
  - Always the last output of the session.
  - May be interactive (focus, keyboard, mouse).
  - Re-rendered in place when invalidated.
  - Height is dynamic (can grow or shrink).

#### Rendering flow elements (append)

The host must support appending arbitrary non-interactive UI blocks while a live region exists.

Conceptual API (names TBD):

- `TerminalApp.Append(Visual block)` (or `Append(IRenderable block)`)
- `TerminalApp.AppendMarkup(string markup)` convenience

Semantics:

- Appending a flow block writes its rendered lines above the live region.
- Implementation can be:
  1) Clear the currently rendered live region lines
  2) Move cursor to the live region origin
  3) Render the block (layout at current viewport width) and write it (scrolling as needed)
  4) Re-reserve live region height and re-render the live region

This is intentionally not limited to “logging” or “markup”; any UI block that renders to lines is valid.

#### Live region sizing and reservation

The host may reserve vertical space even when started near the bottom of the terminal:

- If the live region needs `N` lines and fewer than `N` lines remain below the current cursor position,
  the host may emit newlines to scroll the terminal and reserve the required space.

Dynamic height:

- If the live region grows: reserve additional lines (scroll if needed).
- If the live region shrinks: clear trailing lines that are no longer used so stale content does not remain visible.

Exit behavior:

- On dispose/exit: do not clear the live region; leave the final frame and place the cursor immediately after the region.

Cursor behavior:

- Default: cursor hidden while the inline host is active.
- Cursor is shown only when a focused control requires it (e.g. `TextBox`) and is positioned at the caret.

#### Session input configuration (interactive)

For interactive inline sessions, the host/app should configure terminal input similarly to the `XenoAtom.Terminal` ReadLine editor:

- Ensure the input loop is running (`Terminal.StartInput(...)` when needed).
- Disable input echo while the session is active (`Terminal.SetInputEcho(false)` scope).
- Prefer receiving Ctrl+C as a regular key (best effort) so it can be handled as a command:
  - `TerminalInputOptions.TreatControlCAsInput = true`
- Enable optional input features (best effort, capability-gated):
  - `Terminal.EnableMouseInput(...)`
  - `Terminal.EnableBracketedPasteInput()`

#### Constraints

- While an inline session is active, the live region must remain the last output.
- Any output while the session is active must go through the app/host (append flow blocks or update live region).

## Dispatcher & threading

The UI runtime is single-threaded.

- A `Dispatcher` runs the UI loop and schedules work:
  - `Invoke`, `BeginInvoke`, `InvokeAsync`
  - `Run()`, `Exit()`
- A `DispatcherObject` base class provides `VerifyAccess()` / `CheckAccess()` semantics.
- All visuals and their bindable state are UI-thread affine.

## Visual tree & lifecycle

### `Visual`

`Visual` is the base type for all UI components.

Key requirements:

- Allocation-free child access:
  - `protected int ChildrenCount { get; }`
  - `protected Visual GetChild(int index)`
- State/flags (bitset): attached, enabled, focused, needs-measure, needs-render, etc.
- Lifecycle hooks (names TBD): attach/detach/initialize/dispose.
- Hit-testing support (for pointer events) in cell coordinates.

### Layout participants

A `LayoutVisual` (or similar) participates in measure/arrange.

Layout protocol:

- `Measure(availableSize)` returns desired size (cells).
- `Arrange(finalRect)` commits bounds and prepares for rendering.

The protocol should be closer to SwiftUI in spirit (simple, composable), but implementable in a retained tree.

## Composition & computed subtrees (retained + recomputable)

We want:

- Retained visuals (no full-tree diff engine).
- Ability to recompute subtrees via functions (tracked by bindings).

### `Computed<T>` (renamed from `FuncObservable<T>`)

`Computed<T>` represents a value produced by a function (or a constant) that:

- Is evaluated under dependency tracking.
- Caches its value until dependencies change.
- Notifies observers when its value changes.

Usage examples:

- Text:
  - `new TextBlock(() => $"Count: {model.Count}")`
- Conditional content:
  - `new Button(() => model.IsComplex ? new TextBlock("Complex") : new TextBlock("Simple"))`

### Computed child slots

To avoid general diffing, a computed child is managed through a **slot**:

- A slot owns 0..1 child `Visual`.
- When the computed function changes, the slot replaces the child:
  - detach + dispose old
  - attach new
  - invalidate layout/render for the affected subtree

Dynamic lists are handled via dedicated controls (`ItemsControl`/`ListBox`) rather than arbitrary diffing of children lists.

## Binding model

The binding model tracks dependencies automatically:

- Reads are tracked (dependency registration).
- Writes notify dependents (targeted invalidation).

### Bindable properties (source generated)

User code:

```csharp
[Bindable]
public partial bool IsActive { get; set; }
```

The source generator generates a property wrapper that routes reads/writes through a `BindingManager`.

Generated shape (simplified):

```csharp
partial class MyComponent : MyComponent.IBindings
{
    private bool _isActive;

    public bool IsActive
    {
        get => BindingManager.Current.GetValue(ref _isActive, __IsActive__Accessor.Instance);
        set => BindingManager.Current.SetValue(ref _isActive, value, __IsActive__Accessor.Instance);
    }

    // Access to bindings is provided via a C# extension member (this.Bind).

    Binding<bool> IBindings.IsActive => new Binding<bool>(this, __IsActive__Accessor.Instance);

    public interface IBindings : BindableObject.IBindings
    {
        Binding<bool> IsActive { get; }
    }

    private sealed class __IsActive__Accessor : BindingAccessor<bool>
    {
        public static __IsActive__Accessor Instance { get; } = new(nameof(IsActive), StaticGetter, StaticSetter);

        private static bool StaticGetter(object instance) => ((MyComponent)instance).IsActive;
        private static void StaticSetter(object instance, bool value) => ((MyComponent)instance).IsActive = value;
    }
}
```

Notes:

- `Binding<T>` is a lightweight struct used to reference a bindable property without allocations.
- The generated `IBindings` interface exposes binding handles (e.g. `this.Bind.IsActive`) without reflection.
- `BindingAccessor<T>` is a generated, cached descriptor for the property (name + getter/setter delegates).
- Bindable objects are UI-thread affine:
  - They inherit from a `BindableObject` base (name TBD) which is a `DispatcherObject`, or they implement `IDispatcherObject`.

#### `BindingManager`

`BindingManager` is attached to a `Dispatcher` and is accessed via `BindingManager.Current` (thread-static / dispatcher-local).

Responsibilities:

- Track which bindings were read during an evaluation (dependency set).
- Notify dependents on writes.
- Enforce write restrictions based on evaluation context (e.g. disallow writes during render).

Tracking is scoped:

- `BeginTracking(context)` starts capturing reads.
- `EndTracking()` ends capturing and returns the collected dependency set.

At runtime:

- `get` registers a dependency for the current evaluation context
- `set` notifies dependents

### Evaluation contexts

Dependencies must be tracked per context:

- Build (computed subtree evaluation)
- Measure
- Arrange
- Render
- Input handling (optional tracking for command enablement, etc.)

Invalidation rules:

- If a property was read during Measure, a change invalidates Measure for that visual (and may require bubbling).
- If a property was read during Render, a change invalidates Render for that visual.
- Writes during Render should be disallowed (configurable strictness).

## Environment values

Environment values (SwiftUI-like) allow theming/styling and other contextual values:

- Values are stored in an `Environment` associated with visuals.
- Inheritance: a visual inherits parent environment; overrides are sparse.
- Environment key reads are tracked like bindable reads.

Storage requirements:

- Efficient for small value types (<= 8 bytes) and references.
- Minimal allocations for common access paths.

## Events, commands, focus

### Raw terminal events (from `XenoAtom.Terminal`)

The app consumes terminal events:

- `TerminalKeyEvent`
- `TerminalTextEvent`
- `TerminalPasteEvent` (bracketed paste)
- `TerminalMouseEvent`
- `TerminalResizeEvent`
- `TerminalSignalEvent`

These events are considered **raw** and are not delivered directly to components.
They are translated by the runtime/host into higher-level **routed UI events**.

### Routed events

The UI uses routed events for delivering input and control events through the visual tree.

Routing strategies:

- Preview (tunnel): from root to target.
- Bubble: from target to root.
- Direct: only the target.

Event args:

- Base type: `RoutedEventArgs` (name TBD)
  - `OriginalSource` (the initial target)
  - `Source` (current handler target)
  - `Handled` flag (when set, stops routing depending on strategy and handler options)

Event registration and handlers:

- `RoutedEvent<TEventArgs>` (name TBD) represents an event identifier and contains metadata (name, routing strategy).
- `Visual` exposes:
  - `AddHandler(RoutedEvent<T>, handler, handledEventsToo: bool = false)`
  - `RemoveHandler(...)`
- Controls can raise routed events (e.g. `Button.Click`).

Routed events should be definable via source generator attributes to reduce boilerplate.

Example user code:

```csharp
[RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
protected virtual void OnPointerPressed(PointerPressedEventArgs e)
{
}
```

Generates code similar to:

```csharp
public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
    RoutedEvent.Register<MyVisual, PointerPressedEventArgs>(
        nameof(PointerPressed),
        static (sender, args) => (sender as MyVisual)?.OnPointerPressed(args),
        RoutingStrategy.Preview | RoutingStrategy.Bubble);

public event EventHandler<PointerPressedEventArgs> PointerPressed
{
    add => AddHandler(PointerPressedEvent, value);
    remove => RemoveHandler(PointerPressedEvent, value);
}
```

### Input translation pipeline

The runtime converts raw terminal events into UI routed events:

- `TerminalMouseEvent` -> pointer events (targeted by hit testing):
  - `PointerMoved`, `PointerPressed`, `PointerReleased`, `PointerWheelChanged` (names TBD)
  - In inline live regions, mouse coordinates are translated from global terminal coordinates into local coordinates relative to the live region origin.
- `TerminalKeyEvent` -> key events (targeted at the focused element):
  - `KeyDown` / `KeyPressed` (name TBD)
  - Also participates in command routing (global + scoped).
- `TerminalTextEvent` -> `TextInput` (targeted at focused element)
- `TerminalPasteEvent` -> `Paste` (targeted at focused element)
- `TerminalResizeEvent` -> runtime invalidation (measure/arrange/re-render) and optional `ViewportChanged` notification.
- `TerminalSignalEvent` -> by default mapped to a command (e.g. cancel/exit), configurable.

### Event synthesis / gestures

Controls may synthesize higher-level events from lower-level routed events:

- `Button`:
  - Tracks `PointerPressed` + `PointerReleased` to produce a `Click` routed event.
  - Captures the pointer while pressed (so release is received even if the pointer moves).
  - Only raises `Click` if the release occurs within bounds (policy TBD).

### Focus model

Requirements:

- Focusable elements (`IsTabStop`, `TabIndex`, etc.)
- Focus traversal (Tab/Shift+Tab)
- Mouse press focuses the target (when supported)
- Pointer capture (for drag)

### Commands / key bindings

We need a command system that can be used for:

- Global commands (quit, help, debug overlay)
- Control-local commands (button activate, list selection, textbox editing)

Key bindings should be based on `KeyGesture` and support scoping.

## Text model (Unicode, width, wrapping)

All layout/rendering is cell-based.

Initial (MVP) requirements:

 - Use `TerminalTextUtility` for width (`GetWidth`) and cell-to-index mapping (`TryGetIndexAtCell`).
- Treat text as UTF-16 input but operate in rune boundaries for navigation.

Known limitations (to refine later):

- Grapheme cluster handling (ZWJ emoji sequences, complex combining sequences) is not fully specified for MVP.
- Terminals disagree on width for some sequences; we should follow `TerminalTextUtility` for consistency in tests.

## Rendering

Rendering produces a set of styled characters in a grid (cells), and a cursor state.

We should support two render targets:

- Fullscreen target: viewport-sized.
- Inline live target: region-sized.

Flush strategies:

- MVP inline: full region repaint using cursor positioning and per-line erase.
- MVP fullscreen: full repaint.
- Later: cell-buffer diff + minimal ANSI update.

## Clipboard

Clipboard access uses `XenoAtom.Terminal`:

- `TerminalInstance.Clipboard` / `Terminal.Clipboard`
- Best effort by default (no-op if unsupported unless strict mode is enabled at the terminal/app level).

## Text editing (shared engine)

We should define a reusable line editor engine usable by:

- `TextBox` (single-line, MVP)
- Future multi-line editor
- Potentially `XenoAtom.Terminal` ReadLine editor (shared/backported to avoid duplication)

Core features (MVP subset is acceptable):

- UTF-16 buffer + rune-aware cursor movement
- Selection (shift+arrows)
- Copy/cut/paste via `TerminalClipboard`
- Optional undo/redo later (but keep engine extensible)

## Controls

### MVP control set (v0.1)

The first MVP should enable a compelling demo:

- Layout: `VStack`, `HStack`, `Border`
- Text: `TextBlock`
- Input: `Button`, `CheckBox`, `TextBox` (single-line)
- Lists: `ListBox` (selection + scrolling)
- Scrolling: `ScrollViewer` (if not embedded into `ListBox`)
- Live: `ProgressBar` and/or `Spinner` (common for inline live regions)

### Later controls (post-MVP)

- RadioButton, ComboBox
- TreeView
- TabControl
- Menus (global + context)
- Tables/DataGrid
- Dialogs/Windowing for fullscreen apps
- StatusBar

Control states to support:

- Enabled/Disabled (inheritable)
- Focused
- Hover (where mouse is supported)
- Pressed
- Selected

## Styling & theming

The styling system should be environment-driven:

- Theme values are environment keys (colors, decorations, spacing).
- Styles can be state-based (focused/disabled/selected).
- Changing theme/environment triggers targeted re-render based on binding tracking.

## Diagnostics & testing

Deterministic tests are a core requirement:

- Run apps on `VirtualTerminalBackend` / `InMemoryTerminalBackend`.
- Inject input via `PushEvent`.
- Snapshot rendered output from render targets (stable format).

Diagnostics:

- Optional debug overlay (toggle key) showing focus, bounds, dirty flags, frame time.
- Optional tracing of invalidations (build/layout/render) for performance investigation.

## Source generator

We will provide an incremental Roslyn Source Generator for:

- `[Bindable]` properties
- Routed events (generator support optional for MVP; routed event system is required)
- Potential analyzers to prevent misuse (e.g. writing bindables during render)

MVP note:

- MVP can be implemented with minimal generator features (only `[Bindable]`) if necessary.

## Roadmap

### v0.1 (MVP)

- `TerminalApp` runtime + inline host with:
  - Document flow append of UI blocks (including markup)
  - Live region with multi-line layout and interactivity
- Basic controls listed in MVP set
- Tests on `VirtualTerminalBackend`

### v0.2+

- Fullscreen host (alternate screen) with improved rendering strategy
- More controls (menus, dialogs, tables)
- Animation/timing model
- Undo/redo integration

## Open questions (to resolve during iteration)

- How to represent a “flow block” API surface: app-level append API vs a dedicated `Document` visual.
- How to best model tables so they work both as flow blocks and in live regions.
- How to expose terminal strictness (cursor/clipboard failures) at the UI layer.
