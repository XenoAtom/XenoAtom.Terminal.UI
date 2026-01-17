// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TabControlStabilityTests
{
    [TestMethod]
    [Ignore("Invalid for now for TabControl")]
    public void TabControl_DoesNotDuplicateTabs_When_SelectedIndex_Changes()
    {
        var tabControl = new TabControl()
            .Update(tabs =>
            {
                tabs.AddTab(new TextBlock("One"), new TextBlock("A"));
                tabs.AddTab(new TextBlock("Two"), new TextBlock("B"));
            });

        var root = new VStack(tabControl);
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 25));
        driver.Tick();

        Assert.HasCount(2, tabControl.Tabs);

        tabControl.SelectedIndex = 1;
        driver.Tick();

        Assert.HasCount(2, tabControl.Tabs, "Tabs should not be re-added when SelectedIndex changes.");
    }
}
