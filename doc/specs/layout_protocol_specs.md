---
title: Layout Protocol Specification + Control Guidance
---

# Layout Protocol Specification + Control Guidance

Below is the **full, integrated specification** for the `LayoutConstraints + SizeHints` protocol, updated per your requirements:

* **Unbounded max = `int.MaxValue`**
* **All math stays in `int`**
* **Validation is enforced via runtime exceptions in Release** (no "debug-only asserts")
* **HorizontalAlignment / VerticalAlignment** (including `Stretch`) are integrated correctly

After the spec, you'll find **control-by-control guidance**, grouped by shared behavior, plus **recommended default alignments** for your library.

---

# Layout Protocol Specification (Terminal UI, Integer Coordinates)

## 1. Goals

1. Prevent "infinite desired size" propagation (even when measuring unbounded).
2. Represent **Fill/Stretch** without requiring children to return the available size from Measure.
3. Support ScrollViewer (extent vs viewport), lists, grids, and wrapping text.
4. Use **integer coordinates** and integrate with your existing `Size` and `Rectangle` types.
5. Enforce invariants with **runtime exceptions** (Release included).

---

## 2. Required geometry assumptions

You already have:

* `Size` (integer width/height)

  * Width/Height are **non-negative**
* `Rectangle` (integer x/y/width/height)

  * Width/Height are **non-negative**
  * X/Y are integer positions

This spec refers to conceptual members `Width`, `Height`, `X`, `Y`. Map to your actual names.

---

## 3. Constants and runtime validation

### 3.1 Unbounded maximum

* `Unbounded` / "infinite" maximum is represented by `int.MaxValue`.

### 3.2 Runtime validation and exceptions

**MUST**: If any invariant in this spec is violated, throw a runtime exception (Release included).

Recommended exception types:

* `LayoutException : Exception` for protocol violations (bad sizes, bad constraints, overflow).
* `ArgumentOutOfRangeException` for public API misuse (negative sizes, etc.).

### 3.3 Overflow handling

You asked to keep calculations in `int`. That's fine, but you must define what happens if arithmetic overflows.

**MUST**: Do arithmetic in a way that does not silently overflow.

You have two valid options (pick one and enforce it consistently):

**Option A (recommended): checked arithmetic + LayoutException**

* Perform layout arithmetic in a `checked` context.
* If an overflow occurs, throw `LayoutException`.

**Option B: saturating arithmetic**

* Any operation that would overflow clamps to `int.MaxValue`.
* Still throw if the result would violate invariants (e.g., `Natural == int.MaxValue`).

This spec assumes **Option A** (checked + throw). (You can switch to saturating later, but be consistent.)

---

## 4. Types

### 4.1 `LayoutConstraints`

Represents the allowable size range for a node.

Fields (conceptual):

* `MinWidth`, `MaxWidth`
* `MinHeight`, `MaxHeight`

Invariants (MUST):

* `0 <= MinWidth <= MaxWidth <= int.MaxValue`
* `0 <= MinHeight <= MaxHeight <= int.MaxValue`

Derived:

* `IsWidthBounded  := MaxWidth  < int.MaxValue`
* `IsHeightBounded := MaxHeight < int.MaxValue`

Notes (implementation detail):

* `int.MaxValue` is reserved as the unbounded sentinel and must never be used as a *finite* size.
* The implementation defines `MaxFinite = int.MaxValue - 1` as the largest finite size allowed for `Min`/`Natural`.

### 4.2 `SizeHints`

Returned by Measure.

Fields (conceptual):

* `Min : Size` (finite)
* `Natural : Size` (finite, preferred)
* `Max : Size` (may contain `int.MaxValue`)
* `FlexGrowX : int` (>= 0)
* `FlexGrowY : int` (>= 0)
* `FlexShrinkX : int` (>= 0)
* `FlexShrinkY : int` (>= 0)

Invariants (MUST), per axis:

