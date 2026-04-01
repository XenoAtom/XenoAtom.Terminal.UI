// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectTests
{
    [TestMethod]
    public void Select_Opens_And_Selects_Item_On_Click()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        // Click the select to open the popup.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Tick();

        // Click the second item in the popup list (roughly below the select).
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 3 });
        driver.TickUntil(() => select.SelectedIndex == 1);

        Assert.AreEqual(1, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_IdleTick_DoesNotRebuild_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        driver.Tick();

        Assert.AreSame(initialContent, select.Content);
    }

    [TestMethod]
    public void Select_SelectedItemChange_Rebuilds_SelectedContent()
    {
        var select = new Select<string>()
            .Items(["First", "Second", "Third"]);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var initialContent = select.Content;
        Assert.IsNotNull(initialContent);

        select.Items[0] = "Updated";
        driver.Tick();

        Assert.AreNotSame(initialContent, select.Content);
    }

    [TestMethod]
    public void Select_Bound_SelectedIndex_Does_Not_Write_Source_During_Tick()
    {
        var selectedIndex = new State<int>(0);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        var rewroteSource = false;
        select.SelectionChanged((_, e) =>
        {
            if (rewroteSource || e.NewIndex != 1)
            {
                return;
            }

            rewroteSource = true;
            selectedIndex.Value = 0;
        });

        selectedIndex.Value = 1;

        driver.Tick();

        Assert.IsTrue(rewroteSource, "Expected the bound source change to notify the control once.");
        Assert.AreEqual(0, selectedIndex.Value);
        Assert.AreEqual(0, select.SelectedIndex);
    }

    [TestMethod]
    public void Select_Out_Of_Range_State_Does_Not_Get_Clamped_During_Tick()
    {
        var selectedIndex = new State<int>(10);
        var select = new Select<string>()
            .Items(["First", "Second", "Third"])
            .SelectedIndex(selectedIndex);

        var root = new VStack { select };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        Assert.AreEqual(10, selectedIndex.Value, "The bound source should not be rewritten during prepare/measure.");
        Assert.AreEqual(2, select.SelectedIndex, "The control should clamp its local selected index outside guarded callbacks.");
        Assert.IsNotNull(select.Content);
    }
}
