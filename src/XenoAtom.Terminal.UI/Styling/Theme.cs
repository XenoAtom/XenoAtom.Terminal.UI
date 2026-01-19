// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

/// <summary>
/// Specifies how to interpret a color scheme brightness when creating a <see cref="Theme"/>.
/// </summary>
public enum ThemeSchemeBrightness
{
    /// <summary>
    /// Automatically detect brightness using scheme background/foreground (when available).
    /// </summary>
    Auto,
    /// <summary>
    /// Force a dark theme interpretation.
    /// </summary>
    Dark,
    /// <summary>
    /// Force a light theme interpretation.
    /// </summary>
    Light,
}

/// <summary>
/// Selects which base palette entry is used as the theme accent color.
/// </summary>
public enum ThemeAccentColor
{
    /// <summary>Uses the scheme blue color.</summary>
    Blue,
    /// <summary>Uses the scheme cyan color.</summary>
    Cyan,
    /// <summary>Uses the scheme green color.</summary>
    Green,
    /// <summary>Uses the scheme purple/magenta color.</summary>
    Purple,
    /// <summary>Uses the scheme red color.</summary>
    Red,
    /// <summary>Uses the scheme yellow color.</summary>
    Yellow,
    /// <summary>Uses the scheme white color.</summary>
    White,
    /// <summary>Uses the scheme black color.</summary>
    Black,
}

/// <summary>
/// Defines a theme used to style the UI (semantic colors, surfaces, and glyph sets).
/// </summary>
/// <remarks>
/// Themes are stored in the visual environment and are resolved via <see cref="Visual.GetTheme"/>.
/// A theme is also a style (<see cref="IStyle{T}"/>), so it can be overridden per subtree.
/// </remarks>
public sealed class Theme : IStyle<Theme>
{
    /// <summary>
    /// Gets the default theme (Root Loops dark).
    /// </summary>
    public static Theme Default { get; } = FromScheme(ColorScheme.RootLoopsDark);

    /// <summary>
    /// Gets the default light theme (Root Loops light).
    /// </summary>
    public static Theme DefaultLight { get; } = FromScheme(ColorScheme.RootLoopsLight);

    /// <summary>
    /// Gets a theme that maps to terminal defaults and the indexed 16-color palette.
    /// </summary>
    public static Theme Terminal { get; } = FromScheme(ColorScheme.Terminal);

    /// <summary>
    /// Gets the environment key for the theme style.
    /// </summary>
    public static StyleKey<Theme> Key { get; } = new("Theme", Default);

    /// <summary>
    /// Creates a <see cref="Theme"/> from a 16-color ANSI scheme.
    /// </summary>
    /// <param name="scheme">The color scheme.</param>
    /// <param name="brightness">How to interpret scheme brightness.</param>
    /// <param name="accent">Which scheme color to use as the accent.</param>
    /// <returns>The created theme.</returns>
    public static Theme FromScheme(ColorScheme scheme, ThemeSchemeBrightness brightness = ThemeSchemeBrightness.Auto, ThemeAccentColor accent = ThemeAccentColor.Blue)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        var hasRgbBase = scheme.Background is { } bg && scheme.Foreground is { } fg
            && bg.Kind is ColorKind.Rgb or ColorKind.RgbA
            && fg.Kind is ColorKind.Rgb or ColorKind.RgbA;

        var isLight = brightness switch
        {
            ThemeSchemeBrightness.Light => true,
            ThemeSchemeBrightness.Dark => false,
            _ => hasRgbBase && DetectLightScheme(scheme),
        };

        var accentColor = ResolveAccentColor(scheme, accent);

        // Terminal-like schemes (unknown background/foreground) should avoid RGB(A) overlays.
        if (!hasRgbBase)
        {
            var border = scheme.BrightBlack;
            var focusBorder = scheme.BrightBlue;
            var selection = scheme.BrightBlue;

            return new Theme
            {
                Foreground = scheme.Foreground,
                Background = scheme.Background,
                Surface = scheme.Black,
                SurfaceAlt = scheme.BrightBlack,
                PopupSurface = scheme.Black,
                ControlFill = scheme.BrightBlack,
                ControlFillHover = scheme.White,
                ControlFillPressed = scheme.Blue,
                InputFill = scheme.Black,
                InputFillFocused = scheme.Black,
                Border = border,
                FocusBorder = focusBorder,
                Accent = accentColor,
                Selection = selection,
                Disabled = scheme.BrightBlack,
                Primary = scheme.Blue,
                Success = scheme.Green,
                Warning = scheme.Yellow,
                Error = scheme.Red,
                Muted = scheme.BrightBlack,
                Lines = LineGlyphs.Single,
                ScrollBars = ScrollBarGlyphs.Default,
            };
        }

