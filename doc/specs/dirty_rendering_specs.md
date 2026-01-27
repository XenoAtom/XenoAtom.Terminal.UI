# Dirty Rendering (Internal Spec)

This document explores options to reduce the CPU cost of rendering when only a small part of the visual tree changes.

It is **internal** documentation for framework contributors. It intentionally focuses on correctness and minimal-complexity
approaches that fit the existing **binding/dependency tracking** and **CellBuffer diff rendering** model.

## 1. Context

Today, `TerminalApp.Render()`:

- Runs layout only when needed (measure/arrange dirtiness driven by binding tracking).
- Always performs a **full tree render** into a `CellBuffer` when a frame render is requested.
- Clears the whole render buffer each frame (`buffer.Clear(theme.BaseTextStyle())`).
- Relies on `CellBufferDiffRenderer` to minimize terminal writes (cell-diff).

This keeps terminal output efficient, but it still runs a significant amount of **UI traversal + rendering code** on
every frame, even when the visual change is localized (spinner tick, caret blink, hover change, etc.).

## 2. Goals

- Reduce CPU cost of rendering for localized visual updates.
- Avoid manual invalidation APIs from controls (keep the binding-driven dirty model).
- Keep rendering correctness with:
  - visuals that do **not** paint their full bounds,
  - z-order/overlap (popups, dialogs),
  - alpha blending (future/optional).

## 3. Non-goals (for V1 of this feature)

- Do not require controls to always paint their full bounds.
- Do not introduce a per-control retained “layer cache” (memory heavy, complex invalidation).
- Do not change the external rendering contract of `RenderOverride(CellBuffer)` (controls remain unaware).

## 4. Why “Render dirtiness” is different from Measure/Arrange

Measure/Arrange can be skipped for subtrees because they do not depend on previously rendered pixels.

Rendering, however, is currently done by:

1. Clearing the full buffer to a base style.
2. Repainting the entire scene.

If we clear the whole buffer and then skip rendering parts of the tree, we would end up with missing output.
So any “render dirty” optimization must either:

- stop clearing the whole buffer (retain previous frame), or
- keep full repaint.

Therefore, a practical optimization is usually based on **dirty regions** (rectangles) and **incremental buffer
updates**, not only “dirty flags”.

## 5. Options

### Option A — Full-frame repaint (current behavior)

**Summary**: Always clear the full buffer and render the full tree.

**Pros**
- Simple, robust.
- No additional invalidation complexity.
- Correct for all controls (even those that paint partially).

**Cons**
- Tree traversal + rendering runs for every render request.

### Option B — Incremental rendering into a persistent buffer (recommended)

**Summary**: Keep the render buffer as “current frame”, and on each render only repaint *dirty rectangles* by:

1. Clearing the dirty area (not the whole buffer).
2. Rendering the scene clipped to that dirty region, starting from the root (to preserve z-order/overlap).

**Pros**
- Keeps the existing rendering contract (controls are unchanged).
- Correct for partial-paint controls because repaint is from the root (not only the dirty control).
- Can drastically reduce work when dirty area is small.

**Cons**
- Requires tracking dirty rectangles (at least a union rect).
- Requires correctness rules for “layout changes” (old/new bounds).

### Option C — Only render dirty subtrees (not recommended)

**Summary**: Render only the dirty visual and its children directly into the existing buffer.

**Problem**: This is only correct if every visual paints its entire bounds (including background). That is not true for
many controls (e.g., `TextBlock` typically paints glyphs only), so stale pixels remain.

This option is not recommended unless the framework enforces a “fully paints bounds” rule.

### Option D — Retained sub-buffer per visual (“layers”)

**Summary**: Each visual renders into its own buffer and is composited into the parent.

**Pros**
- Potentially very fast for large trees with small updates.

**Cons**
- High memory cost.
- Complex invalidation, clipping, and composition.
- Hard to make it allocation-free and simple.

Not recommended for now.

## 6. Proposed approach (Option B)

### 6.1. Render buffer lifecycle

Treat `_renderBuffer` as the authoritative “current frame” buffer:

- On startup / resize / full invalidation, do a full clear + full repaint.
- Otherwise, update it incrementally by clearing + repainting only dirty rectangles.

`CellBufferDiffRenderer` continues to diff the buffer against its internal “last frame” state.

### 6.2. Dirty region representation

Start minimal:

- Track a single `Rectangle DirtyUnion`.

Then optionally evolve:

- Track `List<Rectangle>` (bounded count), unioning when too many.
- Track a “full repaint” flag when the dirty area exceeds a threshold.

Recommended heuristics:

- If more than `N` dirty rects, union them.
- If dirty area > ~30–50% of viewport, fall back to full repaint.

### 6.3. When to mark “full repaint”

Full repaint is the safe fallback when:

- The render buffer is reallocated (terminal resize).
- Any visual is re-arranged in a way that could relocate content broadly.
- The tree structure changes significantly (attach/detach many visuals).

