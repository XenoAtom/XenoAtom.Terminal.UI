// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Animation;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using System.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a segmented proportional bar with an optional legend.
/// </summary>
/// <remarks>
/// The breakdown is useful for visualizing how a total is distributed across multiple categories.
/// </remarks>
public sealed partial class BreakdownChart : Visual
{
    private readonly BreakdownBar _bar;
    private readonly BreakdownLegend _legend;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakdownChart"/> class.
    /// </summary>
    public BreakdownChart()
    {
        HorizontalAlignment = Align.Stretch;

        Segments = new BindableList<BreakdownSegment>(
            owner: this,
            name: $"{nameof(BreakdownChart)}.{nameof(Segments)}",
            onAdding: segment => segment.Attach(this),
            onRemoving: segment => segment.Detach(this));

        _bar = new BreakdownBar(this);
        _legend = new BreakdownLegend(this);
        AttachChild(_bar);
        AttachChild(_legend);

        LegendPlacement = BreakdownLegendPlacement.Below;
        ShowPercentages = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreakdownChart"/> class with segments.
    /// </summary>
    /// <param name="segments">The chart segments.</param>
    public BreakdownChart(IEnumerable<BreakdownSegment> segments) : this()
    {
        ArgumentNullException.ThrowIfNull(segments);
        foreach (var segment in segments)
        {
            Segments.Add(segment);
        }
    }

    /// <summary>
    /// Gets the segment collection.
    /// </summary>
    [Bindable]
    public BindableList<BreakdownSegment> Segments { get; }

    /// <summary>
    /// Gets or sets an optional title visual.
    /// </summary>
    [Bindable]
    public partial Visual? Title { get; set; }

    /// <summary>
    /// Gets or sets where the legend is placed relative to the bar.
    /// </summary>
    [Bindable]
    public partial BreakdownLegendPlacement LegendPlacement { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether percentages are displayed in the legend.
    /// </summary>
    [Bindable]
    public partial bool ShowPercentages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether raw values are displayed in the legend.
    /// </summary>
    [Bindable]
    public partial bool ShowValues { get; set; }

    /// <summary>
    /// Raised when a segment is clicked.
    /// </summary>
    [RoutedEvent(RoutingStrategy.Bubble)]
    private void OnSegmentClicked(BreakdownSegmentClickedEventArgs e) { }

    /// <inheritdoc />
    protected override int ChildrenCount => _title is null ? 2 : 3;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
    {
        if (_title is not null)
        {
            if (index == 0)
            {
                return _title;
            }

            index--;
        }

        var legendFirst = LegendPlacement == BreakdownLegendPlacement.Above;

        if (legendFirst)
        {
            if (index == 0) return _legend;
            if (index == 1) return _bar;
        }
        else
        {
            if (index == 0) return _bar;
            if (index == 1) return _legend;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var maxWidth = constraints.MaxWidth;
        var maxHeight = constraints.MaxHeight;

        var title = Title;
        title?.Measure(new LayoutConstraints(0, maxWidth, 0, maxHeight));

        var titleHeight = title?.DesiredSize.Height ?? 0;
        var remainingAfterTitle = maxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, maxHeight - titleHeight);

        var barHeight = 1;
        var remainingAfterBar = remainingAfterTitle == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, remainingAfterTitle - barHeight);

        _bar.Measure(new LayoutConstraints(0, maxWidth, 0, barHeight));
        _legend.Measure(new LayoutConstraints(0, maxWidth, 0, remainingAfterBar));

        var legendHeight = _legend.DesiredSize.Height;

        var naturalWidth = Math.Max(
            Math.Max(title?.DesiredSize.Width ?? 0, _bar.DesiredSize.Width),
            _legend.DesiredSize.Width);

        var naturalHeight = titleHeight + barHeight + legendHeight;

        var min = new Size(Math.Max(0, naturalWidth), Math.Max(0, naturalHeight));
        var natural = min;
        var max = new Size(LayoutConstants.Infinite, Math.Max(0, naturalHeight));
        return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        var x = finalRect.X;
        var y = finalRect.Y;
        var width = finalRect.Width;
        var height = finalRect.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var title = Title;
        if (title is not null)
        {
            var titleHeight = Math.Clamp(title.DesiredSize.Height, 0, height);
            title.Arrange(new Rectangle(x, y, width, titleHeight));
            y += titleHeight;
            height = Math.Max(0, height - titleHeight);
        }

        var barHeight = Math.Min(1, height);
        height = Math.Max(0, height - barHeight);

        if (LegendPlacement == BreakdownLegendPlacement.Above)
        {
            _legend.Arrange(new Rectangle(x, y, width, height));
            y += _legend.Bounds.Height;
            barHeight = Math.Min(1, Math.Max(0, finalRect.Bottom - y));
            _bar.Arrange(new Rectangle(x, y, width, barHeight));
        }
        else
        {
            _bar.Arrange(new Rectangle(x, y, width, barHeight));
            y += barHeight;
            _legend.Arrange(new Rectangle(x, y, width, height));
        }
    }

    private sealed class BreakdownBar : Visual, IAnimatedVisual
    {
        private readonly BreakdownChart _owner;
        private TooltipWindow? _tooltipWindow;
        private bool _tooltipVisible;
        private int _hoveredIndex = -1;
        private int _pressedIndex = -1;
        private int _lastPointerUiX;
        private int _lastPointerUiY;

        public BreakdownBar(BreakdownChart owner)
        {
            _owner = owner;
            HorizontalAlignment = Align.Stretch;
            VerticalAlignment = Align.Start;
            Focusable = false;
        }

        long IAnimatedVisual.NextAnimationTick => _tooltipVisible ? 0 : long.MaxValue;

        bool IAnimatedVisual.AdvanceAnimation(long timestamp) => AdvanceAnimation(timestamp);

        protected override void OnAttachedToApp(TerminalApp app)
        {
            base.OnAttachedToApp(app);
            app.RegisterAnimatedVisual(this);
        }

        protected override void OnDetachedFromApp(TerminalApp app)
        {
            app.UnregisterAnimatedVisual(this);
            CloseTooltip();
            base.OnDetachedFromApp(app);
        }

        private bool AdvanceAnimation(long timestamp)
        {
            _ = timestamp;

            if (!_tooltipVisible)
            {
                return false;
            }

            if (!IsHovered)
            {
                _hoveredIndex = -1;
                CloseTooltip();
                return true;
            }

            return false;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var min = new Size(1, 1);
            var natural = min;
            var max = new Size(LayoutConstants.Infinite, 1);
            return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!Bounds.Contains(e.UiX, e.UiY))
            {
                return;
            }

            _lastPointerUiX = e.UiX;
            _lastPointerUiY = e.UiY;

            var newIndex = HitTestSegment(e.LocalX);
            if (newIndex != _hoveredIndex)
            {
                _hoveredIndex = newIndex;
                UpdateTooltip();
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (!Bounds.Contains(e.UiX, e.UiY))
            {
                return;
            }

            _pressedIndex = HitTestSegment(e.LocalX);
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            if (_pressedIndex < 0)
            {
                return;
            }

            var releasedIndex = Bounds.Contains(e.UiX, e.UiY) ? HitTestSegment(e.LocalX) : -1;
            if (releasedIndex == _pressedIndex && releasedIndex >= 0 && releasedIndex < _owner.Segments.Count)
            {
                var segment = _owner.Segments[releasedIndex];
                _owner.RaiseEvent(BreakdownChart.SegmentClickedEvent, new BreakdownSegmentClickedEventArgs { Index = releasedIndex, Segment = segment });
                e.Handled = true;
            }

            _pressedIndex = -1;
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var style = GetStyle<BreakdownStyle>();
            var theme = GetTheme();

            var baseStyle = style.BarStyle ?? theme.ControlFillStyle();
            var fillRune = style.FillRune;
            var gap = Math.Max(0, style.SegmentGap);

            var segments = _owner.Segments;
            var segmentCount = segments.Count;
            if (segmentCount <= 0)
            {
                FillRange(buffer, rect.X, rect.Y, rect.Width, fillRune, baseStyle);
                return;
            }

            var usable = Math.Max(0, rect.Width - (gap * Math.Max(0, segmentCount - 1)));

            var total = 0.0;
            for (var i = 0; i < segmentCount; i++)
            {
                total += Math.Max(0.0, segments[i].Value);
            }

            if (total <= 0.0 || usable <= 0)
            {
                FillRange(buffer, rect.X, rect.Y, rect.Width, fillRune, baseStyle);
                return;
            }

            Span<int> widths = segmentCount <= 128 ? stackalloc int[segmentCount] : new int[segmentCount];
            var used = 0;
            for (var i = 0; i < segmentCount; i++)
            {
                var value = Math.Max(0.0, segments[i].Value);
                var w = (int)Math.Floor((value / total) * usable);
                widths[i] = Math.Max(0, w);
                used += widths[i];
            }

            var remaining = Math.Max(0, usable - used);
            for (var i = 0; remaining > 0 && i < segmentCount; i++)
            {
                if (segments[i].Value > 0.0)
                {
                    widths[i]++;
                    remaining--;
                }
            }

            var colors = style.DefaultSegmentColors;

            var x = rect.X;
            for (var i = 0; i < segmentCount; i++)
            {
                var w = widths[i];
                if (w > 0)
                {
                    var segmentColor = segments[i].Color;
                    if (segmentColor is null && colors is { Count: > 0 })
                    {
                        segmentColor = colors[i % colors.Count];
                    }

                    var actualColor = segmentColor ?? BreakdownUtilities.GetFallbackSegmentColor(theme, i);

                    var segStyle = baseStyle.WithBackground(actualColor);
                    FillRange(buffer, x, rect.Y, w, fillRune, segStyle);
                    x += w;
                }

                if (gap > 0 && i < segmentCount - 1)
                {
                    FillRange(buffer, x, rect.Y, gap, new Rune(' '), baseStyle);
                    x += gap;
                }
            }

            var tail = rect.Right - x;
            if (tail > 0)
            {
                FillRange(buffer, x, rect.Y, tail, fillRune, baseStyle);
            }
        }

        private int HitTestSegment(int localX)
        {
            var rect = Bounds;
            if (rect.Width <= 0)
            {
                return -1;
            }

            var style = GetStyle<BreakdownStyle>();
            var gap = Math.Max(0, style.SegmentGap);

            var segments = _owner.Segments;
            var segmentCount = segments.Count;
            if (segmentCount <= 0)
            {
                return -1;
            }

            var usable = Math.Max(0, rect.Width - (gap * Math.Max(0, segmentCount - 1)));
            if (usable <= 0)
            {
                return -1;
            }

            var total = 0.0;
            for (var i = 0; i < segmentCount; i++)
            {
                total += Math.Max(0.0, segments[i].Value);
            }

            if (total <= 0.0)
            {
                return -1;
            }

            var used = 0;
            Span<int> widths = segmentCount <= 128 ? stackalloc int[segmentCount] : new int[segmentCount];
            for (var i = 0; i < segmentCount; i++)
            {
                var value = Math.Max(0.0, segments[i].Value);
                var w = (int)Math.Floor((value / total) * usable);
                widths[i] = Math.Max(0, w);
                used += widths[i];
            }

            var remaining = Math.Max(0, usable - used);
            for (var i = 0; remaining > 0 && i < segmentCount; i++)
            {
                if (segments[i].Value > 0.0)
                {
                    widths[i]++;
                    remaining--;
                }
            }

            var x = 0;
            for (var i = 0; i < segmentCount; i++)
            {
                var w = widths[i];
                if (localX >= x && localX < x + w)
                {
                    return i;
                }
                x += w;

                if (gap > 0 && i < segmentCount - 1)
                {
                    x += gap;
                }
            }

            return -1;
        }

        private void UpdateTooltip()
        {
            var app = App;
            if (app is null)
            {
                return;
            }

            if (!IsHovered || _hoveredIndex < 0 || _hoveredIndex >= _owner.Segments.Count)
            {
                CloseTooltip();
                return;
            }

            var segment = _owner.Segments[_hoveredIndex];
            // Tooltips are hosted in a separate window, so the visual instance must not already be attached elsewhere.
            // If a user accidentally reuses a visual as a tooltip from another part of the tree, fall back to the default
            // tooltip rather than crashing.
            var tooltip = segment.Tooltip;
            if (tooltip is not null && tooltip.Parent is not null)
            {
                tooltip = null;
            }

            tooltip ??= CreateDefaultTooltip(segment);
            if (tooltip is null)
            {
                CloseTooltip();
                return;
            }

            if (_tooltipWindow is null)
            {
                _tooltipWindow = new TooltipWindow();
            }

            var window = _tooltipWindow;
            window.Anchor = null;
            window.AnchorRect = new Rectangle(_lastPointerUiX, _lastPointerUiY, 1, 1);
            window.Placement = PopupPlacement.Above;
            window.OffsetX = 0;
            window.OffsetY = 1;
            window.Content = tooltip;

            app.ShowTooltipWindow(window);
            _tooltipVisible = true;
            app.RequestAnimation();
        }

        private void CloseTooltip()
        {
            var app = App;
            if (app is null || _tooltipWindow is null)
            {
                return;
            }

            app.CloseTooltipWindow(_tooltipWindow);
            _tooltipWindow.Content = null;
            _tooltipVisible = false;
        }

        private Visual? CreateDefaultTooltip(BreakdownSegment segment)
        {
            var total = 0.0;
            for (var i = 0; i < _owner.Segments.Count; i++)
            {
                total += Math.Max(0.0, _owner.Segments[i].Value);
            }

            var pct = total <= 0.0 ? 0.0 : (Math.Max(0.0, segment.Value) / total) * 100.0;
            var label = segment.Label;
            var value = _owner.ToStringValue(segment.Value);
            var pctText = _owner.ToStringValue(pct, "0") + "%";

            if (label is null)
            {
                return new VStack
                {
                    $"{value} ({pctText})"
                };
            }

            // Segment labels are typically part of the legend visual tree. We can't reuse the same Visual instance
            // inside the tooltip window, so we create a detached representation of the label when needed.
            var tooltipLabel = label.Parent is null ? label : CreateDetachedTooltipLabel(label);

            return new VStack
            {
                tooltipLabel,
                $"{value} ({pctText})"
            }.Spacing(0);
        }

        private static Visual CreateDetachedTooltipLabel(Visual label)
            => label switch
            {
                TextBlock tb => new TextBlock(tb.Text ?? string.Empty)
                {
                    Wrap = tb.Wrap,
                    TextAlignment = tb.TextAlignment,
                    Trimming = tb.Trimming,
                },
                Markup markup => new Markup(markup.Text ?? string.Empty)
                {
                    Wrap = markup.Wrap,
                    TextAlignment = markup.TextAlignment,
                    Trimming = markup.Trimming,
                },
                _ => new TextBlock(label.ToString() ?? label.GetType().Name) { Wrap = false },
            };

        private static void FillRange(CellBuffer buffer, int x, int y, int width, Rune rune, Style style)
        {
            for (var i = 0; i < width; i++)
            {
                buffer.SetCell(x + i, y, rune, style);
            }
        }
    }

    private sealed class BreakdownLegend : Visual
    {
        private readonly BreakdownChart _owner;
        private readonly List<LegendItem> _items = new();
        private readonly WrapHStack _compactLayout;
        private readonly VStack _expandedLayout;
        private Visual _layout;
        private bool _layoutDirty = true;
        private int _lastSegmentsCount = -1;
        private BreakdownLegendLayout _lastLayout;
        private int _lastLegendItemSpacing = -1;
        private WrapJustify _lastLegendJustify;

        public BreakdownLegend(BreakdownChart owner)
        {
            _owner = owner;
            HorizontalAlignment = Align.Stretch;

            _compactLayout = new WrapHStack
            {
                HorizontalAlignment = Align.Stretch,
                RunSpacing = 0,
                MeasureMode = WrapMeasureMode.Unconstrained,
            };

            _expandedLayout = new VStack
            {
                HorizontalAlignment = Align.Stretch,
                Spacing = 0,
            };

            _layout = _compactLayout;
            AttachChild(_layout);
        }

        protected override int ChildrenCount => 1;

        protected override Visual GetChild(int index) => index == 0 ? _layout : throw new ArgumentOutOfRangeException(nameof(index));

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            EnsureLayout();
            return _layout.Measure(constraints);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            EnsureLayout();
            _layout.Arrange(finalRect);
        }

        private void EnsureLayout()
        {
            var segments = _owner.Segments;
            var segmentsCount = segments.Count;
            var style = GetStyle<BreakdownStyle>();
            var layout = style.LegendLayout;
            var legendItemSpacing = Math.Max(0, style.LegendItemSpacing);
            var legendJustify = style.LegendJustify;

            if (segmentsCount == _lastSegmentsCount
                && layout == _lastLayout
                && legendItemSpacing == _lastLegendItemSpacing
                && legendJustify == _lastLegendJustify
                && !_layoutDirty)
            {
                return;
            }

            _lastLayout = layout;
            _lastLegendItemSpacing = legendItemSpacing;
            _lastLegendJustify = legendJustify;

            if (segmentsCount != _lastSegmentsCount)
            {
                UpdateItems(segmentsCount);
                _lastSegmentsCount = segmentsCount;
                _layoutDirty = true;
            }

            var targetLayout = layout == BreakdownLegendLayout.Expanded ? (Visual)_expandedLayout : _compactLayout;
            if (!ReferenceEquals(_layout, targetLayout))
            {
                // Move legend items to the requested layout container.
                _compactLayout.Children.Clear();
                _expandedLayout.Children.Clear();

                DetachChild(_layout);
                _layout = targetLayout;
                AttachChild(_layout);
                _layoutDirty = true;
            }

            _compactLayout.Spacing = legendItemSpacing;
            _compactLayout.Justify = legendJustify;

            if (!_layoutDirty)
            {
                return;
            }

            _layoutDirty = false;

            _compactLayout.Children.Clear();
            _expandedLayout.Children.Clear();

            if (_items.Count == 0)
            {
                return;
            }

            if (layout == BreakdownLegendLayout.Expanded)
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    _expandedLayout.Children.Add(_items[i]);
                }

                return;
            }

