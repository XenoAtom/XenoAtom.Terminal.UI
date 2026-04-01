---
title: PromptEditor (Prompt-Style Text Editor) Control Specs
---

# PromptEditor (Prompt-Style Text Editor) Control Specs

This document specifies a new control for **XenoAtom.Terminal.UI**: **`PromptEditor`**.

`PromptEditor` is a prompt-like text editor inspired by **XenoAtom.Terminal**’s rich `ReadLine` editor, but implemented as
a retained-mode **UI control** built on top of the existing **TextEditorBase/TextEditorCore** infrastructure.

Why a dedicated control?

- `TextBox` and `TextArea` are general-purpose editors.
- Interactive terminal prompts typically need **prompt prefixes**, **ghost completions**, **history**, and **completion UX**
  that feel like a shell/REPL.

> [!NOTE]
> Name: `PromptEditor` reflects the control’s multi-line capable, programmable “editor” nature, while keeping a
> prompt-oriented UX (prefix, history, completion).

---

## Goals

- Provide a **prompt-like** editor surface with:
  - a **dynamic prompt prefix** (markup-based),
  - a **placeholder** (empty editor) and an optional **ghost completion** (non-empty editor),
  - first-class integration with `TextEditorBase` editing features (caret, selection, word wrap, clipboard, undo/redo).
- Support **single-line and multi-line** workflows:
  - multi-line editing (newlines, scrolling, selection),
  - configurable **accept/cancel** behavior appropriate for a prompt.
- Provide **easy syntax highlighting**:
  - pluggable, allocation-conscious,
  - suitable for REPLs, command lines, and small scripts.
- Provide **completion** that is highly pluggable:
  - inline cycling (readline-like),
  - optional popup list (OptionList-based),
  - host-controlled (the control can surface the request, host chooses presentation).
- Provide **history** in a way that works for multi-line:
  - default bindings should not conflict with cursor movement and multi-line navigation,
  - history navigation is opt-in and configurable.
- Provide a **shortcut/action model**:
  - expose key actions as `Command` (discoverable via CommandBar),
  - allow users to override the gestures and/or hook custom actions.

---

## Non-goals (v1)

- A full IDE/code editor (multi-cursor, LSP, folding, minimap, etc.).
- A full “readline emulation” layer with strict GNU readline parity.
- A general-purpose syntax parser. The control exposes a highlighting surface; parsing/lexing belongs to user code.

---

## Related existing components

- **XenoAtom.Terminal**: `Terminal.ReadLineAsync(...)` and related types:
  - `TerminalReadLineOptions` (prompt markup, completion handler, history, key bindings)
  - `TerminalReadLineController` (imperative editing API)
  - `TerminalTextUtility` (word boundaries, cell widths, grapheme-aware navigation)
- **XenoAtom.Terminal.UI**:
  - `TextBox` / `TextArea` (built on `TextEditorBase`)
  - `TextEditorBase` / `TextEditorCore` (selection/caret/search/scrolling/clipboard/undo)
  - `OptionList` and popup infrastructure (completion list presentation)

PromptEditor SHOULD reuse as much of the existing editor core as possible, and rely on `TerminalTextUtility` for
text-width/word-boundary correctness.

---

## Terminology

- **Prompt prefix**: the visual/markup content displayed *before* the editable region (e.g. `> `, cwd, counter).
- **Continuation prefix**: prefix used for wrapped or subsequent lines (optional).
- **Ghost completion / suggestion**: dim text rendered inline to indicate a likely completion.
- **Completion session**: a temporary state where the user cycles candidates or a popup list is open.
- **History entry**: previously accepted input (often strings), navigable by commands.

---

## High-level architecture

`PromptEditor` is implemented as a specialized `TextEditorBase`-derived control:

- It delegates editing behavior to `TextEditorCore` (caret, selection, editing, scrolling, search popup).
- It adds a small “prompt layer”:
  - measures/renders a prompt prefix (markup -> styled runs),
  - renders a placeholder and ghost completion in a prompt-appropriate way,
  - provides completion/history plumbing and exposes command hooks.

The goal is to avoid re-implementing a second text editor; PromptEditor is primarily:

