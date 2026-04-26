---
title: Terminal Graphics and Inline Images
---

# Terminal Graphics and Inline Images

This spec defines the architecture for first-class terminal image rendering across:

- `XenoAtom.Ansi`
- `XenoAtom.Terminal`
- `XenoAtom.Terminal.UI`

The initial feature target is **images**, including high-frequency/real-time image updates. Video playback is intentionally out of scope for the first design and should only be revisited after the image pipeline is stable.

The target terminal graphics protocols are:

- Kitty graphics protocol
- iTerm2 inline images (`OSC 1337 ; File=...`)
- Sixel

The goal is not to scatter protocol escape strings throughout controls. The goal is a layered, capability-driven graphics stack that fits the current architecture described in [Ecosystem & Foundations](../foundations.md):

```text
XenoAtom.Terminal.UI -> XenoAtom.Terminal -> XenoAtom.Ansi
```

## Summary Recommendation

Build the feature in layers:

1. **`XenoAtom.Ansi`**: add safe low-level writers and tokenizer support for OSC/DCS/APC string sequences and probe replies. No image codecs, no terminal heuristics, no retained graphics state.
2. **`XenoAtom.Terminal`**: add terminal graphics capabilities, pixel metrics, probing, protocol selection, multiplexer handling, and diagnostics. No image decoding or Sixel quantization.
3. **`XenoAtom.Terminal.Graphics`**: optional package for image sources, decoding, resizing, raster processing, protocol encoding, payload chunking, caches, and direct terminal image APIs. This package may use SkiaSharp for the default image pipeline.
4. **`XenoAtom.Terminal.UI`**: add a graphics display-list/render-plane abstraction, an opt-in graphics render interface, and host extension points. Keep `CellBuffer` text-first.
5. **`XenoAtom.Terminal.UI.Graphics`**: optional package containing the `Image` control and protocol presenters that bridge UI graphics commands to `XenoAtom.Terminal.Graphics`.

This keeps the low layers small and AOT-friendly while still allowing a polished retained-mode `Image` control in Terminal.UI.

## Goals

- Support Kitty, iTerm2 inline images, and Sixel behind one capability-driven abstraction.
- Make image controls in `XenoAtom.Terminal.UI` the primary scenario.
- Support direct image output from `XenoAtom.Terminal` through an optional graphics package.
- Work out of the box for common terminals, including Windows Terminal through Sixel and Kitty-protocol terminals through Kitty graphics.
- Provide explicit overrides for terminals, multiplexers, SSH hops, and tests.
- Support static images first, while designing the pipeline so high-frequency frame updates are possible.
- Keep ANSI/text rendering and graphics rendering separated.
- Avoid forcing image codec, resizing, quantization, or native graphics dependencies into `XenoAtom.Ansi`, `XenoAtom.Terminal`, or the core `XenoAtom.Terminal.UI` package.
- Allow the optional graphics packages to use SkiaSharp when it materially simplifies decoding, resizing, pixel conversion, and deterministic image handling.

## Non-goals

- Do not implement video playback in v1.
- Do not turn `XenoAtom.Ansi` into an image processing library.
- Do not store raw image payloads, protocol payloads, or retained image objects inside `CellBuffer` cells.
- Do not make every protocol look retained. Kitty supports retained-style placement; iTerm2 and Sixel are fundamentally streamed/cursor-positioned for this design.
- Do not guarantee identical rendering across terminals. The protocols differ in placement, scaling, deletion, layering, and transport limits.
- Do not rely only on environment heuristics when active probing is available.

## Design Principles

1. **Graphics are a second plane**: text cells remain in `CellBuffer`; images are collected into a display list and presented by the host.
2. **Protocols are selected by capabilities**: controls ask for image rendering, not for Kitty/iTerm2/Sixel directly.
3. **Encoding is optional and pluggable**: image decoding/resizing/quantization belongs in optional packages.
4. **Controls are declarative**: an `Image` control declares source, sizing, clipping, and fallback content. It does not assemble escape sequences.
5. **The terminal owns probing**: only `XenoAtom.Terminal` should coordinate interactive queries and parse replies.
6. **The host owns output ordering**: graphics escapes must be emitted with text diff output in a controlled batch, not from controls.
7. **Real-time updates are latest-wins**: slow encodes should be dropped/cancelled instead of queuing stale frames.
8. **Every heuristic is diagnosable and overridable**: graphics failures are common under multiplexers and remote shells.
9. **Graphics collection is opt-in**: most visuals never emit graphics, so the graphics pass should identify and visit only graphics-capable visuals and their ancestor paths.

## Recommended Package Boundary

Recommended dependency chain:

```text
XenoAtom.Ansi
XenoAtom.Terminal -> XenoAtom.Ansi
XenoAtom.Terminal.Graphics -> XenoAtom.Terminal + XenoAtom.Ansi
XenoAtom.Terminal.UI -> XenoAtom.Terminal + XenoAtom.Ansi
XenoAtom.Terminal.UI.Graphics -> XenoAtom.Terminal.UI + XenoAtom.Terminal.Graphics
```

Why keep graphics packages optional:

- image codecs, resizers, and Sixel encoders are much heavier than ANSI/terminal primitives
- SkiaSharp and its native assets are appropriate for an opt-in graphics package, not for the ANSI/terminal core
- Sixel requires palette generation and dithering
- real-time image scenarios need caches, buffers, throttling, and cancellation
- many CLI/TUI applications never render images
- core packages should remain trimmer/AOT-friendly

A later release can add convenience meta-packages, but the dependency direction should not change.

### Temporary Cross-Repository Development Workflow

Terminal graphics touches `XenoAtom.Ansi`, `XenoAtom.Terminal`, and `XenoAtom.Terminal.UI`. During feature development, use local project references across the sibling checkouts so the full chain can be built and tested before any package is released.

Relative to the `XenoAtom.Terminal.UI` repository root, the expected local project paths are:

```text
../XenoAtom.Ansi/src/XenoAtom.Ansi/XenoAtom.Ansi.csproj
../XenoAtom.Terminal/src/XenoAtom.Terminal/XenoAtom.Terminal.csproj
```

When editing a `.csproj` from another directory, use the equivalent relative path from that project file. The paths above are the canonical workspace locations for this spec.

Temporary reference policy:

- `XenoAtom.Terminal` may temporarily reference the local `XenoAtom.Ansi.csproj` instead of the published `XenoAtom.Ansi` package.
- `XenoAtom.Terminal.UI` may temporarily reference the local `XenoAtom.Terminal.csproj` instead of the published `XenoAtom.Terminal` package.
- Projects that reference `XenoAtom.Terminal.UI` by project reference may also need a temporary direct project reference to the local `XenoAtom.Terminal.csproj` so restore/build has complete project information for the transitive local dependency.
- Any new optional graphics packages should follow the same local-reference strategy while the cross-repository API is still moving.
- Keep these temporary `ProjectReference` changes isolated and easy to remove. They are a development workflow, not the final release shape.
- Add the local dependency projects to the development `.slnx` files when using solution builds so Debug/Release configuration is applied consistently across the chain.
- Do not publish packages that still contain temporary sibling-repository project references.
- Once the full chain has been validated, release bottom-up and replace relative project references with package references progressively.

Repository-local verification policy:

- Verify each repository from its own `src` directory. Do not rely only on a higher-level solution build to validate lower-level repositories transitively.
- When working in `XenoAtom.Ansi`, run from `../XenoAtom.Ansi/src`:
  - `dotnet build -c Release`
  - `dotnet test -c Release`
- When working in `XenoAtom.Terminal`, run from `../XenoAtom.Terminal/src`:
  - `dotnet build -c Release`
  - `dotnet test -c Release`
- When working in `XenoAtom.Terminal.UI`, run from this repository's `src` directory:
  - `dotnet build -c Release`
  - `dotnet test -c Release`
