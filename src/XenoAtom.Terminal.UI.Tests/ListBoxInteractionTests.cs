// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ListBoxInteractionTests
{
    [TestMethod]
    public void ListBox_Changes_Selection_On_Down()
    {
        var listBox = new ListBox<string>
        {
            SelectedIndex = 0,
            MinHeight = 3,
            MaxHeight = 3,
        };
        listBox.Items.AddRange("A", "B", "C");

        var root = new VStack { listBox };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.TickUntil(() => listBox.SelectedIndex == 1);
    }
}
