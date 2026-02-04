---
title: "Remaining Controls & Enhancements (v1)"
---

# Remaining Controls & Enhancements (v1)

This document describes the remaining controls and enhancements planned for the “first complete release” of the library.
It is written to be implementation-oriented: the intent is that an implementer can follow this document and add the
controls with minimal design guesswork.

> [!WARNING]
> This document is kept for historical context. Some details may be outdated compared to the current implementation.

This spec assumes the current architecture:

- Retained-mode `Visual` tree with binding dependency tracking (measure/arrange/render dependencies).
- Style resolution via `Visual.GetStyle<T>()` / `Visual.Style(...)` and record-based style types (`IStyle<T>`).
- Dynamic collections via `BindableList<T>` and `VisualList<T>` (with dependency tracking).
- Delegate bindables use `Delegator<T>` to keep fluent extension methods usable.
- Overlays use `TerminalApp.ShowWindow(...)` with modal scoping via `IModalVisual`.
- `Terminal.Live(...)` and `Terminal.Run(...)` provide hosting scenarios (inline vs fullscreen).

---

## TreeView Enhancements

### Spacing fix for emoji / grapheme clusters

- Change `TreeViewStyle.SpaceBetweenGlyphAndText` default from `2` → `1`.
  - Rationale: grapheme cluster width is now handled correctly across the stack; the previous “2” was compensating for
    emoji width bugs.
  - This should align the TreeView with other list-like controls (ListBox, OptionList, SelectionList) that use “one gap”
    by default.

### Hierarchy guide lines (default ON)

TreeView should be able to render classic tree “guide lines” (vertical continuation + branch joints), configurable
through style.

#### Proposed API / style surface

Add to `TreeViewStyle`:

- `LineGlyphs? HierarchyLines { get; init; }`
  - `null` means “no hierarchy lines”.
  - Default should be `LineGlyphs.Single` (or `theme.Lines`, but style defaults should not depend on theme; prefer
    `LineGlyphs.Single` and allow theme overrides via style environment).
- `Style? HierarchyLineStyle { get; init; }`
  - Optional style override used when drawing the hierarchy lines (foreground only, typically derived from theme border
    or muted text color).
  - If null, use `theme.BorderStyle(focused: false)` or `theme.MutedTextStyle()` (implementation choice; keep consistent
    with the library’s existing borders).

Add convenience variants:

- `TreeViewStyle.NoLines` (`HierarchyLines = null`)
- `TreeViewStyle.HeavyLines` (`HierarchyLines = LineGlyphs.Heavy`)
- `TreeViewStyle.DoubleLines` (`HierarchyLines = LineGlyphs.Double`)

#### Rendering model (deterministic, low allocation)

TreeView already maintains a flattened visible list. Extend the flattened list to also store enough information to draw
lines without scanning siblings at render time.

Recommended representation for each visible row:

- `Depth : int`
- `ContinuationMask : ulong` (bit i = 1 means “draw a vertical line in indent column at depth i”)
- `IsLastSibling : bool` (for choosing `BottomLeft` vs `TeeLeft`)

Compute these during the DFS used to build the visible list:

- Maintain a stack of “hasNextSibling” flags for each depth while traversing.
- For a node at `depth`:
  - `ContinuationMask` is built from the stack (levels where ancestors have more siblings).
  - `IsLastSibling` comes from “hasNextSibling at this depth”.

Drawing rules (assuming `IndentSize = 2`):

- For each ancestor depth level `< depth`:
  - At `x = indentStart + level * IndentSize`, draw `lines.Vertical` if `ContinuationMask` has bit `level`, else space.
  - At `x+1`, draw space (keeps the layout readable).
- At the current depth `depth`:
  - At `x = indentStart + depth * IndentSize`, draw `lines.BottomLeft` if `IsLastSibling` else `lines.TeeLeft`.
  - At `x+1`, draw `lines.Horizontal`.

This produces the familiar output (example):

```
├─ Folder
│  ├─ File
│  └─ File
└─ Folder
```

#### Layout impact

The hierarchy lines consume the same horizontal space as indentation, so the public layout contract does not change:

- `IndentSize` remains the single knob controlling the indentation width.
- If `IndentSize < 2` and lines are enabled, TreeView should clamp the “line indent width” to `2` internally.

#### Tests and demo updates

- Add rendering tests for:
  - No lines (null)
  - Single lines
  - Mixed continuation masks across multiple depths
- Update ControlsDemo TreeView demo to show the line styles and the `NoLines` variant.

---

## Tooltip Control

Tooltips should be usable on any visual without attached properties (which the framework intentionally avoids).

### Public API

#### `TooltipHost`

Introduce a wrapper control that owns a tooltip for a single child:

```csharp
public sealed class TooltipHost : ContentVisual
{
    [Bindable] public partial Visual? TooltipContent { get; set; }
    [Bindable] public partial int ShowDelayMilliseconds { get; set; } = 500;
    [Bindable] public partial PopupPlacement Placement { get; set; } = PopupPlacement.Below;
    [Bindable] public partial int OffsetX { get; set; }
    [Bindable] public partial int OffsetY { get; set; } = 1;
}
```

Notes:

- Tooltip should be **non-modal** (must not steal focus, must not constrain routing scope).
- Tooltip overlay should not interfere with hit testing:
  - Recommended: the tooltip visual should be `IsEnabled = false` so `TerminalApp.DispatchMouseEvent` will skip it and
    deliver input to the underlying element.

#### Fluent syntax

Add a `VisualExtensions.Tooltip(...)` convenience:

```csharp
new Button("Delete").Tooltip(new Markup("[red]Permanently deletes the item[/]"));
// and/or
new Button("Delete").TooltipMarkup("[red]Permanently deletes the item[/]");
```

This should return a `TooltipHost` wrapping the target visual.

### Rendering and hosting

Tooltips should reuse the overlay infrastructure (window layer), but must not behave like popups/dialogs:

- Tooltip overlay can be implemented as an internal `TooltipPopup` visual that:
  - Computes its rectangle like `Popup` (using the same placement rules).
  - Renders a filled surface (using theme `PopupSurface` or dedicated `TooltipStyle`).
  - Does not handle input and is disabled (`IsEnabled = false`).

### Show/hide behavior (v1 required)

- Show after `ShowDelay` when `Content` is hovered (`Visual.IsHovered` becomes true).
- Hide when hover leaves, or when a modal window becomes active, or when the anchor leaves the UI tree.
- Do not show tooltip if `TooltipContent` is null.
- Ensure only one tooltip visible per app at a time (global service in `TerminalApp` or a static manager tied to the app).

### Style

Introduce `TooltipStyle`:

- Surface style (foreground + background)
- Border style and line glyphs (reuse `BorderStyle` patterns)
- Padding
- Optional max width for wrapping (tooltip should wrap by default)

### Tests and demos

- Add tests verifying:
  - Tooltip appears after delay and closes on leave.
  - Tooltip does not steal focus and does not intercept clicks.
- Add ControlsDemo usage (e.g. tooltips in Button demo and in the BreakdownChart control when implemented).

---

## BreakdownChart Control (Segmented Proportional Bar)

Goal: show proportional parts of a whole (resource usage, KPI breakdown, category proportions), with interactivity.

### Public API

```csharp
public sealed class BreakdownChart : Visual
{
    [Bindable] public BindableList<BreakdownSegment> Segments { get; }

    [Bindable] public partial Visual? Title { get; set; }
    [Bindable] public partial BreakdownLegendPlacement LegendPlacement { get; set; } = BreakdownLegendPlacement.Below;
    [Bindable] public partial bool ShowPercentages { get; set; } = true;
    [Bindable] public partial bool ShowValues { get; set; } = false;

    // Interaction
    [RoutedEvent(RoutingStrategy.Bubble)] private void OnSegmentClicked(BreakdownSegmentClickedEventArgs e) { }
}

public sealed class BreakdownSegment : IVisualElement
{
    [Bindable] public partial double Value { get; set; }
    [Bindable] public partial Visual? Label { get; set; }
    [Bindable] public partial Color? Color { get; set; }
    [Bindable] public partial Visual? Tooltip { get; set; }
}
```

