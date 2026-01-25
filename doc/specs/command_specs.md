# Command System & Key Hints (Specs)

This document proposes a **command system** for XenoAtom.Terminal.UI that unifies:

- **Key bindings** (`Ctrl+Z`, `Ctrl+R`, `Ctrl+F`, …)
- **UI actions** (menus, context menus, command palette)
- **Discoverability** (a `CommandBar` / “key hints” control)

The goal is to make “what can I do right now?” obvious without sacrificing performance or the existing retained-mode + binding model.

---

## 1. Goals / non-goals

### 1.1 Goals (v1)

- Provide a first-class concept of an **Action/Command** with:
  - an optional **key gesture**
  - optional **key binding sequences** (e.g. `Ctrl+K` then `Ctrl+P`)
  - a **user-facing label** (supports markup)
  - optional **help/description** (supports markup)
  - optional **enable/disable** logic (context-aware)
  - optional **visibility** rules (context-aware)
- Allow commands to be registered at multiple levels:
  - **Control-local** (e.g. `TextArea`: Find/Replace; `TextEditorBase`: Undo/Redo)
  - **App/global** (e.g. `Ctrl+Q`: quit; `F12`: debug overlay; `Ctrl+P`: command palette)
- Preserve existing **gesture resolution semantics**:
  - focused visual wins; bindings bubble up the tree to the root
  - then fall back to app/global commands
- Provide a built-in control to surface commands:
  - `CommandBar` (key hints row)
  - option to open a “full list” help UI (e.g. `F1`), if desired
- Make it easy for existing controls to expose their “important” shortcuts:
  - focus on **discoverable** shortcuts, not basic navigation (arrows, home/end, etc.)

### 1.2 Non-goals (v1)

- A full WPF-style routed `ICommand` framework.
- Localization infrastructure (the API should allow it, but it’s not required for v1).

---

## 2. Current code map

Existing related types:

- Key input dispatch:
  - `TerminalApp.DispatchKeyEvent(...)` traverses `FocusedElement → Parent` and resolves matching `UiCommand` instances
    registered on each visual (`Visual.Commands`), then falls back to app/global commands (`TerminalApp.GlobalCommands`).
  - Commands are resolved **before** raising `Visual.KeyDownEvent` (so shortcuts behave consistently even if controls don’t handle
    `KeyDownEvent`).
- Convenience key bindings:
  - `Visual.AddKeyBinding(KeyGesture, Action)` is still available, but it is implemented as a hidden `UiCommand`
    (`Presentation = None`) to keep all shortcut routing centralized in the command system.
- Menus / command palette:
  - `MenuBar` and `CommandPalette` exist and represent “actions” in custom ways

The command system should build on this rather than replace everything immediately.

---

## 3. Concepts and terminology

- **Command**: a user-facing action that can be invoked by:
  - keyboard gesture
  - menu / context menu entry
  - command palette entry
- **Gesture**: `KeyGesture` (built on XenoAtom.Terminal input model).
- **Sequence**: an ordered list of gestures that form a shortcut (e.g. `Ctrl+K` then `P`).
- **Prefix**: the first gesture of a sequence (e.g. `Ctrl+K`) while the system waits for subsequent strokes.
- **Local command**: registered on a `Visual` instance.
- **Global command**: registered on `TerminalApp` (or another app-level registry).
- **Presentation**: label/help/icon/category/priority used for UI surfaces.
- **Context**: runtime state used to decide if a command is enabled/visible (focused visual, selection state, etc.).

---

## 4. Proposed API surface (v1)

### 4.1 Command model

Introduce a new type (name can vary, examples below use `UiCommand`):

```csharp
public sealed class UiCommand
{
    public required string Id { get; init; }

    // User-facing label. Markup is supported (Theme tokens should work).
    public required string LabelMarkup { get; init; }

    // Optional longer description used by tooltips / help dialogs.
    public string? DescriptionMarkup { get; init; }

    // Optional gesture to display + execute from keyboard.
    public KeyGesture? Gesture { get; init; }

    // Optional multi-stroke shortcut (e.g. Ctrl+K then Ctrl+P).
    // When set, Gesture must be null.
    public TerminalKeySequence? Sequence { get; init; }

    public UiCommandImportance Importance { get; init; } = UiCommandImportance.Secondary;

    public UiCommandPresentation Presentation { get; init; } = UiCommandPresentation.CommandBar;

    // Execute/can-execute are target-aware so a single static command definition can be reused.
    public required Action<Visual> Execute { get; init; }
    public Func<Visual, bool>? CanExecute { get; init; }
    public Func<Visual, bool>? IsVisible { get; init; }
}

public enum UiCommandImportance
{
    Primary,
    Secondary,
    Tertiary,
}

[Flags]
public enum UiCommandPresentation
{
    None = 0,
    CommandBar = 1,
    CommandPalette = 2,
    Menu = 4,
    ContextMenu = 8,
}
```

Notes:

- `LabelMarkup` / `DescriptionMarkup` are **strings** for ergonomics; they can be rendered by `Markup`.
- `Execute` and `CanExecute` take a `Visual` parameter to avoid per-instance closures (typical controls can use method groups).
- Commands are *not* routed events; they are a higher-level “invocation” concept.
- `Gesture` and `Sequence` are mutually exclusive (one command has a single shortcut representation).
- For Ctrl-modified shortcuts, prefer using the terminal control-character constants (e.g. `TerminalChar.CtrlA`) for
  `KeyGesture(char, TerminalModifiers)` because terminals commonly emit control characters rather than the printable
  letter (see existing usage throughout the codebase).

#### 4.1.1 `TerminalKeySequence`

Introduce a small type to represent sequences and to keep formatting/parsing centralized:

```csharp
public readonly record struct TerminalKeySequence
{
    public TerminalKeySequence(params KeyGesture[] gestures);

    public ReadOnlySpan<KeyGesture> Gestures { get; }

    public override string ToString(); // e.g. "Ctrl+K Ctrl+P"

    public static bool TryParse(ReadOnlySpan<char> text, out TerminalKeySequence sequence);
}
```

Notes:

- Sequences are expected to be short (usually 2 strokes).
- Formatting should use existing `KeyGesture.ToString()` for each stroke.

### 4.2 Registration and lookup

Add an internal list of commands per `Visual` and optionally per `TerminalApp`.

Proposed `Visual` API:

```csharp
public abstract partial class Visual
{
    public IReadOnlyList<UiCommand> Commands { get; }

    public void AddCommand(UiCommand command);
    public bool RemoveCommand(string id);
}
```

Proposed `TerminalApp` API:

```csharp
public sealed partial class TerminalApp
{
    public IReadOnlyList<UiCommand> GlobalCommands { get; }

    public void AddGlobalCommand(UiCommand command);
    public bool RemoveGlobalCommand(string id);
}
```

Lookup algorithm for UI surfaces:

1. Start at `FocusedElement` (or `HoveredElement` for context menus) and walk up `Parent`.
2. Include all commands that are:
   - visible (`IsVisible == null || IsVisible(target)`)
   - match the requested presentation surface flags.
3. Deduplicate by `Id` (and optionally by gesture).
4. Append global commands (same filtering rules).
5. Sort:
   - local-first, then global
   - within each group: `Importance` then stable insertion order

### 4.3 Keyboard execution (gesture routing)

Gesture routing should remain consistent with today’s behavior:

1. Try local focused commands on `FocusedElement → Parent`.
2. Then try global commands.
3. If a matching command is found and `CanExecute` is `true`:
   - execute it
   - mark the event handled

This is implemented by resolving `UiCommand` instances during `TerminalApp.DispatchKeyEvent(...)`, using the same focus-walk
(`FocusedElement → Parent`) and then falling back to `TerminalApp.GlobalCommands`.

### 4.4 Key sequences (multi-stroke shortcuts)

Key sequences must be supported in v1 (e.g. Emacs-style `Ctrl+K` followed by another key).

#### 4.4.1 Resolution rules

When a key event arrives:

1. If no sequence is active:
   - First try **single-stroke commands** (`Gesture`) using the existing focus-walk resolution (focused → parents → global).
   - If there is no direct match, check whether the gesture is a **prefix** for any sequence visible in the current context.
     - If it is a prefix, enter “sequence mode” and wait for the next stroke.
2. If sequence mode is active:
   - Match the next stroke against available sequences under the same resolution rules.
   - If a sequence matches, execute that command and exit sequence mode.
   - If no sequence matches, exit sequence mode and treat the key event as unhandled (or optionally show a brief “no match” hint).

#### 4.4.2 Prefix conflicts

To keep single-key bindings simple and predictable:

- A gesture used as a sequence prefix SHOULD NOT be used as a standalone command gesture in the same scope.
- The implementation SHOULD validate this at registration time (or at least when collecting commands) and either:
  - throw an exception in debug builds, or
  - ignore the standalone binding and prefer the prefix behavior.

This avoids “wait for timeout to disambiguate” behavior.

#### 4.4.3 Cancellation and timeout

- Sequence mode is cancelled by:
  - `Esc`, or
  - focus change, or
  - mouse click (optional), or
  - an inactivity timeout (recommended default: 1–2 seconds).

The timeout should be configurable (likely on `TerminalAppOptions` in the future).

#### 4.4.4 UI surfaces while in sequence mode

While a prefix is active, command discovery UI should adapt:

- `CommandBar` shows only commands that:
  - are reachable from the active prefix, and
  - match the current context.
- The bar should show the prefix as a “pending” keycap (e.g. `Ctrl+K …`) to explain the current state.

### 4.4 Interop with existing `KeyBinding`

XenoAtom.Terminal.UI implements **Option B**: `UiCommand` is the single model for shortcuts and discoverability.

- `Visual.AddKeyBinding(...)` remains for compatibility and ergonomics, but it registers an internal/hidden `UiCommand`
  (so the command router remains the source of truth).
