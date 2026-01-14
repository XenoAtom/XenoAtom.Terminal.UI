// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ThemeFromSchemeTests
{
    [TestMethod]
    public void FromScheme_Uses_Palette_Neutrals_For_Dark_Schemes()
    {
        var scheme = AnsiColorScheme.RootLoopsDark;
        var theme = Theme.FromScheme(scheme, ThemeSchemeBrightness.Auto);

        Assert.AreEqual(scheme.Background, theme.Background);
        Assert.AreEqual(scheme.Foreground, theme.Foreground);
        Assert.AreEqual(scheme.Black, theme.Surface);
        Assert.AreEqual(scheme.BrightBlack, theme.SurfaceAlt);
    }

    [TestMethod]
    public void FromScheme_Derives_Neutrals_Close_To_Background_For_Light_Schemes()
    {
        var scheme = AnsiColorScheme.RootLoopsLight;
        var theme = Theme.FromScheme(scheme, ThemeSchemeBrightness.Auto);

        Assert.AreEqual(scheme.Background, theme.Background);
        Assert.AreEqual(scheme.Foreground, theme.Foreground);

        Assert.IsNotNull(theme.Surface);
        Assert.IsNotNull(theme.SurfaceAlt);

        // For light schemes, the derived surfaces should be closer to the background than to the foreground.
        var bgLum = GetLuma(scheme.Background!.Value);
        var fgLum = GetLuma(scheme.Foreground!.Value);
        var surfaceLum = GetLuma(theme.Surface!.Value);
        var surfaceAltLum = GetLuma(theme.SurfaceAlt!.Value);

        Assert.IsTrue(bgLum > fgLum);
        Assert.IsTrue(bgLum > surfaceLum && surfaceLum > surfaceAltLum && surfaceAltLum > fgLum);

        // They should be derived, not direct palette "black" entries (which can be too saturated on light themes).
        Assert.AreNotEqual(scheme.Black, theme.Surface);
        Assert.AreNotEqual(scheme.BrightBlack, theme.SurfaceAlt);
    }

    private static float GetLuma(AnsiColor color)
    {
        Assert.AreEqual(AnsiColorKind.Rgb, color.Kind);

        static float ToLinear(byte channel)
        {
            var v = channel / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        var r = ToLinear(color.R);
        var g = ToLinear(color.G);
        var b = ToLinear(color.B);
        return (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
    }
}

