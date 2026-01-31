---
title: Getting Started
---

# Getting Started

## Prerequisites

- .NET `net10.0` (C# 14) project.
- Reference `XenoAtom.Terminal.UI` (it pulls `XenoAtom.Terminal` as a dependency).

## Install

```bash
dotnet add package XenoAtom.Terminal.UI
```

## Your first visual

XenoAtom.Terminal.UI integrates into `XenoAtom.Terminal` via C# 14 extension members.

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;

Terminal.Write(new Group(new HStack("Hello", "from", "Terminal.UI").Spacing(1)).Title("Welcome"));
```

## Inline live widget

Use `Terminal.Live` to render a visual that updates repeatedly without clearing previous terminal output.

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var work = new ProgressTask("Work");

Terminal.Live(
    new ProgressTaskGroup().Tasks([work]),
    onUpdate: () =>
    {
        work.Value = Math.Min(1, work.Value + 0.01);
        return work.Value < 1
            ? TerminalLoopResult.Continue
            : TerminalLoopResult.StopAndKeepVisual;
    });
```

## Fullscreen app

Use `Terminal.Run` to run a fullscreen app (alternate screen). Your UI is a `Visual` tree.

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;

Terminal.Run(
    new VStack(
        new TextBox("Type here…"),
        new Button("Exit").Click(() => false)
    ),
    onUpdate: () => true);
```

See also:

- [Hosting](./hosting.md)
- [Controls Reference](./controls/readme.md)
