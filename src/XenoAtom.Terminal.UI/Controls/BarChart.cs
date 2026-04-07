// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using XenoAtom.Terminal.UI.Collections;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays a horizontal bar chart with labels and optional value text.
/// </summary>
public sealed partial class BarChart : Visual
{
    private readonly Grid _grid;
    private readonly List<RowEntry> _rows = new();
    private int _itemsVersion = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChart"/> class.
    /// </summary>
    public BarChart()
    {
        HorizontalAlignment = Align.Stretch;

        Items = new BindableList<BarChartItem>(
            owner: this,
            name: $"{nameof(BarChart)}.{nameof(Items)}",
            onAdding: item => item.Attach(this),
            onRemoving: item => item.Detach(this));

        _grid = new Grid
        {
            AutoGrowRows = false,
            AutoGrowColumns = false,
            ColumnGap = 1,
        };

        _grid.Columns(
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star(1) });

        AttachChild(_grid);

        TitlePlacement = ChartTitlePlacement.Above;
        ShowValues = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChart"/> class with items.
    /// </summary>
    /// <param name="items">The chart items.</param>
    public BarChart(IEnumerable<BarChartItem> items) : this()
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    /// <summary>
    /// Gets the chart items.
    /// </summary>
    [Bindable]
    public BindableList<BarChartItem> Items { get; }

    /// <summary>
    /// Gets or sets an optional title visual.
    /// </summary>
    [Bindable]
    public partial Visual? Title { get; set; }

    /// <summary>
    /// Gets or sets where the title is placed relative to the chart.
    /// </summary>
    [Bindable]
    public partial ChartTitlePlacement TitlePlacement { get; set; }

    /// <summary>
    /// Gets or sets the optional minimum value used for normalization.
    /// </summary>
    [Bindable]
    public partial double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum value used for normalization.
    /// </summary>
    [Bindable]
    public partial double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether values are shown.
    /// </summary>
    [Bindable]
    public partial bool ShowValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether percentages are shown.
    /// </summary>
    [Bindable]
    public partial bool ShowPercentages { get; set; }

    /// <inheritdoc />
    protected override int ChildrenCount => _title is null ? 1 : 2;

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

        if (index == 0)
        {
            return _grid;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        ApplyStyleToGrid();
        EnsureRows();

        var maxWidth = constraints.MaxWidth;
        var maxHeight = constraints.MaxHeight;

        var title = Title;
        title?.Measure(new LayoutConstraints(0, maxWidth, 0, maxHeight));

        var titleHeight = title?.DesiredSize.Height ?? 0;
        var remaining = maxHeight == LayoutConstants.Infinite
            ? LayoutConstants.Infinite
            : Math.Max(0, maxHeight - titleHeight);

        _grid.Measure(new LayoutConstraints(0, maxWidth, 0, remaining));

        var titleHints = title?.MeasureHints ?? SizeHints.Fixed(Size.Zero);
        var gridHints = _grid.MeasureHints;

        var min = new Size(
            LayoutConstants.ClampFinite(Math.Max(titleHints.Min.Width, gridHints.Min.Width)),
            LayoutConstants.ClampFinite(titleHints.Min.Height + gridHints.Min.Height));

        var natural = new Size(
            LayoutConstants.ClampFinite(Math.Max(titleHints.Natural.Width, gridHints.Natural.Width)),
            LayoutConstants.ClampFinite(titleHints.Natural.Height + gridHints.Natural.Height));

        var maxWidthHint = LayoutConstants.IsInfinite(titleHints.Max.Width) || LayoutConstants.IsInfinite(gridHints.Max.Width)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(Math.Max(titleHints.Max.Width, gridHints.Max.Width));
        var maxHeightHint = LayoutConstants.IsInfinite(titleHints.Max.Height) || LayoutConstants.IsInfinite(gridHints.Max.Height)
            ? LayoutConstants.Infinite
            : LayoutConstants.ClampOrInfinite(titleHints.Max.Height + gridHints.Max.Height);
        var max = new Size(maxWidthHint, maxHeightHint);

        return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        ApplyStyleToGrid();
        EnsureRows();

        var x = finalRect.X;
        var y = finalRect.Y;
        var width = finalRect.Width;
        var height = finalRect.Height;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var title = Title;
        var titleHeight = title?.DesiredSize.Height ?? 0;

        if (title is not null && TitlePlacement == ChartTitlePlacement.Above)
        {
            var h = Math.Clamp(titleHeight, 0, height);
            title.Arrange(new Rectangle(x, y, width, h));
            y += h;
            height = Math.Max(0, height - h);
            _grid.Arrange(new Rectangle(x, y, width, height));
            return;
        }

        if (title is not null && TitlePlacement == ChartTitlePlacement.Below)
        {
            var h = Math.Clamp(titleHeight, 0, height);
            _grid.Arrange(new Rectangle(x, y, width, Math.Max(0, height - h)));
            title.Arrange(new Rectangle(x, finalRect.Bottom - h, width, h));
            return;
        }

        _grid.Arrange(new Rectangle(x, y, width, height));
    }

