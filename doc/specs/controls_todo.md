## Core library tracking table

This is the list of controls planned for the core XenoAtom.Terminal.UI library. These are prioritized based on general usefulness and implementation complexity. The goal is to deliver a solid base set of controls that cover most app needs while keeping the core library lightweight and maintainable.

Once a control is completed, test have been added and it has been added to the FullscreenDemo app, it can be checked off the list.

Following this list, you will find compact specs for high and medium priority controls to guide implementation.

| Done | Priority   | Missing component                                     | Category                   | What it unlocks                      | Why it’s worth it                                 |
| --- | ---------- | ----------------------------------------------------- | -------------------------- | ------------------------------------ | ------------------------------------------------- |
| [x] | **High**   | **Rule / Divider**                                    | Layout/visual              | Section separators                   | Ubiquitous readability win                        |
| [x] | **High**   | **Markup element**                                    | Content                    | Easy rich text output via AnsiMarkup | Low effort, high payoff across UI                 |
| [x] | **High**   | **Slider**                                            | Input                      | Adjust a value within a range        | Common control; distinct semantics from scrollbar |
| [x] | **High**   | **HSplitter / VSplitter (interactive)**               | Layout/input               | Mouse + keyboard resizing of panes   | Huge fullscreen UX win; enables IDE-style layouts |
| [x] | **High**   | **Explicit Scrollbar widget**                         | Scrolling                  | Visible scroll affordance            | Immediate UX improvement                          |
| [x] | **High**   | **Select / Dropdown**                                 | Input                      | Compact single-choice                | Common in forms/settings                          |
| [x] | **High**   | **SelectionList (multi-select list widget)**          | Input                      | In-layout multi-select               | More app-like than prompts                        |
| [x] | **High**   | **Header / Footer (app chrome)**                      | App chrome                 | Key hints, status, breadcrumbs       | Better whole-app structure                        |
| [x] | **High**   | **TextArea (multiline editor)**                       | Input                      | Multi-line editing                   | "Real app" workflows beyond TextBox               |
| [x] | **High**   | **TreeView**                                          | Navigation                 | Hierarchical navigation              | Big UX upgrade over flat lists                    |
| [x] | **High**   | **TooltipHost / Tooltip**                             | Overlays                   | Contextual help everywhere           | Modern UX + pairs well with complex UIs           |
| [x] | **High**   | **BreakdownChart (segmented bar)**                    | Visualization              | Proportional parts of a whole        | Great dashboards + interactive UX                 |
| [x] | **High**   | **Inline prompt API**                                 | Input/API                  | Interactive prompts (inline)         | Great CLI UX without full apps                    |
| [x] | **High**   | **ColorPicker**                                       | Input                      | Picking colors + palettes            | Essential for theming/tools + great demo value    |
| [x] | **Medium** | **LoadingIndicator / Spinner**                        | Status                     | Busy state (unknown duration)        | Complements ProgressBar                           |
| [x] | **Medium** | **Collapsible / Accordion**                           | Layout                     | Progressive disclosure               | Keeps screens navigable                           |
| [x] | **Medium** | **Advanced Grid Layout**                              | Layout                     | 2D composition for forms/inspectors/dashboards with consistent alignment | "Killer" layout that removes deep nesting and covers most real screens |
| [x] | **Medium** | **MaskedInput (password/secret)**                     | Input                      | Auth + secrets                       | Often needed early                                |
| [x] | **Medium** | **OptionList (fast menu list)**                       | Navigation/input           | Command menus/quick pickers          | Base for palettes/menus                           |
| [x] | **Medium** | **Menu**                                              | Navigation/input           | Full menu                            | Menu                                              |
| [x] | **Medium** | **Command palette / quick open**                      | Navigation/input           | Searchable action launcher            | Great UX for larger apps                          |
| [x] | **Medium** | **Switch (toggle)**                                   | Input                      | Modern toggle UI                     | Nice settings affordance                          |
| [x] | **Medium** | **Sparkline**                                         | Visualization              | Tiny trends                          | High value / low scope                            |
| [x] | **Medium** | **Basic charts (bar/line)**                           | Visualization              | Dashboards/telemetry                 | Highly desirable                                  |
| [x] | **Medium** | **LogControl (scrolling log viewer)**                 | Status/diagnostics         | Live logs with search + copy         | Common need for apps, progress, and debugging     |
| [x] | **Medium** | **Links (clickable/open URL)**                        | Interaction/content        | Hyperlinks in terminal               | Terminal support varies                           |
| [x] | **Medium** | **ContentSwitcher (view routing)**                  | Layout/state               | Swap views without rebuild           | Great for navigation/wizards                      |
| [ ] | **Low**   | **Toast / Notification**                               | UX feedback                | Non-blocking feedback                | Modern UX; avoids modal spam                      |
| [ ] | **Low**    | **Digits / KPI big-number**                           | Visualization              | Dashboard counters                   | Mostly polish                                     |
| [ ] | **Low**    | **Placeholder / empty-state widget**                  | UX                         | No-results/loading states            | Polish; easy to fake                              |
| [x] | **Low**    | **Figlet / big text**                                 | Visual                     | Banners/headers                      | Fun, rarely essential                             |
| [x] | **Low**    | **Canvas / pixel surface**                            | Visualization              | Images/advanced plots                | Bigger scope; later                               |
| [ ] | **Low** | **DirectoryTree / File picker**                       | Navigation                 | File browsing                        | Popular in tool-like TUIs                         |
| [ ] | **Low** | **Calendar widget**                                   | Visualization/productivity | Schedules/date picking               | App-dependent                                     |
| [ ] | **Low** | **Log view (RichLog / styled log)**                   | Diagnostics                | Streaming logs + scroll + styling    | Huge value for devtools/ops TUIs                  |
| [ ] | **Low** | **Structured text viewer (syntax + JSON)**            | Content                    | Inspect JSON/configs                 | Great for tooling                                 |
| [ ] | **Low** | **DockWorkspace (dockable panes + floating windows)** | Layout/windowing           | Rearrangeable panes                  | Bigger feature, strong differentiator             |

