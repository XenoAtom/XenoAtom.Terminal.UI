# Specifications for ControlsDemo ("MegaDemo")

This document specifies a “ControlsDemo” app showcasing XenoAtom.Terminal.UI controls and composition patterns. It should feel like a polished, modern application and be the primary place to:

- Discover controls and styles quickly.
- Copy/paste practical usage patterns (fluent API + state + events).
- Validate interaction correctness (keyboard, mouse, focus, popups, scrolling, resizing).
- Visually verify theming/styling and ensure regressions are obvious.

The demo must be **fullscreen-first**, with a strong keyboard workflow, and should work well on resize.

## Goals

- **Best-in-class discoverability**: search, categories, tags, keyboard navigation.
- **Strong ergonomics**: every demo is interactive and shows typical usage; minimal boilerplate for adding a demo.
- **Integrated documentation**: a demo should link to its source and (when relevant) to docs/specs.
- **Diagnostics-friendly**: show event logs, focus path, hovered element, popup stack, and basic render stats.
- **Repeatable**: easy to run locally, in CI, and as a global tool; deterministic demos (no random unless seeded).

## Unique strengths to highlight

XenoAtom.Terminal.UI is not just a collection of controls: it is a **bindable, composable UI system** that enables “live UI” without a diff engine and without asking the user to manually refresh.

The demo should explicitly showcase these “killer” features across the app (not hidden in a single page):

- **Everything is bindable**: bindable properties (and routed events) allow concise wiring with the fluent API.
- **`State<T>` for live UI**: `State<T>` is a first-class binding source; changing state drives UI updates smoothly.
- **Inline and fullscreen hosting**: the same visuals can be used for `Terminal.Write(...)` (flow output), `Terminal.Live(...)` (inline live region), and `Terminal.Run(...)` (fullscreen app).
- **Dynamic composition via state**: build “functional” composite UIs by switching subtrees based on state (e.g. `ContentSwitcher.SelectedIndex` bound to `State<int>`).
- **Modern input/event stack**: mouse, keyboard gestures, focus, popups, routed event preview/bubble, mouse capture.

The demo should provide short, copyable “recipes” demonstrating these patterns.

### Must-have binding recipes

1. **Live updating widget with `State<T>`**

```csharp
var progress = new State<double>(0);

Terminal.Live(
    new ProgressBar().Value(progress),
    () =>
    {
        progress.Value = (progress.Value + 0.01) % 1.0;
        return true;
    });
```

2. **Dynamic subtree switching (functional composition)**

```csharp
var view = new State<int>(0);

new ContentSwitcher()
    .SelectedIndex(view)
    .Add(
        new TextBlock("View A"),
        new VStack().Add(new TextBlock("View B"), new Spinner()),
        new Table() /* ... */);
```

## Non-goals (for v1)

- No external dependencies beyond what the repo already uses.
- No recording/export pipeline beyond simple "copy markup"/"copy screenshot as text" primitives.
- No full-blown "property grid" reflection editor (can be added later as a bonus).

## App shell & navigation

### Layout

- **Header**: app title, current demo name, search box (or “Ctrl+P” hint), theme selector, and optional small status icons.
- **Body**: resizable SplitView (`HSplitter`):
  - **Left**: navigation sidebar.
  - **Right**: demo content area.
- **Footer**: key hints and status (focused control name, mouse coordinates when available, errors).

The app should keep a clean, modern look: avoid heavy background fills; reserve backgrounds for surfaces/inputs.

### Sidebar

Must support:

- Categories with collapse/expand (Accordion-like).
- Search/filter (by name, tags, category).
- Type-to-jump in the list.
- Keyboard navigation: Up/Down, PageUp/Down, Home/End.
- Single selection.

Implementation options:

- Use a `TextBox` (filter) + `OptionList` (results) grouped by category.
- Alternatively use a `TreeView` where categories are nodes.

### Quick open / command palette

- `Ctrl+P` opens a command palette (reuse `CommandPalette`) listing demos and actions.
- Typing filters by name/tags.
- Enter opens the demo.
- Also include global commands (theme toggle, open docs panel, show debug overlay, quit).

## Demo page structure (right pane)

Every demo should follow a consistent layout to make learning easy:

1. **Title + short description** (what it demonstrates, what to try).
2. **Showcase area**: the actual control(s) in a realistic mini-layout, not isolated in a vacuum.
3. **Controls panel** (optional but recommended): toggles/sliders/inputs to change common options (style variants, alignment, text trimming, etc.).
4. **Events log**: a small scrollable log showing routed events and state changes relevant to the demo.
5. **Source link**: a `Link` to the exact file on GitHub, plus “copy minimal snippet” action (when feasible).

The “controls panel” and “events log” should be easy to hide/show (e.g. tabs or collapsible groups).

