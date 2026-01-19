// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

// Original Root Loops color scheme algorithm by Hermann "Ham" Vocke
// https://github.com/hamvocke/root-loops/
//
// MIT License
//
// Copyright (c) 2024 Hermann "Ham" Vocke
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Okhsl to sRGB conversion algorithm by Dan Burzo
// https://github.com/Evercoder/culori
//
// MIT License
//
// Copyright (c) 2018 Dan Burzo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

public sealed partial record ColorScheme
{
    /// <summary>
    /// Generates a Root Loops <see cref="ColorScheme"/> from numeric parameters.
    /// </summary>
    /// <param name="sugar">Controls accent lightness (0..10).</param>
    /// <param name="colors">Controls accent saturation (0..10).</param>
    /// <param name="sogginess">Controls base saturation (0..10).</param>
    /// <param name="flavor">Controls hue shifting for accents.</param>
    /// <param name="fruit">Controls the base hue.</param>
    /// <param name="milk">Controls background/foreground lightness (typically 0..1).</param>
    /// <param name="name">Optional scheme name override.</param>
    /// <returns>The generated scheme.</returns>
    public static ColorScheme Generate(
        int sugar,
        int colors,
        int sogginess,
        RootLoopsFlavor flavor,
        RootLoopsFruit fruit,
        double milk,
        string? name = null)
        => Generate(new RootLoopsRecipe(sugar, colors, sogginess, flavor, fruit, milk), name);

    /// <summary>
    /// Generates a Root Loops <see cref="ColorScheme"/> from numeric parameters and a discrete milk amount.
    /// </summary>
    /// <param name="sugar">Controls accent lightness (0..10).</param>
    /// <param name="colors">Controls accent saturation (0..10).</param>
    /// <param name="sogginess">Controls base saturation (0..10).</param>
    /// <param name="flavor">Controls hue shifting for accents.</param>
    /// <param name="fruit">Controls the base hue.</param>
    /// <param name="milk">Controls background/foreground lightness using a discrete preset.</param>
    /// <param name="name">Optional scheme name override.</param>
    /// <returns>The generated scheme.</returns>
    public static ColorScheme Generate(
        int sugar,
        int colors,
        int sogginess,
        RootLoopsFlavor flavor,
        RootLoopsFruit fruit,
        RootLoopsMilkAmount milk,
        string? name = null)
        => Generate(new RootLoopsRecipe(sugar, colors, sogginess, flavor, fruit, (int)milk), name);

    /// <summary>
    /// Generates a Root Loops <see cref="ColorScheme"/> from a recipe.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="name">Optional scheme name override.</param>
    /// <returns>The generated scheme.</returns>
    public static ColorScheme Generate(RootLoopsRecipe recipe, string? name = null)
    {
        var sugar = Math.Clamp(recipe.Sugar, 0, 10);
        var colors = Math.Clamp(recipe.Colors, 0, 10);
        var sogginess = Math.Clamp(recipe.Sogginess, 0, 10);
        var milk = recipe.ClampedMilk;

        var baseHue = GetBaseHue(recipe.FruitMix);
        var baseSaturation = GetSaturation(sogginess);
        var accentSaturation = GetSaturation(colors);
        var accentHueShift = GetAccentHueShift(recipe.Flavor);

        var backgroundL = Logistics(lightness0: 4, lightness3: 96, milk);
        var blackL = Logistics(lightness0: 15, lightness3: 90, milk);
        var brightBlackL = Logistics(lightness0: 35, lightness3: 70, milk);
        var whiteL = Logistics(lightness0: 70, lightness3: 35, milk);
        var brightWhiteL = Logistics(lightness0: 90, lightness3: 15, milk);
        var foregroundL = Logistics(lightness0: 96, lightness3: 4, milk);

        var background = OkhslToColor(new Okhsl(baseHue, baseSaturation, backgroundL / 100.0));
        var black = OkhslToColor(new Okhsl(baseHue, baseSaturation, blackL / 100.0));
        var brightBlack = OkhslToColor(new Okhsl(baseHue, baseSaturation, brightBlackL / 100.0));
        var white = OkhslToColor(new Okhsl(baseHue, baseSaturation, whiteL / 100.0));
        var brightWhite = OkhslToColor(new Okhsl(baseHue, baseSaturation, brightWhiteL / 100.0));
        var foreground = OkhslToColor(new Okhsl(baseHue, baseSaturation, foregroundL / 100.0));

        var accentLightness = GetAccentLightness(sugar);
        var brightAccentLightness = GetAccentLightness(sugar + 1);

        var accents = new Okhsl[7];
        var brightAccents = new Okhsl[7];
        const int numberOfAccentColors = 6;
        const double hueStep = 60.0;
        for (var i = 0; i <= numberOfAccentColors; i++)
        {
            var hue = (hueStep * i) + accentHueShift;
            accents[i] = new Okhsl(hue, accentSaturation, accentLightness);
            brightAccents[i] = new Okhsl(hue, accentSaturation, brightAccentLightness);
        }

        // Accent mapping matches root-loops: red=0, yellow=1, green=2, cyan=3, blue=4, magenta=5.
        var red = OkhslToColor(accents[0]);
        var yellow = OkhslToColor(accents[1]);
        var green = OkhslToColor(accents[2]);
        var cyan = OkhslToColor(accents[3]);
        var blue = OkhslToColor(accents[4]);
        var purple = OkhslToColor(accents[5]);

        var brightRed = OkhslToColor(brightAccents[0]);
        var brightYellow = OkhslToColor(brightAccents[1]);
        var brightGreen = OkhslToColor(brightAccents[2]);
        var brightCyan = OkhslToColor(brightAccents[3]);
        var brightBlue = OkhslToColor(brightAccents[4]);
        var brightPurple = OkhslToColor(brightAccents[5]);

        return new ColorScheme
        {
            Name = name ?? "Root Loops",
            CursorColor = white,
            SelectionBackground = white,
            Background = background,
            Foreground = foreground,
            Black = black,
            Red = red,
            Green = green,
            Yellow = yellow,
            Blue = blue,
            Purple = purple,
            Cyan = cyan,
            White = white,
            BrightBlack = brightBlack,
            BrightRed = brightRed,
            BrightGreen = brightGreen,
            BrightYellow = brightYellow,
            BrightBlue = brightBlue,
            BrightPurple = brightPurple,
            BrightCyan = brightCyan,
            BrightWhite = brightWhite,
        };
    }

