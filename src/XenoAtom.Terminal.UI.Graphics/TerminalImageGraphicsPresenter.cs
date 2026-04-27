// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI.Geometry;
using GraphicsImageScaleMode = XenoAtom.Terminal.Graphics.TerminalImageScaleMode;
using GraphicsImageSource = XenoAtom.Terminal.Graphics.TerminalImageSource;
using UiImageScaleMode = XenoAtom.Terminal.UI.ImageScaleMode;

namespace XenoAtom.Terminal.UI.Graphics;

/// <summary>
/// Provides options for <see cref="TerminalImageGraphicsPresenter"/>.
/// </summary>
public sealed class TerminalImageGraphicsPresenterOptions
{
    /// <summary>
    /// Gets or sets the protocol to use. <see cref="TerminalGraphicsProtocol.None"/> selects the terminal's preferred protocol.
    /// </summary>
    public TerminalGraphicsProtocol Protocol { get; set; } = TerminalGraphicsProtocol.None;

    /// <summary>
    /// Gets or sets an optional rasterizer. When <see langword="null"/>, the default image rasterizer is used.
    /// </summary>
    public ITerminalImageRasterizer? Rasterizer { get; set; }

    /// <summary>
    /// Gets or sets an optional encoded-image cache.
    /// </summary>
    public TerminalImageMemoryCache? Cache { get; set; }

    /// <summary>
    /// Gets or sets the matte color used when alpha must be flattened.
    /// </summary>
    public TerminalImageColor? MatteColor { get; set; }

    /// <summary>
    /// Gets or sets the raster resampling quality.
    /// </summary>
    public TerminalImageResamplingQuality Quality { get; set; } = TerminalImageResamplingQuality.High;

    /// <summary>
    /// Gets or sets Sixel-specific encoding options.
    /// </summary>
    public TerminalSixelEncoderOptions? SixelOptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum payload chunk size for protocols that support chunking.
    /// </summary>
    public int MaxPayloadChunkBytes { get; set; } = AnsiKittyGraphicsSequences.DefaultMaxPayloadChunkChars;

    /// <summary>
    /// Gets or sets the fallback cell width, in pixels, used when terminal pixel metrics are unavailable.
    /// </summary>
    /// <remarks>
    /// Streamed protocols such as Sixel and iTerm2 place images by emitted pixel dimensions instead of retained cell
    /// placement. A conservative fallback keeps explicit cell-sized image controls visible even when the terminal does not
    /// report cell pixel metrics.
    /// </remarks>
    public int FallbackCellPixelWidth { get; set; } = 8;

    /// <summary>
    /// Gets or sets the fallback cell height, in pixels, used when terminal pixel metrics are unavailable.
    /// </summary>
    /// <remarks>
    /// Streamed protocols such as Sixel and iTerm2 place images by emitted pixel dimensions instead of retained cell
    /// placement. A conservative fallback keeps explicit cell-sized image controls visible even when the terminal does not
    /// report cell pixel metrics.
    /// </remarks>
    public int FallbackCellPixelHeight { get; set; } = 16;

    /// <summary>
    /// Gets or sets a value indicating whether an unsupported protocol should throw instead of being ignored.
    /// </summary>
    public bool ThrowIfUnsupported { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retained Kitty images owned by the presenter are deleted when the presenter is disposed.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true"/> for live/fullscreen applications, where images should be cleaned up when
    /// the app exits. One-shot flow output can set this to <see langword="false"/> so retained image output remains visible
    /// after the temporary presenter has been disposed.
    /// </remarks>
    public bool DeleteRetainedImagesOnDispose { get; set; } = true;
}

/// <summary>
/// Presents UI graphics commands by encoding terminal image sources through <c>XenoAtom.Terminal.Graphics</c>.
/// </summary>
public sealed class TerminalImageGraphicsPresenter : IBufferedTerminalGraphicsPresenter, ITerminalGraphicsPresenterDiagnostics
{
    private readonly AnsiBuilder _builder = new(initialCapacity: 4096);
    private readonly AnsiWriter _writer;
    private readonly TerminalImageGraphicsPresenterOptions _options;
    private readonly TerminalImageEncodingService _encodingService;
    private readonly TerminalImageMemoryCache _cache;
    private readonly List<PresentedCommand> _previous = new();
    private readonly List<PresentedCommand> _nextPrevious = new();
    private readonly HashSet<int> _ownedKittyImageIds = new();
    private TerminalGraphicsCapabilities _capabilities = TerminalGraphicsCapabilities.None;
    private TerminalGraphicsProtocol _lastProtocol = TerminalGraphicsProtocol.None;
    private TerminalInstance? _lastTerminal;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalImageGraphicsPresenter"/> class.
    /// </summary>
    /// <param name="options">Optional presenter options.</param>
    public TerminalImageGraphicsPresenter(TerminalImageGraphicsPresenterOptions? options = null)
    {
        _options = options ?? new TerminalImageGraphicsPresenterOptions();
        _writer = new AnsiWriter(_builder);
        _encodingService = new TerminalImageEncodingService(_options.Rasterizer, _options.SixelOptions);
        _cache = _options.Cache ?? new TerminalImageMemoryCache();
    }