## Add-on libraries after core (dependency-minimizing)

These controls will require additional dependencies, so they will be delivered as separate add-on libraries that depend on XenoAtom.Terminal.UI. This keeps the core library lightweight for users who don't need these features.

| [ ] | Component          | Depends on                  | Notes                                                                            |
| --- | ------------------ | --------------------------- | -------------------------------------------------------------------------------- |
| [ ] | **MarkdownViewer** | Markdig (or Markdown stack) | Keep core dependency-free; integrate via core rich-text spans + add-on renderer. |

Beyond the following specs, if there are improvements or features that would make sense for a control based on your experience implementing it, feel free to add them.

# High priority control specs

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

* **API**: `Select<T>` with `Items` (`BindableList<T>`), `SelectedIndex`, `ItemTemplate` (`DataTemplate<T>`)
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

* **Orientation**: horizontal
* **Variants**: Provide as many styles as possible (from ascii to modern unicode)
* **Layout**: can have labels at start/center/end. Use similar API than the Group control. (Always use Visual for labels, not strings)
* **Theming**: glyph set and style from theme

## Header / Footer (app chrome)

* **Docking**: fixed rows docked top/bottom; integrates with DockLayout
* **Regions**: left/center/right slots; supports dynamic updates
* **Key hints**: standard shortcut/hint area
* **Status hooks**: title, breadcrumbs, counters, clock (optional)
* **Focus policy**: typically non-focusable; optional focusable children

# Medium priority control specs

## Collapsible / Accordion

* **API**: `Header` (Visual), `Content` (Visual), `IsExpanded` (bool), optional `ExpandDirection` (Down/Up)
* **Accordion mode**: optional parent/group behavior to keep at most one expanded
* **Layout**: when collapsed, measure/arrange only header; when expanded, include content
* **Interaction**: click header toggles; keyboard Space/Enter toggles when focused
* **States**: normal/hover/focused/disabled styling for the header row
* **Glyphs**: style-driven expand/collapse glyph and spacing
* **Events**: `ExpandedChanged` (and/or `IsExpanded` bindable)

## Advanced Grid Layout (Grid)

* **Row/Column definitions**: `Fixed(n)`, `Auto`, `Star(weight)`
* **Min/Max constraints**: `Min`, `Max` per row/column (applies after sizing mode)
* **Gaps & padding**: `RowGap`, `ColumnGap`, container `Padding`
* **Cell placement**: `Row`, `Column`, `RowSpan`, `ColumnSpan` attached properties
* **Implicit grid growth**: auto-add rows/cols when placing outside defined bounds (configurable)
* **Named lines / areas**: optional `RowName[]`, `ColumnName[]`, and `Area` mapping for readability
* **Alignment**: per-cell `HorizontalAlignment` / `VerticalAlignment` + optional container defaults
* **Stretch behavior**: `Stretch` fills cell; respects child min/max sizing
* **Size distribution**: deterministic integer cell allocation; stable results under resize
* **Span resolution**: multi-pass sizing so spanning children influence `Auto` tracks sensibly
* **Shared size groups**: optional "SharedGroup" to equalize column widths across multiple grids (great for property panels)
* **Hit-testing / focus order**: cell-aware navigation ordering (row-major default; configurable)