    private readonly record struct Okhsl(double H, double S, double L);

    private static double GetAccentHueShift(RootLoopsFlavor flavor) => flavor switch
    {
        RootLoopsFlavor.Fruity => 0,
        RootLoopsFlavor.Classic => 15,
        RootLoopsFlavor.Intense => 30,
        _ => 0,
    };

    private static double GetBaseHue(RootLoopsFruit fruit)
    {
        const double numberOfFruits = 12.0;
        return (double)fruit * (360.0 / numberOfFruits);
    }

    private static double GetBaseHue(double fruitMix)
    {
        fruitMix = Math.Clamp(fruitMix, 0.0, 1.0);
        return (double)fruitMix * 360.0;
    }

    private static double GetAccentLightness(int sugar)
    {
        // The upstream algorithm allows sugar in [0..11] (bright colors use sugar+1).
        var s = Math.Clamp(sugar, 0, 11);
        return Normalize(s, oldMin: 0, oldMax: 11, newMin: 0.05, newMax: 0.95);
    }

    private static double GetSaturation(int saturation)
        => Normalize(Math.Clamp(saturation, 0, 10), oldMin: 0, oldMax: 10, newMin: 0.0, newMax: 1.0);

    private static double Normalize(double number, double oldMin, double oldMax, double newMin, double newMax)
    {
        var oldRange = oldMax - oldMin;
        var newRange = newMax - newMin;
        var newValue = ((number - oldMin) * newRange) / oldRange + newMin;
        return Math.Ceiling(newValue * 100.0) / 100.0;
    }

    private static double Logistics(double lightness0, double lightness3, double x)
    {
        const double b = 5.06;
        const double e = 2.71828;
        const double xOffset = 7.1;
        var exponent = (-b * x) + xOffset;
        return lightness0 + ((lightness3 - lightness0) / (1.0 + Math.Pow(e, exponent)));
    }

    private static Color OkhslToColor(Okhsl color)
    {
        var (r, g, b) = OkhslToSrgb(color);
        return Color.Rgb(r, g, b);
    }

