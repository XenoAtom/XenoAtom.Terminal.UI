# CodeEditor implementation plan

This checklist translates `site/docs/specs/code_editor_specs.md` into an implementation-oriented work plan.

## 0. Spec review and scope lock

- [x] Review `site/docs/specs/code_editor_specs.md` and confirm the initial implementation scope.
- [x] Decide which items are **phase 1 required** vs explicitly deferred.
- [x] Confirm naming for the public API surface before any implementation starts.
- [x] Confirm whether `CodeEditor` ships in a single PR or in multiple staged PRs.

## 1. Core public API skeleton

- [x] Add `CodeEditor : TextEditorBase` in `src/XenoAtom.Terminal.UI/Controls/`.
- [x] Add XML docs for all public types and members.
- [x] Add initial bindable properties:
  - [x] `ShowLineNumbers`
  - [x] `MinLineNumberDigits`
  - [x] `HighlightCurrentLine`
- [x] Add the simple highlighter hook:
  - [x] `CodeEditorLineHighlightRequest`
  - [x] `CodeEditorLineHighlighter`
  - [x] `Highlighter` bindable delegator
- [x] Add the advanced syntax-highlighting contracts:
  - [x] `CodeEditorSyntaxState`
  - [x] `CodeEditorSyntaxHighlighter`
  - [x] optional async interface (`IAsyncCodeEditorSyntaxHighlighter` or equivalent)
  - [x] syntax build/update request structs
  - [x] line-runs request struct
- [x] Add margin contracts:
  - [x] `CodeEditorMarginSide`
  - [x] `CodeEditorVisibleLine`
  - [x] `CodeEditorMargin`
  - [x] margin measure/render/pointer context types
- [x] Add left/right margin collections:
  - [x] `LeftMargins`
  - [x] `RightMargins`

## 2. Styling infrastructure

- [x] Add `CodeEditorStyle` under `src/XenoAtom.Terminal.UI/Styling/`.
- [x] Cover baseline styles:
  - [x] editor background
  - [x] text style
  - [x] selection style reuse / integration
  - [x] current-line style
  - [x] margin background
  - [x] line-number style
  - [x] current-line line-number style
  - [x] optional margin separator style
- [x] Add sensible defaults consistent with existing editor controls.

## 3. Basic control shell and layout

- [x] Implement `MeasureCore` for `CodeEditor`.
- [x] Implement `ArrangeCore` for `CodeEditor`.
- [x] Split arranged bounds into:
  - [x] left margin strip
  - [x] text surface
  - [x] right margin strip
- [x] Ensure the text surface width is the only width passed into the text editor layout engine.
- [x] Ensure margins do not horizontally scroll with the editor text.
- [x] Ensure margins remain vertically aligned with visible wrapped rows.

## 4. Default left line-number margin

- [x] Implement a built-in line-number margin.
- [x] Enable line numbers by default.
- [x] Render numbers only on the first wrapped row of a logical line.
- [x] Render continuation wrapped rows as blank by default.
- [x] Add current-line emphasis for the active line number.
- [x] Implement adaptive width based on the **visible** line range.
- [x] Add `MinLineNumberDigits` support.
- [x] Ensure width changes only when the visible digit bucket changes.
- [ ] Ensure line-number width changes trigger only the minimal required layout refresh.

## 5. Margin infrastructure

- [x] Implement ordered margin rendering for the left side.
- [x] Implement ordered margin rendering for the right side.
- [x] Make margin contexts expose enough information for external extensions:
  - [x] visible wrapped row mapping
  - [x] owning logical line
  - [x] first-row-of-line flag
  - [x] current-line / focus state
  - [x] theme / style access
- [x] Add pointer routing support for margins.
- [x] Verify margins can be implemented from another assembly without requiring them to be `Visual`s.
- [x] Add at least one sample/test custom margin beyond line numbers.

## 6. Integrate with `TextEditorCore.LayoutCache`

- [x] Identify the minimal additional data `CodeEditor` needs from `TextEditorCore`.
- [x] Expose row-to-line mapping needed by margins without duplicating wrap logic.
- [x] Expose wrapped-segment lookup needed for visible rendering without rescanning full lines.
- [x] Ensure `CodeEditor` never reimplements row wrapping outside `TextEditorCore.LayoutCache`.
- [x] Ensure viewport width changes refresh wrap layout but do not invalidate unrelated syntax state.
- [x] Add diagnostics/hooks for tests if needed (similar to current layout diagnostics patterns).

## 7. Simple syntax-highlighting path