1) **prefix + suggestion rendering**, and
2) **completion/history/commands** around the existing editor.

---

## Proposed public API (v1)

```csharp
namespace XenoAtom.Terminal.UI.Controls;

public enum PromptEditorEnterMode
{
    /// <summary>
    /// Enter accepts the prompt, and Ctrl+J inserts a newline.
    /// </summary>
    /// <remarks>
    /// In terminals, Enter is commonly received as <c>TerminalKey.Enter</c> and/or <c>TerminalChar.CtrlM</c> (CR, <c>\r</c>),
    /// while Ctrl+J is <c>TerminalChar.CtrlJ</c> (LF, <c>\n</c>).
    /// </remarks>
    EnterAccepts,

    /// <summary>
    /// Enter inserts a newline, and Ctrl+J accepts the prompt.
    /// </summary>
    EnterInsertsNewLine,
}

public enum PromptEditorCompletionPresentation
{
    /// <summary>Do not show UI; still raise CompletionRequested.</summary>
    None,

    /// <summary>Cycle candidates inline (readline-like).</summary>
    InlineCycle,

    /// <summary>Show a popup list of candidates (OptionList).</summary>
    PopupList,
}

/// <summary>
/// Represents a completion request computed for the current text and caret/selection.
/// </summary>
public readonly record struct PromptEditorCompletion(
    bool Handled,
    IReadOnlyList<string>? Candidates,
    int ReplaceStart,
    int ReplaceLength,
    int SelectedIndex = 0,
    string? GhostText = null);

public readonly record struct PromptEditorCompletionRequest(
    ITextSnapshot Snapshot,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength,
    TerminalModifiers Modifiers);

public delegate PromptEditorCompletion PromptEditorCompletionHandler(in PromptEditorCompletionRequest request);

public readonly record struct PromptEditorHighlightRequest(
    ITextSnapshot Snapshot,
    Theme Theme,
    int CaretIndex,
    int SelectionStart,
    int SelectionLength);

public delegate void PromptEditorHighlighter(in PromptEditorHighlightRequest request, List<StyledRun> runs);

public partial class PromptEditor : TextEditorBase
{
    // Prompt prefix
    [Bindable(NoVisualAttach = true)] public partial Visual? Prompt { get; set; }
    [Bindable] public partial string? PromptMarkup { get; set; }
    [Bindable] public partial string? ContinuationPromptMarkup { get; set; }

    // Accept/cancel and behavior
    [Bindable] public partial PromptEditorEnterMode EnterMode { get; set; } = PromptEditorEnterMode.EnterAccepts;
    [Bindable] public partial bool AcceptOnBlur { get; set; }

    // Optional suggestion line (ghost completion)
    [Bindable] public partial bool EnableGhostCompletion { get; set; } = true;

    // Completion + history
    [Bindable] public partial PromptEditorCompletionPresentation CompletionPresentation { get; set; } = PromptEditorCompletionPresentation.PopupList;
    [Bindable] public partial Delegator<PromptEditorCompletionHandler> CompletionHandler { get; set; }

    // Highlighting + word hints
    [Bindable] public partial bool EnableWordHints { get; set; }
    [Bindable] public partial Delegator<PromptEditorHighlighter> Highlighter { get; set; }

    // Events
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnAccepted(PromptEditorAcceptedEventArgs e) { }

    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnCanceled(PromptEditorCanceledEventArgs e) { }
}
```

Notes:

- `Prompt` is optional. When set, it takes precedence over `PromptMarkup` for the first visual row.
- `PromptMarkup` uses markup syntax (same as `Markup` control / XenoAtom.Ansi) and SHOULD support theme custom tags
  (`primary`, `success`, etc.) like the rest of Terminal.UI.
- `Highlighter` is optional. When empty, the editor renders with its default text style.

> [!IMPORTANT]
> Delegate-valued bindables MUST use `Delegator<TDelegate>` for fluent support (see `Control Development Guide`).

---

## Layout behavior

PromptEditor layout is conceptually:

```
[prompt prefix][ editable region (scrolling, selection, caret) ]
```

### Measuring

