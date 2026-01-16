# XenoAtom.Terminal.UI [![ci](https://github.com/XenoAtom/XenoAtom.Terminal.UI/actions/workflows/ci.yml/badge.svg)](https://github.com/XenoAtom/XenoAtom.Terminal.UI/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/XenoAtom.Terminal.UI.svg)](https://www.nuget.org/packages/XenoAtom.Terminal.UI/)

<img align="right" width="256px" height="256px" src="https://raw.githubusercontent.com/XenoAtom/XenoAtom.Terminal.UI/main/img/XenoAtom.Terminal.UI.png">

XenoAtom.Terminal.UI is a modern retained-mode terminal UI framework for .NET, built on top of [XenoAtom.Terminal](https://github.com/XenoAtom/XenoAtom.Terminal).
It provides a rich set of controls (TextBox, TextArea, lists, tables, dialogs…), a consistent layout system, a styling/theming model, and a binding system designed for smooth live UIs.

## Quick start

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;

Terminal.Write(new TextBlock("Hello from XenoAtom.Terminal.UI"));
```

Inline “live” widget (updates without clearing your output):

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var progress = new State<double>(0);

Terminal.Live(
    new ProgressBar().Value(progress),
    onUpdate: () =>
    {
        progress.Value = Math.Min(1, progress.Value + 0.01);
        return progress.Value < 1;
    });
```

Fullscreen app:

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

Terminal.Run(
    new VStack(
        new TextBox("Type here…"),
        new Button("Exit").Click(() => false)
    ),
    onUpdate: () => true);
```

## Features

- **Two hosting models**:
  - **Inline** widgets via `Terminal.Write(...)` and `Terminal.Live(...)`
  - **Fullscreen** apps via `Terminal.Run(...)` (alternate screen + input loop)
- **Modern control library**:
  - Buttons, toggles, lists, selection lists, tables, tabs, menus, dialogs/popups, charts, progress, spinners…
  - Text editing: **TextBox**, **TextArea**, **MaskedInput**
- **Binding-first UI**:
  - Bindable properties, `State<T>`, automatic dependency tracking, minimal boilerplate
- **Layout system**: consistent measure/arrange protocol (integer cell UI), panels and containers
- **Styling & themes**:
  - Theme + per-control styles, `AnsiColorScheme` palettes (terminal-native and RGB themes)
- **Input**:
  - Keyboard, mouse, resize events; focus navigation; routed events where appropriate
- **Rendering**:
  - Cell-buffer renderer + diffing, efficient batched output, synchronized output (DEC 2026)
- **Cross-platform + AOT-friendly**: `net10.0` and NativeAOT-oriented design (built on XenoAtom.Terminal)

Screenshot placeholder (to be updated):

![XenoAtom.Terminal.UI Fullscreen Demo](https://raw.githubusercontent.com/XenoAtom/XenoAtom.Terminal.UI/main/img/screenshots/fullscreen-demo.png)

> [!NOTE]
> XenoAtom.Terminal.UI depends on XenoAtom.Terminal. The two libraries are designed to be used together:
> Terminal handles safe ANSI/markup output and unified input events; Terminal.UI builds a widget/layout system on top.

## User guide

For details, see `doc/readme.md`.

## Samples

- `samples/Playground`: quick manual repros and experiments.
- `samples/MvpDemo`: inline and fullscreen demos.
- `samples/FullscreenDemo`: fullscreen UI showcase.
- `samples/ControlsDemo`: catalog-style demo of controls and styles.

## License

This software is released under the [BSD-2-Clause license](https://opensource.org/licenses/BSD-2-Clause).

## Author

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