        // Fullscreen schemes: derive a coherent set of design-tokens using RGB(A) overlays.
        var background = ToRgb(scheme.Background!.Value);
        var foreground = ToRgb(scheme.Foreground!.Value);
        accentColor = ToRgb(accentColor);

        Color surface;
        Color controlFill;
        Color controlHover;
        Color controlPressed;
        Color inputFill;
        Color inputFillFocused;
        Color popupSurface;
        Color borderStroke;
        Color focusBorderStroke;
        Color selectionFill;
        Color muted;
        Color disabled;

        if (isLight)
        {
            // Light theme:
            // - background is slightly tinted (not pure white)
            // - surfaces are typically solid white
            // - controls use subtle dark overlays for hover/pressed
            surface = Color.Rgb(255, 255, 255);
            popupSurface = surface;

            controlFill = Color.RgbA(0, 0, 0, 0x0A);
            controlHover = Color.RgbA(0, 0, 0, 0x14);
            controlPressed = Color.RgbA(0, 0, 0, 0x1E);

            // Inputs: unfocused are slightly inset (darker); focused is lifted (solid surface).
            inputFill = Color.RgbA(0, 0, 0, 0x10);
            inputFillFocused = surface;

            borderStroke = Color.RgbA(0, 0, 0, 0x28);
            focusBorderStroke = accentColor;

            selectionFill = WithAlpha(accentColor, 0x30);
            muted = WithAlpha(foreground, 0xB0);
            disabled = WithAlpha(foreground, 0x70);
        }
        else
        {
            // Dark theme:
            // - surfaces and controls are lifted using low-alpha white overlays
            // - editable regions are inset using a low-alpha black overlay
            surface = Color.RgbA(255, 255, 255, 0x08);
            popupSurface = Blend(background, foreground, t: 0.12f);

            controlFill = Color.RgbA(255, 255, 255, 0x0F);
            controlHover = Color.RgbA(255, 255, 255, 0x17);
            controlPressed = Color.RgbA(255, 255, 255, 0x20);

            // Inputs: unfocused are slightly lifted (lighter); focused is inset (darker).
            inputFill = Color.RgbA(255, 255, 255, 0x0C);
            inputFillFocused = Color.RgbA(0, 0, 0, 0x28);

            borderStroke = Color.RgbA(255, 255, 255, 0x26);
            focusBorderStroke = accentColor;

            selectionFill = WithAlpha(accentColor, 0x3A);
            muted = WithAlpha(foreground, 0xC5);
            disabled = WithAlpha(foreground, 0x80);
        }

