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
| [x] | **Medium** | **LoadingIndicator / Spinner**                        | Status                     | Busy state (unknown duration)        | Complements ProgressBar                           |
| [x] | **Medium** | **Collapsible / Accordion**                           | Layout                     | Progressive disclosure               | Keeps screens navigable                           |
| [ ] | **Medium** | **Advanced Grid Layout**                              | Layout                     | 2D composition for forms/inspectors/dashboards with consistent alignment | "Killer" layout that removes deep nesting and covers most real screens |
| [ ] | **Medium** | **MaskedInput (password/secret)**                     | Input                      | Auth + secrets                       | Often needed early                                |
| [ ] | **Medium** | **OptionList (fast menu list)**                       | Navigation/input           | Command menus/quick pickers          | Base for palettes/menus                           |
| [ ] | **Medium** | **Menu**                                              | Navigation/input           | Full menu                            | Menu                                              |
| [x] | **Medium** | **Switch (toggle)**                                   | Input                      | Modern toggle UI                     | Nice settings affordance                          |
| [ ] | **Medium** | **Sparkline**                                         | Visualization              | Tiny trends                          | High value / low scope                            |
| [ ] | **Medium** | **Basic charts (bar/line)**                           | Visualization              | Dashboards/telemetry                 | Highly desirable                                  |
| [ ] | **Medium** | **Links (clickable/open URL)**                        | Interaction/content        | Hyperlinks in terminal               | Terminal support varies                           |
| [x] | **Medium** | **ContentSwitcher (view routing)**                  | Layout/state               | Swap views without rebuild           | Great for navigation/wizards                      |
| [ ] | **Medium** | **Command palette / quick open**                      | Navigation                 | Searchable actions                   | Big productivity boost                            |
| [ ] | **Low**   | **Toast / Notification**                               | UX feedback                | Non-blocking feedback                | Modern UX; avoids modal spam                      |
| [ ] | **Low**    | **Digits / KPI big-number**                           | Visualization              | Dashboard counters                   | Mostly polish                                     |
| [ ] | **Low**    | **Placeholder / empty-state widget**                  | UX                         | No-results/loading states            | Polish; easy to fake                              |
| [ ] | **Low**    | **Figlet / big text**                                 | Visual                     | Banners/headers                      | Fun, rarely essential                             |
| [ ] | **Low**    | **Canvas / pixel surface**                            | Visualization              | Images/advanced plots                | Bigger scope; later                               |
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

## MaskedInput (password/secret)

* **API**: `Text` (string), `MaskGlyph` (Rune/string), `RevealMode` (Never/WhileFocused/Always), `Placeholder` (Visual?)
* **Editing**: TextBox-like movement/selection/clipboard; respects max length; optional allowed character filter
* **Reveal**: optional "momentary reveal" (e.g. last typed char) as a style/behavior flag
* **Clipboard**: configurable copy/cut behavior (copy real text vs disabled)
* **Interaction**: focus, mouse click to set caret, keyboard editing, paste
* **Theming**: same surface/border/caret/selection model as TextBox

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
