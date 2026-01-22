# XenoAtom.Terminal.UI User Guide

XenoAtom.Terminal.UI is a modern retained-mode terminal UI framework built on top of XenoAtom.Terminal.
It supports both:

- **Inline widgets** that render as part of normal terminal output (`Terminal.Write`, `Terminal.Live`)
- **Fullscreen applications** (alternate screen, focus navigation, routed input, dialogs, etc.)

This guide documents the concepts, features, and controls of the library.

## Quick start

- `doc/getting-started.md`

## Hosting & integration

- `doc/hosting.md` (inline vs fullscreen, update loops)

## Core concepts

- `doc/visual-tree.md` (Visuals, fluent API, dynamic composition)
- `doc/binding.md` (`State<T>`, bindable properties, dependency tracking)
- `doc/data-templating.md` (DataTemplates, DataPresenter<T>, item templates)
- `doc/culture.md` (culture-aware value formatting)
- `doc/layout.md` (layout protocol, alignment, margin/padding)
- `doc/input.md` (keyboard/mouse, focus, routed events, capture)
- `doc/styling.md` (Theme, styles, environment)
- `doc/rendering.md` (cell buffer, diff renderer, performance)
- `doc/scrolling.md` (ScrollViewer, scroll models, scrollbars)
- `doc/text-editing.md` (TextBox/TextArea/MaskedInput and the text subsystem)
- `doc/markup-parsing.md` (`MarkupTextParser`, styled runs)

## Controls reference

- `doc/controls/index.md`

## Samples

The `samples` folder contains end-to-end demos:

- `samples/FullscreenDemo`: fullscreen UI showcase.
- `samples/ControlsDemo`: catalog-style demo.
- `samples/MvpDemo`: inline + fullscreen examples.
- `samples/Playground`: experiments and minimal repros.

## Specs and design notes

The `doc/specs` folder contains deeper design documents and implementation notes used during development:

- `doc/specs/layout_protocol_specs.md`
- `doc/specs/text_editor_specs.md`
- `doc/specs/specs.md`
- `doc/specs/original_specs.md`