- Run verification bottom-up: `XenoAtom.Ansi`, then `XenoAtom.Terminal`, then `XenoAtom.Terminal.UI`.
- Do not commit cross-repository terminal graphics work until the feature is working and verified across all three local repositories. Keep pending changes coordinated across repositories until the full chain is green.

Local development checklist:

- [x] Confirm the sibling `XenoAtom.Ansi` checkout exists at `../XenoAtom.Ansi`.
- [x] Confirm the sibling `XenoAtom.Terminal` checkout exists at `../XenoAtom.Terminal`.
- [x] In `XenoAtom.Terminal`, temporarily replace the `XenoAtom.Ansi` package reference with a `ProjectReference` to the local `XenoAtom.Ansi.csproj`.
- [x] In `XenoAtom.Terminal.UI`, temporarily replace the `XenoAtom.Terminal` package reference with a `ProjectReference` to the local `XenoAtom.Terminal.csproj`.
- [x] Add temporary direct local `XenoAtom.Terminal.csproj` references to downstream UI extension/sample/test projects that need complete project information during restore.
- [x] Add the local dependency projects to the development solutions so `dotnet build -c Release` uses the intended configuration across the chain.
- [ ] From `../XenoAtom.Ansi/src`, run `dotnet build -c Release` and `dotnet test -c Release` first.
- [ ] From `../XenoAtom.Terminal/src`, run `dotnet build -c Release` and `dotnet test -c Release` against the local `XenoAtom.Ansi` project.
- [ ] From this repository's `src` directory, run `dotnet build -c Release` and `dotnet test -c Release` against the local `XenoAtom.Terminal` project.
- [ ] Validate terminal graphics behavior end-to-end while all three repositories are connected locally.
- [ ] Commit coordinated cross-repository changes only after the full local chain is working and verified.
- [ ] Release packages bottom-up once the full chain is stable.
- [ ] Remove temporary relative project references after each corresponding package has been released and consumed.
- [ ] Run final validation using package references before publishing the higher-level packages.

### SkiaSharp Dependency Policy

`XenoAtom.Terminal.UI.Extensions.Screenshot` already uses SkiaSharp for raster screenshot export. It is therefore acceptable for `XenoAtom.Terminal.Graphics` to use SkiaSharp as the default implementation for image decoding and raster processing, as long as the dependency remains outside the core stack.

Recommended policy:

- `XenoAtom.Ansi` must never depend on SkiaSharp.
- `XenoAtom.Terminal` must never depend on SkiaSharp.
- core `XenoAtom.Terminal.UI` must never depend on SkiaSharp.
- `XenoAtom.Terminal.Graphics` may depend on SkiaSharp because it is an opt-in graphics/media package.
- `XenoAtom.Terminal.UI.Graphics` may depend transitively on SkiaSharp through `XenoAtom.Terminal.Graphics`.
- `XenoAtom.Terminal.Graphics` should not reference `XenoAtom.Terminal.UI.Extensions.Screenshot`; that package depends on Terminal.UI and would invert the intended dependency direction.

Two packaging variants are acceptable:

1. **Simple v1**: put the SkiaSharp-backed decoder/resizer directly in `XenoAtom.Terminal.Graphics`. This gives the `Image` control and direct terminal image API a working default path.
2. **Stricter split**: keep `XenoAtom.Terminal.Graphics` as abstractions/protocol encoders and add `XenoAtom.Terminal.Graphics.SkiaSharp` for the default raster backend. Use this only if native asset size or deployment constraints become a real problem.

For the first implementation, the simple v1 approach is recommended. The package is already optional, and SkiaSharp avoids spending a large amount of project-specific code on image decoding and high-quality resizing.

## Layer Responsibilities

### `XenoAtom.Ansi`

`XenoAtom.Ansi` should provide reusable VT primitives and tokenizer support only.

Recommended additions:

- generic writers for terminal string controls:
  - OSC (`ESC ] ... BEL/ST`)
  - DCS (`ESC P ... ST`)
  - APC (`ESC _ ... ST`) for protocols that use APC-style strings
- low-allocation helpers for protocol parameter serialization
- tokenizer tokens for DCS/APC/PM/SOS string controls instead of treating all of them as opaque unknown escapes
- reply parsers for syntactic graphics probe replies when the parsing is protocol-level and dependency-free

Example low-level writer surface, reusing the existing `AnsiOscTermination` type for OSC strings:

```csharp
public partial class AnsiWriter
{
    public AnsiWriter WriteOsc(int code, ReadOnlySpan<char> payload, AnsiOscTermination? terminator = null);
    public AnsiWriter WriteDcs(ReadOnlySpan<char> payload);
    public AnsiWriter WriteApc(ReadOnlySpan<char> payload);
}
```

Potential protocol-specific helpers are acceptable **only** when they are pure escape serialization:

```csharp
public static class AnsiKittyGraphicsSequences
{
    public static void WriteCommand(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> payload);
}

public static class AnsiIterm2ImageSequences
{
    public static void WriteFile(AnsiWriter writer, ReadOnlySpan<char> parameters, ReadOnlySpan<char> base64Payload);
}
```

Do not put these in `XenoAtom.Ansi`:

- image decoding
- PNG/JPEG/WebP/GIF parsing beyond optional header sniffing for tests
- resizing
- palette generation
- dithering
- Sixel image encoding
- terminal environment heuristics
- retained image IDs or placement caches

### `XenoAtom.Terminal`

`XenoAtom.Terminal` should own terminal-level graphics capability detection, pixel metrics, probing, overrides, diagnostics, and multiplexer policy.

Recommended core capability types:

```csharp
public enum TerminalGraphicsProtocol
{
    None,
    Kitty,
    ITerm2,
    Sixel,
}

public enum TerminalGraphicsSupportState
{
    Unsupported,
    Disabled,
    Heuristic,
    Confirmed,
    Forced,
}

public enum TerminalGraphicsPresentationModel
{
    None,
    Streamed,
    Retained,
}

public sealed class TerminalGraphicsCapabilities
{
    public static TerminalGraphicsCapabilities None { get; }

    public TerminalGraphicsProtocol PreferredProtocol { get; init; }
    public IReadOnlyList<TerminalGraphicsProtocol> SupportedProtocols { get; init; } = [];
    public TerminalGraphicsSupportState SupportState { get; init; }
    public TerminalGraphicsPresentationModel PresentationModel { get; init; }

    public bool SupportsStaticImages { get; init; }
    public bool SupportsRealTimeUpdates { get; init; }
    public bool SupportsRetainedImages { get; init; }
    public bool SupportsRetainedPlacements { get; init; }
    public bool SupportsDelete { get; init; }
    public bool SupportsMoveOrReplace { get; init; }
    public bool SupportsCellPlacement { get; init; }
    public bool SupportsPixelPlacement { get; init; }
    public bool SupportsTransparency { get; init; }
    public bool RequiresCellReservation { get; init; }

    public int MaxChunkBytes { get; init; }
    public int MaxRecommendedPayloadBytes { get; init; }
    public TerminalPixelMetrics? PixelMetrics { get; init; }

    public string DetectionSource { get; init; } = string.Empty;
    public string? TerminalName { get; init; }
    public bool IsMultiplexer { get; init; }
    public bool IsRemoteSession { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
```

`TerminalCapabilities` should gain a property such as:

```csharp
public TerminalGraphicsCapabilities Graphics { get; init; } = TerminalGraphicsCapabilities.None;
```

or a lazily-refreshable service exposed from `TerminalInstance`:

```csharp
public sealed class TerminalGraphicsService
{
    public TerminalGraphicsCapabilities Capabilities { get; }
    public ValueTask<TerminalGraphicsCapabilities> RefreshCapabilitiesAsync(CancellationToken cancellationToken = default);
    public ValueTask<TerminalPixelMetrics?> QueryPixelMetricsAsync(CancellationToken cancellationToken = default);
}
```