    private static (byte r, byte g, byte b) OkhslToSrgb(Okhsl hsl)
    {
        var h = hsl.H;
        var s = hsl.S;
        var l = hsl.L;

        var L = ToeInv(l);
        double a;
        double b;

        if (s <= 0 || l >= 1.0)
        {
            a = 0;
            b = 0;
        }
        else
        {
            var a_ = Math.Cos((h / 180.0) * Math.PI);
            var b_ = Math.Sin((h / 180.0) * Math.PI);
            var (c0, cMid, cMax) = GetCs(L, a_, b_);

            double t;
            double k0;
            double k1;
            double k2;
            if (s < 0.8)
            {
                t = 1.25 * s;
                k0 = 0;
                k1 = 0.8 * c0;
                k2 = 1 - (k1 / cMid);
            }
            else
            {
                t = 5 * (s - 0.8);
                k0 = cMid;
                k1 = (0.2 * cMid * cMid * 1.25 * 1.25) / c0;
                k2 = 1 - (k1 / (cMax - cMid));
            }

            var C = k0 + ((t * k1) / (1 - (k2 * t)));
            a = C * a_;
            b = C * b_;
        }

        var (lr, lg, lb) = OklabToLinearSrgb(L, a, b);
        return (ToByte(LinearToSrgb(lr)), ToByte(LinearToSrgb(lg)), ToByte(LinearToSrgb(lb)));
    }

    private static double ToeInv(double x)
    {
        const double k1 = 0.206;
        const double k2 = 0.03;
        const double k3 = (1 + k1) / (1 + k2);
        return (x * x + k1 * x) / (k3 * (x + k2));
    }

    private static (double c0, double cMid, double cMax) GetCs(double L, double a_, double b_)
    {
        var cusp = FindCusp(a_, b_);
        var cMax = FindGamutIntersection(a_, b_, L1: L, C1: 1, L0: L, cusp);
        var (sMax, tMax) = GetStMax(cusp);

        var sMid =
            0.11516993 +
            1 /
            (7.4477897 +
             (4.1590124 * b_) +
             (a_ *
              (-2.19557347 +
               (1.75198401 * b_) +
               (a_ *
                (-2.13704948 +
                 (-10.02301043 * b_) +
                 (a_ *
                  (-4.24894561 +
                   (5.38770819 * b_) +
                   (4.69891013 * a_))))))));

        var tMid =
            0.11239642 +
            1 /
            (1.6132032 +
             (-0.68124379 * b_) +
             (a_ *
              (0.40370612 +
               (0.90148123 * b_) +
               (a_ *
                (-0.27087943 +
                 (0.6122399 * b_) +
                 (a_ *
                  (0.00299215 +
                   (-0.45399568 * b_) +
                   (-0.14661872 * a_))))))));

        var k = cMax / Math.Min(L * sMax, (1 - L) * tMax);

        var Ca = L * sMid;
        var Cb = (1 - L) * tMid;
        var cMid =
            0.9 *
            k *
            Math.Sqrt(
                Math.Sqrt(1 / ((1 / (Ca * Ca * Ca * Ca)) + (1 / (Cb * Cb * Cb * Cb)))));

        Ca = L * 0.4;
        Cb = (1 - L) * 0.8;
        var c0 = Math.Sqrt(1 / ((1 / (Ca * Ca)) + (1 / (Cb * Cb))));

        return (c0, cMid, cMax);
    }

    private static (double L, double C) FindCusp(double a, double b)
    {
        var sCusp = ComputeMaxSaturation(a, b);
        var (r, g, bl) = OklabToLinearSrgb(L: 1, a: sCusp * a, b: sCusp * b);
        var max = Math.Max(r, Math.Max(g, bl));
        var lCusp = Math.Cbrt(1 / max);
        var cCusp = lCusp * sCusp;
        return (lCusp, cCusp);
    }

    private static (double S, double T) GetStMax((double L, double C) cusp)
        => (cusp.C / cusp.L, cusp.C / (1 - cusp.L));

