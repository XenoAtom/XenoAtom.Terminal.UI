---
title: ProgressTaskGroup Specs
---

# ProgressTaskGroup Specs

This document captures the design and implementation details of `ProgressTaskGroup` and its related types (`ProgressTask`, `ProgressTaskColumn`, `ProgressTaskColumns`).

> [!NOTE]
> For end-user usage and examples, see [ProgressTaskGroup](../../controls/progresstaskgroup.md).

## Overview

- **Status**: Implemented
- **Purpose**: Display multiple progress tasks as rows in a grid, with configurable columns (label, bar, percentage, spinner, custom cells).
- **Composition-first**: This control composes existing visuals (`Grid`, `TextBlock`, `ProgressBar`, `Spinner`) instead of custom rendering.
- **Customization**: Columns are pluggable; tasks can customize cells per column without changing the group definition.

## Goals

- Provide a convenient "progress dashboard" control for terminal apps without introducing special rendering infrastructure.
- Keep column composition flexible while remaining easy to use with sensible defaults.
- Support high-frequency updates (task value changes) without rebuilding the visual tree each tick.

## Non-goals

- Built-in sorting, filtering, or task lifecycle management.
- Virtualization for huge numbers of tasks (the group renders a grid with one row per task).
- Per-row selection, focus, or input handling (this is a visualization control).

## Public API

### ProgressTaskGroup

`ProgressTaskGroup` is a `Visual` with:

- `Tasks : BindableList<ProgressTask>` (bindable getter)
- `Columns : BindableList<ProgressTaskColumn>` (bindable getter)
  - when empty, columns come from `ProgressTaskGroupStyle.DefaultColumns`

Defaults:

- `HorizontalAlignment = Align.Stretch`
- `VerticalAlignment = Align.Start`

### ProgressTask

`ProgressTask` is a state container (not a `Visual`) and implements `IVisualElement` so it can participate in the binding dirty model when hosted by a control:

- `Label : Visual` - label visual (a `ComputedVisual` can be used when constructed with `Func<Visual>`).
- `Value : double` (bindable)
- `Minimum : double` (bindable)
- `Maximum : double` (bindable, default is 1.0)
- `Progress01 : double` - normalized progress in [0..1]
- `Percentage : int` - progress in [0..100]
- `Increment(double delta)` convenience helper.

Task cell customization:

- `CustomizeCell(columnId, Action<Visual>)` adds an unkeyed customization.
- `SetCellCustomization(columnId, key, Action<Visual>)` replaces (columnId,key) customization.
- `ClearCellCustomizations(columnId)` removes all customizations for that column.
- `OnCellCreated(columnId, cell)` is called by the group for columns that have an `Id` and applies registered customizations.

### ProgressTaskColumn

`ProgressTaskColumn` describes one column:

- `CellFactory : Func<ProgressTask, Visual>` (required)
- `Id : string?` (optional, used for task customization hooks)
- `Width : GridLength` (default Auto)
- `MinWidth : int`
- `MaxWidth : int` (default `int.MaxValue`)

### ProgressTaskColumns

`ProgressTaskColumns` provides built-in columns and stable ids:

- `LabelColumnId`, `BarColumnId`, `PercentageColumnId`, `SpinnerColumnId`
- `Label(Align alignment = Align.End)`
- `Bar(ProgressBarStyle? style = null, int minWidth = 10)`
- `Percentage()`
- `Spinner(SpinnerStyle? style = null)`

## Styling

The group uses `ProgressTaskGroupStyle` (`src/XenoAtom.Terminal.UI/Styling/ProgressTaskGroupStyle.cs`) for layout defaults:

- `DefaultColumns` (label + bar + percentage by default)
- `ColumnSpacing` and `RowSpacing`

Each built-in column uses its corresponding control styles:

- label uses whatever style the label visual resolves
- bar uses `ProgressBarStyle` (default is `ProgressBarStyle.Default`)
- spinner uses `SpinnerStyle` (default is `SpinnerStyle.Default`)

## Tests & demos

Tests:

- `src/XenoAtom.Terminal.UI.Tests/ProgressTaskGroupRenderingTests.cs` (label/percentage rendering, per-task bar styling)

Demo:

- `samples/ControlsDemo/Demos/ProgressTaskGroupDemo.cs`

User documentation:

- `site/docs/controls/progresstaskgroup.md`

## Layout and rendering

### Composition and rebuild model

Internally, `ProgressTaskGroup` hosts a single child `ComputedVisual` that builds the group content:

- `Build()` returns a `Grid` that contains one row per task and one column per column definition.
- When `Tasks`, `Columns`, or relevant style values change, the computed visual rebuilds the grid.

This means:

- Updating `ProgressTask.Value` typically does not require rebuilding the grid, because the built-in column visuals bind to task properties (e.g. `ProgressTask.Progress01`).
- Adding/removing tasks or changing columns rebuilds the grid content.

### Grid construction details

The grid is built with:

- A `ColumnDefinition` per effective column (and optional gap columns if `ColumnSpacing > 0`).
- A `RowDefinition` per task (and optional gap rows if `RowSpacing > 0`).

Row/column gaps are implemented by inserting additional fixed-size definitions between the real tracks (a stride of 2 when gaps are enabled).

### Cell creation and customization

For each (task, column):

1. `ProgressTaskColumn.CellFactory(task)` creates a `Visual` cell.
2. If the column has `Id`, the group calls `task.OnCellCreated(id, cell)` before adding it to the grid.
3. The cell is added to the grid at the computed row/column indices.

Built-in convenience customization (implemented via extension methods in `ProgressTaskExtensions.cs`):

- `task.StyleBar(style)` applies a `ProgressBarStyle` to the bar cell created by `ProgressTaskColumns.Bar()`.
- `task.StyleSpinner(style)` applies a `SpinnerStyle` to the spinner cell created by `ProgressTaskColumns.Spinner()`.

## Input and commands

`ProgressTaskGroup` is display-only and does not handle keyboard/mouse input or expose commands.

## Future ideas

- Add more built-in columns (elapsed time, ETA, value/maximum formatting, throughput).
- Add optional row separators or alternating row backgrounds (style-driven, using `Grid` or wrapper visuals).
- Add deterministic screenshot-style tests for multi-column layout and spacing behavior.
