// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DynamicUpdateTests
{
    [TestMethod]
    public void DynamicUpdates_Clear_Lists_Before_Reapply()
    {
        var countState = new State<int>(1);

        var stack = new VStack()
            .Update(v =>
            {
                v.Children.Clear();
                var count = countState.Value;
                for (var i = 0; i < count; i++)
                {
                    v.Add($"Item {i}");
                }
            });

        using var driver = new TerminalAppTestDriver(stack, TerminalHostKind.Fullscreen, new TerminalSize(80, 25));
        driver.Tick();
        Assert.HasCount(1, stack.Children);

        countState.Value = 3;
        driver.Tick();
        Assert.HasCount(3, stack.Children);

        countState.Value = 2;
        driver.Tick();
        Assert.HasCount(2, stack.Children);
    }
}