        return new Theme
        {
            Foreground = foreground,
            Background = background,
            Surface = surface,
            SurfaceAlt = controlFill,
            PopupSurface = popupSurface,
            ControlFill = controlFill,
            ControlFillHover = controlHover,
            ControlFillPressed = controlPressed,
            InputFill = inputFill,
            InputFillFocused = inputFillFocused,
            Border = borderStroke,
            FocusBorder = focusBorderStroke,
            Accent = accentColor,
            Selection = selectionFill,
            Disabled = disabled,
            Primary = scheme.Blue,
            Success = scheme.Green,
            Warning = scheme.Yellow,
            Error = scheme.Red,
            Muted = muted,
            Lines = LineGlyphs.Single,
            ScrollBars = ScrollBarGlyphs.Default,
        };
    }

    /// <summary>
    /// Gets the default foreground color, or <c>null</c> for terminal default.
    /// </summary>
    public Color? Foreground { get; init; }

    /// <summary>
    /// Gets the default background color, or <c>null</c> for terminal default.
    /// </summary>
    public Color? Background { get; init; }

    /// <summary>
    /// Gets the primary surface background color used for large areas, or <c>null</c>.
    /// </summary>
    public Color? Surface { get; init; }

    /// <summary>
    /// Gets an alternate surface background color, or <c>null</c>.
    /// </summary>
    public Color? SurfaceAlt { get; init; }

    /// <summary>
    /// Gets the background color used for popup surfaces (menus, dropdowns, dialogs), or <c>null</c>.
    /// </summary>
    public Color? PopupSurface { get; init; }

    /// <summary>
    /// Gets the default control fill used for interactive controls (buttons, list rows), or <c>null</c>.
    /// </summary>
    public Color? ControlFill { get; init; }

    /// <summary>
    /// Gets the control fill used for hovered interactive controls, or <c>null</c>.
    /// </summary>
    public Color? ControlFillHover { get; init; }

    /// <summary>
    /// Gets the control fill used for pressed interactive controls, or <c>null</c>.
    /// </summary>
    public Color? ControlFillPressed { get; init; }

    /// <summary>
    /// Gets the fill used for editable regions (text inputs/editors), or <c>null</c>.
    /// </summary>
    public Color? InputFill { get; init; }

    /// <summary>
    /// Gets the fill used for focused editable regions (text inputs/editors), or <c>null</c>.
    /// </summary>
    public Color? InputFillFocused { get; init; }

    /// <summary>
    /// Gets the default border color, or <c>null</c>.
    /// </summary>
    public Color? Border { get; init; }

    /// <summary>
    /// Gets the focused border color, or <c>null</c>.
    /// </summary>
    public Color? FocusBorder { get; init; }

    /// <summary>
    /// Gets the accent color, or <c>null</c>.
    /// </summary>
    public Color? Accent { get; init; }

    /// <summary>
    /// Gets the selection background color, or <c>null</c>.
    /// </summary>
    public Color? Selection { get; init; }

    /// <summary>
    /// Gets the disabled foreground color, or <c>null</c>.
    /// </summary>
    public Color? Disabled { get; init; }

    /// <summary>
    /// Gets the primary semantic color, or <c>null</c>.
    /// </summary>
    public Color? Primary { get; init; }

    /// <summary>
    /// Gets the success semantic color, or <c>null</c>.
    /// </summary>
    public Color? Success { get; init; }

    /// <summary>
    /// Gets the warning semantic color, or <c>null</c>.
    /// </summary>
    public Color? Warning { get; init; }

    /// <summary>
    /// Gets the error semantic color, or <c>null</c>.
    /// </summary>
    public Color? Error { get; init; }

    /// <summary>
    /// Gets a muted/secondary text color, or <c>null</c>.
    /// </summary>
    public Color? Muted { get; init; }

    /// <summary>
    /// Gets the glyph set used for line borders and separators.
    /// </summary>
    public LineGlyphs Lines { get; init; } = LineGlyphs.Single;

    /// <summary>
    /// Gets the glyph set used for scrollbars.
    /// </summary>
    public ScrollBarGlyphs ScrollBars { get; init; } = ScrollBarGlyphs.Default;

    /// <summary>
    /// Builds the base text style using theme foreground/background.
    /// </summary>
    public Style BaseTextStyle()
    {
        var style = Style.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (Background is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds a text style using only the theme foreground.
    /// </summary>
    public Style ForegroundTextStyle()
    {
        var style = Style.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style;
    }

    /// <summary>
    /// Builds a surface style using theme foreground and surface background.
    /// </summary>
    public Style SurfaceStyle()
    {
        var style = Style.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        if (Surface is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds a popup surface style using theme foreground and <see cref="PopupSurface"/>.
    /// </summary>
    public Style PopupSurfaceStyle()
    {
        var style = ForegroundTextStyle();
        if (PopupSurface is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds a control fill style using theme foreground and <see cref="ControlFill"/>.
    /// </summary>
    public Style ControlFillStyle()
    {
        var style = ForegroundTextStyle();
        if (ControlFill is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds an input fill style using theme foreground and <see cref="InputFill"/>.
    /// </summary>
    public Style InputFillStyle()
    {
        var style = ForegroundTextStyle();
        if (InputFill is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds an input fill style for either focused or unfocused editors.
    /// </summary>
    /// <param name="focused">Whether the input is focused.</param>
    public Style InputFillStyle(bool focused)
    {
        var style = ForegroundTextStyle();
        var fill = focused ? (InputFillFocused ?? InputFill) : InputFill;
        if (fill is { } bg)
        {
            style = style.WithBackground(bg);
        }
        return style;
    }

    /// <summary>
    /// Builds a muted text style using <see cref="Muted"/> on top of <see cref="BaseTextStyle"/>.
    /// </summary>
    public Style MutedTextStyle()
    {
        var style = BaseTextStyle();
        if (Muted is { } m)
        {
            style = style.WithForeground(m);
        }
        return style;
    }

    /// <summary>
    /// Builds a border style using either <see cref="Border"/> or <see cref="FocusBorder"/>.
    /// </summary>
    /// <param name="focused">Whether the border should use the focused color.</param>
    public Style BorderStyle(bool focused)
    {
        var color = focused ? FocusBorder : Border;
        var style = Style.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    /// <summary>
    /// Builds the selection style used for focused/selected items.
    /// </summary>
    public Style SelectionStyle()
    {
        var style = Style.None;
        if (Selection is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= TextStyle.Bold;
        return style;
    }

    private static bool DetectLightScheme(ColorScheme scheme)
    {
        if (scheme.Background is not { } bg || scheme.Foreground is not { } fg)
        {
            return false;
        }

        if (!TryGetRelativeLuminance(bg, out var bgLum) || !TryGetRelativeLuminance(fg, out var fgLum))
        {
            return false;
        }

        // We consider it a light scheme when the background is substantially lighter than the foreground.
        return bgLum > fgLum && bgLum >= 0.55f;
    }

    private static bool TryGetRgb(Color? color, out (byte r, byte g, byte b) rgb)
    {
        if (color is not { } c || c.Kind is not (ColorKind.Rgb or ColorKind.RgbA))
        {
            rgb = default;
            return false;
        }

        rgb = (c.R, c.G, c.B);
        return true;
    }

    private static bool TryGetRelativeLuminance(Color color, out float luma)
    {
        if (color.Kind is not (ColorKind.Rgb or ColorKind.RgbA))
        {
            luma = 0;
            return false;
        }

        // Relative luminance using linearized sRGB components.
        // https://www.w3.org/TR/WCAG21/#dfn-relative-luminance
        static float ToLinear(byte channel)
        {
            var v = channel / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        var r = ToLinear(color.R);
        var g = ToLinear(color.G);
        var b = ToLinear(color.B);
        luma = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
        return true;
    }

    private static Color Blend(Color a, Color b, float t)
    {
        // Caller ensures both colors are RGB.
        t = Math.Clamp(t, 0f, 1f);
        var r = (byte)Math.Clamp((int)MathF.Round(a.R + ((b.R - a.R) * t)), 0, 255);
        var g = (byte)Math.Clamp((int)MathF.Round(a.G + ((b.G - a.G) * t)), 0, 255);
        var bl = (byte)Math.Clamp((int)MathF.Round(a.B + ((b.B - a.B) * t)), 0, 255);
        return Color.Rgb(r, g, bl);
    }

    private static Color WithAlpha(Color rgb, byte alpha)
        => Color.RgbA(rgb.R, rgb.G, rgb.B, alpha);

    private static Color ToRgb(Color color)
    {
        return color.Kind switch
        {
            ColorKind.Rgb or ColorKind.RgbA => Color.Rgb(color.R, color.G, color.B),
            ColorKind.Basic16 => ToRgbFromAnsi16(color.Index),
            ColorKind.Indexed256 => ToRgbFromAnsi256(color.Index),
            _ => Color.Rgb(0, 0, 0),
        };
    }

    private static Color ToRgbFromAnsi16(byte index)
    {
        var (r, g, b) = AnsiPalettes.GetBasic16Rgb(index);
        return Color.Rgb(r, g, b);
    }

    private static Color ToRgbFromAnsi256(byte index)
    {
        var (r, g, b) = AnsiPalettes.GetXterm256Rgb(index);
        return Color.Rgb(r, g, b);
    }

    private static Color ResolveAccentColor(ColorScheme scheme, ThemeAccentColor accent)
    {
        return accent switch
        {
            ThemeAccentColor.Blue => scheme.Blue,
            ThemeAccentColor.Cyan => scheme.Cyan,
            ThemeAccentColor.Green => scheme.Green,
            ThemeAccentColor.Purple => scheme.Purple,
            ThemeAccentColor.Red => scheme.Red,
            ThemeAccentColor.Yellow => scheme.Yellow,
            ThemeAccentColor.White => scheme.White,
            ThemeAccentColor.Black => scheme.Black,
            _ => scheme.Purple,
        };
    }
}