## Password mode (TextBox)

* **API**: `IsPassword` (bool), `PasswordRevealMode` (Never/WhileFocused/Always), `ClipboardMode` (Disabled/CopyText)
* **Masking**: glyph configured by `TextBoxStyle.PasswordMaskGlyph`
* **Clipboard**: can disable copy/cut shortcuts (Ctrl+C / Ctrl+X) while still allowing paste
* **Reveal ideas**: optional "momentary reveal" (e.g. last typed char) as a behavior flag

## MaskedInput (template)

* **API**: `Template` (string), `Value` (string), `IsValid` (bool)
* **Template tokens**: per-position constraints (digits/letters/hex/etc.), plus case conversion directives (`>`, `<`, `!`)
* **Placeholder**: always-visible mask; default placeholder from `MaskedInputStyle.DefaultPlaceholderChar` and override via template suffix `;c`
* **Interaction**: focus, click to place caret, typing fills slots, backspace/delete clears
* **Clipboard**: Ctrl+C copies raw value; Ctrl+V pastes and fills next slots (skipping invalid chars)
* **Theming**: reuses input fill/padding logic from `TextBoxStyle` (via `MaskedInputStyle`)

## OptionList (fast menu list)

* **Item model**: `OptionListItem` with `Content` (Visual), optional `Description` (Visual), optional `Shortcut` (Visual), `IsEnabled`
* **Selection**: `SelectedIndex` bindable; keeps selection visible when scrolling
* **Keyboard**: Up/Down, PageUp/Down, Home/End; Enter/Space activates
* **Search**: type-to-filter or type-to-jump (configurable), highlight match
* **Mouse**: click selects/activates; wheel scroll; hover highlight
* **Rendering**: virtualization for large lists; spacing between marker/icon/text is style-driven
* **Events**: `SelectionChanged`, `ItemActivated`

## Menu

* **Model**: `MenuItem` tree (header Visual, optional icon Visual, optional shortcut Visual, `IsEnabled`, optional checked state)
* **Surfaces**: `MenuBar` (top-level) + popup submenu panels; separators supported
* **Keyboard**: Alt focuses menu bar; arrows navigate; Enter/Space activates; Esc closes; type-to-jump within open menu
* **Mouse**: click to open/activate; hover opens adjacent; click outside closes
* **Commands**: per-item `Action` callback (optionally async later); disabled items ignored
* **Theming**: focus/hover/pressed colors; separator glyphs; submenu arrow glyph; padding/spacing

## Switch (toggle)

* **API**: `IsOn` (bool), optional `Content` (Visual), optional `IsThreeState` later
* **Interaction**: Space/Enter toggles; Left/Right sets Off/On; mouse click toggles
* **Rendering**: style-driven track/thumb glyphs (and optional On/Off labels); width measured from glyphs + content
* **States**: normal/hover/focused/disabled styling, including distinct pressed visual
* **Events**: `Toggled` (and/or `IsOn` bindable)

## LogControl (scrolling log viewer)

* **Goal**: a high-performance scrolling log viewer that behaves like “tail -f”, supports appending at high frequency, and still allows selection/copy and search.
* **Core API**
  * `AppendLine(string message)`
  * `AppendMarkupLine(string markup)`
  * `AppendMarkupLine(ref AnsiMarkupInterpolatedStringHandler handler)`
  * `Clear()`
  * `Search(string searchText)`
* **Capacity**
  * `MaxCapacity` (int, optional): limits number of retained entries; when exceeded, oldest entries are removed.
  * Optional future extension: “max total characters” or “max bytes” (more predictable memory cap).
* **Auto-scroll / follow mode**
  * Default: “follow tail” enabled; appending scrolls to bottom.
  * User scroll/selection temporarily disables follow mode; re-enabled by End key / explicit command (future: a small “Follow” toggle).
* **Wrapping**
  * `WrapText` (bool): controls whether long lines wrap; affects measurement and vertical extent.
* **Selectable/copyable**
  * Supports keyboard/mouse selection and copy.
  * Copy operation exports plain text (markup stripped); selected ranges can span multiple entries.
* **Keyboard + mouse navigation**
  * Mouse wheel scroll; trackpad deltas mapped to line scroll.
  * Arrow keys, PageUp/PageDown, Home/End scroll or move caret/selection depending on selection state.
  * Optional: Ctrl+F focuses search box (if present) or starts search mode.
* **Markup support**
  * Supports ANSI markup for display formatting (bold/italic/colors).
  * For selection/search, keep a plain-text representation per entry (used for hit-testing, copying, and match computation).
  * Search highlighting applies on top of markup (overlay highlight style), without mutating original markup.