Notes:

- Use `IVisualElement` on the segment to avoid premature binding notifications while it is not attached (same pattern as
  `TreeNode` / `ProgressTask`).
- `Label` and `Tooltip` should be visuals (avoid string-only APIs; allow composition/markup).

### Fluent construction helpers (v1 required)

In addition to the generator-provided fluent methods for bindable properties (e.g. `Title(...)`, `ShowValues(...)`) and
the list replacement methods for `Segments(...)`, provide a convenience helper that appends a segment in one call:

```csharp
public static partial class BreakdownChartExtensions
{
    public static BreakdownChart Segment(
        this BreakdownChart breakdown,
        double value,
        Visual? label = null,
        Color? color = null,
        Visual? tooltip = null)
    {
        ArgumentNullException.ThrowIfNull(breakdown);
        breakdown.Segments.Add(new BreakdownSegment
        {
            Value = value,
            Label = label,
            Color = color,
            Tooltip = tooltip,
        });
        return breakdown;
    }
}
```

### Style

Introduce `BreakdownStyle` (record):

- `Rune FillRune` (default `' '`)
- `int SegmentGap` (default 0 or 1)
- `BreakdownLegendLayout LegendLayout` (default: Compact)
- `int LegendItemSpacing` (spacing between items in Compact mode)
- `Style? BarStyle` (base style used for segments)
- `Style? LegendStyle` / `Style? LegendMutedStyle`
- `IReadOnlyList<Color?>? DefaultSegmentColors` (optional override; otherwise derived from scheme)
  - Defaults should cycle: `Primary/Success/Warning/Error` then scheme accent colors.

### Rendering and layout

This control should use custom rendering for the bar portion (fast and simple), and optionally compose the legend:

- Bar: compute segment widths deterministically:
  - total = sum(max(0, value))
  - for each segment, width = floor(value/total * availableWidth)
  - distribute remainder left-to-right to ensure sum(widths) == availableWidth
  - enforce min width for non-zero segments (optional; be careful not to exceed availableWidth)
- Use `OnPointerMoved` to track hovered segment index (for hover style/tooltip).
- Use `OnPointerPressed` to click segments and raise `SegmentClicked`.
- Legend can be built as a `Table` or a `VStack` of `HStack`s; keep it simple (segment counts are typically small).

### Tooltip integration

Each segment can optionally expose a tooltip visual:

- When hovered, show tooltip via the new tooltip system anchored to the bar and positioned near the hovered segment.
- Tooltip content should be derived automatically if `Tooltip` is null:
  - `Label` + formatted value + percentage (based on style flags)

### Tests and demos

- Tests for width allocation and deterministic remainder distribution.
- Tests for click hit-testing (segment index resolution).
- ControlsDemo page demonstrating:
  - different segment counts
  - click event
  - tooltip per segment

---

## BarChart (Enhanced, Horizontal)

The existing `BarChart` control is currently a useful but limited renderer (values-only). For v1, replace it entirely
with an enhanced chart that is immediately useful in real apps:

- Horizontal-only (v1).
- First-class labels and value display.
- Per-row coloring and styling.

This is a deliberate breaking change (the library is not released yet). If an internal “values-only” renderer remains
useful, keep it internal (e.g. for sparklines or other primitives), but do not expose it as the public `BarChart`.

### Public API

```csharp
public sealed class BarChart : Visual
{
    [Bindable] public BindableList<BarChartItem> Items { get; }

    [Bindable] public partial Visual? Title { get; set; }
    [Bindable] public partial ChartTitlePlacement TitlePlacement { get; set; } = ChartTitlePlacement.Above;

    // Optional bounds. If null, derive from Items (min = 0, max = max(value)).
    [Bindable] public partial double? Minimum { get; set; }
    [Bindable] public partial double? Maximum { get; set; }

    // Optional value display knobs.
    [Bindable] public partial bool ShowValues { get; set; } = true;
    [Bindable] public partial bool ShowPercentages { get; set; } = false;
}

public sealed class BarChartItem : IVisualElement
{
    [Bindable] public partial Visual? Label { get; set; }
    [Bindable] public partial double Value { get; set; }

    // Optional override. If null and ShowValues is true, compute from Value using culture-aware formatting.
    [Bindable] public partial Visual? ValueLabel { get; set; }

    // Optional per-item bar color. If null, use BarChartStyle.DefaultBarColors (cycle) or Theme tones.
    [Bindable] public partial Color? BarColor { get; set; }
}
```