Pragmatic rule:

> If measure or arrange runs during the frame, perform full repaint.

This keeps the incremental optimization targeted at **render-only** updates (caret, animation, hover, small content
changes).

### 6.4. Render-only dirty rectangle sources

When a binding write affects render-only dependencies:

- `TerminalApp.ProcessBindingWrites()` already identifies impacted visuals via the render dependency index.
- For each impacted visual, add `visual.Bounds` to the dirty union.

If the visual might have moved since last frame (rare without layout), this is still safe because:

- Under the “full repaint if arrange ran” rule, moves come from arrange and trigger full repaint.

### 6.5. Layout dirty rectangle sources (future refinement)

If we want to avoid “full repaint on arrange” later, we must track old and new bounds:

- Store `Visual.LastArrangedBounds` (internal) and update it after a successful arrange.
- When bounds change, mark dirty union with `oldBounds ∪ newBounds`.

This is optional and can be deferred until the render-only optimization is stable.

### 6.6. How to repaint a dirty region correctly

Algorithm for render-only repaint:

1. Push a clip rect equal to dirty region (intersection with viewport).
2. Clear the dirty region in `_renderBuffer` to the base style.
3. Call `Root.RenderTree(_renderBuffer)` with that clip active.

Important: we render **from root** so that overlap/z-order is correct.

### 6.7. Minimizing tree traversal work

Update `Visual.RenderTree` to early-out:

- If `Bounds` does not intersect the current `CellBuffer` clip rectangle, skip:
  - calling `RenderOverride`,
  - recursing into children,
  - dependency tracking for render.

This preserves correctness and reduces CPU time significantly when repainting a small region.

### 6.8. Render dependencies and skipping work

When a visual is skipped because it is outside the dirty region:

- Its render dependencies should not be recomputed.
- The previous dependency set remains valid until a binding write marks the visual dirty again.

This aligns with the existing dependency model used for measure/arrange/prepare children.

## 7. Interaction with CellBufferDiffRenderer

### 7.1. Baseline behavior

Even if rendering becomes incremental, the diff renderer can still diff the full buffer.

This preserves correctness and keeps the optimization localized to rendering CPU time.

### 7.2. Optional future optimization

If necessary later, the diff renderer can accept a dirty region and only scan those cells. This should be considered a
separate optimization step because it complicates cursor/color state reconstruction.

## 8. Fullscreen vs inline host notes

- Fullscreen host: viewport is fixed and a full repaint is always possible.
- Inline host: the live region is anchored and can move due to terminal scrolling; incremental repaint can still work
  but must be coordinated with host anchoring logic.

This spec focuses on the core rendering optimization. Host-specific behavior should be addressed separately.

## 9. Risk assessment / complexity

**Low risk**
- Early-out render traversal when clip does not intersect bounds.

**Medium risk**
- Incremental repaint for render-only updates (requires careful buffer lifecycle and dirty tracking).

**High risk**
- Avoiding full repaint on layout changes (requires old/new bounds tracking and more complex invalidation).

## 10. Recommendation

Implement Option B in two stages:

1. **Stage 1 (safe wins)**:
   - Add bounds/clip intersection early-outs in `RenderTree`.
   - Introduce incremental repaint only for render-only frames (full repaint when measure/arrange ran).
2. **Stage 2 (optional)**:
   - Track old/new bounds to reduce full repaint on layout changes.
   - Consider diff-scanning only dirty regions if profiling shows it matters.

## 11. Current implementation status (Stage 1 applied)

Stage 1 has been implemented with the following concrete changes:

- `CellBuffer`:
  - Exposes the current clip via `CurrentClipRect` and provides `ClipIntersects(Rectangle)`.
  - Adds `ClearCurrentClip(Style)` to clear only the current clip region without clearing hyperlink/text tables.
    This is required for incremental repaint while keeping scalar tokens stable for cells outside the dirty region.

- `Visual.RenderTree(CellBuffer)`:
  - Early-outs before any work when `Bounds` does not intersect the current buffer clip.
  - This reduces traversal/render cost during clipped repaint passes.

- `TerminalApp.Render()`:
  - Tracks a pending dirty rectangle union for render-only invalidations, based on render dependency reads.
  - Uses a “full repaint” fallback when:
    - the viewport size changes,
    - layout ran or produced writes,
    - any layout-impacting bindings were written (prepare/dynamic/measure/arrange),
    - or no dirty region is known for the render request.
  - Otherwise performs incremental repaint:
    - clip to the dirty region,
    - clear only that region to the base style,
    - call `Root.RenderTree(buffer)` with the clip active.

Notes:

- Dirty regions are expanded by 1 cell horizontally to reduce artifacts with wide glyphs at region boundaries.
- Debug overlay currently forces a full repaint (the overlay draws a region that isn’t tracked via bindings).