    /// <inheritdoc />
    public TerminalGraphicsCapabilities Capabilities => _capabilities;

    /// <inheritdoc />
    public bool CanPresent(TerminalGraphicsCapabilities capabilities)
        => _options.Protocol != TerminalGraphicsProtocol.None || capabilities.SupportsStaticImages;

    /// <summary>
    /// Gets runtime presentation diagnostics collected by this presenter.
    /// </summary>
    public TerminalImageGraphicsPresenterMetrics Metrics { get; } = new();

    /// <inheritdoc />
    public TerminalGraphicsPresenterDiagnostics GetDiagnosticsSnapshot()
        => Metrics.GetDiagnosticsSnapshot(_lastProtocol);

    /// <inheritdoc />
    public async ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
    {
        if (!HasPendingOutput(current, context))
        {
            return;
        }

        _builder.Clear();
        _writer.PrivateMode(2026, enabled: true);
        await PresentCoreAsync(current, context, _writer, skipPendingCheck: true, cancellationToken).ConfigureAwait(false);
        _writer.PrivateMode(2026, enabled: false);
        if (_builder.Length == 0)
        {
            return;
        }

        context.Terminal.WriteAtomic((TextWriter textWriter) => textWriter.Write(_builder.UnsafeAsSpan()));
        context.Terminal.Flush();
    }

    /// <inheritdoc />
    public bool HasPendingOutput(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);

        var protocol = SelectProtocol(context);
        if (protocol == TerminalGraphicsProtocol.None)
        {
            return _options.ThrowIfUnsupported || _previous.Count > 0;
        }

        if (NeedsClearChangedOrMissingPrevious(current, protocol, context.ViewportBounds))
        {
            return true;
        }

        foreach (var command in current)
        {
            if (IsImageContent(command.Content) && ShouldDrawCommand(command, protocol, context.ViewportBounds, context))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, AnsiWriter writer, CancellationToken cancellationToken = default)
        => PresentCoreAsync(current, context, writer, skipPendingCheck: false, cancellationToken);

    private async ValueTask PresentCoreAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, AnsiWriter writer, bool skipPendingCheck, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(writer);
        cancellationToken.ThrowIfCancellationRequested();
        var presentationStart = Stopwatch.GetTimestamp();
        var cacheHitsBefore = _cache.HitCount;
        var cacheMissesBefore = _cache.MissCount;
        var cacheStoresBefore = _cache.StoreCount;
        Metrics.RecordPresentation(current.Count);

        try
        {
            _lastTerminal = context.Terminal;
            _capabilities = context.Terminal.Graphics.Capabilities;

            var protocol = SelectProtocol(context);
            _lastProtocol = protocol;
            if (protocol == TerminalGraphicsProtocol.None)
            {
                if (_options.ThrowIfUnsupported)
                {
                    throw new InvalidOperationException("No terminal graphics protocol is available for UI image presentation.");
                }

                if (ClearPreviousStreamedRegions(writer))
                {
                    RequestFullRender(context.App);
                }
                return;
            }

            if (await WritePresentationAsync(writer, protocol, current, context, skipPendingCheck, cancellationToken).ConfigureAwait(false))
            {
                RequestFullRender(context.App);
            }
        }
        finally
        {
            Metrics.RecordPresentationDuration(Stopwatch.GetElapsedTime(presentationStart));
            Metrics.RecordCacheActivity(
                _cache.HitCount - cacheHitsBefore,
                _cache.MissCount - cacheMissesBefore,
                _cache.StoreCount - cacheStoresBefore);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        if (_disposed)
        {
            return;
        }

        DeleteOwnedKittyImages();
        _previous.Clear();
        _ownedKittyImageIds.Clear();
        _builder.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_options.DeleteRetainedImagesOnDispose)
        {
            Reset();
        }
        else
        {
            _previous.Clear();
            _ownedKittyImageIds.Clear();
            _builder.Clear();
        }
        _builder.Dispose();
        _disposed = true;
    }