    internal (double Min, double Max) ResolveRange()
    {
        var min = Minimum ?? 0.0;
        var max = Maximum;
        if (max is null)
        {
            var computedMax = 0.0;
            for (var i = 0; i < Items.Count; i++)
            {
                var v = Items[i].Value;
                if (!double.IsNaN(v) && !double.IsInfinity(v))
                {
                    computedMax = Math.Max(computedMax, v);
                }
            }
            max = computedMax;
        }

        var maxValue = max.GetValueOrDefault();
        if (maxValue <= min)
        {
            maxValue = min + 1.0;
        }

        return (min, maxValue);
    }

    private void ApplyStyleToGrid()
    {
        var style = GetStyle<BarChartStyle>();

        var rowGap = Math.Max(0, style.RowSpacing);
        if (_grid.RowGap != rowGap)
        {
            _grid.RowGap = rowGap;
        }

        if (_grid.Padding != style.Padding)
        {
            _grid.Padding = style.Padding;
        }

        for (var i = 0; i < _rows.Count; i++)
        {
            _rows[i].ApplyStyle(style);
        }
    }

    private void EnsureRows()
    {
        var version = Items.Version;
        if (version == _itemsVersion)
        {
            return;
        }

        _itemsVersion = version;
        _rows.Clear();

        _grid.RowDefinitions.Clear();
        _grid.Cells.Clear();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var entry = new RowEntry(this, item, i);
            _rows.Add(entry);

            _grid.Cell(entry.LabelHost, i, 0);
            _grid.Cell(entry.Bar, i, 1);
        }
    }

    private sealed class RowEntry
    {
        private readonly BarChart _owner;
        private readonly BarChartItem _item;
        private readonly int _index;

        private BarChartStyle? _applied;

        public RowEntry(BarChart owner, BarChartItem item, int index)
        {
            _owner = owner;
            _item = item;
            _index = index;

            LabelHost = new ComputedVisual(() => _item.Label);
            LabelHost.HorizontalAlignment = Align.Start;

            Bar = new BarCell(_owner, _item, _index);
            Bar.HorizontalAlignment = Align.Stretch;
        }

        public Visual LabelHost { get; }

        public Visual Bar { get; }

        public void ApplyStyle(BarChartStyle style)
        {
            if (ReferenceEquals(_applied, style))
            {
                return;
            }

            _applied = style;

            if (style.LabelTextStyle is { } label)
            {
                LabelHost.SetStyle(TextBlockStyle.Key, label);
            }
            
            ((BarCell)Bar).ApplyStyle(style);
        }
    }

    private string BuildDefaultValueText(BarChartItem item)
    {
        var showValues = ShowValues;
        var showPct = ShowPercentages;
        if (!showValues && !showPct)
        {
            return string.Empty;
        }

        var (min, max) = ResolveRange();

        var value = item.Value;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            value = min;
        }

        var valueText = showValues ? ToStringValue(value) : string.Empty;

        var pctText = string.Empty;
        if (showPct)
        {
            var t = (value - min) / (max - min);
            t = Math.Clamp(t, 0.0, 1.0);
            pctText = ToStringValue(t * 100.0, "0") + "%";
        }

        if (showValues && showPct)
        {
            return valueText + "  " + pctText;
        }

        return showValues ? valueText : pctText;
    }

    private sealed class BarCell : Visual
    {
        private readonly BarChart _owner;
        private readonly BarChartItem _item;
        private readonly int _index;

        private readonly ComputedVisual _valueHost;
        private BarChartStyle? _style;

        public BarCell(BarChart owner, BarChartItem item, int index)
        {
            _owner = owner;
            _item = item;
            _index = index;
            HorizontalAlignment = Align.Stretch;

            _valueHost = new ComputedVisual(() => _item.ValueLabel);
            _valueHost.IsHitTestVisible = false;
            AttachChild(_valueHost);
        }

        protected override int ChildrenCount => 1;

        protected override Visual GetChild(int index)
        {
            if (index == 0)
            {
                return _valueHost;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public void ApplyStyle(BarChartStyle style)
        {
            _style = style;
            if (style.ValueTextStyle is { } value)
            {
                _valueHost.SetStyle(TextBlockStyle.Key, value);
            }
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            _valueHost.Measure(new LayoutConstraints(0, constraints.MaxWidth, 0, constraints.MaxHeight));
            var min = new Size(10, 1);
            var natural = new Size(Math.Max(min.Width, _valueHost.DesiredSize.Width), Math.Max(1, _valueHost.DesiredSize.Height));
            var max = new Size(LayoutConstants.Infinite, natural.Height);
            return SizeHints.Flex(min, natural, max, growX: 1, growY: 0, shrinkX: 1, shrinkY: 0);
        }

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            var custom = _item.ValueLabel;
            if (custom is null)
            {
                _valueHost.Arrange(new Rectangle(finalRect.X, finalRect.Y, 0, 0));
                return;
            }

            var chartStyle = _style ?? GetStyle<BarChartStyle>();
            var progressStyle = chartStyle.BarStyle;

            var (min, max) = _owner.ResolveRange();
            var value = _item.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = min;
            }
            var t = (value - min) / (max - min);
            t = Math.Clamp(t, 0.0, 1.0);

            var barY = finalRect.Y + Math.Max(0, (finalRect.Height - 1) / 2);
            var barStartX = finalRect.X;
            var barEndX = finalRect.Right;

            var showFrame = progressStyle.ShowFrame || progressStyle.Variant == ProgressBarVariant.Bracketed;
            if (showFrame && barEndX - barStartX >= 2)
            {
                barStartX++;
                barEndX--;
            }

            var barWidth = Math.Max(0, barEndX - barStartX);
            var valueWidth = Math.Max(0, _valueHost.DesiredSize.Width);
            if (barWidth <= 0 || valueWidth <= 0)
            {
                _valueHost.Arrange(new Rectangle(finalRect.X, barY, 0, 1));
                return;
            }

            var filledCells = GetFilledCells(barWidth, t, progressStyle.Variant);
            // Place the custom value label at the end of the filled bar, like Spectre/Rich.
            // We intentionally avoid adding an extra gap cell so the label stays visually tied to the bar.
            var preferredX = barStartX + Math.Clamp(filledCells, 0, barWidth);

            var maxX = Math.Max(finalRect.X, barEndX - valueWidth);
            var x = Math.Clamp(preferredX, finalRect.X, maxX);
            _valueHost.Arrange(new Rectangle(x, barY, Math.Min(valueWidth, finalRect.Right - x), 1));
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0)
            {
                return;
            }

            var theme = GetTheme();
            var chartStyle = _style ?? GetStyle<BarChartStyle>();
            var progressStyle = chartStyle.BarStyle;

            var (min, max) = _owner.ResolveRange();

            var value = _item.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                value = min;
            }

            var t = (value - min) / (max - min);
            t = Math.Clamp(t, 0.0, 1.0);

            var borderStyle = progressStyle.ResolveBorder(theme);
            var filledStyle = progressStyle.ResolveFilled(theme);
            var unfilledStyle = progressStyle.ResolveUnfilled(theme);

            var itemColor = _item.BarColor;
            if (itemColor is null && chartStyle.DefaultBarColors is { Count: > 0 } defaults)
            {
                itemColor = defaults[_index % defaults.Count];
            }

            if (itemColor is null)
            {
                itemColor = BreakdownUtilities.GetFallbackSegmentColor(theme, _index);
            }

            if (progressStyle.Variant is ProgressBarVariant.Solid or ProgressBarVariant.Shaded)
            {
                filledStyle = filledStyle.WithBackground(itemColor.Value);
            }
            else
            {
                filledStyle = filledStyle.WithForeground(itemColor.Value);
            }

            var barStartX = rect.X;
            var barEndX = rect.X + rect.Width;

            var showFrame = progressStyle.ShowFrame || progressStyle.Variant == ProgressBarVariant.Bracketed;
            if (showFrame && barEndX - barStartX >= 2)
            {
                buffer.SetCell(barStartX, rect.Y, progressStyle.FrameLeftGlyph, borderStyle);
                buffer.SetCell(barEndX - 1, rect.Y, progressStyle.FrameRightGlyph, borderStyle);
                barStartX++;
                barEndX--;
            }

            var barWidth = Math.Max(0, barEndX - barStartX);
            if (barWidth <= 0)
            {
                return;
            }

            if (progressStyle.Variant == ProgressBarVariant.Segmented)
            {
                RenderSegmented(buffer, rect.Y, barStartX, barWidth, t, progressStyle.FillGlyph, progressStyle.TrackGlyph, filledStyle, unfilledStyle);
                RenderValueIfNeeded(buffer, rect.Y, barStartX, barEndX, barWidth, t, progressStyle.Variant, itemColor.Value, chartStyle, theme);
                return;
            }

            if (progressStyle.Variant == ProgressBarVariant.Shaded)
            {
                RenderSolid(buffer, rect.Y, barStartX, barWidth, t, new Rune(0x2593), new Rune(0x2591), filledStyle, unfilledStyle);
                RenderValueIfNeeded(buffer, rect.Y, barStartX, barEndX, barWidth, t, progressStyle.Variant, itemColor.Value, chartStyle, theme);
                return;
            }

            RenderSolid(buffer, rect.Y, barStartX, barWidth, t, progressStyle.FillGlyph, progressStyle.TrackGlyph, filledStyle, unfilledStyle);
            RenderValueIfNeeded(buffer, rect.Y, barStartX, barEndX, barWidth, t, progressStyle.Variant, itemColor.Value, chartStyle, theme);
        }

        private void RenderValueIfNeeded(CellBuffer buffer, int y, int barStartX, int maxX, int barWidth, double t, ProgressBarVariant variant, Color barColor, BarChartStyle chartStyle, Theme theme)
        {
            if (_item.ValueLabel is not null)
            {
                return;
            }

            var text = _owner.BuildDefaultValueText(_item);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var valueWidth = TerminalTextUtility.GetWidth(text.AsSpan());
            if (valueWidth <= 0)
            {
                return;
            }

            var filledCells = GetFilledCells(barWidth, t, variant);
            // Place the text at the end of the filled bar segment.
            var x = barStartX + Math.Clamp(filledCells, 0, barWidth);

            if (x + valueWidth > maxX)
            {
                x = maxX - valueWidth;
            }

            if (x < Bounds.X)
            {
                x = Bounds.X;
            }

            TextBlockStyle textStyle = chartStyle.ValueTextStyle ?? TextBlockStyle.Default;
            if (textStyle.Foreground is null)
            {
                textStyle = textStyle with { Foreground = barColor };
            }

            var style = textStyle.ResolveTextStyle(theme);
            buffer.WriteText(x, y, text.AsSpan(), style);
        }

        private static int GetFilledCells(int width, double value, ProgressBarVariant variant)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            if (width <= 0)
            {
                return 0;
            }

            if (variant == ProgressBarVariant.Segmented)
            {
                var scaled = value * width;
                var whole = (int)Math.Floor(scaled);
                var frac = scaled - whole;
                whole = Math.Clamp(whole, 0, width);
                var remainder = (int)Math.Round(frac * 8.0);
                remainder = Math.Clamp(remainder, 0, 8);
                return remainder > 0 && whole < width ? whole + 1 : whole;
            }

            var filled = (int)Math.Round(width * value);
            return Math.Clamp(filled, 0, width);
        }

        private static readonly Rune[] SegmentGlyphs =
        [
            new Rune(' '),        // 0/8
            new Rune(0x258F),     // ▏ 1/8
            new Rune(0x258E),     // ▎ 2/8
            new Rune(0x258D),     // ▍ 3/8
            new Rune(0x258C),     // ▌ 4/8
            new Rune(0x258B),     // ▋ 5/8
            new Rune(0x258A),     // ▊ 6/8
            new Rune(0x2589),     // ▉ 7/8
            new Rune(0x2588),     // █ 8/8
        ];

        private static void RenderSolid(CellBuffer buffer, int y, int x, int width, double value, Rune fill, Rune track, Style fillStyle, Style trackStyle)
        {
            var filled = (int)Math.Round(width * value);
            filled = Math.Clamp(filled, 0, width);

            for (var i = 0; i < width; i++)
            {
                buffer.SetCell(x + i, y, i < filled ? fill : track, i < filled ? fillStyle : trackStyle);
            }
        }

        private static void RenderSegmented(CellBuffer buffer, int y, int x, int width, double value, Rune fullFill, Rune track, Style fillStyle, Style trackStyle)
        {
            value = Math.Clamp(value, 0.0, 1.0);
            var scaled = value * width;
            var whole = (int)Math.Floor(scaled);
            var frac = scaled - whole;

            whole = Math.Clamp(whole, 0, width);
            var remainder = (int)Math.Round(frac * 8.0);
            remainder = Math.Clamp(remainder, 0, 8);

            for (var i = 0; i < width; i++)
            {
                buffer.SetCell(x + i, y, track, trackStyle);
            }

            for (var i = 0; i < whole; i++)
            {
                buffer.SetCell(x + i, y, fullFill, fillStyle);
            }

            if (whole < width && remainder > 0)
            {
                buffer.SetCell(x + whole, y, SegmentGlyphs[remainder], fillStyle);
            }
        }
    }
}

/// <summary>
/// Defines where a chart title is placed relative to its main content.
/// </summary>
public enum ChartTitlePlacement
{
    /// <summary>
    /// The title is rendered above the chart content.
    /// </summary>
    Above,

    /// <summary>
    /// The title is rendered below the chart content.
    /// </summary>
    Below,
}
