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
public sealed partial class Breakdown : Visual
{
    private readonly BreakdownBar _bar;
    private readonly BreakdownLegend _legend;

    /// <summary>
    /// Initializes a new instance of the <see cref="Breakdown"/> class.
    /// </summary>
    public Breakdown()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        Segments = new BindableList<BreakdownSegment>(
            owner: this,
            name: $"{nameof(Breakdown)}.{nameof(Segments)}",
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
        Bounds = finalRect;

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
        private readonly Breakdown _owner;
        private TooltipWindow? _tooltipWindow;
        private bool _tooltipVisible;
        private int _hoveredIndex = -1;
        private int _pressedIndex = -1;
        private int _lastPointerUiX;
        private int _lastPointerUiY;

        public BreakdownBar(Breakdown owner)
        {
            _owner = owner;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Top;
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

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;
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
                _owner.RaiseEvent(Breakdown.SegmentClickedEvent, new BreakdownSegmentClickedEventArgs { Index = releasedIndex, Segment = segment });
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
            var tooltip = segment.Tooltip ?? CreateDefaultTooltip(segment);
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

            return new VStack
            {
                label,
                $"{value} ({pctText})"
            }.Spacing(0);
        }

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
        private readonly Breakdown _owner;
        private readonly List<LegendRow> _rows = new();
        private int _segmentsVersion = -1;

        public BreakdownLegend(Breakdown owner)
        {
            _owner = owner;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        protected override int ChildrenCount => _rows.Count;

        protected override Visual GetChild(int index) => _rows[index];

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            EnsureRows();

            var width = 0;
            var height = 0;
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].Measure(constraints);
                width = Math.Max(width, _rows[i].DesiredSize.Width);
                height += _rows[i].DesiredSize.Height;
            }

            var min = new Size(width, height);
            var natural = min;
            var max = new Size(LayoutConstants.Infinite, height);
            return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            Bounds = finalRect;

            EnsureRows();

            var y = finalRect.Y;
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var h = Math.Min(row.DesiredSize.Height, Math.Max(0, finalRect.Bottom - y));
                row.Arrange(new Rectangle(finalRect.X, y, finalRect.Width, h));
                y += h;
            }
        }

        private void EnsureRows()
        {
            var segments = _owner.Segments;
            var version = segments.Version;
            if (version == _segmentsVersion)
            {
                return;
            }

            _segmentsVersion = version;
            for (var i = 0; i < _rows.Count; i++)
            {
                DetachChild(_rows[i]);
            }

            _rows.Clear();

            for (var i = 0; i < segments.Count; i++)
            {
                var row = new LegendRow(_owner, i);
                _rows.Add(row);
                AttachChild(row);
            }
        }

        private sealed class LegendRow : Visual
        {
            private readonly Breakdown _owner;
            private readonly int _index;
            private readonly HStack _layout;

            public LegendRow(Breakdown owner, int index)
            {
                _owner = owner;
                _index = index;

                var label = new ComputedVisual(() => ResolveSegment()?.Label ?? string.Empty);
                var value = new TextBlock(() => BuildValueText()).HorizontalAlignment(HorizontalAlignment.Right);

                _layout = new HStack
                {
                    new LegendSwatch(owner, index),
                    label,
                    value,
                }.Spacing(1).HorizontalAlignment(HorizontalAlignment.Stretch);

                AttachChild(_layout);
            }

            protected override int ChildrenCount => 1;

            protected override Visual GetChild(int index) => index == 0 ? _layout : throw new ArgumentOutOfRangeException(nameof(index));

            protected override SizeHints MeasureCore(in LayoutConstraints constraints) => _layout.Measure(constraints);

            protected override void ArrangeCore(in Rectangle finalRect)
            {
                Bounds = finalRect;
                _layout.Arrange(finalRect);
            }

            private BreakdownSegment? ResolveSegment()
                => _index >= 0 && _index < _owner.Segments.Count ? _owner.Segments[_index] : null;

            private string BuildValueText()
            {
                var segment = ResolveSegment();
                if (segment is null)
                {
                    return string.Empty;
                }

                var total = 0.0;
                for (var i = 0; i < _owner.Segments.Count; i++)
                {
                    total += Math.Max(0.0, _owner.Segments[i].Value);
                }

                var showValues = _owner.ShowValues;
                var showPct = _owner.ShowPercentages;
                if (!showValues && !showPct)
                {
                    return string.Empty;
                }

                var valueText = showValues ? _owner.ToStringValue(segment.Value) : string.Empty;
                var pctText = string.Empty;
                if (showPct)
                {
                    var pct = total <= 0.0 ? 0.0 : (Math.Max(0.0, segment.Value) / total) * 100.0;
                    pctText = _owner.ToStringValue(pct, "0") + "%";
                }

                if (showValues && showPct)
                {
                    return valueText + "  " + pctText;
                }

                return showValues ? valueText : pctText;
            }

            private sealed class LegendSwatch : Visual
            {
                private readonly Breakdown _owner;
                private readonly int _index;

                public LegendSwatch(Breakdown owner, int index)
                {
                    _owner = owner;
                    _index = index;
                }

                protected override SizeHints MeasureCore(in LayoutConstraints constraints)
                {
                    _ = constraints;
                    return SizeHints.Fixed(new Size(1, 1));
                }

                protected override void ArrangeCore(in Rectangle finalRect)
                {
                    Bounds = finalRect;
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
