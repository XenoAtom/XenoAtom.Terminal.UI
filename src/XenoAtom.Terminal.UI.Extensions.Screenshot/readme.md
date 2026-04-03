# XenoAtom.Terminal.UI.Extensions.Screenshot

Raster screenshot export for `XenoAtom.Terminal.UI`, powered by SkiaSharp.

The package provides:

- `CellBufferImageExporter` for exporting any `CellBuffer` to PNG, JPEG, or WebP
- `TerminalAppScreenshotExtensions` for saving the current app frame as a raster image
- `RegisterClipboardScreenshotCommand(...)` and `TryCopyScreenshotToClipboard(...)` helpers for copying PNG screenshots to the clipboard, with `Ctrl+F12` and command-bar visibility by default
- `TerminalAppSnapshotImageRenderer` for deterministic screenshot generation on an in-memory backend

By default the package embeds `CaskaydiaCoveNerdFont-Regular.ttf` so screenshots render the Nerd Font glyphs used by the demos without requiring the font to be installed on the host machine.