- Height follows `TextEditorBase` behavior (single-line vs multi-line).
- PromptEditor MUST measure the prompt prefix width in terminal cells:
  - Parse `PromptMarkup` into plain text + runs (via `MarkupTextParser`).
  - Compute cell width via `TerminalTextUtility.GetWidth`.
- When using `PromptMarkup`, the prompt prefix is rendered by PromptEditor itself (like TextBox renders its chrome).
- When using `Prompt`, the prompt is a child visual arranged in the left prompt column (first visual row only).
- The editable region width is `Bounds.Width - prefixWidth` (minus style padding).

### Arranging

- PromptEditor arranges the editor viewport to start after the prefix.
- For multi-line:
  - the prefix is shown on line 0,
  - subsequent lines use `ContinuationPromptMarkup` when set,
  - otherwise, the continuation prefix is equivalent to “spaces” that align with the prompt prefix width.

This preserves the “prompt column” look and prevents wrapped text from jumping under the prompt.

---

## Rendering behavior

PromptEditor rendering consists of:

1) Background/chrome (similar conventions as `TextArea` or prompt-style surface).
2) Prompt prefix (markup runs rendered into the buffer).
3) Text editor content (delegated to `TextEditorCore`), with optional:
   - syntax highlighting runs,
   - ghost completion, placeholder.
4) Optional completion UI (inline hints or popup overlay).

### Prompt prefix rendering

- Parse prompt markup once per render pass (or cache by version) using `MarkupTextParser`.
- Render the prefix text and apply styles from runs.
- The prefix must be clipped to the available prefix region width.

### Syntax highlighting

If `Highlighter` is set:

- PromptEditor requests runs for the current snapshot (prefer snapshot version-based caching).
- Runs are applied on top of the default text style (like a paint layer).
- Highlighter must be able to apply text decorations such as underline for detected tokens.

> [!NOTE]
> `MarkupTextParser` can be used by implementers to easily produce style runs from markup without building visuals.

### Ghost completion

Ghost completion is a dim suggestion that does not alter the document:

- It is rendered after the visible caret position (or after the typed prefix).
- It uses a style derived from the theme foreground with high transparency / dimming.
- It is shown only when:
  - `EnableGhostCompletion=true`,
  - there is no selection (or selection is collapsed),
  - and the completion system has provided a `GhostText`.

Ghost completion should never interfere with:

- caret visibility
- selection rendering
- hit testing (it is visual only)

---

## Input behavior

PromptEditor inherits the editor input model from `TextEditorCore` and adds prompt-specific actions.

### Accept/cancel

- **Accept** produces an “accepted text” payload and raises `Accepted`.
  - Default: Enter accepts (CR, `\r`), and Ctrl+J inserts a newline (LF, `\n`).
  - `EnterMode` allows swapping the meaning of Enter and Ctrl+J.
- **Cancel** clears any completion session and raises `Canceled`.
  - Default: `Esc` cancels completion if active; otherwise cancels the prompt.

The accept/cancel design must avoid breaking multi-line editing.

> [!IMPORTANT]
> The editor document uses `\n` for line breaks internally. Even when input arrives as CR (`\r`) from Enter, PromptEditor
> should insert `\n` when it needs to create a new line.

### Completion

Completion is triggered by a command (default gesture is typically `Tab`):

- If `AcceptTab=true`, Tab inserts a tab character and completion should be triggered via a separate gesture
  (e.g. `Ctrl+Space`).
- Otherwise, Tab requests completion.

PromptEditor MUST expose completion as both:

- a `CompletionHandler` callback (easy for simple use), and
- a routed event / command that lets the host implement custom UX.

Default completion behavior:

- Inline cycle for `InlineCycle`:
  - applying a candidate replaces `[ReplaceStart..ReplaceStart+ReplaceLength)` in the document,
  - repeated triggers cycle the candidate list until any other key is pressed.
- Popup list for `PopupList`:
  - if there is exactly one candidate, apply it immediately without opening a popup,
  - otherwise open an overlay `Popup` containing an `OptionList`,
  - selecting a candidate applies it and closes the popup.

### History

PromptEditor history navigation should not be hard-coded to Up/Down in multi-line mode.

Suggested defaults:

- History previous/next: `Alt+Up` / `Alt+Down` (or `Ctrl+Up` / `Ctrl+Down` depending on terminal support).
- Reverse search: `Ctrl+R` (optional; matches Terminal.ReadLine defaults).