`XenoAtom.Terminal` must not decode or resize image data. The service should answer: "What can this terminal do?" not "How do I encode this PNG?"

### `XenoAtom.Terminal.Graphics`

`XenoAtom.Terminal.Graphics` should own image preparation and protocol encoding.

Responsibilities:

- image source abstraction
- decode image bytes/streams/files when needed
- read image metadata
- resize/crop/pad to a target pixel rectangle
- convert pixel formats
- provide a default SkiaSharp-backed raster pipeline for common formats and high-quality resizing
- encode protocol payloads:
  - Kitty graphics payloads and command chunks
  - iTerm2 inline image OSC payloads
  - Sixel DCS payloads
- apply protocol-specific chunking and transport limits
- cache decoded frames, resized frames, and encoded payloads
- expose direct `Terminal`/`TerminalInstance` image APIs as extension members

The package should define codec/raster abstractions even if the default implementation uses SkiaSharp. This keeps protocol encoders testable and prevents SkiaSharp types from leaking into public APIs.

Recommended raster service shape:

```csharp
public readonly record struct TerminalImageSize(int Width, int Height);

public readonly record struct TerminalImageColor(byte R, byte G, byte B, byte A = 255);

public enum TerminalImageFormat
{
    Png,
    Jpeg,
    Webp,
    Gif,
    RawRgb24,
    RawRgba32,
}

public readonly record struct TerminalImageInfo(
    TerminalImageFormat Format,
    int PixelWidth,
    int PixelHeight,
    bool HasAlpha,
    int? FrameCount = null);

public enum TerminalPixelFormat
{
    Rgb24,
    Rgba32,
}

public enum TerminalImageResamplingQuality
{
    Nearest,
    Linear,
    Medium,
    High,
}

public interface ITerminalImageRasterizer
{
    ValueTask<TerminalImageInfo> IdentifyAsync(
        ReadOnlyMemory<byte> encodedImage,
        CancellationToken cancellationToken = default);

    ValueTask<TerminalRasterImage> RasterizeAsync(
        TerminalImageFrame frame,
        TerminalRasterizeRequest request,
        CancellationToken cancellationToken = default);
}

public readonly record struct TerminalRasterizeRequest(
    TerminalImageSize TargetPixelSize,
    TerminalImageScaleMode ScaleMode,
    bool PreserveAspectRatio,
    TerminalImageColor? MatteColor,
    TerminalImageResamplingQuality Quality);

public sealed class TerminalRasterImage : IAsyncDisposable
{
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    public TerminalPixelFormat PixelFormat { get; init; }
    public ReadOnlyMemory<byte> PixelBytes { get; init; }
    public int StrideBytes { get; init; }
    public ValueTask DisposeAsync();
}
```

The SkiaSharp implementation should be internal or replaceable, for example `SkiaTerminalImageRasterizer`, but public contracts should stay expressed in XenoAtom types.

Recommended SkiaSharp use:

- use `SKCodec`/`SKBitmap`/`SKPixmap` for decode and pixel access
- use Skia resizing/sampling for `Fit`, `Fill`, `Stretch`, and `Center`
- normalize decoded pixels to a small set of explicit formats, preferably `Rgba32` and optionally `Rgb24`
- flatten alpha against `MatteColor` when the selected protocol or terminal cannot preserve alpha
- pass through original encoded PNG/JPEG/WebP bytes for Kitty/iTerm2 when no resize/crop/alpha conversion is required and the protocol supports the format
- rasterize to pixels for Sixel, because Sixel encoding requires palette/quantization over the final target pixels
- keep SkiaSharp object lifetimes tightly scoped and dispose `SKData`, `SKImage`, `SKBitmap`, `SKPixmap`, `SKCodec`, and `SKSurface` instances deterministically

Do not leak these into public APIs:

- `SKBitmap`
- `SKImage`
- `SKPixmap`
- `SKData`
- `SKCodec`

Native asset packaging should mirror the screenshot extension's cross-platform approach where possible, but Terminal.Graphics should document exactly which runtime native assets it brings in. HarfBuzz is not required for image decoding/resizing and should not be added unless a future feature specifically needs text shaping inside generated images.

Recommended source/frame model:

```csharp
public sealed class TerminalImageFrame : IAsyncDisposable
{
    public required TerminalImageFormat Format { get; init; }
    public required ReadOnlyMemory<byte> Data { get; init; }
    public required int PixelWidth { get; init; }
    public required int PixelHeight { get; init; }

    // Monotonic version from the source. Used by caches and real-time frame dropping.
    public long Version { get; init; }

    // Optional timestamp for real-time frame sources.
    public TimeSpan Timestamp { get; init; }

    public ValueTask DisposeAsync();
}

public abstract class TerminalImageSource
{
    public abstract ValueTask<TerminalImageFrame?> GetFrameAsync(
        TerminalImageFrameRequest request,
        CancellationToken cancellationToken = default);
}
```

Recommended encoder model:

```csharp
public readonly record struct TerminalImageEncodeRequest(
    TerminalGraphicsProtocol Protocol,
    TerminalImageSize PixelSize,
    TerminalImageSize CellSize,
    TerminalPixelMetrics? PixelMetrics,
    TerminalImageScaleMode ScaleMode,
    TerminalImageColor? MatteColor,
    bool PreserveAspectRatio);

public interface ITerminalImageEncoder
{
    TerminalGraphicsProtocol Protocol { get; }

    ValueTask<TerminalEncodedImage> EncodeAsync(
        TerminalImageFrame frame,
        TerminalImageEncodeRequest request,
        CancellationToken cancellationToken = default);
}
```

A `TerminalEncodedImage` should represent protocol-ready payload data plus any metadata the presenter needs:

```csharp
public sealed class TerminalEncodedImage
{
    public TerminalGraphicsProtocol Protocol { get; init; }
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    public TerminalImageSize CellSize { get; init; }
    public ReadOnlyMemory<byte> PayloadUtf8 { get; init; } // ASCII/UTF-8 terminal payload
    public IReadOnlyList<ReadOnlyMemory<byte>> Chunks { get; init; } = [];
    public string CacheKey { get; init; } = string.Empty;
}
```

Cache keys should include:

- source identity or content hash
- source frame version
- protocol
- target pixel size
- target cell size
- scale mode
- pixel metrics/cell size used for conversion
- matte/background color when transparency must be flattened
- raster backend and version when output can vary by backend
- Sixel palette/dither options

### Decode, Resize, And Encode Strategy

The graphics package should prefer a two-path image pipeline:

1. **Encoded passthrough path** for protocols that can consume the source encoding directly.
2. **Rasterized path** for transformations and protocols that require final pixels.

Use encoded passthrough when all of the following are true:

- the source bytes are already in a terminal-supported encoded format, such as PNG/JPEG/WebP where supported by the protocol
- no resize, crop, padding, matte, or alpha conversion is required
- the protocol can carry that exact encoding without quality or compatibility loss
- payload size is within configured limits

Use the SkiaSharp rasterized path when any of these are true:

- the image must be resized to match terminal cell/pixel bounds
- the image must be cropped, padded, or aspect-fit/fill transformed
- alpha must be flattened for a protocol or terminal
- the source format is not accepted directly by the selected protocol
- Sixel output is required
- deterministic dimensions are required for cache keys or layout diagnostics

Recommended first supported source formats through SkiaSharp:

- PNG
- JPEG
- WebP
- GIF first frame only
- raw RGB/RGBA frames supplied by application code

GIF animation and multi-frame image handling should not be part of v1 real-time support. Real-time sources should provide explicit frames through `ITerminalRealtimeImageSource` instead of relying on image container animation semantics.

Sixel encoding should consume the final raster pixels produced by the rasterizer. The Sixel encoder itself should remain independent from SkiaSharp so it can be tested against synthetic pixel buffers and reused with non-Skia frame sources.