    private TerminalGraphicsProtocol SelectProtocol(TerminalGraphicsPresentContext context)
    {
        if (_options.Protocol != TerminalGraphicsProtocol.None)
        {
            return _options.Protocol;
        }

        var capabilities = context.Terminal.Graphics.Capabilities;
        return capabilities.SupportsStaticImages ? capabilities.PreferredProtocol : TerminalGraphicsProtocol.None;
    }

    private async ValueTask<bool> WritePresentationAsync(AnsiWriter writer, TerminalGraphicsProtocol protocol, GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, bool skipPendingCheck, CancellationToken cancellationToken)
    {
        var viewportBounds = context.ViewportBounds;
        if (!skipPendingCheck && !HasPendingOutput(current, context))
        {
            return false;
        }

        writer.SaveCursor();
        try
        {
            var needsTextRepaint = ClearChangedOrMissingPrevious(writer, current, protocol, viewportBounds);
            _nextPrevious.Clear();

            foreach (var command in current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryResolveImageSource(command.Content, out var source))
                {
                    continue;
                }

                var imageId = protocol == TerminalGraphicsProtocol.Kitty ? CreateKittyImageId(command) : (int?)null;
                if (!ShouldDrawCommand(command, protocol, viewportBounds, context))
                {
                    _nextPrevious.Add(new PresentedCommand(command.VisualRenderId, command.PaintOrder, command, protocol, imageId, viewportBounds));
                    continue;
                }

                var cellSize = new TerminalImageSize(Math.Max(1, command.CellBounds.Width), Math.Max(1, command.CellBounds.Height));
                var pixelSize = ResolvePixelSize(cellSize, _capabilities.PixelMetrics);
                var encodeRequest = new TerminalImageEncodeRequest(
                    protocol,
                    pixelSize,
                    cellSize,
                    _capabilities.PixelMetrics,
                    MapScaleMode(command.ScaleMode),
                    _options.MatteColor,
                    command.PreserveAspectRatio,
                    _options.Quality,
                    _options.MaxPayloadChunkBytes,
                    imageId,
                    PlacementId: null);

                var encodeStart = Stopwatch.GetTimestamp();
                var encoded = await _encodingService.EncodeAsync(source, TerminalImageFrameRequest.Default, encodeRequest, _cache, cancellationToken).ConfigureAwait(false);
                if (encoded is null)
                {
                    continue;
                }

                _nextPrevious.Add(new PresentedCommand(command.VisualRenderId, command.PaintOrder, command, protocol, imageId, viewportBounds));

                Metrics.RecordEncodedFrame(encoded, Stopwatch.GetElapsedTime(encodeStart));
                Metrics.RecordDroppedFrames(CountDroppedRealtimeFrames(command));

                if (imageId.HasValue)
                {
                    _ownedKittyImageIds.Add(imageId.Value);
                }

                WriteCommandImage(writer, command, encoded, viewportBounds);
            }

            _previous.Clear();
            _previous.AddRange(_nextPrevious);
            _nextPrevious.Clear();
            return needsTextRepaint;
        }
        finally
        {
            writer.RestoreCursor();
        }
    }

    private bool ShouldDrawCommand(GraphicsCommand command, TerminalGraphicsProtocol protocol, Rectangle viewportBounds, TerminalGraphicsPresentContext context)
    {
        foreach (var previous in _previous)
        {
            if (previous.VisualRenderId != command.VisualRenderId || previous.PaintOrder != command.PaintOrder)
            {
                continue;
            }

            if (previous.Protocol != protocol || previous.ViewportBounds != viewportBounds || !CommandsEquivalent(previous.Command, command))
            {
                return true;
            }

            return protocol is TerminalGraphicsProtocol.ITerm2 or TerminalGraphicsProtocol.Sixel
                && context.TextFrameKind != TerminalGraphicsTextFrameKind.None
                && Intersects(command.CellBounds, context.TextRepaintBounds);
        }

        return true;
    }

    private bool NeedsClearChangedOrMissingPrevious(GraphicsCommandBuffer current, TerminalGraphicsProtocol protocol, Rectangle currentViewportBounds)
    {
        foreach (var previous in _previous)
        {
            if (!IsPreviousPlacementUnchangedInCurrent(previous, current, protocol, currentViewportBounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool ClearChangedOrMissingPrevious(AnsiWriter writer, GraphicsCommandBuffer current, TerminalGraphicsProtocol currentProtocol, Rectangle currentViewportBounds)
    {
        var needsTextRepaint = false;
        foreach (var previous in _previous)
        {
            if (IsPreviousPlacementUnchangedInCurrent(previous, current, currentProtocol, currentViewportBounds))
            {
                continue;
            }

            if (previous.Protocol == TerminalGraphicsProtocol.Kitty && previous.KittyImageId.HasValue)
            {
                WriteKittyDelete(writer, previous.KittyImageId.Value);
            }
            else if (currentProtocol is TerminalGraphicsProtocol.ITerm2 or TerminalGraphicsProtocol.Sixel || previous.Protocol is TerminalGraphicsProtocol.ITerm2 or TerminalGraphicsProtocol.Sixel)
            {
                ClearRegion(writer, Offset(previous.Command.CellBounds, previous.ViewportBounds));
                needsTextRepaint = true;
            }
        }

        return needsTextRepaint;
    }

    private static bool IsPreviousPlacementUnchangedInCurrent(PresentedCommand previous, GraphicsCommandBuffer current, TerminalGraphicsProtocol protocol, Rectangle currentViewportBounds)
    {
        foreach (var command in current)
        {
            if (previous.VisualRenderId == command.VisualRenderId && previous.PaintOrder == command.PaintOrder && IsImageContent(command.Content))
            {
                return CommandsEquivalentForClearing(previous.Command, command) && previous.Protocol == protocol && previous.ViewportBounds == currentViewportBounds;
            }
        }

        return false;
    }

    private void WriteCommandImage(AnsiWriter writer, GraphicsCommand command, TerminalEncodedImage encoded, Rectangle viewportBounds)
    {
        var bounds = Offset(command.CellBounds, viewportBounds);
        writer.CursorPosition(bounds.Y + 1, bounds.X + 1);
        TerminalGraphicsExtensions.WriteEncodedImage(writer, encoded, _options.MaxPayloadChunkBytes);
    }

    private static void ClearRegion(AnsiWriter writer, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var rented = ArrayPool<char>.Shared.Rent(Math.Min(bounds.Width, 256));
        try
        {
            var spaces = rented.AsSpan(0, Math.Min(bounds.Width, rented.Length));
            spaces.Fill(' ');
            for (var y = bounds.Y; y < bounds.Bottom; y++)
            {
                writer.CursorPosition(y + 1, bounds.X + 1);
                WriteSpaces(writer, spaces, bounds.Width);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    private static void WriteSpaces(AnsiWriter writer, ReadOnlySpan<char> spaces, int count)
    {
        while (count > 0)
        {
            var chunk = Math.Min(count, spaces.Length);
            writer.Write(spaces[..chunk]);
            count -= chunk;
        }
    }

    private bool ClearPreviousStreamedRegions(AnsiWriter writer)
    {
        if (_previous.Count == 0)
        {
            return false;
        }

        writer.SaveCursor();
        var needsTextRepaint = false;
        foreach (var previous in _previous)
        {
            if (previous.Protocol == TerminalGraphicsProtocol.Kitty && previous.KittyImageId.HasValue)
            {
                WriteKittyDelete(writer, previous.KittyImageId.Value);
            }
            else
            {
                ClearRegion(writer, Offset(previous.Command.CellBounds, previous.ViewportBounds));
                needsTextRepaint = true;
            }
        }
        writer.RestoreCursor();
        _previous.Clear();
        return needsTextRepaint;
    }

    private void DeleteOwnedKittyImages()
    {
        if (_lastTerminal is null || _ownedKittyImageIds.Count == 0)
        {
            return;
        }

        try
        {
            _builder.Clear();
            foreach (var imageId in _ownedKittyImageIds)
            {
                WriteKittyDelete(_writer, imageId);
            }

            _lastTerminal.WriteAtomic((TextWriter textWriter) => textWriter.Write(_builder.UnsafeAsSpan()));
            _lastTerminal.Flush();
        }
        catch
        {
            // Best-effort cleanup only; the terminal may already be disposed or unavailable during app shutdown.
        }
    }

    private static void WriteKittyDelete(AnsiWriter writer, int imageId)
    {
        const string Prefix = "a=d,d=i,i=";
        writer.Write("\x1b_G");
        writer.Write(Prefix);
        var rented = ArrayPool<char>.Shared.Rent(11);
        try
        {
            var chars = rented.AsSpan(0, rented.Length);
            if (!imageId.TryFormat(chars, out var charsWritten, provider: null))
            {
                throw new InvalidOperationException("Unable to format the Kitty image id.");
            }

            writer.Write(chars[..charsWritten]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
        writer.Write("\x1b\\");
    }

    private static void RequestFullRender(TerminalApp app)
    {
        if (app.CheckAccess())
        {
            app.RequestFullRender();
        }
        else
        {
            app.Post(app.RequestFullRender);
        }
    }

    private TerminalImageSize ResolvePixelSize(TerminalImageSize cellSize, TerminalPixelMetrics? metrics)
    {
        if (metrics is { CellPixelWidth: > 0, CellPixelHeight: > 0 })
        {
            var terminalMetrics = metrics.Value;
            return new TerminalImageSize(
                Math.Max(1, cellSize.Width * terminalMetrics.CellPixelWidth),
                Math.Max(1, cellSize.Height * terminalMetrics.CellPixelHeight));
        }

        var fallbackCellPixelWidth = Math.Max(1, _options.FallbackCellPixelWidth);
        var fallbackCellPixelHeight = Math.Max(1, _options.FallbackCellPixelHeight);
        return new TerminalImageSize(
            (int)Math.Min(int.MaxValue, Math.Max(1L, (long)cellSize.Width * fallbackCellPixelWidth)),
            (int)Math.Min(int.MaxValue, Math.Max(1L, (long)cellSize.Height * fallbackCellPixelHeight)));
    }

    private static bool TryResolveImageSource(TerminalGraphicContent content, out GraphicsImageSource source)
    {
        switch (content.Kind)
        {
            case TerminalGraphicContentKind.Bytes when !content.Bytes.IsEmpty:
                source = GraphicsImageSource.FromEncodedBytes(content.Bytes, content.CacheKey);
                return true;
            case TerminalGraphicContentKind.File when content.FilePath is not null:
                source = GraphicsImageSource.FromFile(content.FilePath);
                return true;
            case TerminalGraphicContentKind.Object when content.Source is GraphicsImageSource imageSource:
                source = imageSource;
                return true;
            default:
                source = null!;
                return false;
        }
    }

    private static bool IsImageContent(TerminalGraphicContent content) => content.Kind switch
    {
        TerminalGraphicContentKind.Bytes => !content.Bytes.IsEmpty,
        TerminalGraphicContentKind.File => content.FilePath is not null,
        TerminalGraphicContentKind.Object => content.Source is GraphicsImageSource,
        _ => false,
    };

    private static bool CommandsEquivalent(GraphicsCommand left, GraphicsCommand right)
    {
        return left.VisualRenderId == right.VisualRenderId
            && left.PaintOrder == right.PaintOrder
            && left.CellBounds == right.CellBounds
            && left.ClipBounds == right.ClipBounds
            && left.ScaleMode == right.ScaleMode
            && left.PreserveAspectRatio == right.PreserveAspectRatio
            && left.ReserveCells == right.ReserveCells
            && string.Equals(left.AccessibilityText, right.AccessibilityText, StringComparison.Ordinal)
            && ContentEquivalent(left.Content, right.Content);
    }

    private static bool CommandsEquivalentForClearing(GraphicsCommand left, GraphicsCommand right)
    {
        return left.VisualRenderId == right.VisualRenderId
            && left.PaintOrder == right.PaintOrder
            && left.CellBounds == right.CellBounds
            && left.ClipBounds == right.ClipBounds
            && left.ScaleMode == right.ScaleMode
            && left.PreserveAspectRatio == right.PreserveAspectRatio
            && left.ReserveCells == right.ReserveCells
            && string.Equals(left.AccessibilityText, right.AccessibilityText, StringComparison.Ordinal)
            && ContentEquivalentForClearing(left.Content, right.Content);
    }

    private long CountDroppedRealtimeFrames(GraphicsCommand command)
    {
        var currentVersion = command.Content.Version;
        if (currentVersion <= 0)
        {
            return 0;
        }

        foreach (var previous in _previous)
        {
            if (previous.VisualRenderId == command.VisualRenderId && previous.PaintOrder == command.PaintOrder)
            {
                var previousVersion = previous.Command.Content.Version;
                return previousVersion <= 0 ? 0 : Math.Max(0, currentVersion - previousVersion - 1);
            }
        }

        return 0;
    }

    private static bool ContentEquivalent(TerminalGraphicContent left, TerminalGraphicContent right)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (!string.Equals(left.MediaType, right.MediaType, StringComparison.Ordinal) ||
            !string.Equals(left.CacheKey, right.CacheKey, StringComparison.Ordinal) ||
            left.Version != right.Version)
        {
            return false;
        }

        return left.Kind switch
        {
            TerminalGraphicContentKind.None => true,
            TerminalGraphicContentKind.Bytes => left.Bytes.Span.SequenceEqual(right.Bytes.Span),
            TerminalGraphicContentKind.File => string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal),
            TerminalGraphicContentKind.Object => ReferenceEquals(left.Source, right.Source),
            _ => false,
        };
    }

    private static bool ContentEquivalentForClearing(TerminalGraphicContent left, TerminalGraphicContent right)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (!string.Equals(left.MediaType, right.MediaType, StringComparison.Ordinal) ||
            !string.Equals(left.CacheKey, right.CacheKey, StringComparison.Ordinal))
        {
            return false;
        }

        return left.Kind switch
        {
            TerminalGraphicContentKind.None => true,
            TerminalGraphicContentKind.Bytes => left.Bytes.Span.SequenceEqual(right.Bytes.Span),
            TerminalGraphicContentKind.File => string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal),
            TerminalGraphicContentKind.Object => ReferenceEquals(left.Source, right.Source),
            _ => false,
        };
    }

    private static GraphicsImageScaleMode MapScaleMode(UiImageScaleMode scaleMode) => scaleMode switch
    {
        UiImageScaleMode.None => GraphicsImageScaleMode.Center,
        UiImageScaleMode.Fit => GraphicsImageScaleMode.Fit,
        UiImageScaleMode.Fill => GraphicsImageScaleMode.Fill,
        UiImageScaleMode.Stretch => GraphicsImageScaleMode.Stretch,
        UiImageScaleMode.Center => GraphicsImageScaleMode.Center,
        _ => GraphicsImageScaleMode.Fit,
    };

    private static int CreateKittyImageId(GraphicsCommand command)
    {
        var hash = HashCode.Combine(command.VisualRenderId, command.PaintOrder);
        var id = hash & 0x7fffffff;
        return id == 0 ? 1 : id;
    }

    private static Rectangle Offset(Rectangle bounds, Rectangle viewportBounds)
        => new(bounds.X + viewportBounds.X, bounds.Y + viewportBounds.Y, bounds.Width, bounds.Height);

    private static bool Intersects(Rectangle left, Rectangle right)
        => left.Width > 0
        && left.Height > 0
        && right.Width > 0
        && right.Height > 0
        && left.X < right.Right
        && left.Right > right.X
        && left.Y < right.Bottom
        && left.Bottom > right.Y;

    private readonly record struct PresentedCommand(
        ulong VisualRenderId,
        int PaintOrder,
        GraphicsCommand Command,
        TerminalGraphicsProtocol Protocol,
        int? KittyImageId,
        Rectangle ViewportBounds);
}