            for (var i = 0; i < _items.Count; i++)
            {
                _compactLayout.Children.Add(_items[i]);
            }
        }

        private void UpdateItems(int segmentsCount)
        {
            while (_items.Count > segmentsCount)
            {
                _items.RemoveAt(_items.Count - 1);
            }

            while (_items.Count < segmentsCount)
            {
                _items.Add(new LegendItem(_owner, _items.Count));
            }
        }

        private sealed class LegendItem : Visual
        {
            private readonly BreakdownChart _owner;
            private readonly int _index;
            private readonly HStack _layout;
            private readonly TextBlock _suffix;
            private BreakdownStyle? _appliedStyle;

            public LegendItem(BreakdownChart owner, int index)
            {
                _owner = owner;
                _index = index;

                var label = new ComputedVisual(() => ResolveSegment()?.Label ?? string.Empty);
                _suffix = new TextBlock(() =>
                {
                    var text = BuildSuffixText();
                    return string.IsNullOrEmpty(text) ? string.Empty : " " + text;
                });

                var labelWithSuffix = new HStack(label, _suffix).Spacing(0);

                _layout = new HStack
                {
                    new LegendSwatch(owner, index),
                    labelWithSuffix,
                }.Spacing(1);

                AttachChild(_layout);
            }

            protected override int ChildrenCount => 1;

            protected override Visual GetChild(int index) => index == 0 ? _layout : throw new ArgumentOutOfRangeException(nameof(index));

            protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            {
                ApplyStyle();
                return _layout.Measure(constraints);
            }

            protected override void ArrangeCore(in Rectangle finalRect)
            {
                ApplyStyle();
                _layout.Arrange(finalRect);
            }

            private void ApplyStyle()
            {
                var style = GetStyle<BreakdownStyle>();
                if (ReferenceEquals(_appliedStyle, style))
                {
                    return;
                }

                _appliedStyle = style;

                if (style.LegendStyle is { } legendStyle)
                {
                    SetStyle(TextBlockStyle.Key, ToTextBlockStyle(legendStyle));
                }

                if (style.LegendMutedStyle is { } mutedStyle)
                {
                    _suffix.SetStyle(TextBlockStyle.Key, ToTextBlockStyle(mutedStyle));
                }
            }

            private BreakdownSegment? ResolveSegment()
                => _index >= 0 && _index < _owner.Segments.Count ? _owner.Segments[_index] : null;

            private string BuildSuffixText()
            {
                var segment = ResolveSegment();
                if (segment is null)
                {
                    return string.Empty;
                }

                var showValues = _owner.ShowValues;
                var showPct = _owner.ShowPercentages;
                if (!showValues && !showPct)
                {
                    return string.Empty;
                }

                var total = 0.0;
                for (var i = 0; i < _owner.Segments.Count; i++)
                {
                    total += Math.Max(0.0, _owner.Segments[i].Value);
                }

                var builder = new StringBuilder();
                var first = true;

                if (showPct)
                {
                    var pct = total <= 0.0 ? 0.0 : (Math.Max(0.0, segment.Value) / total) * 100.0;
                    builder.Append('(');
                    builder.Append(_owner.ToStringValue(pct, "0"));
                    builder.Append("%)");
                    first = false;
                }

                if (showValues)
                {
                    if (!first)
                    {
                        builder.Append(' ');
                    }
                    builder.Append(_owner.ToStringValue(segment.Value));
                }

                return builder.ToString();
            }

            private static TextBlockStyle ToTextBlockStyle(Style style)
            {
                Color? fg = null;
                Color? bg = null;

                if (style.TryGetForeground(out var foreground))
                {
                    fg = foreground;
                }

                if (style.TryGetBackground(out var background))
                {
                    bg = background;
                }

                return new TextBlockStyle
                {
                    Foreground = fg,
                    Background = bg,
                    TextStyle = style.TextStyle,
                };
            }

            private sealed class LegendSwatch : Visual
            {
                private readonly BreakdownChart _owner;
                private readonly int _index;

                public LegendSwatch(BreakdownChart owner, int index)
                {
                    _owner = owner;
                    _index = index;
                }

                protected override SizeHints MeasureCore(in LayoutConstraints constraints)
                {
                    _ = constraints;
                    return SizeHints.Fixed(new Size(1, 1));
                }

                protected override void RenderOverride(CellBuffer buffer)
                {
                    var rect = Bounds;
                    if (rect.Width <= 0 || rect.Height <= 0)
                    {
                        return;
                    }

                    var theme = GetTheme();
                    var style = GetStyle<BreakdownStyle>();

                    var segment = _index >= 0 && _index < _owner.Segments.Count ? _owner.Segments[_index] : null;
                    var color = segment?.Color;
                    if (color is null && style.DefaultSegmentColors is { Count: > 0 } list)
                    {
                        color = list[_index % list.Count];
                    }

                    var actualColor = color ?? BreakdownUtilities.GetFallbackSegmentColor(theme, _index);

                    buffer.SetCell(rect.X, rect.Y, new Rune('■'), Style.None.WithForeground(actualColor));
                }
            }
        }
    }
}

internal static class BreakdownUtilities
{
    internal static Color GetFallbackSegmentColor(Theme theme, int index)
    {
        var scheme = theme.Scheme;

        return index switch
        {
            0 => theme.Primary ?? theme.Accent ?? scheme?.Blue ?? Colors.TerminalBlue,
            1 => theme.Success ?? scheme?.Green ?? Colors.TerminalGreen,
            2 => theme.Warning ?? scheme?.Yellow ?? Colors.TerminalYellow,
            3 => theme.Error ?? scheme?.Red ?? Colors.TerminalRed,
            _ => theme.Accent ?? scheme?.Blue ?? Colors.TerminalBlue,
        };
    }
}

/// <summary>
/// Specifies where the legend is placed relative to a breakdown bar.
/// </summary>
public enum BreakdownLegendPlacement
{
    /// <summary>
    /// The legend is rendered above the bar.
    /// </summary>
    Above,

    /// <summary>
    /// The legend is rendered below the bar.
    /// </summary>
    Below,
}
