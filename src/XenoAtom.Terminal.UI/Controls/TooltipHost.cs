// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Wraps a visual and displays a tooltip in an overlay when the content is hovered.
/// </summary>
/// <remarks>
/// Tooltips are only supported in fullscreen apps (they require the window layer).
/// </remarks>
public sealed partial class TooltipHost : ContentVisual, IAnimatedVisual
{
    private readonly TooltipWindow _tooltipWindow;
    private Visual? _tooltipContent;
    private bool _isOpen;
    private long _scheduledShowTick = long.MaxValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipHost"/> class.
    /// </summary>
    public TooltipHost()
    {
        _tooltipWindow = new TooltipWindow();
        this.ShowDelayMilliseconds(500);
        this.Placement(PopupPlacement.Below);
        this.OffsetY(1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipHost"/> class with a content visual.
    /// </summary>
    /// <param name="content">The content to wrap.</param>
    public TooltipHost(Visual content) : this()
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipHost"/> class with a content factory.
    /// </summary>
    /// <param name="contentFactory">A factory that provides the content visual.</param>
    public TooltipHost(Func<Visual> contentFactory) : this()
    {
        this.Content(contentFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipHost"/> class with bound content.
    /// </summary>
    /// <param name="content">A binding that supplies the content to wrap.</param>
    public TooltipHost(Binding<Visual?> content) : this()
    {
        this.Content(content);
    }

    /// <summary>
    /// Gets or sets the tooltip content.
    /// </summary>
    [Bindable]
    public Visual? TooltipContent
    {
        get => BindingManager.Current.GetValue(this, ref _tooltipContent, __TooltipContent__BindingAccessor.Instance);
        set
        {
            if (BindingManager.Current.SetValue(this, ref _tooltipContent, value, __TooltipContent__BindingAccessor.Instance))
            {
                CloseTooltip();
                _scheduledShowTick = long.MaxValue;
            }
        }
    }

    /// <summary>
    /// Gets or sets the delay (in milliseconds) before showing the tooltip.
    /// </summary>
    [Bindable]
    public partial int ShowDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the tooltip placement relative to the anchor visual.
    /// </summary>
    [Bindable]
    public partial PopupPlacement Placement { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset applied to the tooltip position.
    /// </summary>
    [Bindable]
    public partial int OffsetX { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset applied to the tooltip position.
    /// </summary>
    [Bindable]
    public partial int OffsetY { get; set; }

    long IAnimatedVisual.NextAnimationTick => 0;

    bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

    /// <inheritdoc />
    protected override void OnDetachedFromApp(TerminalApp app)
    {
        CloseTooltip();
        base.OnDetachedFromApp(app);
    }

    private bool AdvanceAnimation(long timestamp)
    {
        if (App is null || !IsVisible || !IsEnabled)
        {
            CloseTooltip();
            return false;
        }

        var tooltipContent = TooltipContent;
        if (tooltipContent is null)
        {
            CloseTooltip();
            _scheduledShowTick = long.MaxValue;
            return false;
        }

        if (!IsHovered)
        {
            CloseTooltip();
            _scheduledShowTick = long.MaxValue;
            return false;
        }

        if (_isOpen)
        {
            return false;
        }

        if (_scheduledShowTick == long.MaxValue)
        {
            var delayMs = Math.Max(0, ShowDelayMilliseconds);
            _scheduledShowTick = timestamp + ToStopwatchTicks(TimeSpan.FromMilliseconds(delayMs));
            return false;
        }

        if (timestamp < _scheduledShowTick)
        {
            return false;
        }

        OpenTooltip(tooltipContent);
        _scheduledShowTick = long.MaxValue;
        return true;
    }

    private void OpenTooltip(Visual tooltipContent)
    {
        if (_isOpen)
        {
            return;
        }

        var app = App;
        if (app is null)
        {
            return;
        }

        // Tooltips are implemented as a non-interactive overlay window.
        if (tooltipContent.Parent is not null)
        {
            throw new InvalidOperationException("Tooltip content is already part of a UI tree.");
        }

        _tooltipWindow.Anchor = Content ?? this;
        _tooltipWindow.AnchorRect = null;
        _tooltipWindow.Placement = Placement;
        _tooltipWindow.OffsetX = OffsetX;
        _tooltipWindow.OffsetY = OffsetY;
        _tooltipWindow.Content = tooltipContent;

        _isOpen = true;
        app.ShowTooltipWindow(_tooltipWindow);
    }

    private void CloseTooltip()
    {
        if (!_isOpen)
        {
            return;
        }

        var app = App;
        if (app is null)
        {
            _isOpen = false;
            return;
        }

        _isOpen = false;
        app.CloseTooltipWindow(_tooltipWindow);
        _tooltipWindow.Content = null;
    }

    private static long ToStopwatchTicks(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            return 1;
        }

        var ticks = interval.TotalSeconds * Stopwatch.Frequency;
        if (ticks < 1)
        {
            return 1;
        }

        return (long)ticks;
    }

}
