# XenoAtom.Terminal.UI.Graphics

Optional terminal image controls and presenters for XenoAtom.Terminal.UI.

This package bridges the core UI graphics display list to `XenoAtom.Terminal.Graphics` image encoding for Kitty, iTerm2 inline images, and Sixel. It does not depend on text shaping components such as HarfBuzz.

## Usage

```csharp
using XenoAtom.Terminal;
using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Graphics;
using UiImageScaleMode = XenoAtom.Terminal.UI.ImageScaleMode;

var image = new Image(TerminalImageSource.FromFile("logo.png"))
{
    CellWidth = 24,
    CellHeight = 12,
    ScaleMode = UiImageScaleMode.Fit,
    PreserveAspectRatio = true,
    AccessibilityText = "Project logo",
    FallbackContent = new TextBlock("[logo]"),
};

Terminal.Run(
    image,
    _ => TerminalLoopResult.Continue,
    new TerminalRunOptions
    {
        GraphicsPresenter = new TerminalImageGraphicsPresenter(),
    });
```

`TerminalImageGraphicsPresenter` uses terminal graphics capabilities detected by `XenoAtom.Terminal`. It can present fullscreen apps through `TerminalRunOptions.GraphicsPresenter` and inline/live regions through `TerminalLiveOptions.GraphicsPresenter`. If no presenter is configured, graphics are disabled, or the presenter cannot select a supported protocol, the `Image` control renders its `FallbackContent` instead.

When terminal pixel metrics are unavailable, the presenter uses conservative fallback cell pixel dimensions (8×16 by default) so streamed protocols such as Sixel and iTerm2 still produce visible cell-sized images. Override `TerminalImageGraphicsPresenterOptions.FallbackCellPixelWidth` and `FallbackCellPixelHeight` if your terminal uses substantially different cell metrics.

Real-time sources can implement `ITerminalRealtimeImageSource` alongside `TerminalImageSource`. The `Image` control subscribes to `FrameAvailable`, coalesces frame notifications on the app loop, and asks the presenter to encode the latest frame. `TerminalImageGraphicsPresenter.Metrics` reports encode duration, payload bytes, skipped frame versions, and effective FPS.

For manual validation, force a protocol with `TerminalImageGraphicsPresenterOptions.Protocol` or `XENOATOM_TERMINAL_GRAPHICS=sixel|kitty|iterm2` and run the app in a terminal that supports that protocol. Windows Terminal should normally be detected as Sixel; Visual Studio debug sessions that omit `WT_SESSION`/`WT_PROFILE_ID` are handled through `VSAPPIDNAME`/`__VSAPPIDDIR`. If a host still strips those signals, forcing `XENOATOM_TERMINAL_GRAPHICS=sixel` is the recommended validation override.

The `samples/ControlsDemo` app includes an **Images & Graphics** page that renders `Assets/snow_photo.jpg` and a live SkiaSharp-generated frame stream through this presenter.
