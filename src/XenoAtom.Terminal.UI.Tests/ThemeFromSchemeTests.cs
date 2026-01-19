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
    public void FromScheme_DarkScheme_Provides_Fullscreen_Tokens()
    {
        var scheme = ColorScheme.RootLoopsDark;
        var theme = Theme.FromScheme(scheme, ThemeSchemeBrightness.Auto);

        Assert.AreEqual(scheme.Background, theme.Background);
        Assert.AreEqual(scheme.Foreground, theme.Foreground);

        Assert.IsNotNull(theme.Surface);
        Assert.IsNotNull(theme.PopupSurface);
        Assert.IsNotNull(theme.ControlFill);
        Assert.IsNotNull(theme.ControlFillHover);
        Assert.IsNotNull(theme.ControlFillPressed);
        Assert.IsNotNull(theme.InputFill);
        Assert.IsNotNull(theme.InputFillFocused);
        Assert.IsNotNull(theme.Border);
        Assert.IsNotNull(theme.FocusBorder);
        Assert.IsNotNull(theme.Selection);
        Assert.IsNotNull(theme.Muted);
        Assert.IsNotNull(theme.Disabled);

        Assert.AreEqual(ColorKind.RgbA, theme.Surface!.Value.Kind);
        Assert.AreEqual(ColorKind.Rgb, theme.PopupSurface!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.ControlFill!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.InputFill!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.InputFillFocused!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.Border!.Value.Kind);
        Assert.AreEqual(ColorKind.Rgb, theme.FocusBorder!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.Selection!.Value.Kind);

        // Dark theme: unfocused inputs are slightly lifted (lighter), focused inputs are inset (darker).
        Assert.AreEqual(128, theme.InputFill.Value.R);
        Assert.AreEqual(128, theme.InputFill.Value.G);
        Assert.AreEqual(128, theme.InputFill.Value.B);
        Assert.AreEqual(0, theme.InputFillFocused.Value.R);
        Assert.AreEqual(0, theme.InputFillFocused.Value.G);
        Assert.AreEqual(0, theme.InputFillFocused.Value.B);
    }

    [TestMethod]
    public void FromScheme_LightScheme_Uses_Light_Surfaces_And_Dark_Overlays()
    {
        var scheme = ColorScheme.RootLoopsLight;
        var theme = Theme.FromScheme(scheme, ThemeSchemeBrightness.Auto);

        Assert.AreEqual(scheme.Background, theme.Background);
        Assert.AreEqual(scheme.Foreground, theme.Foreground);

        Assert.IsNotNull(theme.Surface);
        Assert.IsNotNull(theme.ControlFill);
        Assert.IsNotNull(theme.InputFill);
        Assert.IsNotNull(theme.InputFillFocused);
        Assert.IsNotNull(theme.PopupSurface);
        Assert.IsNotNull(theme.Border);

        Assert.AreEqual(ColorKind.Rgb, theme.Surface.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.ControlFill!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.InputFill!.Value.Kind);
        Assert.AreEqual(ColorKind.Rgb, theme.InputFillFocused!.Value.Kind);
        Assert.AreEqual(ColorKind.Rgb, theme.PopupSurface!.Value.Kind);
        Assert.AreEqual(ColorKind.RgbA, theme.Border!.Value.Kind);

        // Light scheme surfaces are intended to be lighter than the background.
        var bgLum = GetLuma(scheme.Background!.Value);
        var surfaceLum = GetLuma(theme.Surface.Value);
        Assert.IsGreaterThan(bgLum, surfaceLum);
    }

    private static float GetLuma(Color color)
    {
        Assert.IsTrue(color.Kind is ColorKind.Rgb or ColorKind.RgbA);

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
