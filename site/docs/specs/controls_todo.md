---
title: Controls Roadmap
---

# Controls Roadmap

This page tracks control work at a high level.

- ✅ = implemented in the core library
- ⬜ = planned / not implemented yet

For detailed design notes, each control has a dedicated spec under:

- [Control Specs](controls/readme.md)

> [!NOTE]
> The previous, more verbose planning document is kept for historical context:
> [controls_todo_original.md](archive/controls_todo_original.md).

## Planned work (backlog)

| Status | Component | Category | Notes |
| --- | --- | --- | --- |
| ⬜ | DirectoryTree / File picker | Navigation | File browsing for tool-like TUIs |
| ⬜ | Calendar widget | Visualization / productivity | Date picking / schedule views |
| ⬜ | RichLog (styled log viewer) | Diagnostics | Styled log entries + scrolling/search |
| ⬜ | Structured text viewer (JSON / syntax) | Content | Inspect configs/data with highlighting |
| ⬜ | DockWorkspace (dockable panes) | Layout / windowing | Rearrangeable panes + floating windows |
| ⬜ | Digits / KPI big-number | Visualization | Dashboard counters |

## Add-on libraries (separate packages)

Some controls are intentionally left out of the core to keep dependencies minimal.

| Status | Component | Depends on | Notes |
| --- | --- | --- | --- |
| ⬜ | MarkdownViewer | Markdig (or similar) | Keep core dependency-free; integrate via rich text spans |
