---
title: Getting Started
---

# Getting Started

## Prerequisites

- .NET `net10.0` (C# 14) project.
- Reference `XenoAtom.Terminal.UI` (it pulls `XenoAtom.Terminal` as a dependency).

Rationale: Terminal.UI integrates into `XenoAtom.Terminal` via **C# 14 extension members**, so the hosting APIs are
available directly as `Terminal.Write(...)`, `Terminal.Live(...)`, and `Terminal.Run(...)`.

## Install

```bash
dotnet add package XenoAtom.Terminal.UI --prerelease
```

## Your first visual

XenoAtom.Terminal.UI integrates into `XenoAtom.Terminal` via C# 14 extension members.

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

Terminal.Write(new Group("Welcome")
    .Content(new VStack("Hello", "from", "Terminal.UI").Spacing(1))
);
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

> [!NOTE]
> Inline live regions do not enable terminal mouse reporting by default (so your terminal can keep text selection).
> If you need mouse input for interactive controls, use the `TerminalLiveOptions` overload with `EnableMouse = true`.

## Fullscreen app

Use `Terminal.Run` to run a fullscreen app (alternate screen). Your UI is a `Visual` tree.

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

State<string?> text = new("Type here");
State<bool> exit = new(false);

Terminal.Run(
    new VStack(
        new TextBox(text),
        new TextBlock(() => $"The text typed is: {text.Value}"),
        new Button("Exit").Click(() => exit.Value = true)
    ),
    onUpdate: () => exit.Value
        ? TerminalLoopResult.StopAndKeepVisual 
        : TerminalLoopResult.Continue
    );
```

See also:

- [Ecosystem & Foundations](foundations.md)
- [XenoAtom.Terminal](terminal.md)
- [XenoAtom.Ansi](ansi.md)
- [Hosting](hosting.md)
- [Controls Reference](controls/readme.md)
