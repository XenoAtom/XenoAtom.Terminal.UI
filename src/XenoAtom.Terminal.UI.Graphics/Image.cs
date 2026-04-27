// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using GraphicsImageSource = XenoAtom.Terminal.Graphics.TerminalImageSource;
using UiImageScaleMode = XenoAtom.Terminal.UI.ImageScaleMode;

namespace XenoAtom.Terminal.UI.Graphics;

/// <summary>
/// Displays an image in a terminal UI cell rectangle.
/// </summary>
/// <remarks>
/// The control reserves its arranged cells during the text render pass and emits a graphics display-list command during
/// the graphics render pass. Protocol-specific escape sequences are produced by the configured graphics presenter, not
/// by this control.
/// </remarks>
public sealed partial class Image : Visual, IGraphicsRenderableVisual
{
    private GraphicsImageSource? _subscribedRealtimeSource;
    private long _realtimeFrameVersion;
    private int _realtimeFrameNotificationPending;

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class.
    /// </summary>
    public Image()
    {
        ScaleMode = UiImageScaleMode.Fit;
        PreserveAspectRatio = true;
        ReserveCells = true;
        CellWidth = 1;
        CellHeight = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Image"/> class with the specified source.
    /// </summary>
    /// <param name="source">The terminal image source.</param>
    public Image(GraphicsImageSource? source)
        : this()
    {
        Source = source;
    }

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    [Bindable]
    public partial GraphicsImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the preferred width of the image control, in terminal cells.
    /// </summary>
    /// <remarks>
    /// Set this to zero to let the control use the minimum width supplied by its parent layout constraints.
    /// </remarks>
    [Bindable]
    public partial int CellWidth { get; set; }

    /// <summary>
    /// Gets or sets the preferred height of the image control, in terminal cells.
    /// </summary>
    /// <remarks>
    /// Set this to zero to let the control use the minimum height supplied by its parent layout constraints.
    /// </remarks>
    [Bindable]
    public partial int CellHeight { get; set; }

    /// <summary>
    /// Gets or sets how the image is mapped to the arranged cell rectangle.
    /// </summary>
    [Bindable]
    public partial UiImageScaleMode ScaleMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether aspect ratio should be preserved when the selected scale mode supports it.
    /// </summary>
    [Bindable]
    public partial bool PreserveAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets optional fallback content shown when no source or graphics presenter is available.
    /// </summary>
    [Bindable]
    public partial Visual? FallbackContent { get; set; }

    /// <summary>
    /// Gets or sets optional descriptive text associated with the image command.
    /// </summary>
    [Bindable]
    public partial string? AccessibilityText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the control clears/reserves its cell rectangle during text rendering.
    /// </summary>
    [Bindable]
    public partial bool ReserveCells { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _fallbackContent is null ? 0 : 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 && _fallbackContent is not null ? _fallbackContent : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        if (ShouldUseFallback && FallbackContent is { } fallback)
        {
            return fallback.Measure(constraints);
        }

        var width = CellWidth > 0 ? CellWidth : Math.Max(1, constraints.MinWidth);
        var height = CellHeight > 0 ? CellHeight : Math.Max(1, constraints.MinHeight);

        if (constraints.MaxWidth != int.MaxValue)
        {
            width = Math.Min(width, Math.Max(0, constraints.MaxWidth));
        }

        if (constraints.MaxHeight != int.MaxValue)
        {
            height = Math.Min(height, Math.Max(0, constraints.MaxHeight));
        }

        return SizeHints.Fixed(new Size(Math.Max(0, width), Math.Max(0, height)));
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        FallbackContent?.Arrange(ShouldUseFallback ? finalRect : default);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        if (ReserveCells && !ShouldUseFallback)
        {
            buffer.ClearCurrentClip(GetTheme().BaseTextStyle());
        }
    }

    /// <inheritdoc />
    void IGraphicsRenderableVisual.RenderGraphics(GraphicsRenderContext context)
    {
        var source = Source;
        if (source is null || ShouldUseFallback)
        {
            return;
        }

        context.Add(
            Bounds,
            TerminalGraphicContent.FromObject(source, TerminalImageGraphicsContentTypes.TerminalImageSource, source.ToString(), global::System.Threading.Volatile.Read(ref _realtimeFrameVersion)),
            ScaleMode,
            PreserveAspectRatio,
            ReserveCells,
            AccessibilityText);
    }

    /// <inheritdoc />
    protected override void OnAttachedToApp(TerminalApp app)
    {
        SetRealtimeSourceSubscription(Source);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromApp(TerminalApp app)
    {
        SetRealtimeSourceSubscription(null);
    }

    private bool ShouldUseFallback => Source is null || App is not { IsGraphicsPresentationEnabled: true };

    private void SetRealtimeSourceSubscription(GraphicsImageSource? source)
    {
        if (ReferenceEquals(_subscribedRealtimeSource, source))
        {
            return;
        }

        if (_subscribedRealtimeSource is ITerminalRealtimeImageSource oldRealtimeSource)
        {
            oldRealtimeSource.FrameAvailable -= OnRealtimeFrameAvailable;
        }

        _subscribedRealtimeSource = source;
        if (source is ITerminalRealtimeImageSource realtimeSource)
        {
            realtimeSource.FrameAvailable += OnRealtimeFrameAvailable;
        }
    }

    private void OnRealtimeFrameAvailable(object? sender, TerminalImageFrameAvailableEventArgs e)
    {
        if (!ReferenceEquals(sender, Source))
        {
            return;
        }

        global::System.Threading.Volatile.Write(ref _realtimeFrameVersion, e.Version);
        var app = App;
        if (app is null)
        {
            return;
        }

        if (global::System.Threading.Interlocked.Exchange(ref _realtimeFrameNotificationPending, 1) != 0)
        {
            return;
        }

        app.Post(() =>
        {
            global::System.Threading.Interlocked.Exchange(ref _realtimeFrameNotificationPending, 0);
            if (!ReferenceEquals(sender, Source) || App is not { } currentApp)
            {
                return;
            }

            currentApp.RequestGraphicsRender();
        });
    }

    partial void OnSourceChanged(GraphicsImageSource? value)
    {
        global::System.Threading.Volatile.Write(ref _realtimeFrameVersion, 0);
        global::System.Threading.Interlocked.Exchange(ref _realtimeFrameNotificationPending, 0);
        if (App is not null)
        {
            SetRealtimeSourceSubscription(value);
        }
    }

    partial void OnCellWidthChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);

    partial void OnCellHeightChanging(ref int value) => ArgumentOutOfRangeException.ThrowIfNegative(value);
}