### `XenoAtom.Terminal.UI`

Core `XenoAtom.Terminal.UI` should not depend on codecs or protocol encoders. It should add graphics-aware render infrastructure and extension points.

Recommended additions:

- a graphics display list type
- a graphics render context/pass
- an opt-in graphics render interface implemented only by controls that can emit graphics commands
- attach-time detection/registration of graphics-capable visuals, mirroring the existing `IAnimatedVisual` pattern
- an internal subtree marker/index so branches with no graphics-capable descendants are skipped during graphics collection
- host extension points for graphics presenters
- stable visual render identities
- invalidation hooks for graphics-only changes

Keep `CellBuffer` focused on text cells:

- glyphs
- styles
- hyperlinks
- text element tokens

Do not add raw image bytes or protocol payloads to individual cells.

### `XenoAtom.Terminal.UI.Graphics`

`XenoAtom.Terminal.UI.Graphics` should contain the public `Image` control and host presenter implementations.

Responsibilities:

- `Image` / `RealtimeImage` controls or one `Image` control with static and dynamic sources
- fallback content rendering
- source binding and frame subscription
- conversion from arranged cell bounds to graphics commands
- Kitty/iTerm2/Sixel presenters for fullscreen and inline hosts
- integration tests using fake presenters and fake encoders

## Protocol Reality

The target protocols are not equivalent. The abstraction must preserve those differences.

| Protocol | Terminal sequence family | Transport model | Placement model | Deletion/update model | Best UI role |
|---|---|---|---|---|---|
| Kitty | APC-like `ESC _ G ... ST` | chunked command payloads | retained image/placement model | explicit delete/replace/move semantics | primary retained UI protocol |
| iTerm2 | OSC 1337 `File=...` | base64 file payload at cursor | streamed at cursor | no general retained scene model | static inline fallback |
| Sixel | DCS `ESC P ... q ... ST` | streamed raster at cursor | streamed at cursor | clear/redraw region | Windows Terminal/static fallback |

Practical consequences:

- Kitty can support stable image IDs and retained placement. This is the best match for a retained UI framework.
- iTerm2 and Sixel should be treated as streamed protocols. In UI, they require conservative region repainting.
- Sixel is the important path for Windows Terminal. It should be a first-class target, not an afterthought.
- Real-time images are possible on all three only in the sense of repeated frame redraws. Kitty can be much smoother; iTerm2/Sixel need throttling and full-region invalidation.

## Capability Detection

Detection should be automatic, centralized in `XenoAtom.Terminal`, and explicitly overridable.

### Detection Inputs

Use these sources in order:

1. **Explicit options** from `TerminalOptions.Graphics`.
2. **Environment override** for scripts/tests and remote terminals.
3. **Backend facts**: ANSI enabled, input/output redirected, interactive input availability.
4. **Environment heuristics**: terminal name/program/session variables.
5. **Active probes** when an interactive input stream is available.
6. **Multiplexer policy**: disable, pass through, or degrade capabilities based on known multiplexer constraints.

Recommended options:

```csharp
public sealed class TerminalGraphicsOptions
{
    public TerminalGraphicsProtocol PreferredProtocol { get; set; } = TerminalGraphicsProtocol.None;
    public bool DisableGraphics { get; set; }
    public bool DisableProbing { get; set; }
    public bool AllowHeuristicEnablement { get; set; } = true;
    public bool AllowMultiplexerPassthrough { get; set; } = true;
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
    public TerminalPixelMetrics? ForcedPixelMetrics { get; set; }
    public IReadOnlyList<TerminalGraphicsProtocol>? ProtocolOrder { get; set; }
}
```

Recommended environment overrides:

```text
XENOATOM_TERMINAL_GRAPHICS=auto|none|kitty|iterm2|sixel
XENOATOM_TERMINAL_GRAPHICS_PROBING=0|1
XENOATOM_TERMINAL_GRAPHICS_PASSTHROUGH=0|1
XENOATOM_TERMINAL_CELL_SIZE=9x18
```

### Heuristic Detection

Useful environment hints include:

- `TERM`
- `TERM_PROGRAM`
- `LC_TERMINAL`
- `KITTY_WINDOW_ID`
- `ITERM_SESSION_ID`
- `WT_SESSION`
- `WT_PROFILE_ID`
- `KONSOLE_VERSION`
- `TMUX`
- `STY`
- `SSH_TTY`

Recommended heuristic behavior:

- If ANSI output is disabled or output is redirected, graphics are disabled unless forced.
- If Kitty-specific environment variables are present, score Kitty highly.
- If iTerm2-specific environment variables are present, score iTerm2 highly.
- If Windows Terminal session variables are present, score Sixel highly.
- If a multiplexer is detected, do not blindly enable graphics unless pass-through support is known or explicitly allowed.
- If an SSH session is detected, keep heuristics but make diagnostics clear that local terminal facts may be unavailable.

### Active Probing

Active probing should be attempted when:

- input is interactive
- output is interactive
- raw/VT input can capture terminal replies
- probing is not disabled
- the application is not in a mode where probes would corrupt user input

Probe handling must be centralized. Probe replies should be consumed by the terminal input/probe coordinator and must not leak as keypresses or text input.

Recommended probes:

- Kitty graphics query/reply helpers.
- iTerm2 feature reporting or image capability query when available.
- Sixel support via terminal feature reporting/device attributes where available; otherwise use strong heuristics plus an explicit diagnostic.
- Pixel metrics queries:
  - cell pixel size (`CSI 16 t` response family)
  - window pixel size (`CSI 14 t` response family)

Probe results should include:

- protocol confirmed/denied/timeout
- raw reply tokens for diagnostics
- source of the decision
- whether the final result was forced, heuristic, or confirmed

### Protocol Selection

Selection should be feature-scored, not a fixed global order.

Default preference when multiple protocols are supported:

1. Kitty, because it maps best to retained UI and real-time placement updates.
2. Sixel, especially when the terminal is Windows Terminal or when Sixel is confirmed.
3. iTerm2, for terminals where iTerm2 inline images are the native/confirmed option.

User preference overrides the order if the selected protocol is supported or explicitly forced.

Selection examples:

| Environment/probe result | Preferred protocol |
|---|---|
| Kitty confirmed | Kitty |
| Windows Terminal with Sixel confirmed or strongly indicated | Sixel |
| iTerm2 confirmed | iTerm2 |
| Kitty and Sixel both confirmed | Kitty |
| Sixel and iTerm2 both confirmed | Sixel for UI, unless user prefers iTerm2 |
| No probe, no strong heuristic | None |

## Pixel Metrics And Sizing

Images are laid out in terminal cells, but encoded in pixels. The stack needs a shared pixel metric model.

Recommended type in `XenoAtom.Terminal`:

```csharp
public readonly record struct TerminalPixelMetrics(
    int WindowPixelWidth,
    int WindowPixelHeight,
    int CellPixelWidth,
    int CellPixelHeight,
    int Columns,
    int Rows);
```

Rules:

- `XenoAtom.Terminal.UI` layout remains cell-based.
- `Image` measures/arranges in cells.
- Encoders use pixel metrics to convert arranged cell bounds to target pixel dimensions.
- If metrics are unknown, protocols that accept cell dimensions should use cell dimensions directly.
- If metrics are unknown and pixel dimensions are required, use conservative defaults only when the user supplied explicit cell bounds; otherwise render fallback content or defer until metrics are available.

Recommended default behavior for an `Image` control:

- If width and height are explicitly constrained in cells, use those cells.
- If only width is constrained, compute height from image aspect ratio and cell metrics.
- If only height is constrained, compute width from image aspect ratio and cell metrics.
- If metrics are unknown, assume a conservative cell aspect ratio only for measuring fallback; re-render when metrics arrive.
- Preserve aspect ratio by default.

