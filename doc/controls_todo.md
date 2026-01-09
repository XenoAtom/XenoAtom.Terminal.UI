## Core library tracking table

This is the list of controls planned for the core XenoAtom.Terminal.UI library. These are prioritized based on general usefulness and implementation complexity. The goal is to deliver a solid base set of controls that cover most app needs while keeping the core library lightweight and maintainable.

Once a control is completed, test have been added and it has been added to the FullscreenDemo app, it can be checked off the list.

Following this list, you will find a compact spec for each high-priority control to guide implementation.

| Done | Priority   | Missing component                                     | Category                   | What it unlocks                      | Why it’s worth it                                 |
| --- | ---------- | ----------------------------------------------------- | -------------------------- | ------------------------------------ | ------------------------------------------------- |
| [ ] | **High**   | **Rule / Divider**                                    | Layout/visual              | Section separators                   | Ubiquitous readability win                        |
| [ ] | **High**   | **Markup element**                                    | Content                    | Easy rich text output via AnsiMarkup | Low effort, high payoff across UI                 |
| [ ] | **High**   | **Slider**                                            | Input                      | Adjust a value within a range        | Common control; distinct semantics from scrollbar |
| [ ] | **High**   | **HSplitter / VSplitter (interactive)**               | Layout/input               | Mouse + keyboard resizing of panes   | Huge fullscreen UX win; enables IDE-style layouts |
| [ ] | **High**   | **Explicit Scrollbar widget**                         | Scrolling                  | Visible scroll affordance            | Immediate UX improvement                          |
| [ ] | **High**   | **Select / Dropdown**                                 | Input                      | Compact single-choice                | Common in forms/settings                          |
| [ ] | **High**   | **SelectionList (multi-select list widget)**          | Input                      | In-layout multi-select               | More app-like than prompts                        |
| [ ] | **High**   | **Header / Footer (app chrome)**                      | App chrome                 | Key hints, status, breadcrumbs       | Better whole-app structure                        |
| [ ] | **High**   | **TextArea (multiline editor)**                       | Input                      | Multi-line editing                   | “Real app” workflows beyond TextBox               |
| [ ] | **High**   | **TreeView**                                          | Navigation                 | Hierarchical navigation              | Big UX upgrade over flat lists                    |
| [ ] | **Medium** | **LoadingIndicator / Spinner**                        | Status                     | Busy state (unknown duration)        | Complements ProgressBar                           |
| [ ] | **Medium** | **MaskedInput (password/secret)**                     | Input                      | Auth + secrets                       | Often needed early                                |
| [ ] | **Medium** | **OptionList (fast menu list)**                       | Navigation/input           | Command menus/quick pickers          | Base for palettes/menus                           |
| [ ] | **Medium** | **Command palette / quick open**                      | Navigation                 | Searchable actions                   | Big productivity boost                            |
| [ ] | **Medium** | **Switch (toggle)**                                   | Input                      | Modern toggle UI                     | Nice settings affordance                          |
| [ ] | **Medium** | **Collapsible / Accordion**                           | Layout                     | Progressive disclosure               | Keeps screens navigable                           |
| [ ] | **Medium** | **DirectoryTree / File picker**                       | Navigation                 | File browsing                        | Popular in tool-like TUIs                         |
| [ ] | **Medium** | **Log view (RichLog / styled log)**                   | Diagnostics                | Streaming logs + scroll + styling    | Huge value for devtools/ops TUIs                  |
| [ ] | **Medium** | **Structured text viewer (syntax + JSON)**            | Content                    | Inspect JSON/configs                 | Great for tooling                                 |
| [ ] | **Medium** | **Sparkline**                                         | Visualization              | Tiny trends                          | High value / low scope                            |
| [ ] | **Medium** | **Basic charts (bar/line)**                           | Visualization              | Dashboards/telemetry                 | Highly desirable                                  |
| [ ] | **Medium** | **Calendar widget**                                   | Visualization/productivity | Schedules/date picking               | App-dependent                                     |
| [ ] | **Medium** | **Links (clickable/open URL)**                        | Interaction/content        | Hyperlinks in terminal               | Terminal support varies                           |
| [ ] | **Medium** | **DockWorkspace (dockable panes + floating windows)** | Layout/windowing           | Rearrangeable panes                  | Bigger feature, strong differentiator             |
| [ ] | **Medium**   | **ContentSwitcher (view routing)**                  | Layout/state               | Swap views without rebuild           | Great for navigation/wizards                      |
| [ ] | **Low**   | **Toast / Notification**                               | UX feedback                | Non-blocking feedback                | Modern UX; avoids modal spam                      |
| [ ] | **Low**    | **Digits / KPI big-number**                           | Visualization              | Dashboard counters                   | Mostly polish                                     |
| [ ] | **Low**    | **Placeholder / empty-state widget**                  | UX                         | No-results/loading states            | Polish; easy to fake                              |
| [ ] | **Low**    | **Figlet / big text**                                 | Visual                     | Banners/headers                      | Fun, rarely essential                             |
| [ ] | **Low**    | **Canvas / pixel surface**                            | Visualization              | Images/advanced plots                | Bigger scope; later                               |

## Add-on libraries after core (dependency-minimizing)

These controls will require additional dependencies, so they will be delivered as separate add-on libraries that depend on XenoAtom.Terminal.UI. This keeps the core library lightweight for users who don't need these features.

