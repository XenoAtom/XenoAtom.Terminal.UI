// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

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

        var theme = Theme.FromScheme(AnsiColorScheme.RootLoopsDark);
        tabControl.Set(Theme.Key, theme);

        tabControl.Measure(new Size(40, 6));
        tabControl.Arrange(new Rectangle(0, 0, 40, 6));

        var buffer = new CellBuffer(40, 6);
        buffer.Clear();

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var cells = (CellStyle[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        // First tab background should differ from the strip background.
        Assert.IsTrue(cells[0].TryGetBackground(out var tabBg), "Expected tab header cell to have a background color.");
        Assert.IsTrue(cells[39].TryGetBackground(out var stripBg), "Expected strip cell to have a background color.");
        Assert.AreNotEqual(stripBg, tabBg, "Expected tab header background to differ from the header strip background.");

        // Pressing the first tab should use the theme selection background.
        var selection = theme.Selection ?? throw new AssertFailedException("Theme is expected to provide a selection background.");
        typeof(TabControl).GetField("_pressedIndex", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, 0);
        typeof(TabControl).GetField("_pressedInside", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(tabControl, true);

        buffer.Clear();
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });
        cells = (CellStyle[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        Assert.IsTrue(cells[0].TryGetBackground(out var pressedBg));
        Assert.AreEqual(selection, pressedBg);
    }

    [TestMethod]
    public void TabControl_Applies_TabContentTemplateFactory()
    {
        var tabControl = new TabControl(
            new TabPage("One", new TextBlock("A")));

        tabControl.Set(Theme.Key, Theme.FromScheme(AnsiColorScheme.RootLoopsDark));
        tabControl.Set(TabControlStyle.Rounded);

        tabControl.Measure(new Size(20, 6));
        tabControl.Arrange(new Rectangle(0, 0, 20, 6));

        var buffer = new CellBuffer(20, 6);
        buffer.Clear();

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(tabControl, new object[] { buffer });

        var scalars = (int[])typeof(CellBuffer).GetField("_scalars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;

        // Border is rendered below the header strip.
        var expectedTopLeft = LineGlyphs.Rounded.TopLeft.Value;
        Assert.AreEqual(expectedTopLeft, scalars[buffer.Width], "Expected the tab content to be wrapped by the rounded border template.");
    }
}
