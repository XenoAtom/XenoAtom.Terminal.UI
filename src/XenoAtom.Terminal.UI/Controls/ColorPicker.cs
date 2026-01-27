// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Provides a control for selecting a <see cref="Color"/> (including RGBA).
/// </summary>
/// <remarks>
/// <para>
/// The color picker supports channel sliders (RGB + optional Alpha), hex editing, and an optional palette grid.
/// </para>
/// <para>
/// When <see cref="AllowAlpha"/> is enabled, the picker uses <see cref="ColorKind.RgbA"/> values so that alpha can be
/// previewed using the library's internal blending pipeline.
/// </para>
/// </remarks>
public sealed partial class ColorPicker : Visual
{
    private readonly Visual _root;
    private readonly SwatchVisual _swatch;
    private readonly Slider<int> _sliderR;
    private readonly Slider<int> _sliderG;
    private readonly Slider<int> _sliderB;
    private readonly Slider<int> _sliderA;
    private readonly TextBox _hexBox;
    private readonly ValidationPresenter _hexValidation;
    private readonly ComputedVisual _paletteHost;

    private bool _updatingFromValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorPicker"/> class.
    /// </summary>
    public ColorPicker()
    {
        Focusable = false;
        HorizontalAlignment = Align.Stretch;
        VerticalAlignment = Align.Start;

        _swatch = new SwatchVisual(this);

        _sliderR = CreateChannelSlider();
        _sliderG = CreateChannelSlider();
        _sliderB = CreateChannelSlider();
        _sliderA = CreateChannelSlider();

        _hexBox = new TextBox().Placeholder("#RRGGBB / #RRGGBBAA").MinWidth(11).MaxWidth(11);
        _hexValidation = new ValidationPresenter { Content = _hexBox };

        _paletteHost = new ComputedVisual(BuildPalette) { IsVisible = true };

        var channels = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Fixed(2) },
                new ColumnDefinition { Width = GridLength.Star(1) },
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
            }
        };

        channels
            .Cell("R", 0, 0)
            .Cell(_sliderR, 0, 1)
            .Cell("G", 1, 0)
            .Cell(_sliderG, 1, 1)
            .Cell("B", 2, 0)
            .Cell(_sliderB, 2, 1)
            .Cell("A", 3, 0)
            .Cell(_sliderA, 3, 1);

        var top = new HStack(
                _swatch,
                new VStack(
                        channels,
                        new HStack("Hex:", _hexValidation).Spacing(1)
                    )
                    .Spacing(1)
                    .HorizontalAlignment(Align.Stretch)
            )
            .Spacing(2)
            .HorizontalAlignment(Align.Stretch);

        _root = new VStack(top, _paletteHost).Spacing(1).HorizontalAlignment(Align.Stretch);
        AttachChild(_root);

        this.AllowAlpha(true);
        this.ShowPalette(true);

        // Pull initial state from Value (default).
        UpdateControlsFromValue();

        // Keep Value in sync with input controls.
        _sliderR.ValueChanged((_, _) => UpdateValueFromSliders());
        _sliderG.ValueChanged((_, _) => UpdateValueFromSliders());
        _sliderB.ValueChanged((_, _) => UpdateValueFromSliders());
        _sliderA.ValueChanged((_, _) => UpdateValueFromSliders());
        _hexBox.TextDocument.Changed += (_, _) => UpdateValueFromHex();
    }

    /// <summary>
    /// Gets or sets the selected color value.
    /// </summary>
    [Bindable]
    public partial Color Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the alpha channel can be edited.
    /// </summary>
    [Bindable]
    public partial bool AllowAlpha { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display the palette grid.
    /// </summary>
    [Bindable]
    public partial bool ShowPalette { get; set; }

    /// <summary>
    /// Gets or sets an optional palette override.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/> and <see cref="ShowPalette"/> is enabled, the picker derives its palette from the
    /// current <see cref="Theme.Scheme"/>.
    /// </remarks>
    [Bindable]
    public partial IReadOnlyList<Color?>? Palette { get; set; }

    partial void OnAllowAlphaChanged(bool value)
    {
        _sliderA.IsVisible = value;
        UpdateHexPlaceholder();
        UpdateHexTextFromValueIfAllowed();
    }

    partial void OnShowPaletteChanged(bool value)
    {
        _paletteHost.IsVisible = value;
    }

    partial void OnValueChanged(Color value)
    {
        _ = value;
        UpdateControlsFromValue();
    }

    partial void OnPaletteChanged(IReadOnlyList<Color?>? value)
    {
        _ = value;
    }

    /// <inheritdoc />
    protected override int ChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetChild(int index)
        => index == 0 ? _root : throw new ArgumentOutOfRangeException(nameof(index));

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => _root.Measure(constraints);

    /// <inheritdoc />
    protected override void ArrangeCore(in Rectangle finalRect)
    {
        Bounds = finalRect;
        _root.Arrange(finalRect);
    }

    private static Slider<int> CreateChannelSlider()
        => new Slider<int>()
            .Minimum(0)
            .Maximum(255)
            .Step(1)
            .LargeStep(16)
            .ShowValueLabel(true)
            .HorizontalAlignment(Align.Stretch);

    private void UpdateControlsFromValue()
    {
        if (_updatingFromValue)
        {
            return;
        }

        _updatingFromValue = true;
        try
        {
            var rgb = Value.ToRgb();
            var r = rgb.Kind == ColorKind.Default ? 0 : rgb.R;
            var g = rgb.Kind == ColorKind.Default ? 0 : rgb.G;
            var b = rgb.Kind == ColorKind.Default ? 0 : rgb.B;
            var a = AllowAlpha ? (Value.IsRgbLike ? Value.A : (byte)255) : (byte)255;

            _sliderR.Value = r;
            _sliderG.Value = g;
            _sliderB.Value = b;
            _sliderA.Value = a;

            UpdateHexPlaceholder();
            UpdateHexTextFromValueIfAllowed();
        }
        finally
        {
            _updatingFromValue = false;
        }
    }

    private void UpdateHexPlaceholder()
    {
        _hexBox.Placeholder(AllowAlpha ? "#RRGGBB / #RRGGBBAA" : "#RRGGBB");
    }

    private void UpdateHexTextFromValueIfAllowed()
    {
        // Avoid overwriting while the user is editing the hex field.
        if (IsHexFocused())
        {
            return;
        }

        var style = GetStyle<ColorPickerStyle>();
        var formatted = FormatHex(Value, includeAlpha: AllowAlpha, uppercase: style.UppercaseHex);
        if (!string.Equals(_hexBox.Text, formatted, StringComparison.Ordinal))
        {
            _hexBox.Text = formatted;
        }
    }

    private void UpdateValueFromHex()
    {
        if (_updatingFromValue)
        {
            return;
        }

        var hexText = _hexBox.Text;
        var parsedFromHex = TryParseHex(hexText, allowAlpha: AllowAlpha, out var parsedColor, out var hexError);

        if (parsedFromHex)
        {
            var normalized = NormalizeForMode(parsedColor);
            if (!Equals(Value, normalized))
            {
                Value = normalized;
            }

            _hexValidation.Message = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(hexText))
        {
            _hexValidation.Message = new ValidationMessage(ValidationSeverity.Error, hexError ?? "Invalid color");
        }
        else
        {
            _hexValidation.Message = null;
        }
    }

    private void UpdateValueFromSliders()
    {
        if (_updatingFromValue)
        {
            return;
        }

        var r = (byte)Math.Clamp(_sliderR.Value, 0, 255);
        var g = (byte)Math.Clamp(_sliderG.Value, 0, 255);
        var b = (byte)Math.Clamp(_sliderB.Value, 0, 255);
        var a = (byte)Math.Clamp(_sliderA.Value, 0, 255);

        var channelColor = AllowAlpha ? Color.RgbA(r, g, b, a) : Color.Rgb(r, g, b);
        var normalizedChannel = NormalizeForMode(channelColor);

        if (!Equals(Value, normalizedChannel))
        {
            Value = normalizedChannel;
        }
    }

    private Color NormalizeForMode(Color color)
    {
        if (color.Kind == ColorKind.Default)
        {
            return Color.Default;
        }

        var rgb = color.ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Color.Default;
        }

        if (!AllowAlpha)
        {
            return Color.Rgb(rgb.R, rgb.G, rgb.B);
        }

        var a = color.IsRgbLike ? color.A : (byte)255;
        return Color.RgbA(rgb.R, rgb.G, rgb.B, a);
    }

    private Visual? BuildPalette()
    {
        if (!ShowPalette)
        {
            return null;
        }

        var style = GetStyle<ColorPickerStyle>();
        var palette = Palette ?? DeriveDefaultPalette(GetTheme());
        if (palette.Count == 0)
        {
            return null;
        }

        var columns = Math.Max(1, style.PaletteColumns);
        var gap = Math.Max(0, style.PaletteGap);

        var rows = new List<Visual>();
        for (var i = 0; i < palette.Count; i += columns)
        {
            var row = new HStack().Spacing(gap);
            for (var j = 0; j < columns && i + j < palette.Count; j++)
            {
                var c = palette[i + j];
                row.Add(new PaletteSwatch(this, c));
            }
            rows.Add(row);
        }

        return new VStack(rows.ToArray()).Spacing(gap);
    }

    private static IReadOnlyList<Color?> DeriveDefaultPalette(Theme theme)
    {
        var scheme = theme.Scheme;
        if (scheme is null)
        {
            return Array.Empty<Color?>();
        }

        return new Color?[]
        {
            scheme.Background, scheme.Black, scheme.Red, scheme.Green, scheme.Yellow, scheme.Blue, scheme.Purple, scheme.Cyan, scheme.White,
            scheme.Foreground, scheme.BrightBlack, scheme.BrightRed, scheme.BrightGreen, scheme.BrightYellow, scheme.BrightBlue, scheme.BrightPurple, scheme.BrightCyan, scheme.BrightWhite,
        };
    }

    private static bool TryParseHex(string? text, bool allowAlpha, out Color color, out string? error)
    {
        color = default;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a color (e.g. #RRGGBB)";
            return false;
        }

        var span = text.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#')
        {
            span = span[1..];
        }

        if (span.Length != 6 && span.Length != 8)
        {
            error = allowAlpha ? "Expected #RRGGBB or #RRGGBBAA" : "Expected #RRGGBB";
            return false;
        }

        if (span.Length == 8 && !allowAlpha)
        {
            error = "Alpha is disabled";
            return false;
        }

        if (!uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            error = "Expected hexadecimal digits";
            return false;
        }

        if (span.Length == 6)
        {
            var r = (byte)((parsed >> 16) & 0xFF);
            var g = (byte)((parsed >> 8) & 0xFF);
            var b = (byte)(parsed & 0xFF);
            color = allowAlpha ? Color.RgbA(r, g, b, 255) : Color.Rgb(r, g, b);
            return true;
        }
        else
        {
            // RRGGBBAA
            var r = (byte)((parsed >> 24) & 0xFF);
            var g = (byte)((parsed >> 16) & 0xFF);
            var b = (byte)((parsed >> 8) & 0xFF);
            var a = (byte)(parsed & 0xFF);
            color = Color.RgbA(r, g, b, a);
            return true;
        }
    }

    private static string FormatHex(Color color, bool includeAlpha, bool uppercase)
    {
        if (color.Kind == ColorKind.Default)
        {
            return "#000000";
        }

        var rgb = color.ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return "#000000";
        }

        var sb = new StringBuilder(9);
        sb.Append('#');

        var format = uppercase ? "X2" : "x2";
        sb.Append(rgb.R.ToString(format, CultureInfo.InvariantCulture));
        sb.Append(rgb.G.ToString(format, CultureInfo.InvariantCulture));
        sb.Append(rgb.B.ToString(format, CultureInfo.InvariantCulture));

        if (includeAlpha)
        {
            var a = color.IsRgbLike ? color.A : (byte)255;
            sb.Append(a.ToString(format, CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private bool IsHexFocused()
    {
        var app = App;
        if (app is null)
        {
            return false;
        }

        for (var v = app.FocusedElement; v is not null; v = v.Parent)
        {
            if (ReferenceEquals(v, _hexBox))
            {
                return true;
            }
        }

        return false;
    }

    internal sealed class PaletteSwatch : Visual
    {
        private readonly ColorPicker _owner;
        private readonly Color? _color;

        public PaletteSwatch(ColorPicker owner, Color? color)
        {
            _owner = owner;
            _color = color;
            Focusable = false;
            HorizontalAlignment = Align.Start;
            VerticalAlignment = Align.Start;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var style = _owner.GetStyle<ColorPickerStyle>();
            var width = Math.Min(constraints.MaxWidth, Math.Max(0, style.PaletteSwatchWidth));
            var height = Math.Min(constraints.MaxHeight, Math.Max(0, style.PaletteSwatchHeight));
            return SizeHints.Fixed(new Size(width, height));
        }

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var theme = _owner.GetTheme();
            var style = _owner.GetStyle<ColorPickerStyle>();

            var fillColor = _color;
            var fillStyle = theme.ForegroundTextStyle();
            if (fillColor is { } c)
            {
                fillStyle = fillStyle.WithBackground(c);
            }

            for (var y = 0; y < rect.Height; y++)
            {
                for (var x = 0; x < rect.Width; x++)
                {
                    buffer.SetCell(rect.X + x, rect.Y + y, new Rune(' '), fillStyle);
                }
            }

            var selected = fillColor is { } swatch && _owner.Value.ToRgb().Equals(swatch.ToRgb());
            if (selected && rect.Width >= 2 && rect.Height >= 2)
            {
                var borderStyle = style.ResolvePaletteSelectionStyle(theme);
                var g = style.PaletteSelectionGlyphs;
                DrawBorder(buffer, rect, g, borderStyle);
            }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != TerminalMouseButton.Left)
            {
                return;
            }

            _owner.Value = _color ?? Color.Default;
            e.Handled = true;
        }
    }

    private sealed class SwatchVisual : Visual
    {
        private readonly ColorPicker _owner;

        public SwatchVisual(ColorPicker owner)
        {
            _owner = owner;
            Focusable = false;
            HorizontalAlignment = Align.Start;
            VerticalAlignment = Align.Start;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        {
            var style = _owner.GetStyle<ColorPickerStyle>();
            var width = Math.Min(constraints.MaxWidth, Math.Max(0, style.SwatchWidth));
            var height = Math.Min(constraints.MaxHeight, Math.Max(0, style.SwatchHeight));
            return SizeHints.Fixed(new Size(width, height));
        }

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            var style = _owner.GetStyle<ColorPickerStyle>();
            var theme = _owner.GetTheme();
            var borderStyle = style.ResolveSwatchBorderStyle(theme);
            var glyphs = style.SwatchGlyphs;

            DrawBorder(buffer, rect, glyphs, borderStyle);

            if (rect.Width < 3 || rect.Height < 3)
            {
                return;
            }

            var inner = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            var (light, dark) = style.ResolveCheckerboardColors(theme);
            var lightStyle = theme.ForegroundTextStyle().WithBackground(light);
            var darkStyle = theme.ForegroundTextStyle().WithBackground(dark);

            for (var y = 0; y < inner.Height; y++)
            {
                for (var x = 0; x < inner.Width; x++)
                {
                    var checker = ((x + y) & 1) == 0 ? lightStyle : darkStyle;
                    buffer.SetCell(inner.X + x, inner.Y + y, new Rune(' '), checker);
                }
            }

            var value = _owner.Value;
            if (value.Kind == ColorKind.Default)
            {
                return;
            }

            var overlayStyle = theme.ForegroundTextStyle().WithBackground(value);
            for (var y = 0; y < inner.Height; y++)
            {
                for (var x = 0; x < inner.Width; x++)
                {
                    buffer.SetCell(inner.X + x, inner.Y + y, new Rune(' '), overlayStyle);
                }
            }
        }
    }

    private static void DrawBorder(CellBuffer buffer, Rectangle rect, LineGlyphs glyphs, Style style)
    {
        var left = rect.X;
        var top = rect.Y;
        var right = rect.X + rect.Width - 1;
        var bottom = rect.Y + rect.Height - 1;

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        if (rect.Width == 1 && rect.Height == 1)
        {
            buffer.SetCell(left, top, glyphs.TopLeft, style);
            return;
        }

        if (rect.Width == 1)
        {
            buffer.SetCell(left, top, glyphs.TopLeft, style);
            for (var y = top + 1; y < bottom; y++)
            {
                buffer.SetCell(left, y, glyphs.Vertical, style);
            }
            buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
            return;
        }

        if (rect.Height == 1)
        {
            buffer.SetCell(left, top, glyphs.TopLeft, style);
            for (var x = left + 1; x < right; x++)
            {
                buffer.SetCell(x, top, glyphs.Horizontal, style);
            }
            buffer.SetCell(right, top, glyphs.TopRight, style);
            return;
        }

        buffer.SetCell(left, top, glyphs.TopLeft, style);
        buffer.SetCell(right, top, glyphs.TopRight, style);
        buffer.SetCell(left, bottom, glyphs.BottomLeft, style);
        buffer.SetCell(right, bottom, glyphs.BottomRight, style);

        for (var x = left + 1; x < right; x++)
        {
            buffer.SetCell(x, top, glyphs.Horizontal, style);
            buffer.SetCell(x, bottom, glyphs.Horizontal, style);
        }

        for (var y = top + 1; y < bottom; y++)
        {
            buffer.SetCell(left, y, glyphs.Vertical, style);
            buffer.SetCell(right, y, glyphs.Vertical, style);
        }
    }
}