Notes:

- `BarChartItem` should be `IVisualElement` to avoid premature binding notifications while it is not attached (same
  pattern as `TreeNode` / `ProgressTask`).
- `Label`/`ValueLabel` are visuals to allow composition and markup (avoid string-only APIs).

### Implementation approach (practical v1)

Avoid custom text measurement and custom multi-line rendering. Compose existing controls:

- Represent each item as a row in a `Grid`:
  - Column 0: `Label` (`Auto`)
  - Column 1: the bar (`Star(1)`)
  - Column 2: the value text (`Auto`) when `ShowValues` is true
- The bar is a tiny internal visual (or a reused `ProgressBar`) where the normalized value is:
  - `progress = (Value - min) / (max - min)` clamped to `[0..1]`
  - `min/max` resolved from bindables or derived from items
- When `ShowPercentages` is enabled, append the percentage to `ValueLabel` (or render as a separate column; keep v1
  simple and consistent).

### Style

Introduce `BarChartStyle` (record):

- `Thickness Padding`
- `int RowSpacing`
- `Style? LabelStyle`, `Style? ValueStyle`
- `ProgressBarStyle BarStyle` (or `Style? BarStyle` + glyphs if a dedicated bar renderer is used)
- `IReadOnlyList<Color?>? DefaultBarColors` (optional override; otherwise derived from theme tones/accent colors)

### Tests and demo updates

- Unit tests:
  - normalization with and without explicit min/max
  - derived max when Maximum is null
  - Measure/Arrange row counts in nested layouts
- ControlsDemo:
  - update BarChart demo to show the enhanced chart (different item counts, per-row colors, ShowValues/ShowPercentages)

---

## Validation Messages (Infrastructure)

Goal: provide a consistent, reusable validation/message surface that can be displayed **above or below** a control.
This is intended to replace one-off “error text under input” implementations (e.g. in `NumberBox`) and to be the shared
mechanism used by prompts, pickers, and any text input control.

### Requirements (v1)

- Works with any `Visual` (wrapper/decorator; no attached properties).
- Message content is a `Visual` (supports `Markup`, links, icons, etc.).
- Supports severity: `Info`, `Warning`, `Error` (theme-driven colors).
- Supports placement: above or below the control (default: below).
- Validation must be able to change dynamically (severity and content) based on bindings/state.
- Must not steal focus and must not intercept pointer events intended for the wrapped control.
- Must be easy to use from controls and user code (minimal boilerplate).

### Proposed API

```csharp
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
}

public enum ValidationPlacement
{
    Above,
    Below,
}

public readonly record struct ValidationMessage(ValidationSeverity Severity, Visual Content);

public sealed class ValidationPresenter : ContentVisual
{
    [Bindable] public partial ValidationMessage? Message { get; set; }
    [Bindable] public partial ValidationPlacement Placement { get; set; } = ValidationPlacement.Below;
}
```

Layout behavior:

- When `Message` is null, the presenter must not reserve space for the message (0 height).
- The message line should measure/arrange with the same width as the wrapped content and wrap by default.

### Fluent usage (v1 required)

Provide convenience wrappers similar to `Tooltip(...)`:

```csharp
// Fixed message:
new TextBox("Port")
    .Validation(new ValidationMessage(ValidationSeverity.Error, new TextBlock("Port is required.")));

// Dynamic message (recommended pattern):
var port = new State<string>("8080");

new TextBox().Text(port)
    .Validate(
        port.Bind.Value,
        value =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new(ValidationSeverity.Error, new TextBlock("Port is required."));
            }

            if (!int.TryParse(value, out var parsed) || parsed is < 1 or > 65535)
            {
                return new(ValidationSeverity.Error, new TextBlock("Port must be in [1..65535]."));
            }

            return null;
        });
```