- There is no separate `KeyBinding` data structure for routing.

---

## 5. `CommandBar` control (key hints)

### 5.1 Purpose

`CommandBar` is a lightweight control that displays the most relevant commands for the current context (focused element),
similar to “key hints”:

```
Ctrl+F Find | Ctrl+H Replace | Ctrl+Z Undo | Ctrl+R Redo | Ctrl+Q Quit
```

### 5.2 Suggested API

```csharp
public sealed class CommandBar : Visual
{
    [Bindable] public partial UiCommandImportance MinImportance { get; set; } = UiCommandImportance.Secondary;
    [Bindable] public partial bool ShowDisabled { get; set; }
    [Bindable] public partial bool IncludeGlobalCommands { get; set; } = true;
    [Bindable] public partial int MaxItems { get; set; } = 6;
}
```

Behavior:

- The bar watches `App.FocusedElement` changes and refreshes accordingly.
- Commands are rendered as:
  - **Key** segment (styled “keycap” / accent)
  - **Label** segment (normal foreground)
- When not enough space is available:
  - show fewer items; optionally show a trailing “More…” hint that can open a help popup.

### 5.3 Styling

Add `CommandBarStyle`:

- `KeyStyle` (foreground/background/border for the keycap)
- `LabelStyle`
- `DisabledKeyStyle` / `DisabledLabelStyle`
- `SeparatorText` (e.g. two spaces, `|`, double pipe, etc.)

Markup usage:

- `LabelMarkup` should be parsed via `MarkupTextParser` with the current theme tokens.
- The key segment should not rely on markup; it should be styled consistently via the style.
- The CommandBarStyle should provide several built-in styles (e.g. “default”, “compact”, etc.) for easy theming.

---

## 6. Command surfaces beyond the bar

### 6.1 Command palette integration

CommandPalette should be able to show all commands that have `UiCommandPresentation.CommandPalette`.

Longer term:

- unify `CommandPaletteItem` with `UiCommand` (palette entries can be commands)
- allow palette to include “dynamic” commands (e.g. per selected node)

### 6.2 MenuBar and context menus

Menu items and context menu entries should be able to reference a `UiCommand`:

- label: `LabelMarkup`
- enabled: `CanExecute`
- execute: `Execute`
- gesture: displayed as a right-aligned hint

Context menus:

- Right-click should open a context menu built from commands with `UiCommandPresentation.ContextMenu`.
- The target should typically be the hovered visual (or focused visual if hover is not applicable).

This creates a cohesive path:

- **same command** → key binding, command palette, menu bar, context menu, command bar.

---

## 7. Guidance for updating existing controls (v1)

Focus on “discoverable” shortcuts that users benefit from seeing:

### 7.1 Text editors (`TextBox`, `TextArea`, `MaskedInput`, `NumberBox<T>`)

Register commands (typically local):

- Undo (`Ctrl+Z`)
- Redo (`Ctrl+R`)
- Copy / Cut / Paste (depending on clipboard mode)
- Select all (`Ctrl+A`) (if supported)
- Find / Replace (`Ctrl+F` / `Ctrl+H`) where applicable

### 7.2 LogControl

- Find (`Ctrl+F`)
- Next / previous match (`Enter` / `Shift+Enter`) could be included if helpful

### 7.3 App/global

Register at `TerminalApp`:

- Quit (default `Ctrl+Q` in fullscreen; `Esc` in inline, if kept)
- Toggle debug overlay (`F12`)
- Command palette (`Ctrl+P`)

---

## 8. Dependency tracking and performance notes

- `CanExecute` / `IsVisible` may read bindable properties (e.g. `TextEditorBase.CanUndo`).
  - When `CommandBar` renders/evaluates these, dependency tracking should naturally trigger re-render when these values change.
- Avoid per-frame allocations:
  - cache collected command lists between focus changes
  - reuse internal buffers/lists (e.g. `UnsafeList<UiCommand>` or pooled lists)
- Do not parse markup repeatedly when not needed:
  - `CommandBar` can cache parsed `Markup` visuals per command instance if necessary, or parse lazily.

---

## 9. Testing plan

- Unit tests for resolution order:
  - focused command overrides parent/global for same gesture
  - prefix detection and sequence matching (local-first, then global)
  - cancel behavior (Esc, timeout)
  - dedup by `Id`
- Rendering tests for `CommandBar`:
  - ensures key and label segments render
  - ensures disabled commands render differently
- Integration tests:
  - `TextArea` exposes undo/redo/find/replace commands in context
  - command palette lists app/global commands

---

## 10. Implementation steps (recommended)

1. Introduce `UiCommand` + per-visual registration (`Visual.AddCommand`) and app/global registration.
2. Route gestures to commands (reuse existing focus-walk logic).
3. Implement `CommandBar` with styling + demo integration (ControlsDemo footer).
4. Update key controls (TextEditorBase, LogControl, SearchReplacePopup integration, etc.).
5. Integrate menus/context menus and optionally command palette.
