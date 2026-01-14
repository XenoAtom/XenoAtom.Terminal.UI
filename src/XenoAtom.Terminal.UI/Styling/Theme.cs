// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

public enum ThemeSchemeBrightness
{
    Auto,
    Dark,
    Light,
}

public sealed class Theme : IStyle<Theme>
{
    public static Theme Default { get; } = FromScheme(AnsiColorScheme.RootLoopsDark);

    public static Theme DefaultLight { get; } = FromScheme(AnsiColorScheme.RootLoopsLight);

    public static Theme Terminal { get; } = FromScheme(AnsiColorScheme.Terminal);

    public static StyleKey<Theme> Key { get; } = new("Theme", Default);

    public static Theme FromScheme(AnsiColorScheme scheme, ThemeSchemeBrightness brightness = ThemeSchemeBrightness.Auto)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        var isLight = brightness switch
        {
            ThemeSchemeBrightness.Light => true,
            ThemeSchemeBrightness.Dark => false,
            _ => DetectLightScheme(scheme),
        };

        AnsiColor? surface = scheme.Black;
        AnsiColor? surfaceAlt = scheme.BrightBlack;
        AnsiColor? disabled = scheme.BrightBlack;
        AnsiColor? muted = scheme.White;
        AnsiColor? border = scheme.CursorColor;
        AnsiColor? focusBorder = scheme.BrightWhite;

        if (isLight && TryGetRgb(scheme.Background, out _) && TryGetRgb(scheme.Foreground, out _))
        {
            // For light schemes, derive neutrals close to the background so the overall UI keeps a "light" feel.
            // This avoids using palette entries like Black/BrightBlack as large surfaces, which can be too saturated.
            surface = Blend(scheme.Background!.Value, scheme.Foreground!.Value, t: 0.04f);
            surfaceAlt = Blend(scheme.Background!.Value, scheme.Foreground!.Value, t: 0.08f);
            disabled = Blend(scheme.Background!.Value, scheme.Foreground!.Value, t: 0.35f);
            muted = Blend(scheme.Foreground!.Value, scheme.Background!.Value, t: 0.55f);
            border = Blend(scheme.Background!.Value, scheme.Foreground!.Value, t: 0.15f);
            focusBorder = scheme.CursorColor;
        }
        else if (isLight)
        {
            focusBorder = scheme.CursorColor;
        }

        return new Theme
        {
            Foreground = scheme.Foreground,
            Background = scheme.Background,
            Surface = surface,
            SurfaceAlt = surfaceAlt,
            Border = border,
            FocusBorder = focusBorder,
            Accent = scheme.Purple,
            Selection = scheme.SelectionBackground,
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

    public AnsiColor? Foreground { get; init; }

    public AnsiColor? Background { get; init; }

    public AnsiColor? Surface { get; init; }

    public AnsiColor? SurfaceAlt { get; init; }

    public AnsiColor? Border { get; init; }

    public AnsiColor? FocusBorder { get; init; }

    public AnsiColor? Accent { get; init; }

    public AnsiColor? Selection { get; init; }

    public AnsiColor? Disabled { get; init; }

    public AnsiColor? Primary { get; init; }

    public AnsiColor? Success { get; init; }

    public AnsiColor? Warning { get; init; }

    public AnsiColor? Error { get; init; }

    public AnsiColor? Muted { get; init; }

    public LineGlyphs Lines { get; init; } = LineGlyphs.Single;

    public ScrollBarGlyphs ScrollBars { get; init; } = ScrollBarGlyphs.Default;

    public CellStyle BaseTextStyle()
    {
        var style = CellStyle.None;
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

    public CellStyle ForegroundTextStyle()
    {
        var style = CellStyle.None;
        if (Foreground is { } fg)
        {
            style = style.WithForeground(fg);
        }
        return style;
    }

    public CellStyle SurfaceStyle()
    {
        var style = CellStyle.None;
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

    public CellStyle MutedTextStyle()
    {
        var style = BaseTextStyle();
        if (Muted is { } m)
        {
            style = style.WithForeground(m);
        }
        return style;
    }

    public CellStyle BorderStyle(bool focused)
    {
        var color = focused ? FocusBorder : Border;
        var style = CellStyle.None;
        if (color is { } c)
        {
            style = style.WithForeground(c);
        }
        return style;
    }

    public CellStyle SelectionStyle()
    {
        var style = CellStyle.None;
        if (Selection is { } c)
        {
            style = style.WithBackground(c);
        }
        style |= TextStyle.Bold;
        return style;
    }

    private static bool DetectLightScheme(AnsiColorScheme scheme)
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

    private static bool TryGetRgb(AnsiColor? color, out (byte r, byte g, byte b) rgb)
    {
        if (color is not { } c || c.Kind != AnsiColorKind.Rgb)
        {
            rgb = default;
            return false;
        }

        rgb = (c.R, c.G, c.B);
        return true;
    }

    private static bool TryGetRelativeLuminance(AnsiColor color, out float luma)
    {
        if (color.Kind != AnsiColorKind.Rgb)
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

    private static AnsiColor Blend(AnsiColor a, AnsiColor b, float t)
    {
        // Caller ensures both colors are RGB.
        t = Math.Clamp(t, 0f, 1f);
        var r = (byte)Math.Clamp((int)MathF.Round(a.R + ((b.R - a.R) * t)), 0, 255);
        var g = (byte)Math.Clamp((int)MathF.Round(a.G + ((b.G - a.G) * t)), 0, 255);
        var bl = (byte)Math.Clamp((int)MathF.Round(a.B + ((b.B - a.B) * t)), 0, 255);
        return AnsiColor.Rgb(r, g, bl);
    }
}
