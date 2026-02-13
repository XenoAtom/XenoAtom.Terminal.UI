---
title: Command Line Integration
---

# Command Line Integration

**XenoAtom.CommandLine** is a companion library for composition-first CLI parsing.
With the optional **XenoAtom.CommandLine.Terminal** package, it integrates directly with
XenoAtom.Terminal.UI to produce rich terminal help and error output.

- `XenoAtom.CommandLine` (core parser and command model)
- `XenoAtom.CommandLine.Terminal` (Terminal + Terminal.UI integration)

![XenoAtom.CommandLine Terminal integration demo](https://raw.githubusercontent.com/XenoAtom/XenoAtom.CommandLine/main/site/img/xenoatom-commandline-show.gif){.terminal}

## Why it pairs well with Terminal.UI

- `TerminalVisualCommandOutput` renders help with Terminal.UI visuals (tables/groups/layout).
- `TerminalMarkupCommandOutput` renders styled markup output for interactive terminals.
- `ToHelpVisual()` generates a reusable `Visual` you can embed in fullscreen Terminal.UI apps.
- Command initializers can include Terminal.UI visuals (for example `TextFiglet` banners).

## Install

```shell
dotnet add package XenoAtom.CommandLine
dotnet add package XenoAtom.CommandLine.Terminal
```

> [!NOTE]
> `XenoAtom.CommandLine.Terminal` targets `net10.0` and depends on `XenoAtom.Terminal.UI`.

## Quick start (visual help output)

This is based on `XenoAtom.CommandLine/samples/TerminalVisualHelp/Program.cs`.

```csharp
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var app = new CommandApp("my-tool", config: new CommandConfig
{
    OutputFactory = _ => new TerminalVisualCommandOutput(new TerminalVisualOutputOptions
    {
        SectionGroupMinWidth = 70,
        ErrorGroupMinWidth = 70,
    })
})
{
    new CommandUsage(),
    "Options:",
    { "n|name=", "The {NAME}", _ => { } },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0)
};

// Also available:
// var visual = app.ToHelpVisual();
// XenoAtom.Terminal.Terminal.Write(visual);

return await app.RunAsync(args);
```

## Links

- XenoAtom.CommandLine repository: <https://github.com/XenoAtom/XenoAtom.CommandLine>
- XenoAtom.CommandLine documentation: <https://xenoatom.github.io/commandline/docs/>
- Terminal integration sample: <https://github.com/XenoAtom/XenoAtom.CommandLine/tree/main/samples/TerminalVisualHelp>
