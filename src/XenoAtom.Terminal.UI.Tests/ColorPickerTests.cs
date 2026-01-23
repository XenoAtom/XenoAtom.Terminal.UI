// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ColorPickerTests
{
    [TestMethod]
    public void ColorPicker_Updates_Value_From_Hex()
    {
        var picker = new ColorPicker();
        var root = new VStack { picker };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 20));
        driver.Tick();

        var hexBox = picker.EnumerateVisualsDepthFirst().OfType<TextBox>().Single();
        hexBox.TextDocument.Replace(0, hexBox.TextDocument.CurrentSnapshot.Length, "#112233".AsSpan());
        driver.Tick();

        var rgb = picker.Value.ToRgb();
        Assert.AreEqual(0x11, rgb.R);
        Assert.AreEqual(0x22, rgb.G);
        Assert.AreEqual(0x33, rgb.B);
    }

    [TestMethod]
    public void ColorPicker_Does_Not_Update_Value_From_Invalid_Hex()
    {
        var picker = new ColorPicker { Value = Color.Rgb(0x10, 0x20, 0x30) };
        var root = new VStack { picker };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 20));
        driver.Tick();

        var hexBox = picker.EnumerateVisualsDepthFirst().OfType<TextBox>().Single();
        hexBox.TextDocument.Replace(0, hexBox.TextDocument.CurrentSnapshot.Length, "#GGGGGG".AsSpan());
        driver.Tick();

        var rgb = picker.Value.ToRgb();
        Assert.AreEqual(0x10, rgb.R);
        Assert.AreEqual(0x20, rgb.G);
        Assert.AreEqual(0x30, rgb.B);
    }

    [TestMethod]
    public void ColorPicker_Updates_Value_From_Channel_Sliders()
    {
        var picker = new ColorPicker();
        var root = new VStack { picker };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 20));
        driver.Tick();

        var sliders = picker.EnumerateVisualsDepthFirst().OfType<Slider<int>>().ToArray();
        Assert.HasCount(4, sliders);

        sliders[0].Value = 12;  // R
        sliders[1].Value = 34;  // G
        sliders[2].Value = 56;  // B
        sliders[3].Value = 78;  // A
        driver.Tick();

        var rgb = picker.Value.ToRgb();
        Assert.AreEqual(12, rgb.R);
        Assert.AreEqual(34, rgb.G);
        Assert.AreEqual(56, rgb.B);
        Assert.AreEqual(78, picker.Value.A);
    }

    [TestMethod]
    public void ColorPicker_Palette_Click_Sets_Value()
    {
        var picker = new ColorPicker
        {
            Palette = new Color?[]
            {
                Color.Rgb(0x01, 0x02, 0x03),
                Color.Rgb(0xAA, 0xBB, 0xCC),
            },
        };

        var root = new VStack { picker };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 20));
        driver.Tick();

        var swatches = picker.EnumerateVisualsDepthFirst().OfType<ColorPicker.PaletteSwatch>().ToArray();
        Assert.HasCount(2, swatches);

        var swatch = swatches[1];
        var x = swatch.Bounds.X + 1;
        var y = swatch.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = x, Y = y });
        driver.Tick();

        var rgb = picker.Value.ToRgb();
        Assert.AreEqual(0xAA, rgb.R);
        Assert.AreEqual(0xBB, rgb.G);
        Assert.AreEqual(0xCC, rgb.B);
    }
}