## Demo authoring model (minimize boilerplate)

### Demo discovery

The registry should not require manual edits for every new demo.

Preferred approach:

- Introduce a `DemoAttribute` on demo classes with metadata: `Name`, `Category`, `Tags`, `Order`, `Description`.
- Use a **source generator** (in this repo) to produce a registry of demos at compile time (no reflection at runtime).

Fallback (acceptable for v1 if needed):

- Reflection scan of the demo assembly on startup to build the list (but keep it isolated so it can be replaced by a generator later).

### Demo contract

Each demo is its own file (e.g. `ButtonDemo.cs`) and implements a small interface:

```csharp
public interface IControlsDemo
{
    DemoMetadata Metadata { get; }
    Visual Build(DemoContext context);
}

public sealed class DemoContext
{
    public required TerminalApp App { get; init; }
    public required Action<string> Log { get; init; }
    public required Action<Visual> NavigateTo { get; init; } // or NavigateToDemo(string id)
}
```

Notes:

- Demos should use `State<T>` and bindables rather than manual `RequestRender` calls.
- “Dynamic updates” should be used only when needed (bindings/lambdas/state), not for constant static setup.
- Demos must be safe to rebuild multiple times when switching between demos (no leaked windows/popups).

### Shared “demo chrome”

Provide helper code to apply the standard structure (title, description, log, source link) so individual demos mostly focus on “showcase area”.

## Content: what to demonstrate

The demo should include every control currently in the library, but also include "patterns" pages:

- **State & bindings**: `State<T>` usage, lambda bindings, styling via `.Style(...)`, and what gets invalidated (measure/arrange/render).
- **Functional composition**: `ContentSwitcher` (and other patterns) to swap subtrees based on state; show how to avoid dynamic updates for static setup.
- **Live UI**: `Terminal.Live(...)` (inline) + “tick-driven” animations (spinner/progress) without input; show logging while live.
- **Layouts**: StackPanels, Grid, Splitters, alignment, trimming, margins/padding.
- **Input**: TextBox, MaskedInput, TextArea, Slider, Select, Switch, CheckBox.
- **Navigation**: Tabs, MenuBar, TreeView, OptionList, CommandPalette.
- **Overlays**: Popup, Dialog/Window, modal behavior, window movement.
- **Visualization**: Progress variants, Spinner styles, Sparkline, charts.
- **Content**: Markup, Links (mouse + OSC8), Table.
- **Advanced**: mouse capture, routed events preview/bubble, keyboard gestures.

Each demo should include at least one “typical layout” example and a “stress case” example (small sizes, long text, scrolling, resize).

## Theming & styles

- Provide a theme selector (at least: dark default + a lighter variant).
- Provide per-control style overrides to show how `.Style(...)` affects visuals.
- Demonstrate hover/pressed/disabled/focused states for interactive controls.
- Provide a “glyph preview” page showing glyph sets used by the theme (lines, scrollbars, etc.).

## Diagnostics & developer UX

Add an optional “Debug overlay” (toggleable via key binding, e.g. `F12`) showing:

- Focused element name/type and bounds.
- Hovered element name/type and bounds.
- Popup/window stack info (which popup is open, placement, bounds).
- Render stats (frame count, last frame duration, dirty reasons if available).

Add a “Log” panel that can capture:

- Routed events (click, key down, mouse move, wheel).
- Property changes (selected index, toggles, text changes).
- Errors/exceptions from demo code (caught and shown in the log UI).

## Input and key bindings (global)

- `Ctrl+P`: command palette / quick open.
- `Tab` / `Shift+Tab`: focus navigation (existing).
- `Esc`: close popups/dialogs first; if nothing open, show “quit?” dialog (optional).
- `F12`: debug overlay.
- `Ctrl+T`: theme toggle (optional).

## Packaging & naming

Project location: `samples/ControlsDemo`

Name suggestions (pick one):

- `XenoAtom.Terminal.UI.ControlsDemo`

Global tool packaging (later step):

- Publish as a `dotnet tool` that runs the fullscreen demo by default.
- Provide flags to start directly on a demo page (e.g. `--demo TextArea`).

## Quality bar / acceptance criteria

- Runs fullscreen without flicker and handles resize reliably.
- Every demo is keyboard-usable and mouse-usable.
- Every demo has at least one user-adjustable option.
- Every demo provides a link to source file.
- Switching demos does not leak UI state (popups closed, focus restored, logs scoped to demo).

## Future ideas (optional extensions)

- A “copy snippet” action per demo (minimal code sample generated from metadata).
- A lightweight “property editor” for common primitives (bool, int/double, enum, string) to reduce per-demo UI wiring.
- A “gallery” mode: tile view of controls for quick visual scan.