* `0 <= Min <= Natural <= Max <= int.MaxValue`
* `Natural.Width` and `Natural.Height` MUST be **finite** (`<= int.MaxValue - 1`)
* `Min.Width` and `Min.Height` MUST be **finite** (`<= int.MaxValue - 1`)

Interpretation:

* `Min`: smallest usable size
* `Natural`: shrink-wrap / preferred size
* `Max`: largest meaningful size (can be unbounded)
* Flex fields: how a *parent* distributes surplus/deficit space when computing slots

### 4.3 Alignment

Each node exposes:

* `HorizontalAlignment ∈ { Start, Center, End, Stretch }`
* `VerticalAlignment   ∈ { Start, Center, End, Stretch }`

**Default framework alignment** (your current default):

* Horizontal: `Align.Start`
* Vertical: `Align.Start`

This spec integrates alignment as an **arrange-time policy**.

---

## 5. Node protocol

### 5.1 Interface

```csharp
public class Visual
{
    public SizeHints Measure(in LayoutConstraints constraints) //...
    protected virtual SizeHints MeasureCore(in LayoutConstraints constraints) //...
    public void Arrange(Rectangle finalRect) //...
    protected virtual void ArrangeCore(in Rectangle finalRect)
}
```

Notes:

* The codebase also exposes `public void Measure(Size availableSize)` as a convenience wrapper for callers that only have a max size.
  It must not be used by controls internally: control layout code must always call `Measure(in LayoutConstraints)` on children so that
  constraints remain explicit and unboundedness is never confused with “request infinity”.

### 5.2 Measure contract (normative)

When `Measure(constraints)` is called, a node:

1. MUST return `SizeHints` satisfying invariants.
2. MUST NOT set `Natural` (or `Min`) to `int.MaxValue`.
3. MUST treat unbounded max as **"no upper limit"**, not "be infinite".

**Critical rule (prevents your bug):**

> "Stretch/Fill" MUST NOT be implemented by returning `Natural = constraints.Max`.
> Stretch is handled during Arrange by how the parent sizes the child's slot.

Measure is allowed to be called multiple times with different constraints. Results must be deterministic for the same inputs.

### 5.3 Arrange contract (normative)

When `Arrange(finalRect)` is called, a node:

* Receives a finite rectangle with Width/Height >= 0
* Must layout itself and its children inside that rectangle
* Must not trigger upward layout invalidation during the same Arrange pass

Nodes MAY re-measure children during Arrange **only** for dependency resolution (e.g., text wrapping based on final width). Such internal re-measure must not cause recursive relayout of ancestors.

---

## 6. Slot-based layout and alignment integration

A parent container computes a **slot rectangle** for each child. Then it applies the child's alignment and hints to produce the final rectangle passed to `child.Arrange`.

### 6.1 Alignment rule: alignment is applied to a slot

Given:

* `slot : Rectangle` (finite)
* `hints : SizeHints`
* child alignments `AlignX`, `AlignY`

Compute final child width/height:

**Horizontal size**

* If `AlignX == Stretch`:

  * `childW = slot.Width`
* Else:

  * `childW = min(hints.Natural.Width, slot.Width)`

Then clamp to capability:

* `childW = clamp(childW, hints.Min.Width, min(hints.Max.Width, slot.Width))`

**Vertical size**

* If `AlignY == Stretch`:

  * `childH = slot.Height`
* Else:

  * `childH = min(hints.Natural.Height, slot.Height)`

Clamp:

* `childH = clamp(childH, hints.Min.Height, min(hints.Max.Height, slot.Height))`

Now compute position:

**Horizontal position**

* `Left`:   dx = 0
* `Center`: dx = (slot.Width - childW) / 2
* `Right`:  dx = slot.Width - childW
* `Stretch`: dx = 0

**Vertical position**

* `Top`:    dy = 0
* `Center`: dy = (slot.Height - childH) / 2
* `Bottom`: dy = slot.Height - childH
* `Stretch`: dy = 0

