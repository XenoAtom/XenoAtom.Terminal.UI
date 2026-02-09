---
title: Logging Integration
---

# Logging Integration

XenoAtom.Terminal.UI includes a high-performance fullscreen log viewer control: [`LogControl`](controls/logcontrol.md).

For application logging, the companion project **XenoAtom.Logging** provides a production logging runtime plus a
Terminal.UI sink that can write directly into `LogControl`:

- `XenoAtom.Logging` (core runtime)
- `XenoAtom.Logging.Terminal` (Terminal + Terminal.UI integration, including `TerminalLogControlWriter`)

![XenoAtom.Logging + LogControl video](https://raw.githubusercontent.com/XenoAtom/XenoAtom.Logging/main/img/xenoatom-logcontrol.webm){.terminal}

## Install

```shell
dotnet add package XenoAtom.Logging --prerelease
dotnet add package XenoAtom.Logging.Terminal --prerelease
```

## Quick start (LogControl sink)

This example is based on `XenoAtom.Logging/samples/HelloLogControl/Program.cs`.

```csharp
using XenoAtom.Logging;
using XenoAtom.Logging.Writers;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

using var session = Terminal.Open();

var logControl = new LogControl { MaxCapacity = 4000 }.WrapText(true);
var terminalWriter = new TerminalLogControlWriter(logControl)
{
    EnableRichFormatting = true,
    EnableMarkupMessages = true,
};

var config = new LogManagerConfig
{
    RootLogger =
    {
        MinimumLevel = LogLevel.Info,
        Writers = { terminalWriter },
    }
};

LogManager.Initialize(config);
var logger = LogManager.GetLogger("MyApp");

logger.Info("Hello from XenoAtom.Logging");
logger.WarnMarkup("[yellow]Careful[/]");

Terminal.Run(logControl, () => TerminalLoopResult.Continue);

LogManager.Shutdown();
```

## Notes

- `LogControl` provides built-in Find (`Ctrl+F`), selection/copy, and tail-style viewing.
- `TerminalLogControlWriter` is intended for concurrent logging (e.g. background tasks) while the UI runs on the main thread.

## Links

- XenoAtom.Logging repository: <https://github.com/XenoAtom/XenoAtom.Logging>
- HelloLogControl sample: <https://github.com/XenoAtom/XenoAtom.Logging/tree/main/samples/HelloLogControl>
