// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Describes how much of the text scene was rendered for the frame associated with a graphics presentation pass.
/// </summary>
public enum TerminalGraphicsTextFrameKind
{
    /// <summary>
    /// No text cells were rendered for this frame.
    /// </summary>
    None = 0,

    /// <summary>
    /// A dirty subset of the text scene was rendered for this frame.
    /// </summary>
    Dirty = 1,

    /// <summary>
    /// The full text scene was rendered for this frame.
    /// </summary>
    Full = 2,
}

/// <summary>
/// Provides frame metadata to a terminal graphics presenter.
/// </summary>
public sealed class TerminalGraphicsPresentContext
{
    internal TerminalGraphicsPresentContext(
        TerminalApp app,
        TerminalInstance terminal,
        TerminalHostKind hostKind,
        Rectangle viewportBounds,
        int frameIndex,
        Rectangle textRepaintBounds,
        TerminalGraphicsTextFrameKind textFrameKind)
    {
        App = app;
        Terminal = terminal;
        HostKind = hostKind;
        ViewportBounds = viewportBounds;
        FrameIndex = frameIndex;
        TextRepaintBounds = textRepaintBounds;
        TextFrameKind = textFrameKind;
    }

    /// <summary>
    /// Gets the application that owns the frame.
    /// </summary>
    public TerminalApp App { get; }

    /// <summary>
    /// Gets the terminal instance used by the host.
    /// </summary>
    public TerminalInstance Terminal { get; }

    /// <summary>
    /// Gets the host kind for the frame.
    /// </summary>
    public TerminalHostKind HostKind { get; }

    /// <summary>
    /// Gets the viewport represented by the current frame, in terminal cells.
    /// </summary>
    /// <remarks>
    /// For fullscreen hosts this rectangle normally starts at <c>(0, 0)</c>. Inline hosts may use <see cref="Rectangle.X"/>
    /// and <see cref="Rectangle.Y"/> as the current terminal-screen origin for the live region, while graphics commands
    /// remain relative to the rendered root visual.
    /// </remarks>
    public Rectangle ViewportBounds { get; }

    /// <summary>
    /// Gets the application render frame index.
    /// </summary>
    public int FrameIndex { get; }

    /// <summary>
    /// Gets the text-scene rectangle repainted for the frame, in the same root-relative cell coordinates used by graphics commands.
    /// </summary>
    /// <remarks>
    /// The rectangle is empty when <see cref="TextFrameKind"/> is <see cref="TerminalGraphicsTextFrameKind.None"/>. It covers the
    /// full rendered root when <see cref="TextFrameKind"/> is <see cref="TerminalGraphicsTextFrameKind.Full"/>.
    /// </remarks>
    public Rectangle TextRepaintBounds { get; }

    /// <summary>
    /// Gets the text-scene render mode for the frame.
    /// </summary>
    public TerminalGraphicsTextFrameKind TextFrameKind { get; }
}

/// <summary>
/// Presents collected UI graphics commands to a terminal.
/// </summary>
public interface ITerminalGraphicsPresenter : IDisposable
{
    /// <summary>
    /// Gets the graphics capabilities used by this presenter.
    /// </summary>
    TerminalGraphicsCapabilities Capabilities { get; }

    /// <summary>
    /// Determines whether visuals should emit graphics commands instead of fallback content for the supplied terminal capabilities.
    /// </summary>
    /// <param name="capabilities">The graphics capabilities detected for the current terminal.</param>
    /// <returns><see langword="true"/> when the presenter can present graphics for the current terminal; otherwise, <see langword="false"/>.</returns>
    bool CanPresent(TerminalGraphicsCapabilities capabilities) => true;