- [x] Implement the simple `Highlighter` delegate path first.
- [x] Define highlight runs relative to logical lines, not wrapped rows.
- [x] Normalize/merge overlapping runs where needed.
- [x] Apply highlighting only to visible wrapped segments.
- [ ] Ensure syntax styles compose correctly with:
  - [x] selection
  - [x] search highlights
  - [x] current-line background
- [x] Keep the render path allocation-conscious.
- [x] Verify scrolling does not recompute simple highlighting outside visible lines.

## 8. Advanced incremental syntax-highlighting infrastructure

- [x] Add persistent syntax-state storage on the editor.
- [x] Track syntax-state snapshot version.
- [x] Integrate document change notifications with syntax-state update scheduling.
- [x] Define diff/update input using document change information.
- [ ] Implement the rule: start from the first affected line and continue until line state stabilizes.
- [x] Add line-run lookup by logical line using cached syntax state.
- [x] Ensure pure scrolling never triggers a full syntax rebuild.
- [x] Add fallback behavior when no syntax state is yet available.

## 9. Optional async highlighter support

- [x] Define version-gated async application of syntax state.
- [x] Ensure stale async results are discarded.
- [x] Ensure typing/scrolling is responsive while async work is running.
- [x] Define cancellation or replacement policy for outdated async tasks.
- [x] Add tests for version gating and stale result discard.

## 10. Current-line rendering

- [x] Add current-line background rendering in the text surface.
- [x] Add current-line emphasis in the line-number margin.
- [x] Ensure current-line styling composes correctly with syntax highlighting and selection.

## 11. Search / inherited editor features validation

- [x] Verify `TextEditorBase` search popup integration still behaves correctly under `CodeEditor` chrome.
- [x] Verify clipboard operations still behave correctly.
- [x] Verify undo/redo still behaves correctly.
- [x] Verify cursor placement stays correct when margins are present.
- [x] Verify horizontal scrolling behavior when wrapping is disabled.

## 12. Performance validation for large files / long lines

- [ ] Add tests for very large documents with many logical lines.
- [ ] Add tests for a single extremely long wrapped line.
- [ ] Add tests that scrolling only touches visible lines plus bounded cache windows.
- [ ] Add tests that syntax-highlighting does not re-highlight the whole document on scroll.
- [x] Add tests that viewport width changes reuse syntax state and only refresh wrapping.
- [ ] Add tests that line-number width does not reserve large width near the start of a huge file.
- [ ] Add tests that crossing a digit bucket updates margin width correctly.
- [ ] Add tests for custom left/right margins with wrapped lines.

## 13. Samples

- [ ] Add a small `CodeEditor` sample to `samples/ControlsDemo` or another suitable sample.
- [ ] Demonstrate:
  - [ ] line numbers
  - [ ] a simple custom margin
  - [ ] a simple syntax highlighter
  - [ ] very large document scrolling
- [ ] Optionally add a playground/demo scenario specifically for long files.

## 14. Documentation

- [ ] Update end-user docs when the control exists.
- [ ] Add a user-facing control page under `site/docs/controls/`.
- [ ] Update `site/docs/readme.md` controls/spec references as needed.
- [ ] Keep `site/docs/specs/code_editor_specs.md` aligned with the implemented API.

## 15. Possible phase cuts

### Phase 1

- [x] `CodeEditor` shell
- [x] line numbers
- [x] left/right margin infrastructure
- [x] simple synchronous highlighter

### Phase 2

- [x] advanced incremental syntax-highlighting state
- [x] async provider support
- [x] stronger performance diagnostics/tests

### Phase 3

- [ ] richer built-in margins (diff markers, breakpoints, diagnostics)
- [ ] ecosystem package consuming the public contracts

## 16. Explicitly deferred items

- [ ] Folding implementation
- [ ] Multi-caret / rectangular selection
- [ ] Minimap
- [ ] LSP integration
- [ ] Bundled language packs in the core package

## Completion criteria for the first shippable version

- [ ] `CodeEditor` exists as a supported control in `XenoAtom.Terminal.UI`.
- [ ] It derives from `TextEditorBase` and reuses `TextEditorCore`.
- [ ] Line numbers are enabled by default and adapt to the visible range.
- [ ] Margins are pluggable on both left and right.
- [ ] A simple syntax highlighter can be plugged in.
- [ ] The infrastructure for advanced incremental syntax highlighting is public and usable.
- [ ] Large-file / very-long-line behavior remains aligned with current text editor performance optimizations.
- [ ] Tests cover correctness and core performance regression scenarios.