    private static double ComputeMaxSaturation(double a, double b)
    {
        double k0;
        double k1;
        double k2;
        double k3;
        double k4;
        double wl;
        double wm;
        double ws;

        if ((-1.88170328 * a) - (0.80936493 * b) > 1)
        {
            k0 = 1.19086277;
            k1 = 1.76576728;
            k2 = 0.59662641;
            k3 = 0.75515197;
            k4 = 0.56771245;
            wl = 4.0767416621;
            wm = -3.3077115913;
            ws = 0.2309699292;
        }
        else if ((1.81444104 * a) - (1.19445276 * b) > 1)
        {
            k0 = 0.73956515;
            k1 = -0.45954404;
            k2 = 0.08285427;
            k3 = 0.1254107;
            k4 = 0.14503204;
            wl = -1.2684380046;
            wm = 2.6097574011;
            ws = -0.3413193965;
        }
        else
        {
            k0 = 1.35733652;
            k1 = -0.00915799;
            k2 = -1.1513021;
            k3 = -0.50559606;
            k4 = 0.00692167;
            wl = -0.0041960863;
            wm = -0.7034186147;
            ws = 1.707614701;
        }

        var S = k0 + (k1 * a) + (k2 * b) + (k3 * a * a) + (k4 * a * b);

        var kL = (0.3963377774 * a) + (0.2158037573 * b);
        var kM = (-0.1055613458 * a) + (-0.0638541728 * b);
        var kS = (-0.0894841775 * a) + (-1.291485548 * b);

        var l_ = 1 + (S * kL);
        var m_ = 1 + (S * kM);
        var s_ = 1 + (S * kS);

        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;

        var l_dS = 3 * kL * l_ * l_;
        var m_dS = 3 * kM * m_ * m_;
        var s_dS = 3 * kS * s_ * s_;

        var l_dS2 = 6 * kL * kL * l_;
        var m_dS2 = 6 * kM * kM * m_;
        var s_dS2 = 6 * kS * kS * s_;

        var f = (wl * l) + (wm * m) + (ws * s);
        var f1 = (wl * l_dS) + (wm * m_dS) + (ws * s_dS);
        var f2 = (wl * l_dS2) + (wm * m_dS2) + (ws * s_dS2);

        S = S - ((f * f1) / ((f1 * f1) - (0.5 * f * f2)));

        return S;
    }

    private static double FindGamutIntersection(double a, double b, double L1, double C1, double L0, (double L, double C) cusp)
    {
        // Find the intersection for upper and lower half separately.
        double t;
        if (((L1 - L0) * cusp.C) - ((cusp.L - L0) * C1) <= 0)
        {
            // Lower half.
            t = (cusp.C * L0) / ((C1 * cusp.L) + (cusp.C * (L0 - L1)));
        }
        else
        {
            // Upper half.
            t = (cusp.C * (L0 - 1)) / ((C1 * (cusp.L - 1)) + (cusp.C * (L0 - L1)));

            var dL = L1 - L0;
            var dC = C1;

            var kL = (0.3963377774 * a) + (0.2158037573 * b);
            var kM = (-0.1055613458 * a) + (-0.0638541728 * b);
            var kS = (-0.0894841775 * a) + (-1.291485548 * b);

            var lDt = dL + (dC * kL);
            var mDt = dL + (dC * kM);
            var sDt = dL + (dC * kS);

            var L = (L0 * (1 - t)) + (t * L1);
            var C = t * C1;

            var l_ = L + (C * kL);
            var m_ = L + (C * kM);
            var s_ = L + (C * kS);

            var l = l_ * l_ * l_;
            var m = m_ * m_ * m_;
            var s = s_ * s_ * s_;

            var ldt = 3 * lDt * l_ * l_;
            var mdt = 3 * mDt * m_ * m_;
            var sdt = 3 * sDt * s_ * s_;

            var ldt2 = 6 * lDt * lDt * l_;
            var mdt2 = 6 * mDt * mDt * m_;
            var sdt2 = 6 * sDt * sDt * s_;

            var r = (4.0767416621 * l) - (3.3077115913 * m) + (0.2309699292 * s) - 1;
            var r1 = (4.0767416621 * ldt) - (3.3077115913 * mdt) + (0.2309699292 * sdt);
            var r2 = (4.0767416621 * ldt2) - (3.3077115913 * mdt2) + (0.2309699292 * sdt2);
            var uR = r1 / ((r1 * r1) - (0.5 * r * r2));
            var tR = -r * uR;

            var g = (-1.2684380046 * l) + (2.6097574011 * m) - (0.3413193965 * s) - 1;
            var g1 = (-1.2684380046 * ldt) + (2.6097574011 * mdt) - (0.3413193965 * sdt);
            var g2 = (-1.2684380046 * ldt2) + (2.6097574011 * mdt2) - (0.3413193965 * sdt2);
            var uG = g1 / ((g1 * g1) - (0.5 * g * g2));
            var tG = -g * uG;

            var bl = (-0.0041960863 * l) - (0.7034186147 * m) + (1.707614701 * s) - 1;
            var b1 = (-0.0041960863 * ldt) - (0.7034186147 * mdt) + (1.707614701 * sdt);
            var b2 = (-0.0041960863 * ldt2) - (0.7034186147 * mdt2) + (1.707614701 * sdt2);
            var uB = b1 / ((b1 * b1) - (0.5 * bl * b2));
            var tB = -bl * uB;

            tR = uR >= 0 ? tR : 1_000_000;
            tG = uG >= 0 ? tG : 1_000_000;
            tB = uB >= 0 ? tB : 1_000_000;

            t += Math.Min(tR, Math.Min(tG, tB));
        }

        return t;
    }