## Rendering Model In Terminal.UI

### Do Not Embed Graphics In Text Lines

A tempting shortcut is to make an `Image` control return text lines containing graphics escape sequences. Avoid this in Terminal.UI.

That shortcut causes architectural problems:

- `visibleWidth` and clipping utilities must special-case large escape payloads.
- diff renderers treat image payload changes as text line changes.
- controls can write protocol-specific sequences directly.
- old retained images are not deleted reliably.
- streamed images become difficult to clear when visuals move or disappear.
- dirty rendering can skip a graphics update because the visible text cells did not change.

Instead, Terminal.UI should render:

```text
TerminalRenderFrame
  ├── CellBuffer              // text plane
  └── GraphicsCommandBuffer   // graphics plane/display list
```

### Recommended Core UI Types

```csharp
public sealed class TerminalRenderFrame
{
    public CellBuffer Cells { get; }
    public GraphicsCommandBuffer Graphics { get; }
}

public sealed class GraphicsCommandBuffer
{
    public int Count { get; }
    public void Clear();
    public void Add(in GraphicsCommand command);
    public ReadOnlySpan<GraphicsCommand> AsSpan();
}

public sealed class GraphicsRenderContext
{
    public GraphicsCommandBuffer Commands { get; }
    public ulong CurrentVisualRenderId { get; }
    public Rectangle ClipBounds { get; }

    public void Add(
        Rectangle cellBounds,
        TerminalGraphicContent content,
        TerminalImageScaleMode scaleMode,
        bool preserveAspectRatio,
        bool reserveCells,
        string? accessibilityText = null);
}

public readonly record struct GraphicsCommand(
    ulong VisualRenderId,
    Rectangle CellBounds,
    Rectangle ClipBounds,
    TerminalGraphicContent Content,
    TerminalImageScaleMode ScaleMode,
    bool PreserveAspectRatio,
    int PaintOrder,
    bool ReserveCells,
    string? AccessibilityText);

public interface IGraphicsRenderableVisual
{
    void RenderGraphics(GraphicsRenderContext context);
}
```

`TerminalGraphicContent`, `TerminalImageScaleMode`, and other types used by `GraphicsCommand` should be lightweight, codec-free descriptors in core UI or dependency-neutral shared abstractions. The optional graphics packages resolve them to frames/encoded payloads. Do not make core `XenoAtom.Terminal.UI` reference `XenoAtom.Terminal.Graphics` only to define the display-list contract.

`IGraphicsRenderableVisual` should be an opt-in interface, not a virtual method on every `Visual`. Most controls never produce graphics commands, and the framework should not require every visual to participate in a graphics-specific render callback.

`GraphicsRenderContext` should be created by the framework for the current graphics visual. It should supply the current clip and visual render identity, and it should assign `PaintOrder` when commands are added. Controls should not allocate their own retained image IDs or infer global z-order.

Graphics-capable visuals should be detected when a `Visual` is attached to or detached from a `TerminalApp`, similarly to the existing animation registration pattern for `IAnimatedVisual`. The framework should own this registration; controls should normally only implement `IGraphicsRenderableVisual`, not call app registration methods themselves.

Attachment-time detection is sufficient because implementing `IGraphicsRenderableVisual` is a type-level capability. A particular instance may still emit zero commands when its `Source` is null, graphics are disabled, or fallback content is active; it should remain registered until detached.

### Graphics Collection Pass

Do not rely only on `RenderOverride(CellBuffer)` to collect graphics. The current UI renderer supports dirty rendering and can skip `RenderTree` when only the host/cursor needs updating. A robust graphics design needs a display list that represents the current visual tree each frame.

Recommended approach:

- keep `RenderOverride(CellBuffer)` for text cells
- add a separate graphics pass that calls only visuals implementing `IGraphicsRenderableVisual`:

```csharp
public sealed partial class Image : Visual, IGraphicsRenderableVisual
{
    void IGraphicsRenderableVisual.RenderGraphics(GraphicsRenderContext context)
    {
        // Add one or more GraphicsCommand entries.
    }
}
```

- collect the full graphics display list after layout, even when the text scene render mode is `None`
- track binding reads from the graphics pass, either as `DependencyKind.GraphicsRender` or by merging them into existing render dependencies
- diff graphics commands separately from cell diffs
- avoid calling a no-op graphics method on every visual
- use attach/detach-time interface detection to maintain the graphics-capable visual registry/index
- maintain an internal per-subtree graphics count/flag so the traversal can skip entire branches with no graphics-capable descendants

Why a full graphics list is recommended:

- disappearing visuals can be detected and deleted
- moving visuals can update placement even when their text cells did not change
- real-time frame updates can be presented without relying on text payload changes
- streamed protocols can know which previous regions must be cleared

"Full" means every currently visible graphics command must be present in the frame snapshot. It does not require invoking graphics callbacks on visuals that cannot produce graphics.

Why an opt-in interface is recommended:

- graphics-capable controls are expected to be rare compared to text/layout controls
- a virtual `RenderGraphicsOverride(...)` on `Visual` would add a callback and dependency-tracking session for many visuals that can never emit graphics
- an interface keeps the base `Visual` contract smaller and makes graphics support explicit in controls such as `Image`
- explicit interface implementation prevents most application code from calling graphics collection methods directly while still allowing the framework to invoke them efficiently
- an internal subtree marker keeps collection close to `O(number of graphics visuals + their ancestor paths)`, not `O(total visuals)`, for typical UI trees
- attach-time detection amortizes interface checks to structural changes instead of doing repeated type checks across the whole tree every frame

The graphics pass should still respect:

- `IsVisible`
- visual bounds
- parent clipping
- child render order
- overlays and `ZStack` order

Suggested collection algorithm:

1. When `Visual.AttachToApp(TerminalApp)` runs, check whether the visual implements `IGraphicsRenderableVisual`.
2. If it does, register the visual with the app's graphics render registry and increment `GraphicsRenderableSubtreeCount` or equivalent markers on the visual and its ancestors.
3. When `Visual.DetachFromApp()` runs, unregister the visual before clearing `App` and decrement the same ancestor markers while the parent chain is still available.
4. Child attach/detach and dynamic child replacement should use the same hooks, so `ComputedVisual`, popups, dialogs, and collection-backed panels update the graphics registry automatically.
5. After layout, collect graphics by walking the root tree in normal render order but immediately returning from subtrees whose count is zero.
6. Apply visibility and clip checks before descending into a marked subtree.
7. When a visited visual is registered as graphics-renderable, start a graphics dependency-tracking session and call `RenderGraphics(context)`.
8. Continue into marked children to preserve normal child render order and overlay/z-order semantics.
9. Assign `PaintOrder` from the actual traversal order so presenters can diff and replay commands deterministically.

Conceptual attach/detach integration:

```csharp
internal void AttachToApp(TerminalApp app)
{
    App = app;
    OnAttachedToApp(app);

    if (this is IAnimatedVisual animated)
    {
        app.RegisterAnimatedVisual(animated);
    }

    if (this is IGraphicsRenderableVisual graphics)
    {
        app.RegisterGraphicsRenderableVisual(this, graphics);
    }

    AttachChildrenToApp(app);
}

internal void DetachFromApp()
{
    var app = App;
    if (app is null)
    {
        return;
    }

    DetachChildrenFromApp();

    if (this is IGraphicsRenderableVisual graphics)
    {
        app.UnregisterGraphicsRenderableVisual(this, graphics);
    }

    if (this is IAnimatedVisual animated)
    {
        app.UnregisterAnimatedVisual(animated);
    }

    OnDetachedFromApp(app);
    App = null;
}
```

The exact order can follow the existing `Visual.AttachToApp`/`DetachFromApp` implementation, but the important rule is that unregistering happens before `App` and `Parent` context needed by the graphics index is lost.