The `.Validation(...)` / `.Validate(...)` helpers should return a `ValidationPresenter` wrapping the original visual.

### Validation helpers (recommended)

To make dynamic validation ergonomic without requiring users to manually create a derived state, provide helpers:

```csharp
public static partial class ValidationExtensions
{
    public static ValidationPresenter Validation(this Visual content, ValidationMessage? message);
    public static ValidationPresenter Validation(this Visual content, Binding<ValidationMessage?> message);
    public static ValidationPresenter Validation(this Visual content, State<ValidationMessage?> message);

    // The validator is called as part of the binding system (dependency tracking).
    public static ValidationPresenter Validate<T>(
        this Visual content,
        Binding<T> value,
        Func<T, ValidationMessage?> validator,
        ValidationPlacement placement = ValidationPlacement.Below);
}
```

Notes:

- The `Validate<T>(...)` helper can be implemented by binding `Message` to a computed value based on `value`.
- Exceptions should not be used as the primary validation mechanism (avoid exceptions as control flow). If desired, a
  small optional helper can catch a custom `ValidationException` and convert it to a `ValidationMessage`.

### Style

Introduce `ValidationStyle` (record):

- `Style? InfoStyle`, `WarningStyle`, `ErrorStyle` (foreground/background)
- Optional glyphs per severity (e.g. `Rune InfoGlyph`, `WarningGlyph`, `ErrorGlyph`)
- Padding/spacing around the message line

If the wrapped control already has a border (e.g. `BorderStyle`), the validation line should not “double border” by
default; it should look like a compact status line aligned with the input.

### Integration guidance

- Prompts should use `ValidationPresenter` (not ad-hoc text under the input).
- Controls that currently render bespoke validation UI (notably `NumberBox<T>`) should be refactored to use this shared
  infrastructure in v1 by composing `ValidationPresenter` internally.
  - The control should compute its `ValidationMessage?` from its current value + validator and bind it to the presenter.
  - This enables dynamic severity/message changes without imperative “set message on every key press” logic.
- For ad-hoc validation, users should be able to wrap any input control (e.g. `TextBox`) externally using `.Validate(...)`.
- Validation presentation should remain separate from the editor core (selection/caret/scrolling logic).

---

## Prompt (Inline Interactive Prompts)

Goal: provide a high-level, Rich/Spectre-like prompt API for inline/live scenarios, built on top of the UI controls and
binding system (not a separate prompt engine).

### Scope

- Prompts are for **inline** hosting (`Terminal.Live`) only.
- Fullscreen apps should use dialogs/popups within `Terminal.Run` instead.

### Public API

Provide both sync and async forms:

```csharp
public static class TerminalPrompts
{
    public static T Prompt<T>(TerminalPrompt<T> prompt);
    public static ValueTask<T> PromptAsync<T>(TerminalPrompt<T> prompt, CancellationToken cancellationToken = default);
}
```

The prompt instances describe content + behavior; `Prompt` builds visuals and runs an inline host until completion.

### Prompt types (v1)

- `TextPrompt` → `string`
- `NumberPrompt<T>` → `T where T : struct, INumber<T>` (reuse `NumberBox<T>` parsing and validation)
- `MaskedPrompt` → `string` (reuse `MaskedInput`)
- `ConfirmationPrompt` → `bool` (use `Select<bool>` or `Switch`)
- `SelectionPrompt<T>` → `T` (use `Select<T>` with template support)
- `MultiSelectionPrompt<T>` → `IReadOnlyList<T>` (use `SelectionList<T>` or `SelectionList` with templating)

### Prompt base capabilities

`TerminalPrompt<T>` should include:

- `Visual Message` (typically markup)
- Optional `Visual? Help` (displayed under input)
- Validation callback:
  - Use `Delegator<Func<T, string?>>` (null means valid, else error message)
- `TerminalLiveOptions` knobs:
  - remove on end vs keep final visual should be controlled by returning `TerminalLoopResult.StopAndKeepVisual` etc.

### Implementation guidance