History should be user-provided (like Terminal.ReadLine options) so it can be scoped and shared.

---

## Commands and discoverability

PromptEditor SHOULD register commands so CommandBar / CommandPalette can discover them, similar to `TextEditorBase`.

Required commands (v1):

- `PromptEditor.Accept`
- `PromptEditor.Cancel`
- `PromptEditor.InsertNewLine`
- `PromptEditor.Complete` (request completion)
- `PromptEditor.CompletionNext` / `PromptEditor.CompletionPrevious` (when cycling)
- `PromptEditor.HistoryPrevious` / `PromptEditor.HistoryNext` (when history enabled)
- `TextEditor.*` commands inherited from `TextEditorBase` (undo/redo/copy/paste/select-all/search)

Default gestures (v1 suggested):

- `PromptEditor.Accept`: `Enter` (CR, `TerminalKey.Enter` and/or `TerminalChar.CtrlM`)
- `PromptEditor.InsertNewLine`: `Ctrl+J` (LF, `TerminalChar.CtrlJ`)

Users can swap the behavior by either:

- setting `EnterMode`, or
- overriding command gestures (commands are first-class and can be replaced by id).

Each command should have:

- a stable `Id`
- a label/description markup
- a default gesture (where possible)
- clear `CanExecute` behavior (e.g. completion commands disabled when no candidates)

---

## Styling

PromptEditor has its own style record (v1), but should reuse familiar input styling:

```csharp
public readonly record struct PromptEditorStyle : IStyle<PromptEditorStyle>
{
    public Thickness Padding { get; init; }

    public Func<Theme, bool, Style> BackgroundStyle { get; init; }
    public Func<Theme, Style> TextStyle { get; init; }
    public Func<Theme, Style> SelectionStyle { get; init; }
    public Func<Theme, Style> PlaceholderStyle { get; init; }

    public Func<Theme, Style> PromptStyle { get; init; }
    public Func<Theme, Style> GhostStyle { get; init; }

    public Func<Theme, Style> WordHintStyle { get; init; } // underline/dim, optional
}
```

Guidance:

- The prompt prefix style should be distinct but not overpowering (often dim/cyan).
- Ghost completion should be subtle (dim foreground) and clearly different from placeholder.

---

## Testing plan

Add tests in `src/XenoAtom.Terminal.UI.Tests` (rendering-focused) to cover:

- Prompt prefix width:
  - prompt markup parsing and width calculation align the editor region.
- Continuation prompt:
  - multi-line text renders with correct indentation on subsequent lines.
- Accept mode:
  - default mapping: Enter accepts, Ctrl+J inserts newline; verify `EnterMode` swap.
- Completion:
  - applying a candidate replaces the expected range and updates ghost text.
- Highlighting:
  - a simple `PromptEditorHighlighter` styles a token range (e.g. underline a word).

---

## Demo plan (ControlsDemo / FullscreenDemo)

Add a demo that mirrors the feel of `samples/HelloReadLine` from XenoAtom.Terminal:

- A dynamic markup prompt:
  - counter + cwd + `>` glyph, theme colors.
- A small command set with completion:
  - inline cycle for simple commands,
  - popup list for richer completion (show descriptions).
- History:
  - scoped to the demo session, navigable with a prompt-friendly gesture.
- Syntax highlighting:
  - highlight a few keywords/flags and numbers.
- Ghost completion:
  - show “best candidate” remainder in dim style.
- Custom shortcuts:
  - `Ctrl+L` clears the document,
  - `Esc` cancels completion or the prompt (depending on state),
  - demonstrate commands in a CommandBar.

---

## Future ideas (v2+)

- Prompt prefix as a `Visual` slot (not only markup) for richer composition (icons, small status widgets).
- Structured completion model:
  - candidates as visuals (label + description + kind icon),
  - fuzzy matching and scoring helpers.
- Inline diagnostics:
  - validation messages and squiggles (underline styles).
- Multi-line history view:
  - a dedicated popup showing a searchable list of prior entries.
- Embeddable “inline widgets” (e.g. argument hints) rendered above/below the editor.