* **Built-in search (required for v1)**
  * **Shortcut**: Ctrl+F opens the search UI (Esc closes it). Search UI is part of the control (not external).
  * **UI**: a small popup overlay anchored to the log viewer (top-right recommended) showing:
    * Search input box
    * Match count (e.g. `12/57` for current match / total matches, or `0 matches`)
    * Next/Previous controls (buttons and/or key gestures)
    * Toggles/options: Case sensitive, Whole word, Regex
  * **API**:
    * `Search(string)` sets the current query, computes matches, and highlights them.
    * `SearchText` (string?) bindable is recommended so apps can drive/search externally if needed.
    * `SearchOptions` (flags) includes CaseSensitive / WholeWord / Regex.
    * `GoToNextMatch()` / `GoToPreviousMatch()` scrolls to the match and sets the active match index.
  * **Key gestures** (suggested defaults):
    * Ctrl+F: open search (focus search box)
    * Enter / F3: next match
    * Shift+Enter / Shift+F3: previous match
    * Esc: close search
  * **Behavior**:
    * Searching highlights all matches; “active match” is highlighted more strongly.
    * Navigating matches scrolls to reveal the match and may temporarily disable follow-tail.
    * Regex errors should be shown inline in the search UI (e.g. “invalid regex”) and matches treated as 0.
* **Rendering and performance**
  * Virtualizes entries (only materialize visuals for visible viewport + small overscan).
  * Reuse/pool visuals and parsed markup runs where possible.
  * Avoid per-append full rebuild; append should be O(1) for data + amortized minimal invalidation.
* **Integration**
  * Prefer composition over custom rendering where possible:
    * A `ScrollViewer` hosting a virtualized stack of entry visuals (likely a specialized internal list).
  * The log viewer itself owns scrolling even without explicit ScrollViewer wrapping (it should be usable standalone).
* **Optional future features**
  * Severity levels (info/warn/error) with per-entry styles.
  * Timestamps and/or source/category columns (structured log entries).
  * Filtering by text/severity.
  * “Pause” mode to stop auto-append rendering while still buffering entries.
  * Export to file / copy-with-styles (rich text) if terminal supports it.
* **Styling**
  * The main log viewport style should mirror `TextAreaStyle` (padding, input fill, focused/hovered states, selection styling), except that placeholder concepts are not relevant.
  * Search UI should have its own style (derived from popup/dialog conventions) and expose:
    * Highlight styles for matches (match/all-matches/active-match)
    * Colors for search chrome and error message

## Sparkline

* **Data**: numeric series (list/span), optional explicit `Min`/`Max` (auto-scale by default)
* **Rendering**: style chooses rune set (blocks/braille/shades), optional gradient/threshold coloring
* **Size**: default 1 row; optional multi-row for denser plots
* **Markers**: optional current/min/max marker glyphs (style-driven)
* **Performance**: avoids allocations per render; caches computed frames when input unchanged

## Basic charts (bar/line)

* **Controls**: `BarChart` (horizontal/vertical) and `LineChart` (optionally builds on Sparkline logic)
* **Data**: single series for MVP; extensible to multi-series and stacked bars
* **Labels**: optional axis labels/titles/legend via Visual slots
* **Scaling**: auto-scale + clamp; supports fixed range
* **Theming**: per-series colors; gridline/axis glyphs; compact terminals-first layout rules

## Links (clickable/open URL)

* **API**: `Uri`/`Href` + `Content` (Visual)
* **Output**: emits terminal hyperlink sequences when supported; otherwise styled fallback (underline/primary color)
* **Interaction**: mouse click activates; keyboard Enter activates when focused; hover styling
* **Behavior**: `OnOpen` callback; optional copy-to-clipboard helper action
* **Accessibility**: focus rectangle/underline; tooltip-like display (optional later)

## ContentSwitcher (view routing)

* **API**: content collection + `SelectedIndex`/`SelectedKey` bindable
* **Layout**: only selected child is measured/arranged/rendered
* **Lifetime**: optional `KeepAlive` (cache created children) vs recreate on switch
* **Navigation**: integrates well with Tabs/Menu; supports programmatic switching
* **Events**: `SelectionChanged`

## Command palette / quick open

* **Composition**: search input + results list (reuse OptionList); works as popup or embedded
* **Providers**: async-friendly item source with cancellation; supports grouping/categories later
* **Search**: fuzzy matching + highlighting; configurable minimum query length
* **Keyboard**: open command (host-defined), Esc closes, Up/Down navigate, Enter runs, Ctrl+Enter reserved
* **Mouse**: click selects/runs; click outside closes when used as popup
* **UX**: optional "recent" + history; empty state Visual; busy state spinner integration
