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
    private readonly TooltipPopup _tooltipPopup;
    private Visual? _tooltipContent;
    private bool _isOpen;
    private long _scheduledShowTick = long.MaxValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipHost"/> class.
    /// </summary>
    public TooltipHost()
    {
        _tooltipPopup = new TooltipPopup();
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

        _tooltipPopup.Anchor = Content ?? this;
        _tooltipPopup.Placement = Placement;
        _tooltipPopup.OffsetX = OffsetX;
        _tooltipPopup.OffsetY = OffsetY;
        _tooltipPopup.Content = tooltipContent;

        _isOpen = true;
        app.ShowTooltipWindow(_tooltipPopup);
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
        app.CloseTooltipWindow(_tooltipPopup);
        _tooltipPopup.Content = null;
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

    private sealed class TooltipPopup : ContentVisual
    {
        private Rectangle _popupRect;

        public TooltipPopup()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            IsHitTestVisible = false;
            IsEnabled = false;
        }

        public Visual? Anchor { get; set; }

        public PopupPlacement Placement { get; set; } = PopupPlacement.Below;

        public int OffsetX { get; set; }

        public int OffsetY { get; set; } = 1;

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var style = GetStyle<TooltipStyle>();
            var padding = style.Padding;

            var maxWidth = constraints.MaxWidth;
            if (style.MaxWidth is int cap)
            {
                maxWidth = Math.Min(maxWidth, cap);
            }

            var innerWidth = Math.Max(0, maxWidth - padding.Horizontal - 2);
            var innerHeight = constraints.MaxHeight == LayoutConstants.Infinite
                ? LayoutConstants.Infinite
                : Math.Max(0, constraints.MaxHeight - padding.Vertical - 2);

            Content?.Measure(new LayoutConstraints(0, innerWidth, 0, innerHeight));

            // Fill the available space so we can position relative to the anchor.
            return SizeHints.Flex(
                min: Size.Zero,
                natural: Size.Zero,
                max: new Size(LayoutConstants.Infinite, LayoutConstants.Infinite),
                growX: 1,
                growY: 1,
                shrinkX: 0,
                shrinkY: 0);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;

            var style = GetStyle<TooltipStyle>();
            var padding = style.Padding;

            var content = Content;
            var desired = content?.DesiredSize ?? default;

            var desiredWidth = Math.Clamp(desired.Width + padding.Horizontal + 2, 1, finalRect.Width);
            var desiredHeight = Math.Clamp(desired.Height + padding.Vertical + 2, 1, finalRect.Height);

            var x = finalRect.X + Math.Max(0, (finalRect.Width - desiredWidth) / 2);
            var y = finalRect.Y + Math.Max(0, (finalRect.Height - desiredHeight) / 2);

            if (Anchor is { } anchor)
            {
                var belowY = anchor.Bounds.Y + anchor.Bounds.Height;
                var aboveY = anchor.Bounds.Y - desiredHeight;
                var rightX = anchor.Bounds.X + anchor.Bounds.Width;
                var leftX = anchor.Bounds.X - desiredWidth;

                switch (Placement)
                {
                    case PopupPlacement.Above:
                        x = anchor.Bounds.X;
                        y = aboveY;
                        if (y < finalRect.Y && belowY + desiredHeight <= finalRect.Bottom)
                        {
                            y = belowY;
                        }
                        break;

                    case PopupPlacement.Right:
                        x = rightX;
                        y = anchor.Bounds.Y;
                        if (x + desiredWidth > finalRect.Right && leftX >= finalRect.X)
                        {
                            x = leftX;
                        }
                        break;

                    case PopupPlacement.Left:
                        x = leftX;
                        y = anchor.Bounds.Y;
                        if (x < finalRect.X && rightX + desiredWidth <= finalRect.Right)
                        {
                            x = rightX;
                        }
                        break;

                    case PopupPlacement.Below:
                    default:
                        x = anchor.Bounds.X;
                        y = belowY;
                        if (y + desiredHeight > finalRect.Bottom && aboveY >= finalRect.Y)
                        {
                            y = aboveY;
                        }
                        break;
                }
            }

            x += OffsetX;
            y += OffsetY;

            x = Math.Clamp(x, finalRect.X, Math.Max(finalRect.X, finalRect.Right - desiredWidth));
            y = Math.Clamp(y, finalRect.Y, Math.Max(finalRect.Y, finalRect.Bottom - desiredHeight));

            _popupRect = new Rectangle(x, y, desiredWidth, desiredHeight);

            if (content is not null)
            {
                var inner = new Rectangle(
                    _popupRect.X + 1 + padding.Left,
                    _popupRect.Y + 1 + padding.Top,
                    Math.Max(0, _popupRect.Width - 2 - padding.Horizontal),
                    Math.Max(0, _popupRect.Height - 2 - padding.Vertical));

                content.Arrange(inner);
            }
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = _popupRect;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var theme = GetTheme();
            var style = GetStyle<TooltipStyle>();

            var surface = style.ResolveSurfaceStyle(theme);
            var border = style.ResolveBorderStyle(theme);
            var glyphs = style.Glyphs;

            // Fill surface.
            for (var y = rect.Y; y < rect.Bottom; y++)
            {
                for (var x = rect.X; x < rect.Right; x++)
                {
                    buffer.SetCell(x, y, new Rune(' '), surface);
                }
            }

            if (rect.Width < 2 || rect.Height < 2)
            {
                return;
            }

            // Draw border.
            buffer.SetCell(rect.X, rect.Y, glyphs.TopLeft, border);
            buffer.SetCell(rect.Right - 1, rect.Y, glyphs.TopRight, border);
            buffer.SetCell(rect.X, rect.Bottom - 1, glyphs.BottomLeft, border);
            buffer.SetCell(rect.Right - 1, rect.Bottom - 1, glyphs.BottomRight, border);

            for (var x = rect.X + 1; x < rect.Right - 1; x++)
            {
                buffer.SetCell(x, rect.Y, glyphs.Horizontal, border);
                buffer.SetCell(x, rect.Bottom - 1, glyphs.Horizontal, border);
            }

            for (var y = rect.Y + 1; y < rect.Bottom - 1; y++)
            {
                buffer.SetCell(rect.X, y, glyphs.Vertical, border);
                buffer.SetCell(rect.Right - 1, y, glyphs.Vertical, border);
            }
        }
    }
}
