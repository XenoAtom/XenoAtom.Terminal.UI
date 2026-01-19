// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.CompilerServices;
using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI;

/// <summary>
/// Selects which color space is used when mixing and transforming colors.
/// </summary>
public enum ColorMixSpace
{
    /// <summary>Mix in gamma-encoded sRGB space.</summary>
    Srgb,
    /// <summary>Mix in linear RGB space.</summary>
    LinearRgb,
    /// <summary>Mix in OKLab space.</summary>
    Oklab,
    /// <summary>Mix in OKLCH space.</summary>
    Oklch,
}

public readonly partial record struct Color
{
    /// <summary>
    /// Returns <see langword="true"/> when this color is either <see cref="ColorKind.Rgb"/> or <see cref="ColorKind.RgbA"/>.
    /// </summary>
    public bool IsRgbLike => Kind is ColorKind.Rgb or ColorKind.RgbA;

    /// <summary>
    /// Returns the alpha component as a normalized value in the range [0..1].
    /// </summary>
    /// <remarks>
    /// For <see cref="ColorKind.Rgb"/>, this returns 1. For non-RGB colors, it returns 0.
    /// </remarks>
    public float Alpha
    {
        get
        {
            if (!IsRgbLike)
            {
                return 0f;
            }

            var a = Kind == ColorKind.Rgb ? byte.MaxValue : A;
            return a / 255f;
        }
    }

    /// <summary>
    /// Returns this color as a 24-bit RGB color when possible.
    /// </summary>
    /// <remarks>
    /// This resolves palette colors (<see cref="ColorKind.Basic16"/> and <see cref="ColorKind.Indexed256"/>) using xterm palettes.
    /// Alpha is discarded. If the color is <see cref="ColorKind.Default"/>, the returned value is <see cref="Default"/>.
    /// </remarks>
    public Color ToRgb()
    {
        return Kind switch
        {
            ColorKind.Rgb or ColorKind.RgbA => Rgb(R, G, B),
            ColorKind.Basic16 => FromBasic16ToRgb(Index),
            ColorKind.Indexed256 => FromIndexed256ToRgb(Index),
            _ => Default,
        };
    }

    /// <summary>
    /// Returns a copy of this color with the specified alpha component.
    /// </summary>
    /// <remarks>
    /// When the color is not RGB-like, it is first resolved to RGB via <see cref="ToRgb"/>. For <see cref="Default"/>,
    /// this method returns <see cref="Default"/>.
    /// </remarks>
    public Color WithAlpha(byte alpha)
    {
        if (Kind == ColorKind.Default)
        {
            return Default;
        }

        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        return RgbA(rgb.R, rgb.G, rgb.B, alpha);
    }

    /// <summary>
    /// Returns a copy of this color with the specified opacity in the range [0..1].
    /// </summary>
    public Color WithOpacity(float opacity)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        var a = (byte)Math.Clamp((int)(opacity * 255f + 0.5f), 0, 255);
        return WithAlpha(a);
    }

    /// <summary>
    /// Mixes this color with another color by a factor <paramref name="t"/> in the range [0..1].
    /// </summary>
    /// <remarks>
    /// Colors are mixed as RGB. Palette colors are resolved using <see cref="ToRgb"/>.
    /// </remarks>
    public Color Mix(Color other, float t, ColorMixSpace space = ColorMixSpace.Srgb)
        => Mix(this, other, t, space);

    /// <summary>
    /// Mixes two colors by a factor <paramref name="t"/> in the range [0..1].
    /// </summary>
    /// <remarks>
    /// Colors are mixed as RGB. Palette colors are resolved using <see cref="ToRgb"/>.
    /// </remarks>
    public static Color Mix(Color a, Color b, float t, ColorMixSpace space = ColorMixSpace.Srgb)
    {
        t = Math.Clamp(t, 0f, 1f);
        a = a.ToRgb();
        b = b.ToRgb();

        if (a.Kind == ColorKind.Default)
        {
            return b.Kind == ColorKind.Default ? Default : b;
        }

        if (b.Kind == ColorKind.Default)
        {
            return a;
        }

        return space switch
        {
            ColorMixSpace.LinearRgb => MixLinearRgb(a, b, t),
            ColorMixSpace.Oklab => MixOklab(a, b, t),
            ColorMixSpace.Oklch => MixOklch(a, b, t),
            _ => MixSrgb(a, b, t),
        };
    }

    /// <summary>
    /// Returns a lighter version of this color by adjusting OKLab lightness.
    /// </summary>
    /// <param name="amount">A delta in the range [0..1].</param>
    public Color Lighten(float amount) => AdjustLightness(Math.Abs(amount));

    /// <summary>
    /// Returns a darker version of this color by adjusting OKLab lightness.
    /// </summary>
    /// <param name="amount">A delta in the range [0..1].</param>
    public Color Darken(float amount) => AdjustLightness(-Math.Abs(amount));

    /// <summary>
    /// Adjusts OKLab lightness (L) by the specified delta.
    /// </summary>
    /// <param name="delta">A delta in the range [-1..1].</param>
    public Color AdjustLightness(float delta)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToOklab(out var l, out var a, out var b);
        l = Math.Clamp(l + delta, 0f, 1f);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromOklab(l, a, b, alpha);
    }

    /// <summary>
    /// Adjusts OKLCH chroma (C) by the specified delta.
    /// </summary>
    /// <param name="delta">A delta in the range [-1..1].</param>
    public Color AdjustChroma(float delta)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToOklch(out var l, out var c, out var h);
        c = Math.Max(0f, c + delta);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromOklch(l, c, h, alpha);
    }

    /// <summary>
    /// Returns a copy of this color with its OKLCH hue replaced (in degrees).
    /// </summary>
    public Color WithHue(float degrees)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToOklch(out var l, out var c, out _);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromOklch(l, c, degrees, alpha);
    }

    /// <summary>
    /// Returns a copy of this color with its OKLab lightness replaced.
    /// </summary>
    public Color WithLightness(float lightness)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToOklab(out _, out var a, out var b);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromOklab(Math.Clamp(lightness, 0f, 1f), a, b, alpha);
    }

    /// <summary>
    /// Adjusts HSL saturation by the specified delta.
    /// </summary>
    /// <param name="delta">A delta in the range [-1..1].</param>
    public Color AdjustSaturation(float delta)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToHsl(out var h, out var s, out var l);
        s = Math.Clamp(s + delta, 0f, 1f);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromHsl(h, s, l, alpha);
    }

    /// <summary>
    /// Returns a more saturated version of this color.
    /// </summary>
    /// <param name="amount">A delta in the range [0..1].</param>
    public Color Saturate(float amount) => AdjustSaturation(Math.Abs(amount));

    /// <summary>
    /// Returns a less saturated version of this color.
    /// </summary>
    /// <param name="amount">A delta in the range [0..1].</param>
    public Color Desaturate(float amount) => AdjustSaturation(-Math.Abs(amount));

    /// <summary>
    /// Returns a grayscale version of this color while preserving alpha.
    /// </summary>
    public Color Grayscale()
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        var y = rgb.GetRelativeLuminance();
        var ch = ToByte01(y);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return alpha == byte.MaxValue ? Rgb(ch, ch, ch) : RgbA(ch, ch, ch, alpha);
    }

    /// <summary>
    /// Converts this color to a hex string (<c>#RRGGBB</c> or <c>#RRGGBBAA</c>).
    /// </summary>
    /// <param name="includeAlpha">Whether to include alpha for <see cref="ColorKind.RgbA"/>.</param>
    public string ToHexString(bool includeAlpha = false)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return "#000000";
        }

        if (includeAlpha && Kind == ColorKind.RgbA)
        {
            return $"#{rgb.R:x2}{rgb.G:x2}{rgb.B:x2}{A:x2}";
        }

        return $"#{rgb.R:x2}{rgb.G:x2}{rgb.B:x2}";
    }

    /// <summary>
    /// Returns a copy of this color with its hue rotated by the specified amount (in degrees) in OKLCH.
    /// </summary>
    public Color RotateHue(float degrees)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return Default;
        }

        rgb.ToOklch(out var l, out var c, out var h);
        h = WrapDegrees(h + degrees);
        var alpha = Kind == ColorKind.RgbA ? A : byte.MaxValue;
        return FromOklch(l, c, h, alpha);
    }

    /// <summary>
    /// Converts this color to HSL components.
    /// </summary>
    /// <remarks>
    /// When the color is not RGB-like, it is first resolved to RGB via <see cref="ToRgb"/>. For <see cref="Default"/>,
    /// all outputs are set to zero.
    /// </remarks>
    public void ToHsl(out float hDegrees, out float s, out float l)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            hDegrees = 0;
            s = 0;
            l = 0;
            return;
        }

        var r = rgb.R / 255f;
        var g = rgb.G / 255f;
        var b = rgb.B / 255f;

        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;

        l = (max + min) * 0.5f;
        if (delta <= 0f)
        {
            hDegrees = 0f;
            s = 0f;
            return;
        }

        s = delta / (1f - MathF.Abs((2f * l) - 1f));

        float h;
        if (max == r)
        {
            h = ((g - b) / delta) % 6f;
        }
        else if (max == g)
        {
            h = ((b - r) / delta) + 2f;
        }
        else
        {
            h = ((r - g) / delta) + 4f;
        }

        hDegrees = WrapDegrees(h * 60f);
    }

    /// <summary>
    /// Creates an RGB color from HSL components.
    /// </summary>
    /// <param name="hDegrees">Hue in degrees. Values are wrapped to [0..360).</param>
    /// <param name="s">Saturation in the range [0..1].</param>
    /// <param name="l">Lightness in the range [0..1].</param>
    public static Color FromHsl(float hDegrees, float s, float l)
        => FromHsl(hDegrees, s, l, alpha: byte.MaxValue);

    /// <summary>
    /// Creates an RGBA color from HSL components.
    /// </summary>
    /// <param name="hDegrees">Hue in degrees. Values are wrapped to [0..360).</param>
    /// <param name="s">Saturation in the range [0..1].</param>
    /// <param name="l">Lightness in the range [0..1].</param>
    /// <param name="alpha">Alpha in the range [0..255].</param>
    public static Color FromHsl(float hDegrees, float s, float l, byte alpha)
    {
        s = Math.Clamp(s, 0f, 1f);
        l = Math.Clamp(l, 0f, 1f);
        hDegrees = WrapDegrees(hDegrees);

        var c = (1f - MathF.Abs((2f * l) - 1f)) * s;
        var x = c * (1f - MathF.Abs(((hDegrees / 60f) % 2f) - 1f));
        var m = l - (c * 0.5f);

        float r1, g1, b1;
        if (hDegrees < 60f) { r1 = c; g1 = x; b1 = 0; }
        else if (hDegrees < 120f) { r1 = x; g1 = c; b1 = 0; }
        else if (hDegrees < 180f) { r1 = 0; g1 = c; b1 = x; }
        else if (hDegrees < 240f) { r1 = 0; g1 = x; b1 = c; }
        else if (hDegrees < 300f) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        var r = ToByte01(r1 + m);
        var g = ToByte01(g1 + m);
        var b = ToByte01(b1 + m);
        return alpha == byte.MaxValue ? Rgb(r, g, b) : RgbA(r, g, b, alpha);
    }

    /// <summary>
    /// Converts this color to OKLab.
    /// </summary>
    public void ToOklab(out float l, out float a, out float b)
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            l = 0;
            a = 0;
            b = 0;
            return;
        }

        var lr = SrgbToLinear(rgb.R);
        var lg = SrgbToLinear(rgb.G);
        var lb = SrgbToLinear(rgb.B);

        var lmsL = (0.4122214708f * lr) + (0.5363325363f * lg) + (0.0514459929f * lb);
        var lmsM = (0.2119034982f * lr) + (0.6806995451f * lg) + (0.1073969566f * lb);
        var lmsS = (0.0883024619f * lr) + (0.2817188376f * lg) + (0.6299787005f * lb);

        var l_ = Cbrt(lmsL);
        var m_ = Cbrt(lmsM);
        var s_ = Cbrt(lmsS);

        l = (0.2104542553f * l_) + (0.7936177850f * m_) - (0.0040720468f * s_);
        a = (1.9779984951f * l_) - (2.4285922050f * m_) + (0.4505937099f * s_);
        b = (0.0259040371f * l_) + (0.7827717662f * m_) - (0.8086757660f * s_);
    }

    /// <summary>
    /// Creates an RGB color from OKLab components.
    /// </summary>
    public static Color FromOklab(float l, float a, float b)
        => FromOklab(l, a, b, alpha: byte.MaxValue);

    /// <summary>
    /// Creates an RGBA color from OKLab components.
    /// </summary>
    public static Color FromOklab(float l, float a, float b, byte alpha)
    {
        l = Math.Clamp(l, 0f, 1f);

        var l_ = l + (0.3963377774f * a) + (0.2158037573f * b);
        var m_ = l - (0.1055613458f * a) - (0.0638541728f * b);
        var s_ = l - (0.0894841775f * a) - (1.2914855480f * b);

        var l3 = l_ * l_ * l_;
        var m3 = m_ * m_ * m_;
        var s3 = s_ * s_ * s_;

        var lr = (4.0767416621f * l3) - (3.3077115913f * m3) + (0.2309699292f * s3);
        var lg = (-1.2684380046f * l3) + (2.6097574011f * m3) - (0.3413193965f * s3);
        var lb = (-0.0041960863f * l3) - (0.7034186147f * m3) + (1.7076147010f * s3);

        var r = LinearToSrgbByte(lr);
        var g = LinearToSrgbByte(lg);
        var bl = LinearToSrgbByte(lb);
        return alpha == byte.MaxValue ? Rgb(r, g, bl) : RgbA(r, g, bl, alpha);
    }

    /// <summary>
    /// Converts this color to OKLCH (OKLab in cylindrical form).
    /// </summary>
    public void ToOklch(out float l, out float c, out float hDegrees)
    {
        ToOklab(out l, out var a, out var b);
        c = MathF.Sqrt((a * a) + (b * b));
        hDegrees = WrapDegrees(MathF.Atan2(b, a) * (180f / MathF.PI));
    }

    /// <summary>
    /// Creates an RGB color from OKLCH components.
    /// </summary>
    public static Color FromOklch(float l, float c, float hDegrees)
        => FromOklch(l, c, hDegrees, alpha: byte.MaxValue);

    /// <summary>
    /// Creates an RGBA color from OKLCH components.
    /// </summary>
    public static Color FromOklch(float l, float c, float hDegrees, byte alpha)
    {
        l = Math.Clamp(l, 0f, 1f);
        c = Math.Max(0f, c);
        hDegrees = WrapDegrees(hDegrees);

        var hr = hDegrees * (MathF.PI / 180f);
        var a = c * MathF.Cos(hr);
        var b = c * MathF.Sin(hr);
        return FromOklab(l, a, b, alpha);
    }

    /// <summary>
    /// Computes the W3C relative luminance of this color.
    /// </summary>
    /// <remarks>
    /// Only RGB-like colors participate. Palette colors are resolved via <see cref="ToRgb"/>.
    /// For <see cref="Default"/>, the luminance is 0.
    /// </remarks>
    public float GetRelativeLuminance()
    {
        var rgb = ToRgb();
        if (rgb.Kind == ColorKind.Default)
        {
            return 0f;
        }

        // Relative luminance using linearized sRGB components.
        // https://www.w3.org/TR/WCAG21/#dfn-relative-luminance
        var r = SrgbToLinear(rgb.R);
        var g = SrgbToLinear(rgb.G);
        var b = SrgbToLinear(rgb.B);
        return (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
    }

    private static Color MixSrgb(Color a, Color b, float t)
    {
        var r = (byte)Math.Clamp((int)MathF.Round(a.R + ((b.R - a.R) * t)), 0, 255);
        var g = (byte)Math.Clamp((int)MathF.Round(a.G + ((b.G - a.G) * t)), 0, 255);
        var bl = (byte)Math.Clamp((int)MathF.Round(a.B + ((b.B - a.B) * t)), 0, 255);
        return Rgb(r, g, bl);
    }

    private static Color MixLinearRgb(Color a, Color b, float t)
    {
        var ar = SrgbToLinear(a.R);
        var ag = SrgbToLinear(a.G);
        var ab = SrgbToLinear(a.B);
        var br = SrgbToLinear(b.R);
        var bg = SrgbToLinear(b.G);
        var bb = SrgbToLinear(b.B);

        var rr = (ar * (1f - t)) + (br * t);
        var rg = (ag * (1f - t)) + (bg * t);
        var rb = (ab * (1f - t)) + (bb * t);

        return Rgb(LinearToSrgbByte(rr), LinearToSrgbByte(rg), LinearToSrgbByte(rb));
    }

    private static Color MixOklab(Color a, Color b, float t)
    {
        a.ToOklab(out var al, out var aa, out var ab);
        b.ToOklab(out var bl, out var ba, out var bb);

        var l = (al * (1f - t)) + (bl * t);
        var x = (aa * (1f - t)) + (ba * t);
        var y = (ab * (1f - t)) + (bb * t);
        return FromOklab(l, x, y);
    }

    private static Color MixOklch(Color a, Color b, float t)
    {
        a.ToOklch(out var al, out var ac, out var ah);
        b.ToOklch(out var bl, out var bc, out var bh);

        // Hue interpolation across the shortest arc.
        var dh = bh - ah;
        if (dh > 180f) dh -= 360f;
        if (dh < -180f) dh += 360f;

        var l = (al * (1f - t)) + (bl * t);
        var c = (ac * (1f - t)) + (bc * t);
        var h = WrapDegrees(ah + (dh * t));
        return FromOklch(l, c, h);
    }

    private static Color FromBasic16ToRgb(byte index)
    {
        var (r, g, b) = AnsiPalettes.GetBasic16Rgb(index);
        return Rgb(r, g, b);
    }

    private static Color FromIndexed256ToRgb(byte index)
    {
        var (r, g, b) = AnsiPalettes.GetXterm256Rgb(index);
        return Rgb(r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ToByte01(float v)
        => (byte)Math.Clamp((int)(v * 255f + 0.5f), 0, 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SrgbToLinear(byte channel)
    {
        var v = channel / 255f;
        return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte LinearToSrgbByte(float linear)
    {
        if (linear <= 0f) return 0;
        if (linear >= 1f) return 255;
        var v = linear <= 0.0031308f ? 12.92f * linear : (1.055f * MathF.Pow(linear, 1f / 2.4f)) - 0.055f;
        return ToByte01(v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Cbrt(float v)
        => v <= 0f ? -MathF.Pow(-v, 1f / 3f) : MathF.Pow(v, 1f / 3f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float WrapDegrees(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
        {
            degrees += 360f;
        }
        return degrees;
    }
}