A flat app-level list of `IGraphicsRenderableVisual` instances is useful for quick checks and diagnostics, but it should not be the only source used for presentation order. Graphics output needs normal visual-tree order, inherited clipping, visibility, and overlay semantics, so collection should still traverse marked ancestor paths rather than replaying an arbitrary registration order.

The subtree marker is an optimization only. Correctness should not depend on it being perfect during exceptional tree mutation paths: a safe implementation may fall back to a full visible-tree scan after structural invalidation, then rebuild the marker/index.

Debug/performance metrics should separate graphics collection from text rendering. Useful counters include skipped non-graphics subtrees, visited graphics-path visuals, invoked `IGraphicsRenderableVisual` count, emitted command count, collection time, encode time, and presenter output time.

### Text And Graphics Output Ordering

The host should own frame output ordering.

Recommended frame order:

1. begin synchronized output when supported
2. hide/suppress the cursor if needed
3. delete or clear stale graphics from the previous frame
4. render text cell diffs / clear dirty text regions
5. present current graphics commands
6. restore style/hyperlink state
7. restore cursor position/visibility
8. end synchronized output

For default `Image` behavior, the control should reserve its cell rectangle and write blank/background cells in that rectangle during the text render pass. The graphics presenter then paints the image over that reserved region.

Text-over-image should not be promised in v1. It can be a later Kitty-only feature because streamed protocols do not provide a portable retained layering model.

## Graphics Presenters

Terminal.UI hosts should not know protocol details directly. Use presenter implementations selected from terminal capabilities.

```csharp
internal interface ITerminalGraphicsPresenter : IDisposable
{
    TerminalGraphicsCapabilities Capabilities { get; }

    ValueTask PresentAsync(
        GraphicsCommandBuffer current,
        TerminalGraphicsPresentContext context,
        CancellationToken cancellationToken = default);

    void Reset();
}
```

Concrete presenters:

- `NoGraphicsPresenter`
- `KittyGraphicsPresenter`
- `ITerm2GraphicsPresenter`
- `SixelGraphicsPresenter`

The presenter owns:

- previous graphics command snapshot
- retained image ID mapping
- previous streamed dirty regions
- protocol-specific deletion/clear strategy
- payload caching and encoder calls
- output chunking
- throttling for real-time updates

### Fullscreen Host

Fullscreen is the first UI host to support graphics because it owns the viewport and can reliably clear regions.

Recommended policy:

- collect full graphics command list every frame
- diff text cells as today
- diff graphics commands separately
- Kitty:
  - allocate stable image IDs from stable visual render IDs plus source/size hashes
  - upload content once per content+size where possible
  - place/move retained images when bounds change
  - delete images when commands disappear
  - delete all images owned by the presenter on reset/dispose
- Sixel/iTerm2:
  - treat commands as streamed region snapshots
  - clear previous image regions when commands move/disappear/change
  - redraw changed image regions after text clears
  - initially prefer full image-region redraw over clever partial updates

### Inline/Live Host

Inline/live rendering is harder because normal output can scroll, and the live region can move.

Recommended staged support:

1. direct `Terminal.WriteImageAsync(...)` for static flow output
2. fullscreen UI image controls
3. inline/live UI static images
4. inline/live UI real-time images only after region movement/resize behavior is proven

Inline/live policy:

- reserve a cell region exactly as text live rendering does
- anchor images to the live region top, not absolute terminal history coordinates unless the protocol supports scroll-following placement
- on resize or flow output above the live region, invalidate all streamed graphics and repaint the full live region
- for Kitty, use placement behavior that scrolls with the buffer only where supported and tested
- for Sixel/iTerm2, treat the whole live region as dirty after scroll/resize

## The `Image` Control

The first public UI control should be a declarative image visual in `XenoAtom.Terminal.UI.Graphics`.

Suggested API shape:

```csharp
public sealed partial class Image : Visual
{
    [Bindable] public partial TerminalImageSource? Source { get; set; }
    [Bindable] public partial TerminalImageScaleMode ScaleMode { get; set; }
    [Bindable] public partial bool PreserveAspectRatio { get; set; }
    [Bindable] public partial Visual? FallbackContent { get; set; }
    [Bindable] public partial string? AccessibilityText { get; set; }
    [Bindable] public partial bool ReserveCells { get; set; }
}
```

Suggested scale modes:

```csharp
public enum TerminalImageScaleMode
{
    None,
    Fit,
    Fill,
    Stretch,
    Center,
}
```

Behavior:

- measure in cells
- arrange to a cell rectangle
- fill/reserve the arranged cell rectangle in `RenderOverride(CellBuffer)` when `ReserveCells` is true
- implement `IGraphicsRenderableVisual` and emit graphics commands from `RenderGraphics(...)`
- rely on the framework's attach/detach-time graphics registration; do not manually register with the app from the control
- render `FallbackContent` when graphics are unavailable, disabled, still probing, or source loading fails
- preserve aspect ratio by default
- never write protocol escape sequences from the control

Fallback content should default to an accessible text placeholder containing:

- optional file/name label
- image dimensions if known
- protocol/capability diagnostic only in debug/developer mode

## Real-Time Images

Real-time images mean repeated image frame updates: camera frames, charts, previews, generated images, remote screen snapshots, or other dynamic raster sources. This is not video playback in the media-player sense.

Recommended abstractions:

```csharp
public interface ITerminalRealtimeImageSource : IAsyncDisposable
{
    event EventHandler<TerminalImageFrameAvailableEventArgs>? FrameAvailable;

    ValueTask<TerminalImageFrame?> GetLatestFrameAsync(
        TerminalImageFrameRequest request,
        CancellationToken cancellationToken = default);
}
```

Rules for real-time updates:

- latest frame wins; stale frames may be dropped
- only one encode per image/control/protocol should run at a time unless explicitly configured
- if a newer frame arrives while encoding an older frame, cancel or skip the older frame when possible
- throttle by frame rate and byte budget
- do not write directly to the terminal from a frame producer
- frame notifications should schedule a UI render through the dispatcher/app loop
- the `Image` control should invalidate its own bounds with `layoutImpact: false` when only frame content changes
- if source dimensions change, layout may need invalidation
- when frames are already raw pixels, avoid round-tripping through encoded formats before resizing/encoding
- reuse raster buffers where possible; repeated SkiaSharp allocations can dominate small high-frequency image updates

Recommended options:

```csharp
public sealed class TerminalRealtimeImageOptions
{
    public int MaxFramesPerSecond { get; set; } = 30;
    public int MaxBytesPerSecond { get; set; } = 4_000_000;
    public bool DropLateFrames { get; set; } = true;
    public TimeSpan EncodeTimeout { get; set; } = TimeSpan.FromMilliseconds(100);
}
```

Protocol guidance for real-time images:

- Kitty: preferred; use retained IDs/placements and replace content with minimal flicker.
- Sixel: acceptable for low-to-moderate rates and small regions; redraw the region and throttle aggressively.
- iTerm2: acceptable for static or low-frequency updates; do not assume high frame rates.

SkiaSharp-specific real-time guidance:

- use SkiaSharp for high-quality resizing, color conversion, and optional alpha flattening
- avoid per-frame `SKBitmap`/`SKImage` churn when the source can provide stable raw pixel buffers
- prefer raw `Rgba32`/`Rgb24` frames for generated or camera-like sources
- keep PNG/WebP re-encoding out of the hot path unless the selected protocol benefits enough from compression to offset encode cost
- include encode duration and rasterization duration separately in diagnostics so bottlenecks are visible

## Direct Terminal Image API

Direct image output should live in `XenoAtom.Terminal.Graphics` as extension members, not in the `XenoAtom.Terminal` core assembly.

Suggested API:

```csharp
public static class TerminalGraphicsExtensions
{
    public static TerminalGraphicsService GetGraphics(this TerminalInstance terminal);

    public static ValueTask WriteImageAsync(
        this TerminalInstance terminal,
        TerminalImageSource source,
        TerminalImageWriteOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

Options:

```csharp
public sealed class TerminalImageWriteOptions
{
    public TerminalGraphicsProtocol PreferredProtocol { get; set; } = TerminalGraphicsProtocol.None;
    public int? WidthCells { get; set; }
    public int? HeightCells { get; set; }
    public int? WidthPixels { get; set; }
    public int? HeightPixels { get; set; }
    public TerminalImageScaleMode ScaleMode { get; set; } = TerminalImageScaleMode.Fit;
    public bool PreserveAspectRatio { get; set; } = true;
    public string? FallbackText { get; set; }
    public string? FallbackMarkup { get; set; }
}
```

Direct output policy:

- use selected terminal graphics protocol
- reserve the required number of rows before writing the graphics payload
- leave the cursor after the image region
- if graphics are unavailable, write fallback text
- avoid retained Kitty images unless the direct API returns a handle that can delete/update them

For direct real-time output, prefer an explicit handle:

```csharp
public interface ITerminalImageHandle : IAsyncDisposable
{
    ValueTask UpdateAsync(TerminalImageFrame frame, CancellationToken cancellationToken = default);
}
```

## Protocol-Specific Guidance

### Kitty

Use Kitty as the primary retained UI protocol.

Recommended v1 features:

- inline/direct upload using base64 payload chunks
- stable image IDs owned by the presenter
- placement in cell bounds
- update/replace when image content or target size changes
- delete on visual removal, presenter reset, and app exit
- diagnostics for unsupported replies/timeouts

Implementation notes:

- use protocol chunking instead of constructing very large strings
- separate content identity from placement identity when the protocol supports it
- prefer direct payload transport by default; file-based transport can leak paths and should require explicit opt-in
- keep all IDs within a presenter-owned namespace to avoid collisions with user-written graphics
- on full presenter reset, delete only images owned by this presenter where possible; otherwise use a conservative cleanup mode only when the app owns the screen

### iTerm2 Inline Images

Treat iTerm2 inline images as streamed output at the cursor.

Recommended v1 features:

- static images
- cell width/height parameters where supported
- fallback content when unsupported
- full region redraw for UI changes

Do not model iTerm2 as a fully retained scene. It should not be the first target for high-rate UI images.

### Sixel

Treat Sixel as the primary streamed fallback and the important Windows Terminal path.

Recommended v1 features:

- static image rendering
- resizing to target pixel bounds
- palette generation/quantization
- optional dithering
- region clear + full redraw when content changes
- conservative frame-rate throttling for real-time sources

Implementation notes:

- Sixel payloads can be large; encode to an `IBufferWriter<byte>`/`IBufferWriter<char>` style target to avoid giant intermediate strings
- use the rasterizer output as the input to quantization; the Sixel encoder should not decode image formats itself
- cache encoded Sixel by content hash + target size + palette/dither options
- on Windows Terminal, prefer active confirmation when possible, but allow strong heuristic enablement with clear diagnostics and overrides
- do not promise retained deletion; clearing/redrawing the cell region is the portable model

## Multiplexers, SSH, And Passthrough

Multiplexers and remote sessions are the most common source of graphics failure.

Detection should record:

- whether a multiplexer appears to be active
- which multiplexer was detected
- whether passthrough is enabled or forced
- which protocol was disabled or degraded because of the multiplexer

Rules:

- controls never wrap protocol escapes for multiplexers
- `XenoAtom.Terminal` decides whether passthrough is allowed
- presenters ask the terminal graphics service how to wrap or whether to disable
- active probing should run through the same passthrough path that real graphics output would use
- user override must be able to force a protocol under SSH/multiplexers

## Reliability And Security

Image protocols can send very large terminal payloads. The implementation needs hard limits.

Recommended safeguards:

- max source byte size
- max metadata/probe decode time before full raster allocation
- max decoded pixel count
- max target pixel count
- max encoded payload size
- max payload bytes per second for real-time images
- timeout/cancellation for decode, resize, quantize, and encode
- no file-path transport by default
- sanitize protocol parameters
- do not allow arbitrary escape injection through filenames, alt text, or metadata
- do not probe when input/output are redirected unless forced
- expose diagnostics but avoid logging raw image payloads
- validate SkiaSharp decode results before allocation-heavy transformations
- treat unsupported or malformed image data as recoverable source errors that render fallback content

Memory ownership rules:

- avoid copying frames unnecessarily
- use pooled buffers for decoded/resized frames where possible
- make frame ownership explicit (`IAsyncDisposable`, `IMemoryOwner<byte>`, or documented copy semantics)
- do not store user-provided mutable buffers directly in long-lived caches without hashing/copying
- dispose native SkiaSharp resources deterministically and never keep `SK*` objects in long-lived public models

## Testing Strategy

### `XenoAtom.Ansi`

- [ ] Writer tests for OSC, DCS, APC, and terminators.
- [ ] Tokenizer tests for DCS/APC/PM/SOS tokens across chunk boundaries.
- [ ] Tokenizer limit tests for oversized payloads.
- [ ] Parser tests for graphics probe replies that are decoded in Ansi.

### `XenoAtom.Terminal`

- [ ] Environment snapshot tests for detection heuristics.
- [ ] Explicit override tests.
- [ ] Active probe tests with fake input/output backends.
- [ ] Timeout and reply-consumption tests.
- [ ] Pixel metrics query parsing tests.
- [ ] Multiplexer passthrough policy tests.
- [ ] Diagnostics tests.

### `XenoAtom.Terminal.Graphics`

- [ ] Image metadata tests.
- [ ] Scaling/cropping tests.
- [ ] SkiaSharp rasterizer tests for PNG/JPEG/WebP/GIF-first-frame decode.
- [ ] Alpha flattening and matte-color tests.
- [ ] Protocol payload/chunking tests.
- [ ] Sixel quantization tests using small deterministic images.
- [ ] Cache key tests.
- [ ] Cancellation/latest-wins tests.
- [ ] Direct terminal API tests with virtual backends.

### `XenoAtom.Terminal.UI`

- [ ] Graphics display list collection tests.
- [ ] Opt-in graphics interface collection tests.
- [ ] Attach/detach-time graphics registration tests, mirroring the `IAnimatedVisual` attachment pattern.
- [ ] Subtree graphics marker/index tests, including attach/detach and dynamic child replacement.
- [ ] Graphics binding dependency tests.
- [ ] Dirty-rendering tests where only graphics content changes.
- [ ] Presenter lifecycle tests with a fake presenter.
- [ ] Deletion tests when image visuals disappear.
- [ ] Fallback rendering tests.
- [ ] Fullscreen host graphics ordering tests.
- [ ] Inline/live invalidation tests.

### Manual/Integration Validation

Maintain a small manual validation matrix:

- [ ] Kitty protocol terminal: static image, move/resize, delete, real-time updates.
- [ ] Windows Terminal: Sixel static image, resize, region clear, low-rate real-time updates.
- [ ] iTerm2 protocol terminal: static inline image and fallback behavior.
- [ ] tmux or equivalent multiplexer: disabled by default, passthrough when configured.
- [ ] Redirected output: no graphics by default.

## Phased Implementation Checklist

Complete the work bottom-up so lower layers are stable before the UI layer depends on them.

### Phase 0: Local Workspace Wiring

- [x] Verify the local `XenoAtom.Ansi` checkout is present at `../XenoAtom.Ansi`.
- [x] Verify the local `XenoAtom.Terminal` checkout is present at `../XenoAtom.Terminal`.
- [x] Wire `XenoAtom.Terminal` to the local `XenoAtom.Ansi.csproj` with a temporary `ProjectReference` using the correct relative path from the edited `.csproj`.
- [x] Wire `XenoAtom.Terminal.UI` to the local `XenoAtom.Terminal.csproj` with a temporary `ProjectReference` using the correct relative path from the edited `.csproj`.
- [x] Add temporary direct local `XenoAtom.Terminal.csproj` references to downstream UI extension/sample/test projects that need complete project information during restore.
- [x] Add local dependency projects to the development `.slnx` files so solution builds use the intended configuration across the chain.
- [x] Keep the temporary project-reference changes separate from unrelated work so they can be reverted cleanly after package releases.

### Phase 1: `XenoAtom.Ansi` Low-Level ANSI/VT Support

- [ ] Add OSC/DCS/APC writer helpers to `XenoAtom.Ansi`.
- [ ] Add tokenizer support for DCS/APC string tokens.
- [ ] Add tests for chunked parsing and malformed/oversized sequences.
- [ ] From `../XenoAtom.Ansi/src`, run `dotnet build -c Release` and `dotnet test -c Release` before moving up the stack.

### Phase 2: `XenoAtom.Terminal` Capabilities And Probing

- [ ] Add `TerminalGraphicsCapabilities`.
- [ ] Add `TerminalGraphicsOptions`.
- [ ] Add heuristic detection for Kitty, iTerm2, Sixel, Windows Terminal, multiplexers, and redirected output.
- [ ] Add active probe coordinator and reply consumption.
- [ ] Add pixel metrics queries.
- [ ] Expose diagnostics.
- [ ] From `../XenoAtom.Terminal/src`, run `dotnet build -c Release` and `dotnet test -c Release` against the local `XenoAtom.Ansi` project reference.

### Phase 3: `XenoAtom.Terminal.Graphics` Optional Encoding Package

- [ ] Create `XenoAtom.Terminal.Graphics`.
- [ ] Define image source/frame abstractions.
- [ ] Add the SkiaSharp-backed rasterizer as the default decoder/resizer.
- [ ] Implement static image encode paths for Kitty, iTerm2, and Sixel.
- [ ] Implement resizing and Sixel quantization.
- [ ] Add payload chunking and caches.
- [ ] Add direct `WriteImageAsync(...)` APIs.
- [ ] From `../XenoAtom.Terminal/src`, run `dotnet build -c Release` and `dotnet test -c Release` with local lower-layer project references.

### Phase 4: `XenoAtom.Terminal.UI` Graphics Plane

- [ ] Add graphics display-list types to Terminal.UI core.
- [ ] Add `IGraphicsRenderableVisual` and the optimized graphics collection pass.
- [ ] Wire `IGraphicsRenderableVisual` registration into `Visual.AttachToApp`/`DetachFromApp`, similar to `IAnimatedVisual`.
- [ ] Add internal subtree graphics markers so non-graphics branches are skipped.
- [ ] Add host graphics presenter extension points.
- [ ] Add fake/no-op presenter tests.
- [ ] Ensure dirty rendering can present graphics-only changes.
- [ ] From this repository's `src` directory, run `dotnet build -c Release` and `dotnet test -c Release` against the local `XenoAtom.Terminal` project reference.

### Phase 5: `XenoAtom.Terminal.UI.Graphics` Fullscreen `Image` Control

- [ ] Create `XenoAtom.Terminal.UI.Graphics`.
- [ ] Implement the `Image` control.
- [ ] Implement the fullscreen Kitty presenter.
- [ ] Implement the fullscreen Sixel presenter for Windows Terminal and other Sixel terminals.
- [ ] Implement the iTerm2 presenter as streamed fallback.
- [ ] Add fallback content behavior.
- [ ] Add samples or manual validation hooks for static image rendering.

### Phase 6: Direct And Inline Refinement

- [ ] Refine direct terminal image output.
- [ ] Add inline/live UI static image support.
- [ ] Add resize/scroll invalidation rules.
- [ ] Validate under multiplexers and remote sessions.

### Phase 7: Real-Time Image Updates

- [ ] Add `ITerminalRealtimeImageSource`.
- [ ] Add frame scheduling/throttling/latest-wins encoding.
- [ ] Optimize Kitty updates.
- [ ] Add conservative Sixel/iTerm2 redraw paths.
- [ ] Add metrics/diagnostics for dropped frames, encode time, payload bytes, and effective FPS.

### Phase 8: Package Release And Reference Unwinding

- [ ] Before any release or commit, confirm the coordinated local changes are working across `XenoAtom.Ansi`, `XenoAtom.Terminal`, and `XenoAtom.Terminal.UI`.
- [ ] Release `XenoAtom.Ansi` after its graphics protocol primitives are validated.
- [ ] Replace the temporary `XenoAtom.Ansi.csproj` reference in `XenoAtom.Terminal` with the released package reference.
- [ ] Release `XenoAtom.Terminal` after capabilities/probing are validated against the released `XenoAtom.Ansi` package.
- [ ] Release `XenoAtom.Terminal.Graphics` after encoding/rasterization/direct output are validated.
- [ ] Replace the temporary `XenoAtom.Terminal.csproj` reference in `XenoAtom.Terminal.UI` with the released package reference.
- [ ] Release `XenoAtom.Terminal.UI` after the core graphics plane is validated against released lower-layer packages.
- [ ] Release `XenoAtom.Terminal.UI.Graphics` after the `Image` control and presenters are validated.
- [ ] Run final end-to-end validation using package references, not sibling project references.

## Follow-Up Specs To Prepare

This document is the cross-stack architecture. It should be followed by focused specs for each repository/package.

### `XenoAtom.Ansi` Spec

Cover:

- [ ] Exact OSC/DCS/APC writer APIs.
- [ ] Tokenizer token model for terminal string controls.
- [ ] Safety limits.
- [ ] Probe reply parsing boundaries.
- [ ] Tests and golden sequences.

### `XenoAtom.Terminal` Spec

Cover:

- [ ] `TerminalGraphicsCapabilities` exact API.
- [ ] `TerminalGraphicsOptions` exact API.
- [ ] Detection heuristics.
- [ ] Active probing lifecycle.
- [ ] Input demultiplexing for probe replies.
- [ ] Pixel metrics querying.
- [ ] Multiplexer passthrough.
- [ ] Diagnostics format.

### `XenoAtom.Terminal.Graphics` Spec

Cover:

- [ ] Image source/frame ownership.
- [ ] Supported source formats.
- [ ] SkiaSharp dependency/native asset policy.
- [ ] Rasterizer abstractions and SkiaSharp implementation details.
- [ ] Resize/scale/crop algorithms.
- [ ] Encoder interfaces.
- [ ] Kitty/iTerm2/Sixel payload formats.
- [ ] Sixel quantization/dithering.
- [ ] Cache architecture.
- [ ] Direct terminal APIs.
- [ ] Real-time frame scheduling.

### `XenoAtom.Terminal.UI` Spec

Cover:

- [ ] `TerminalRenderFrame` / `GraphicsCommandBuffer`.
- [ ] `IGraphicsRenderableVisual` and explicit graphics opt-in rules.
- [ ] Attach-time registration lifecycle in `Visual.AttachToApp`/`DetachFromApp`.
- [ ] Optimized graphics collection pass, subtree markers/indexing, and binding dependencies.
- [ ] Host presenter extension points.
- [ ] Fullscreen and inline ordering.
- [ ] Interaction with dirty rendering and debug overlay.
- [ ] Visual render identity allocation.

### `XenoAtom.Terminal.UI.Graphics` Spec

Cover:

- [ ] `Image` control API.
- [ ] Fallback content.
- [ ] Source binding and frame subscriptions.
- [ ] Presenter implementations.
- [ ] Real-time image control behavior.
- [ ] Samples and screenshots/manual validation.

## References

- [Kitty graphics protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [iTerm2 inline images](https://iterm2.com/documentation-images.html)
- [iTerm2 feature reporting](https://iterm2.com/feature-reporting/)
- [xterm control sequences, including Sixel-related sequences](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html)