    private static (double r, double g, double b) OklabToLinearSrgb(double L, double a, double b)
    {
        var l_ = L + (0.3963377774 * a) + (0.2158037573 * b);
        var m_ = L + (-0.1055613458 * a) + (-0.0638541728 * b);
        var s_ = L + (-0.0894841775 * a) + (-1.291485548 * b);

        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;

        var r = (4.0767416621 * l) - (3.3077115913 * m) + (0.2309699292 * s);
        var g = (-1.2684380046 * l) + (2.6097574011 * m) - (0.3413193965 * s);
        var bl = (-0.0041960863 * l) - (0.7034186147 * m) + (1.707614701 * s);
        return (r, g, bl);
    }

    private static double LinearToSrgb(double x)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        return x <= 0.0031308 ? 12.92 * x : (1.055 * Math.Pow(x, 1.0 / 2.4)) - 0.055;
    }

    private static byte ToByte(double x)
    {
        x = Math.Clamp(x, 0.0, 1.0);
        var v = (int)Math.Round(x * 255.0, MidpointRounding.AwayFromZero);
        if (v < 0) v = 0;
        if (v > 255) v = 255;
        return (byte)v;
    }
}

/// <summary>
/// Discrete presets for the Root Loops “milk” parameter.
/// </summary>
public enum RootLoopsMilkAmount
{
    /// <summary>
    /// No milk (darkest background).
    /// </summary>
    None = 0,
    /// <summary>
    /// A small splash of milk.
    /// </summary>
    Splash = 1,
    /// <summary>
    /// A moderate amount of milk.
    /// </summary>
    Glug = 2,
    /// <summary>
    /// A large amount of milk (lightest background).
    /// </summary>
    Cup = 3,
}

/// <summary>
/// Controls the Root Loops accent flavor (hue shift).
/// </summary>
public enum RootLoopsFlavor
{
    /// <summary>
    /// Fruity flavor (no hue shift).
    /// </summary>
    Fruity = 0,
    /// <summary>
    /// Classic flavor (small hue shift).
    /// </summary>
    Classic = 1,
    /// <summary>
    /// Intense flavor (larger hue shift).
    /// </summary>
    Intense = 2,
}

/// <summary>
/// Controls the Root Loops base hue.
/// </summary>
public enum RootLoopsFruit
{
    /// <summary>Cherry.</summary>
    Cherry = 0,
    /// <summary>Tomato.</summary>
    Tomato = 1,
    /// <summary>Orange.</summary>
    Orange = 2,
    /// <summary>Pineapple.</summary>
    Pineapple = 3,
    /// <summary>Apple.</summary>
    Apple = 4,
    /// <summary>Kiwi.</summary>
    Kiwi = 5,
    /// <summary>Kale.</summary>
    Kale = 6,
    /// <summary>Blueberry.</summary>
    Blueberry = 7,
    /// <summary>Plum.</summary>
    Plum = 8,
    /// <summary>Elderberry.</summary>
    Elderberry = 9,
    /// <summary>Blackberry.</summary>
    Blackberry = 10,
    /// <summary>Raspberry.</summary>
    Raspberry = 11,
}

/// <summary>
/// Represents a Root Loops recipe used to generate an <see cref="ColorScheme"/>.
/// </summary>
/// <param name="Sugar">Controls accent lightness (0..10).</param>
/// <param name="Colors">Controls accent saturation (0..10).</param>
/// <param name="Sogginess">Controls base saturation (0..10).</param>
/// <param name="Flavor">Controls hue shifting for accents.</param>
/// <param name="Fruit">Controls the base hue.</param>
/// <param name="Milk">Controls background/foreground lightness.</param>
public readonly record struct RootLoopsRecipe(
    int Sugar,
    int Colors,
    int Sogginess,
    RootLoopsFlavor Flavor,
    RootLoopsFruit Fruit,
    double Milk)
{
    /// <summary>
    /// Gets the milk value clamped to a valid range.
    /// </summary>
    public double ClampedMilk => Math.Clamp(Milk, 0.0, 3.0);

    /// <summary>
    /// Gets or initializes a continuous fruit mix (0..1) mapped to the base hue.
    /// </summary>
    public double FruitMix { get; init; } = Math.Clamp((double)(int)Fruit / 12.0, 0.0, 11.0);
}
