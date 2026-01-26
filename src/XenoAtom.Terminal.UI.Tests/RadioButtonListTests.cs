// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class RadioButtonListTests
{
    [TestMethod]
    public void RadioButtonList_Scrolls_Rendered_Viewport_When_Selection_Moves()
    {
        var list = new RadioButtonList<string> { MinHeight = 3, MaxHeight = 3 };
        for (var i = 0; i < 7; i++)
        {
            list.Items.Add($"Item {i:00}");
        }

        var scrollViewer = new ScrollViewer(list) { MinHeight = 3, MaxHeight = 3 };
        var root = new VStack(scrollViewer).Spacing(0);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Tick();

        var screen = new AnsiTestScreen(30, 8);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsFalse(rendered.Contains("Item 00", StringComparison.Ordinal), "Expected the viewport to scroll past the first item.");
        StringAssert.Contains(rendered, "Item 03", "Expected the selected item to be visible after scrolling.");
    }
}

