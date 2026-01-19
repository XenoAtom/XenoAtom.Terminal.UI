// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OptionListTests
{
    [TestMethod]
    public void OptionList_ArrowDown_Raises_SelectionChanged()
    {
        var list = new OptionList { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        (int OldIndex, int NewIndex)? selectionChanged = null;
        list.SelectionChanged((_, e) => selectionChanged = (e.OldIndex, e.NewIndex));

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.TickUntil(() => selectionChanged is not null);

        Assert.AreEqual(0, selectionChanged!.Value.OldIndex);
        Assert.AreEqual(1, selectionChanged!.Value.NewIndex);
    }

    [TestMethod]
    public void OptionList_Enter_Raises_ItemActivated()
    {
        var list = new OptionList { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("First"),
            new OptionListItem("Second"),
            new OptionListItem("Third"));

        int? activated = null;
        list.ItemActivated((_, e) => activated = e.Index);

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => activated is not null);

        Assert.AreEqual(1, activated);
    }

    [TestMethod]
    public void OptionList_Renders_Descriptions_On_Second_Line()
    {
        var list = new OptionList { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("Build", "Ctrl+B") { Description = "Build the project" },
            new OptionListItem("Run", "F5") { Description = "Run the app" });

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Build the project");
        StringAssert.Contains(rendered, "Run the app");
    }

    [TestMethod]
    public void OptionList_MouseWheel_Skips_Disabled_Items()
    {
        var list = new OptionList { MinHeight = 4, MaxHeight = 4 };
        list.Items.AddRange(
            new OptionListItem("Header") { IsEnabled = false },
            new OptionListItem("First"),
            new OptionListItem("Second"));

        list.SelectedIndex = 1;

        var root = new VStack { list };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        // Wheel up from "First": should skip the disabled header and remain on the first enabled item.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = 1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(1, list.SelectedIndex);

        // Wheel down: should move to "Second".
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(2, list.SelectedIndex);

        // Wheel up from "Second": should go back to "First".
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = 1, X = 1, Y = 0 });
        driver.Tick();
        Assert.AreEqual(1, list.SelectedIndex);
    }
}