Final rect:

* `childRect = (slot.X + dx, slot.Y + dy, childW, childH)`

**Important consequence:**
A leaf can be "Stretch" without ever returning "available" from Measure. Unbounded constraints are safe.

---

## 7. Flex allocation (how parents compute slots)

Containers that distribute space along one axis (HStack/VStack, some grids, etc.) use **flex allocation**.

Per child i, choose a "base" size along the main axis:

* `base[i] = hints.Natural.(Axis)`

Then:

* If `sum(base) < available` distribute **extra** to children with `FlexGrow > 0` (weight-based)
* If `sum(base) > available` distribute **deficit** by shrinking children with `FlexShrink > 0` down to Min (weight-based)

Rules (MUST):

* Never allocate below `Min` or above `Max`.
* Handle integer remainder deterministically (e.g., left-to-right or stable index order).

**Note:** Alignment is applied **after** the slot is computed. Flex alloc decides slot sizes; alignment decides how the child uses them.

### 7.1 Relationship between Alignment and Flex

In this framework, `Stretch` alignment implies “willingness to grow” on that axis when a parent runs a flex allocation algorithm.

Normative behavior:

* A `Visual` with `HorizontalAlignment == Stretch` must be treated as having `FlexGrowX > 0` (typically 1) when participating in flex allocation.
* A `Visual` with `VerticalAlignment == Stretch` must be treated as having `FlexGrowY > 0` (typically 1) when participating in flex allocation.

Implementation note:

* This is achieved by the `Visual.Measure(...)` wrapper adjusting the returned `SizeHints` from `MeasureCore(...)` so that flex containers
  (HStack/VStack/etc.) can allocate remaining space to stretched children without requiring children to “ask for available size” in Measure.

---

## 8. ScrollViewer specification (extent vs viewport)

ScrollViewer must distinguish:

* **Extent**: content size (from measuring content unbounded in scroll directions)
* **Viewport**: arranged size (finalRect)

### 8.1 Measure

Given parent `constraints`, define `childConstraints`:

* If horizontal scrolling enabled:

  * `childConstraints.MaxWidth = int.MaxValue`
  * else `childConstraints.MaxWidth = constraints.MaxWidth`
* If vertical scrolling enabled:

  * `childConstraints.MaxHeight = int.MaxValue`
  * else `childConstraints.MaxHeight = constraints.MaxHeight`

Call `content.Measure(childConstraints)` and set:

* `extent = contentHints.Natural` (must be finite)

Then ScrollViewer hints:

Per axis:

* If parent axis is bounded:

  * `Natural = min(extent, constraints.Max)` (don't ask bigger than viewport)
* If parent axis is unbounded:

  * `Natural = extent` (no reason to scroll; can expand)

ScrollViewer typically reports:

* `Max` = constraints.Max (it shouldn't exceed what parent allows)
* FlexGrow default: usually 1 in both axes (it's a viewport-y control)

### 8.2 Arrange

Given `finalRect` (viewport):

* Arrange content at size:

  * common: `contentW = max(extent.Width, finalRect.Width)` if you want "stretch if smaller"
  * otherwise `contentW = extent.Width`
  * similarly for height

Place with offsets:

* `contentX = finalRect.X - ScrollX`
* `contentY = finalRect.Y - ScrollY`

Clip rendering to `finalRect`.

**Content alignment inside ScrollViewer:**
If content is smaller than viewport and you want centering, apply alignment between viewport slot and content rect using the alignment rules from §6.

---

## 9. Text wrapping ("for width" dependency)

Some nodes' height depends on width (TextBlock with wrapping, TextArea, Table, etc.)

Rules:

* In Measure: if `constraints.MaxWidth` is bounded, compute wrapped height using that width.
* In Arrange: parent may re-measure with a **tight width constraint**:

  * `MinWidth = MaxWidth = allocatedWidth`
  * then use the returned height for final placement

Any such internal re-measure must not invalidate ancestors during the same pass.

---

## 10. Mandatory runtime checks

The layout engine MUST validate at runtime (Release included):

* `LayoutConstraints` invariants
* `SizeHints` invariants
* `Natural` and `Min` are never `int.MaxValue`
* No negative widths/heights
* No arithmetic overflow (per §3.3 policy)

On failure: throw `LayoutException` (or your chosen equivalent).

---

# Control guidance (grouped) + default alignments

Below, I assume your general default is `Horizontal=Left`, `Vertical=Top`. I'll only call out controls that should override to better defaults.

I'll group controls by how they should respect the spec: **leaf**, **decorator**, **layout container**, **scrolling/virtualized**, **overlay**, etc.

---

## A) Pure leaf visuals (intrinsic size, no children)

### Rule.cs

* **Measure**: `Natural.Height = 1`, `Natural.Width = 0` (finite). `Max.Width = int.MaxValue`.
* **Default alignment**: **Horizontal = Stretch**, Vertical = Top.
* **Flex**: `FlexGrowX = 1` is often appropriate (acts like spacer line in HStack/VStack).
* Never return `Natural.Width = constraints.MaxWidth`.

### Spinner.cs, Sparkline.cs (if 1-row)

* If rendered as a 1-row component: `Natural.Height = 1`.
* Sparkline often benefits from taking available width:

  * **Default Horizontal = Stretch**
  * `Natural.Width` finite (e.g., minimal 1 or 0), `Max.Width = int.MaxValue`, `FlexGrowX = 1`.

### ProgressBar.cs, Slider.cs

* These are "track" controls; best as horizontal stretch.
* **Default alignment**: **Horizontal = Stretch**, Vertical = Top.
* `Natural.Height = 1`.
* `Max.Width = int.MaxValue`, and typically `FlexGrowX = 1`.

### BarChart.cs, LineChart.cs

* Typically benefit from stretching to available area.
* **Default alignment**: **Horizontal = Stretch**, **Vertical = Stretch**.
* `Natural` finite: choose a sensible default like `(minW, minH)` (e.g., `(10, 4)`), never `int.MaxValue`.

---

## B) Text-like leaf controls

### TextBlock.cs, Link.cs, Markup.cs

* **Measure**:

  * If no wrapping: `Natural.Width = text length`, `Natural.Height = 1`.
  * If wrapping enabled and bounded width: `Natural.Width <= MaxWidth`, `Natural.Height = wrapped lines`.
* `Max.Width` can be `int.MaxValue` (clipping/truncation allowed).
* **Default alignment**: Left/Top is fine.

---

## C) Input controls (leaf or single-child internal rendering)

### TextBox.cs, MaskedInput.cs, Select.cs

* Usually **1 row** high.
* Best default: **Horizontal = Stretch**, Vertical = Top.
* `Natural.Height = 1`, `Natural.Width` finite (e.g., minimal 4–10 or based on placeholder).
* `Max.Width = int.MaxValue`, `FlexGrowX = 1` common.

### TextArea.cs

* Multi-line; should behave like a viewport.
* **Default alignment**: **Stretch/Stretch**
* `Natural` finite (e.g., `(20, 5)`), `Max` unbounded, flex-grow both axes often 1.

### CheckBox.cs, RadioButton.cs, Switch.cs

* Intrinsic width = indicator + spacing + label
* `Natural.Height = 1`
* **Default alignment**: Left/Top.
* Don't make them stretch by default; let containers allocate.

### Slider.cs already covered above.

---

## D) Decorators and content presenters (1 child, add chrome/positioning)

### Border.cs

* **Measure**: subtract border/padding from constraints passed to child; add back to hints.
* **Arrange**: compute child slot = inner rect; then apply child alignment inside that slot.
* **Default alignment**: Border itself can be Left/Top; typically does not force stretch.

### Group.cs (often border + header)

* Same as Border, but includes title row and content area.
* Needs careful height math (title consumes 1 row typically).
* Default alignment: Left/Top; content alignment inside group often Left/Top.

### Center.cs

* This is a container whose job is to center its child.
* **Arrange**: child slot = full rect, but force child alignment behavior:

  * Either override child alignment to Center/Center
  * Or compute a centered slot using hints.Natural
* **Default alignment**: Center should usually be **Stretch/Stretch** as a container (it wants full area to center within).

### ContentVisual.cs, ComputedVisual.cs

* Treat as wrappers:

  * Measure: forward to child (or computed content) and return its hints (possibly adjusted)
  * Arrange: forward rect
* Default alignment: follow framework default (Left/Top).

### Backdrop.cs

* Usually intended to fill the whole viewport behind a dialog/popup.
* **Default alignment**: **Stretch/Stretch**
* Measure natural finite (e.g., `(0,0)`), max unbounded.

---

## E) Layout containers (multiple children, compute slots)

### HStack.cs, VStack.cs

* Must implement the flex allocator (§7) on the main axis.
* Cross-axis:

  * Common behavior: slot cross-size = container final cross-size
  * Apply each child's cross-axis alignment (Stretch default fills cross-axis)
* Default alignment of the container itself: typically **Stretch/Stretch** (containers usually want to occupy offered area).

### ZStack.cs

* All children share the same slot = container content rect.
* Apply each child alignment within the shared slot (§6).
* Default: Stretch/Stretch.

### DockLayout.cs

* Docked children consume edges; remaining rect goes to fill child.
* Alignment matters for children *within* their dock slot.
* Typical defaults:

  * The "fill" child: Stretch/Stretch
  * Docked bars: Horizontal Stretch (for Top/Bottom), Vertical Stretch (for Left/Right)

### Grid.cs + GridLength.cs

* Grid is the main consumer of `Min/Natural/Max`.
* Recommended approach:

  * Measure pass collects each child's hints.
  * Auto columns/rows take max of children's Natural in that band.
  * Star columns/rows distribute remaining space (flex-like).
* Apply child alignment inside each cell slot (§6).
* Default: container Stretch/Stretch.

### Table.cs

* Very grid-like; column widths depend on content.
* When measured unbounded, Natural widths may grow with data; keep it finite:

  * Natural width = computed column widths, but must not overflow
* Usually acts like a viewport:

  * **Default alignment**: Stretch/Stretch
  * Often used inside ScrollViewer, so it should behave well when measured unbounded along the scroll axis.

### Panel.cs

* Base class: ensure it enforces runtime checks and provides slot+alignment helper.

---

## F) Scrolling and scroll-related controls

### ScrollViewer.cs

* Must implement **extent vs viewport** per §8.
* Should never report `Natural = int.MaxValue`.
* **Default alignment**: **Stretch/Stretch** (it is a viewport).
* Content alignment inside viewport: either provide properties or honor child alignment.

### ScrollBar.cs

* Orientation-specific:

  * Horizontal scrollbar: `Natural.Height = 1`, **Horizontal = Stretch**
  * Vertical scrollbar: `Natural.Width = 1`, **Vertical = Stretch**
* The non-length axis should be fixed (Max = 1) so Stretch doesn't bloat thickness.

---

## G) Lists, trees, selection controls (often scrollable)

These are similar in layout behavior. Grouping:

* ListBox\<T>.cs
* OptionList\<T>.cs (data-driven, often using `OptionListItem` as the item type)
* SelectionList\<T>.cs
* TreeView.cs
* MenuItem.cs (when used in vertical menus)
* CommandPaletteItem.cs

Key points:

* Items are typically `Height = 1` each (tree may have indentation).
* If measured with unbounded height, reporting full extent is okay but must stay finite:

  * extent height = `itemCount` (or visible computed height), with overflow protection.
* Most of these behave as viewports:

  * **Default alignment for the container controls**: **Stretch/Stretch**
  * **Default alignment for item controls**: **Horizontal = Stretch**, Vertical = Top (items fill row width)
* Virtualization (optional but recommended):

  * In Arrange, only arrange visible items within viewport clip.

---

## H) Tabs / switchers / accordions / collapsibles (state-dependent containers)

* Accordion.cs
* Collapsible.cs
* Collapsible-related (AccordionItem if present)
* ContentSwitcher.cs
* TabControl.cs

Key points:

* Measure returns hints based on **currently visible/expanded** content.
* If collapsed, content size may be `(0,0)` or minimal header-only size.
* Default alignment: container usually Stretch/Stretch.
* Tab headers are bar-like: horizontal stretch, height 1.

---

## I) Bars and headers/footers

* Header.cs, Footer.cs, StatusBar.cs, MenuBar.cs
* Possibly also BarChart "legends" if any

Key points:

* Typically single-row, want to span width.
* **Default alignment**: **Horizontal = Stretch**, Vertical = Top.
* `Natural.Height = 1`
* `Max.Width = int.MaxValue`, `FlexGrowX = 1`

Menu items inside:

* `MenuItem`: row height 1, usually horizontal stretch within the bar or popup menu.

---

## J) Overlays / popups / dialogs / command palette

* Dialog.cs
* Popup.cs
* CommandPalette.cs
* Backdrop.cs (already covered)

Key points:

* These are usually placed by an overlay host (often a ZStack/Layer).
* Recommended defaults:

  * **Dialog / CommandPalette**: **Horizontal = Center**, **Vertical = Center**
  * `Natural` derived from content, clamped to viewport constraints
* Popups may prefer:

  * Horizontal Left, Vertical Top, but placed relative to an anchor; alignment may be controlled externally.

Implementation:

* Measure should be intrinsic (content-based), finite.
* Arrange: the overlay host gives them a slot (often full screen) then alignment centers them.

---

## K) Splitters

* SplitterBase.cs
* HSplitter.cs
* VSplitter.cs
* VSplitter/HSplitter handle orientation thickness

Key points:

* Thickness is fixed (usually 1 cell):

  * HSplitter: `Natural.Height = 1`, `Max.Height = 1`, **Horizontal = Stretch**
  * VSplitter: `Natural.Width = 1`, `Max.Width = 1`, **Vertical = Stretch**
* This ensures `Stretch` never inflates thickness.

---

## L) Buttons and small interactive controls

* Button.cs
* CheckBox.cs / RadioButton.cs / Switch.cs (already covered)
* Link.cs (already covered)

Buttons:

* Measure = text + padding, height typically 1.
* Default alignment can remain Left/Top.
* Inside the button, you may center the label (internal content alignment ≠ layout alignment).

---

## M) Misc / structural

* Accordion.cs / Collapsible.cs: covered
* Footer/Header/StatusBar: covered

---

# Recommended default alignments summary

Keep global defaults `Left/Top`, but override these for better UX:

**Stretch horizontally by default**

* Rule
* ProgressBar
* Slider
* MenuBar
* Header
* Footer
* StatusBar
* HSplitter
* Horizontal ScrollBar
* Most "row item" controls: `OptionListItem`, selection/list items

**Stretch both axes by default (viewport/container-like)**

* ScrollViewer
* TextArea
* ListBox\<T>
* TreeView
* Table
* Grid
* DockLayout
* HStack / VStack / ZStack
* Backdrop

**Center by default (overlay-like)**

* Dialog
* CommandPalette
* (Popup depends on anchoring; often Left/Top)

---

If you want, I can also provide a **single reusable helper** in your codebase:

* `ApplyAlignment(slotRect, hints, hAlign, vAlign) -> Rectangle`
* plus a reference `AllocateFlexAxis(...)` function

Both would encode §6 + §7 in one place so every container uses identical, bug-free behavior.