    /// <summary>
    /// Presents the current frame graphics commands.
    /// </summary>
    /// <param name="current">The current frame graphics command buffer.</param>
    /// <param name="context">The presentation context for the current frame.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task-like value that completes when presentation work for the frame is complete.</returns>
    ValueTask PresentAsync(
        GraphicsCommandBuffer current,
        TerminalGraphicsPresentContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets presenter-owned retained graphics state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Presents graphics by appending escape sequences to the active terminal frame output.
/// </summary>
/// <remarks>
/// Implement this interface to integrate graphics with the normal cell-buffer renderer so a frame is emitted with one
/// terminal write and one synchronized-output block.
/// </remarks>
public interface IBufferedTerminalGraphicsPresenter : ITerminalGraphicsPresenter
{
    /// <summary>
    /// Determines whether the current graphics state can produce output for this frame.
    /// </summary>
    /// <param name="current">The current frame graphics command buffer.</param>
    /// <param name="context">The presentation context for the current frame.</param>
    /// <returns><see langword="true"/> when <see cref="PresentAsync(GraphicsCommandBuffer, TerminalGraphicsPresentContext, AnsiWriter, CancellationToken)"/> should be called.</returns>
    bool HasPendingOutput(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context);

    /// <summary>
    /// Appends current frame graphics escape sequences to an existing terminal frame writer.
    /// </summary>
    /// <param name="current">The current frame graphics command buffer.</param>
    /// <param name="context">The presentation context for the current frame.</param>
    /// <param name="writer">The ANSI writer for the active terminal frame.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task-like value that completes when presentation work for the frame is complete.</returns>
    ValueTask PresentAsync(
        GraphicsCommandBuffer current,
        TerminalGraphicsPresentContext context,
        AnsiWriter writer,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides a terminal graphics presenter diagnostics snapshot for debug overlays and telemetry.
/// </summary>
/// <remarks>
/// Fields that are not meaningful for a presenter should be left at their default values. The core UI layer consumes this
/// type without depending on any image codec or renderer-specific package.
/// </remarks>
public readonly record struct TerminalGraphicsPresenterDiagnostics
{
    /// <summary>
    /// Gets a short human-readable presenter name, or <see langword="null"/> to use the presenter type name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the graphics protocol used by the presenter, when applicable.
    /// </summary>
    public TerminalGraphicsProtocol Protocol { get; init; }

    /// <summary>
    /// Gets the number of presentation passes handled by the presenter since its metrics were reset.
    /// </summary>
    public long PresentationCount { get; init; }

    /// <summary>
    /// Gets the number of graphics commands observed in the latest presentation pass.
    /// </summary>
    public int LastCommandCount { get; init; }

    /// <summary>
    /// Gets the duration of the latest presentation pass.
    /// </summary>
    public TimeSpan LastPresentationDuration { get; init; }

    /// <summary>
    /// Gets the cumulative number of encoded frames produced by the presenter.
    /// </summary>
    public long EncodedFrameCount { get; init; }

    /// <summary>
    /// Gets the number of frames encoded in the latest presentation pass.
    /// </summary>
    public int LastEncodedFrameCount { get; init; }

    /// <summary>
    /// Gets the total time spent encoding graphics payloads since metrics were reset.
    /// </summary>
    public TimeSpan TotalEncodeDuration { get; init; }

    /// <summary>
    /// Gets the average encode duration per encoded frame.
    /// </summary>
    public TimeSpan AverageEncodeDuration { get; init; }

    /// <summary>
    /// Gets the total encoding duration for the latest presentation pass.
    /// </summary>
    public TimeSpan LastEncodeDuration { get; init; }

    /// <summary>
    /// Gets the cumulative terminal payload bytes produced by encoded graphics.
    /// </summary>
    public long PayloadByteCount { get; init; }

    /// <summary>
    /// Gets the terminal payload bytes produced by the latest presentation pass.
    /// </summary>
    public long LastPayloadByteCount { get; init; }

    /// <summary>
    /// Gets the cumulative number of real-time source frame versions skipped between presented frames.
    /// </summary>
    public long DroppedFrameCount { get; init; }

    /// <summary>
    /// Gets the number of real-time source frame versions skipped in the latest presentation pass.
    /// </summary>
    public long LastDroppedFrameCount { get; init; }

    /// <summary>
    /// Gets the effective encoded-frame rate reported by the presenter since metrics were reset.
    /// </summary>
    public double EffectiveFramesPerSecond { get; init; }

    /// <summary>
    /// Gets the cumulative number of encoded-image cache hits reported by the presenter.
    /// </summary>
    public long CacheHitCount { get; init; }

    /// <summary>
    /// Gets the cumulative number of encoded-image cache misses reported by the presenter.
    /// </summary>
    public long CacheMissCount { get; init; }

    /// <summary>
    /// Gets the cumulative number of encoded-image cache stores reported by the presenter.
    /// </summary>
    public long CacheStoreCount { get; init; }

    /// <summary>
    /// Gets the number of encoded-image cache hits during the latest presentation pass.
    /// </summary>
    public long LastCacheHitCount { get; init; }

    /// <summary>
    /// Gets the number of encoded-image cache misses during the latest presentation pass.
    /// </summary>
    public long LastCacheMissCount { get; init; }

    /// <summary>
    /// Gets the number of encoded-image cache stores during the latest presentation pass.
    /// </summary>
    public long LastCacheStoreCount { get; init; }
}

/// <summary>
/// Implemented by graphics presenters that can expose diagnostic counters to the built-in debug overlay.
/// </summary>
public interface ITerminalGraphicsPresenterDiagnostics
{
    /// <summary>
    /// Gets a point-in-time diagnostics snapshot for the presenter.
    /// </summary>
    /// <returns>The diagnostics snapshot.</returns>
    TerminalGraphicsPresenterDiagnostics GetDiagnosticsSnapshot();
}

/// <summary>
/// A graphics presenter that ignores all graphics commands.
/// </summary>
public sealed class NoTerminalGraphicsPresenter : ITerminalGraphicsPresenter
{
    /// <summary>
    /// Gets a shared no-op graphics presenter instance.
    /// </summary>
    public static NoTerminalGraphicsPresenter Instance { get; } = new();

    /// <inheritdoc />
    public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

    /// <inheritdoc />
    public bool CanPresent(TerminalGraphicsCapabilities capabilities) => false;

    /// <inheritdoc />
    public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Reset()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