| [ ] | Component          | Depends on                  | Notes                                                                            |
| --- | ------------------ | --------------------------- | -------------------------------------------------------------------------------- |
| [ ] | **MarkdownViewer** | Markdig (or Markdown stack) | Keep core dependency-free; integrate via core rich-text spans + add-on renderer. |

---

# High priority control specs

Beyond these specs, if there are improvements or features that would make sense for a control based on your experience implementing it, feel free to add them.

## TreeView

* **Node model**: id/key, label, optional icon/glyph, expanded, selected, disabled
* **Virtualization**: visible-row enumeration from expanded nodes; large trees stay fast
* **Selection**: single (default), optional multi; anchor for shift-range
* **Keyboard**: Up/Down, Left collapse, Right expand, Home/End, type-to-search
* **Scrolling integration**: ensures focused/selected row is visible
* **Lazy children**: expand triggers load callback (async-friendly)
* **Rendering**: indent guides, expand/collapse glyphs, optional connector lines
* **Events**: SelectionChanged, ExpandedChanged, Activated

## TextArea (multiline editor)

* **Editing core**: insert/delete, newline, backspace join, undo/redo
* **Caret + selection**: range selection, word/line helpers
* **Navigation**: arrows, Ctrl+arrows word-wise, Home/End, Ctrl+Home/End doc
* **Scrolling**: vertical + optional horizontal; caret kept in view
* **Clipboard**: copy/cut/paste via platform abstraction
* **Wrapping**: none / soft wrap; optional wrap indicator
* **Modes**: read-only, max length/lines, accept-tab toggle
* **IME hooks**: composition support points even if minimal initially

## Select / Dropdown

* **API**: items source, selected value, display renderer
* **Popup**: anchored list; clamps to viewport; supports flip above/below
* **Search**: type-to-filter or type-to-jump; configurable timeout
* **Keyboard**: Enter/Space open, Esc close, Up/Down navigate, PageUp/Down
* **Mouse**: click open/select; wheel adjust when closed (optional)
* **Performance**: virtualization for long lists (reuse ListBox patterns)
* **Events**: Opened/Closed, SelectionChanged, Confirmed

## SelectionList (multi-select list widget)

* **State**: checked + disabled per item; tri-state reserved for later
* **Keyboard**: Space toggles, Ctrl+A check all, Ctrl+I invert (optional)
* **Range ops**: Shift+Up/Down range toggle (optional)
* **Search**: type-to-jump/filter
* **Bulk actions**: check all / uncheck all / invert as commands
* **Scrolling**: keeps focus item visible; integrates with ScrollViewer
* **Rendering**: pluggable checkbox glyphs; focused-row highlight
* **Events**: ItemToggled, CheckedSetChanged

## Slider

* **Range**: min/max/value, step, large-step; clamp + snap
* **Orientation**: horizontal + vertical
* **Keyboard**: arrows step, PageUp/Down large-step, Home/End min/max
* **Mouse**: click track jump (configurable), drag thumb, wheel adjust
* **Display**: optional value label, optional ticks; formatting callback
* **Mapping**: linear default; hook for custom mapping (log scale)
* **Events**: ValueChanged

## HSplitter / VSplitter (interactive)

* **Orientation**: HSplitter (vertical bar between left/right panes), VSplitter (horizontal bar between top/bottom panes)
* **Layout contract**: manages 2 (or N) panes; maintains per-pane sizes/ratios with minimum sizes
* **Mouse drag**: pointer capture on press; live resize on move; release commits
* **Keyboard resize**: focusable; arrows adjust; Shift/Ctrl for larger steps; Home/End snap to limits
* **Visual states**: normal/hover/focused styling; theme-driven glyphs and colors. Rich styling and runes options for splitter bar.
* **Constraints**: per-pane min/max; optional collapse/expand behavior with threshold. Select the best/easiest API for this.
* **Persistence**: exposes ratios/sizes for saving and restoring layouts
* **Composition**: nests cleanly;

## Markup element

* **Input**: markup string
* **Parsing**: You can use possibly `XenoAtom.Ansi.IAnsiBasicWriter` to implement internally a custom output used by this control to analyze/collect styles parts from plain text.
* **Output model**: produces styled text spans with attributes to the CellBuffer. 
* **Performance**: caches parsed spans until text changes; avoids reparse on every draw. Cache custom internal AnsiBasicWriter to avoid any allocations when measuring/arranging/rendering.
* **Interoperability**: similar to TextBlock in terms of API

## Explicit Scrollbar widget

* **Model**: min/max/value plus viewport size for thumb sizing
* **Thumb sizing**: proportional to viewport/content with minimum size
* **Mouse**: drag thumb; click track pages; wheel passthrough option
* **Keyboard**: arrows small, PageUp/Down large, Home/End extremes
* **Binding**: clean two-way sync with ScrollViewer
* **Visibility**: auto/hide/always; overlay vs reserved gutter

## Rule (or Divider)

* **Orientation**: horizontal + vertical
* **Variants**: Provide as many styles as possible (from ascii to modern unicode)
* **Layout**: can have labels at start/center/end. Use similar API than the Group control. (Always use Visual for labels, not strings)
* **Theming**: glyph set and style from theme

## Header / Footer (app chrome)

* **Docking**: fixed rows docked top/bottom; integrates with DockLayout
* **Regions**: left/center/right slots; supports dynamic updates
* **Key hints**: standard shortcut/hint area
* **Status hooks**: title, breadcrumbs, counters, clock (optional)
* **Focus policy**: typically non-focusable; optional focusable children
