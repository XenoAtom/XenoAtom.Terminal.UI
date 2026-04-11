# CodeEditor implementation plan

This checklist translates `site/docs/specs/code_editor_specs.md` into an implementation-oriented work plan.

## 0. Spec review and scope lock

- [x] Review `site/docs/specs/code_editor_specs.md` and confirm the initial implementation scope.
- [x] Decide which items are **phase 1 required** vs explicitly deferred.
- [x] Confirm naming for the public API surface before any implementation starts.
- [x] Confirm whether `CodeEditor` ships in a single PR or in multiple staged PRs.

## 1. Core public API skeleton

- [ ] Add `CodeEditor : TextEditorBase` in `src/XenoAtom.Terminal.UI/Controls/`.
- [ ] Add XML docs for all public types and members.
- [ ] Add initial bindable properties:
  - [ ] `ShowLineNumbers`
  - [ ] `MinLineNumberDigits`
  - [ ] `HighlightCurrentLine`
- [ ] Add the simple highlighter hook:
  - [ ] `CodeEditorLineHighlightRequest`
  - [ ] `CodeEditorLineHighlighter`
  - [ ] `Highlighter` bindable delegator
- [ ] Add the advanced syntax-highlighting contracts:
  - [ ] `CodeEditorSyntaxState`
  - [ ] `CodeEditorSyntaxHighlighter`
  - [ ] optional async interface (`IAsyncCodeEditorSyntaxHighlighter` or equivalent)
  - [ ] syntax build/update request structs
  - [ ] line-runs request struct
- [ ] Add margin contracts:
  - [ ] `CodeEditorMarginSide`
  - [ ] `CodeEditorVisibleLine`
  - [ ] `CodeEditorMargin`
  - [ ] margin measure/render/pointer context types
- [ ] Add left/right margin collections:
  - [ ] `LeftMargins`
  - [ ] `RightMargins`

## 2. Styling infrastructure

- [ ] Add `CodeEditorStyle` under `src/XenoAtom.Terminal.UI/Styling/`.
- [ ] Cover baseline styles:
  - [ ] editor background
  - [ ] text style
  - [ ] selection style reuse / integration
  - [ ] current-line style
  - [ ] margin background
  - [ ] line-number style
  - [ ] current-line line-number style
  - [ ] optional margin separator style
- [ ] Add sensible defaults consistent with existing editor controls.

## 3. Basic control shell and layout

- [ ] Implement `MeasureCore` for `CodeEditor`.
- [ ] Implement `ArrangeCore` for `CodeEditor`.
- [ ] Split arranged bounds into:
  - [ ] left margin strip
  - [ ] text surface
  - [ ] right margin strip
- [ ] Ensure the text surface width is the only width passed into the text editor layout engine.
- [ ] Ensure margins do not horizontally scroll with the editor text.
- [ ] Ensure margins remain vertically aligned with visible wrapped rows.

## 4. Default left line-number margin

- [ ] Implement a built-in line-number margin.
- [ ] Enable line numbers by default.
- [ ] Render numbers only on the first wrapped row of a logical line.
- [ ] Render continuation wrapped rows as blank by default.
- [ ] Add current-line emphasis for the active line number.
- [ ] Implement adaptive width based on the **visible** line range.
- [ ] Add `MinLineNumberDigits` support.
- [ ] Ensure width changes only when the visible digit bucket changes.
- [ ] Ensure line-number width changes trigger only the minimal required layout refresh.

## 5. Margin infrastructure

- [ ] Implement ordered margin rendering for the left side.
- [ ] Implement ordered margin rendering for the right side.
- [ ] Make margin contexts expose enough information for external extensions:
  - [ ] visible wrapped row mapping
  - [ ] owning logical line
  - [ ] first-row-of-line flag
  - [ ] current-line / focus state
  - [ ] theme / style access
- [ ] Add pointer routing support for margins.
- [ ] Verify margins can be implemented from another assembly without requiring them to be `Visual`s.
- [ ] Add at least one sample/test custom margin beyond line numbers.

## 6. Integrate with `TextEditorCore.LayoutCache`

- [ ] Identify the minimal additional data `CodeEditor` needs from `TextEditorCore`.
- [ ] Expose row-to-line mapping needed by margins without duplicating wrap logic.
- [ ] Expose wrapped-segment lookup needed for visible rendering without rescanning full lines.
- [ ] Ensure `CodeEditor` never reimplements row wrapping outside `TextEditorCore.LayoutCache`.
- [ ] Ensure viewport width changes refresh wrap layout but do not invalidate unrelated syntax state.
- [ ] Add diagnostics/hooks for tests if needed (similar to current layout diagnostics patterns).

## 7. Simple syntax-highlighting path

- [ ] Implement the simple `Highlighter` delegate path first.
- [ ] Define highlight runs relative to logical lines, not wrapped rows.
- [ ] Normalize/merge overlapping runs where needed.
- [ ] Apply highlighting only to visible wrapped segments.
- [ ] Ensure syntax styles compose correctly with:
  - [ ] selection
  - [ ] search highlights
  - [ ] current-line background
- [ ] Keep the render path allocation-conscious.
- [ ] Verify scrolling does not recompute simple highlighting outside visible lines.

## 8. Advanced incremental syntax-highlighting infrastructure

- [ ] Add persistent syntax-state storage on the editor.
- [ ] Track syntax-state snapshot version.
- [ ] Integrate document change notifications with syntax-state update scheduling.
- [ ] Define diff/update input using document change information.
- [ ] Implement the rule: start from the first affected line and continue until line state stabilizes.
- [ ] Add line-run lookup by logical line using cached syntax state.
- [ ] Ensure pure scrolling never triggers a full syntax rebuild.
- [ ] Add fallback behavior when no syntax state is yet available.

## 9. Optional async highlighter support

- [ ] Define version-gated async application of syntax state.
- [ ] Ensure stale async results are discarded.
- [ ] Ensure typing/scrolling is responsive while async work is running.
- [ ] Define cancellation or replacement policy for outdated async tasks.
- [ ] Add tests for version gating and stale result discard.

## 10. Current-line rendering

- [ ] Add current-line background rendering in the text surface.
- [ ] Add current-line emphasis in the line-number margin.
- [ ] Ensure current-line styling composes correctly with syntax highlighting and selection.

## 11. Search / inherited editor features validation

- [ ] Verify `TextEditorBase` search popup integration still behaves correctly under `CodeEditor` chrome.
- [ ] Verify clipboard operations still behave correctly.
- [ ] Verify undo/redo still behaves correctly.
- [ ] Verify cursor placement stays correct when margins are present.
- [ ] Verify horizontal scrolling behavior when wrapping is disabled.

## 12. Performance validation for large files / long lines

- [ ] Add tests for very large documents with many logical lines.
- [ ] Add tests for a single extremely long wrapped line.
- [ ] Add tests that scrolling only touches visible lines plus bounded cache windows.
- [ ] Add tests that syntax-highlighting does not re-highlight the whole document on scroll.
- [ ] Add tests that viewport width changes reuse syntax state and only refresh wrapping.
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

- [ ] `CodeEditor` shell
- [ ] line numbers
- [ ] left/right margin infrastructure
- [ ] simple synchronous highlighter

### Phase 2

- [ ] advanced incremental syntax-highlighting state
- [ ] async provider support
- [ ] stronger performance diagnostics/tests

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
