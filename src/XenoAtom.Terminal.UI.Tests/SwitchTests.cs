// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Hosting;
using System.Reflection;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SwitchTests
{
    [TestMethod]
    public void Toggled_Event_Is_Raised_With_Old_And_New_Values()
    {
        var sw = new Switch();

        ToggleChangedEventArgs? args = null;
        sw.ToggledRouted += (_, e) => args = e;

        sw.IsOn = true;

        Assert.IsNotNull(args);
        Assert.IsFalse(args.OldValue);
        Assert.IsTrue(args.NewValue);
    }

    [TestMethod]
    public void Space_Key_Toggles_Switch()
    {
        var sw = new Switch();
        var root = new VStack { sw };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        driver.Tick();

        Assert.IsTrue(sw.IsOn);
    }

    [TestMethod]
    public void Switch_Renders_Segmented_Track_With_Different_Left_And_Right_Backgrounds()
    {
        var theme = Theme.FromScheme(XenoAtom.Terminal.UI.Styling.ColorScheme.RootLoopsDark);

        var sw = new Switch();
        sw.Style(theme);

        sw.Measure(new Size(10, 1));
        sw.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear(theme.BaseTextStyle());
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var leftOffBg));
        Assert.IsTrue(cells[3].TryGetBackground(out var rightOffBg));
        Assert.AreNotEqual(leftOffBg, rightOffBg);

        // Thumb cell should keep the underlying track background.
        Assert.IsTrue(cells[1].TryGetBackground(out var thumbOffBg));
        Assert.AreEqual(leftOffBg, thumbOffBg);

        sw.IsOn = true;
        buffer.Clear(theme.BaseTextStyle());
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });
        cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var leftOnBg));
        Assert.IsTrue(cells[3].TryGetBackground(out var rightOnBg));
        Assert.AreNotEqual(leftOnBg, rightOnBg);

        Assert.IsTrue(cells[2].TryGetBackground(out var thumbOnBg));
        Assert.AreEqual(rightOnBg, thumbOnBg);
    }

    [TestMethod]
    public void Switch_Renders_Different_Thumb_Glyphs_For_On_And_Off()
    {
        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);

        var sw = new Switch();
        sw.Style(theme);
        sw.Measure(new Size(10, 1));
        sw.Arrange(new Rectangle(0, 0, 10, 1));

        var buffer = new CellBuffer(10, 1);
        buffer.Clear(theme.BaseTextStyle());

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.AreEqual(RadioButtonStyle.Default.UncheckedGlyph.Value, scalars[1], "Expected off-state thumb glyph at the default off thumb position.");

        sw.IsOn = true;
        buffer.Clear(theme.BaseTextStyle());
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(sw, new object[] { buffer });
        scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.AreEqual(RadioButtonStyle.Default.CheckedGlyph.Value, scalars[2], "Expected on-state thumb glyph at the default on thumb position.");
    }
}
