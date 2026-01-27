// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TabControlRenderingTests
{
    [TestMethod]
    public void TabControl_Renders_TabHeaders_Like_Buttons_With_Pressed_State()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")),
            new TabPage("Two", new TextBlock("B")));

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        tabControl.Style(theme);

        tabControl.Measure(new Size(40, 6));
        tabControl.Arrange(new Rectangle(0, 0, 40, 6));

        var buffer = new CellBuffer(40, 6);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        // First tab background should differ from the strip background.
        Assert.IsTrue(cells[0].TryGetBackground(out var tabBg), "Expected tab header cell to have a background color.");
        Assert.IsTrue(cells[39].TryGetBackground(out var stripBg), "Expected strip cell to have a background color.");
        Assert.AreNotEqual(stripBg, tabBg, "Expected tab header background to differ from the header strip background.");

        // Pressing the first tab should use the theme selection background.
        var selection = theme.Selection ?? throw new AssertFailedException("Theme is expected to provide a selection background.");
        typeof(TabControl).GetProperty("PressedIndex", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, 0);
        typeof(TabControl).GetProperty("IsPressedInside", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, true);

        buffer.Clear(theme.BaseTextStyle());
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });
        cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var pressedBg));
        Assert.IsTrue(cells[39].TryGetBackground(out var stripBgPressed), "Expected strip cell to have a background color.");

        // Selection backgrounds can be RGBA overlays; they should be blended over the header strip background.
        var expected = selection.Kind == ColorKind.RgbA ? BlendLinear(selection, stripBgPressed) : selection;
        AssertClose(expected, pressedBg);
    }

    [TestMethod]
    public void TabControl_Applies_TabContentTemplateFactory()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")));

        tabControl.Style(Theme.FromScheme(ColorScheme.RootLoopsDark));
        tabControl.Style(TabControlStyle.Rounded);

        tabControl.Measure(new Size(20, 6));
        tabControl.Arrange(new Rectangle(0, 0, 20, 6));

        var buffer = new CellBuffer(20, 6);
        buffer.Clear(tabControl.GetTheme().BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        // Border is rendered below the header strip.
        var expectedTopLeft = LineGlyphs.Rounded.TopLeft.Value;
        Assert.AreEqual(expectedTopLeft, scalars[buffer.Width], "Expected the tab content to be wrapped by the rounded border template.");
    }

    private static void AssertClose(Color expected, Color actual)
    {
        // The production code uses LUTs for speed; allow a small tolerance.
        Assert.AreEqual(ColorKind.Rgb, actual.Kind);

        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.R - actual.R));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.G - actual.G));
        Assert.IsLessThanOrEqualTo(1, Math.Abs(expected.B - actual.B));
    }

    private static Color BlendLinear(Color src, Color dst)
    {
        Assert.AreEqual(ColorKind.RgbA, src.Kind);
        Assert.AreEqual(ColorKind.Rgb, dst.Kind);

        var sa = src.A / 255.0;
        var invSa = 1.0 - sa;

        var srcR = SrgbToLinear(src.R);
        var srcG = SrgbToLinear(src.G);
        var srcB = SrgbToLinear(src.B);

        var dstR = SrgbToLinear(dst.R);
        var dstG = SrgbToLinear(dst.G);
        var dstB = SrgbToLinear(dst.B);

        var outR = (srcR * sa) + (dstR * invSa);
        var outG = (srcG * sa) + (dstG * invSa);
        var outB = (srcB * sa) + (dstB * invSa);

        return Color.Rgb(LinearToSrgb(outR), LinearToSrgb(outG), LinearToSrgb(outB));
    }

    private static double SrgbToLinear(byte value)
    {
        var srgb = value / 255.0;
        return srgb <= 0.04045 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4);
    }

    private static byte LinearToSrgb(double linear)
    {
        linear = Math.Clamp(linear, 0.0, 1.0);
        var srgb = linear <= 0.0031308 ? 12.92 * linear : (1.055 * Math.Pow(linear, 1.0 / 2.4)) - 0.055;
        var value = (int)Math.Round(srgb * 255.0);
        return (byte)Math.Clamp(value, 0, 255);
    }
}