- Prompts should be built from existing controls:
  - Use `Group`/`Border` for framing and `VStack` for layout.
  - Use the shared validation infrastructure (`ValidationPresenter`) for “message under input” (info/warning/error).
- Completion:
  - Enter confirms if valid.
  - Esc cancels (throw `OperationCanceledException` unless prompt provides a default).
- Prompts must be deterministic in tests:
  - Use `TerminalApp.BeginRun/EndRun` and `Tick()` to simulate frames.

---

## ColorPicker Control

Goal: select a `Color` (including RGB/RGBA) using a friendly UI:

- Direct editing of channels (RGB/HSL/OKLCH)
- Hex input with live preview and validation
- Optional palette from the current `ColorScheme` (16 colors + derived accents)

### Public API (v1)

```csharp
public sealed class ColorPicker : Visual
{
    [Bindable] public partial Color Value { get; set; }

    [Bindable] public partial bool AllowAlpha { get; set; } = true;
    [Bindable] public partial bool ShowPalette { get; set; } = true;

    // Optional palette override. If null and ShowPalette is true, derive from Theme.Scheme.
    [Bindable] public partial IReadOnlyList<Color?>? Palette { get; set; }
}
```

### UI composition (practical v1)

Start with a layout that is achievable without heavy custom rendering:

- Left: preview swatch (use `Canvas` or a `Border` with background).
  - For alpha, show a checkerboard pattern (rendered in `Canvas`) under the swatch color.
- Middle: channel sliders:
  - RGB sliders: `Slider<int>` (0..255)
  - Alpha slider: `Slider<int>` (0..255) only when `AllowAlpha`
  - Provide numeric boxes next to sliders (`NumberBox<int>`) for precise input
- Right: hex input:
  - TextBox with validation; supports `#RRGGBB` and `#RRGGBBAA` when alpha enabled
- Optional palette grid below:
  - A `Grid` of clickable swatches (from `Palette` or theme scheme)

Advanced visual pickers (wheel/field) can be v2. For v1, keep interactions straightforward but “modern” (good default
styles, clear preview).

### Style

`ColorPickerStyle`:

- Swatch size, border style, checkerboard colors
- Palette swatch size and selection indication
- Formatting (hex upper/lowercase, include alpha)

### Tests and demos

- Parsing tests: hex to Color, Color to hex (round-trip where possible)
- Binding tests: changing sliders updates `Value`, setting `Value` updates sliders
- ControlsDemo page demonstrating:
  - palette selection
  - alpha enabled/disabled
  - light vs dark schemes (local theming)

---

## Full Sync & Async Model (Guidance + small API additions)

The library already supports async hosting (`Terminal.LiveAsync`/`RunAsync`) and installs a dispatcher-backed
`SynchronizationContext`, so `await` continuations naturally resume on the UI thread.

For v1, focus on **practical async** rather than introducing a separate async event system:

- Document the pattern:
  - Start background tasks (e.g. downloads) and update `State<T>` on the UI thread using `Dispatcher.Invoke(...)`.
  - Avoid blocking the update callback; it should remain lightweight (polling state + returning `Continue`/`Stop...`).

Optional small additions (if needed by prompts/tooltips):

- Add a simple `TerminalApp`-scoped scheduler:
  - `Dispatcher.Post(Action)` and `Dispatcher.PostDelayed(TimeSpan, Action)` (implemented using the app loop tick).
  - Used by tooltips (hover delay) and prompt timeouts without relying on `Task.Delay` in tests.

If async event handlers are needed in v1, prefer adding explicit helpers (non-breaking, opt-in):

- `Button.Click(Func<Task>)` extension that wraps the task and logs exceptions (optional).

---

## Deliverables checklist per control (v1)

For each control/feature above:

- XML API docs for public types/members.
- A user guide page under `site/docs/controls/` (or `site/docs/` for non-control features).
- A ControlsDemo page (or additions to an existing page where appropriate).
- Unit tests:
  - Measure/Arrange in nested layouts
  - Rendering where it matters (glyphs, style resolution)
  - Input routing (mouse/keyboard) when interactive
