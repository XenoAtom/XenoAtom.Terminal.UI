---
title: ProgressTaskGroup Specs
---

# ProgressTaskGroup Specs

This document captures design and implementation notes for `ProgressTaskGroup`.

> [!NOTE]
> For end-user usage and examples, see [ProgressTaskGroup](../../controls/progresstaskgroup.md).

## Overview

- **Status**: Implemented
- **Primary purpose**: Provide `ProgressTaskGroup` as a retained-mode control with bindable properties and predictable layout/rendering behavior.
- **Key design constraints**:
  - reactive dependency tracking (measure/arrange/render)
  - allocation-conscious rendering
  - AOT/trimming friendliness (no runtime reflection by default)

## Implementation notes

- Source code lives under `src/XenoAtom.Terminal.UI` (search for `ProgressTaskGroup` and `ProgressTaskGroupStyle`).
- Public properties are typically `[Bindable]` (generated accessors) and participate in the binding dirty model.

## Layout & rendering

- Follows the standard `Measure` → `Arrange` → `Render` pipeline.
- Uses style inheritance from the visual tree; control-specific style is typically `ProgressTaskGroupStyle`.

## Input & commands

- Keyboard/mouse behaviors (when applicable) are exposed via commands so they are discoverable (e.g., CommandBar / CommandPalette).

## Styling

- Styling is controlled via the theme and `ProgressTaskGroupStyle` (where applicable).

## Tests & demos

- Look for rendering/input tests in `src/XenoAtom.Terminal.UI.Tests`.
- See the ControlsDemo for interactive examples.

## Future / v2 ideas

- Consider documenting additional style knobs and adding more deterministic rendering tests as features grow.
