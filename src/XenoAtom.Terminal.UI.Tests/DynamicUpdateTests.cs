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
                var count = countState.Value;
                for (var i = 0; i < count; i++)
                {
                    v.Add($"Item {i}");
                }
            });

        using var driver = new TerminalAppTestDriver(stack, TerminalHostKind.Fullscreen, new TerminalSize(80, 25));
        driver.TickUntil(() => stack.Children.Count == 1);

        countState.Value = 3;
        driver.TickUntil(() => stack.Children.Count == 3);

        countState.Value = 2;
        driver.TickUntil(() => stack.Children.Count == 2);
    }

    [TestMethod]
    public void DynamicUpdates_Cannot_Mutate_StaticallyInitialized_List()
    {
        var stack = new VStack();
        stack.Add("Static");

        stack.Update(v => v.Add("Dynamic"));

        Assert.Throws<InvalidOperationException>(() => stack.Measure(new Size(80, 25)));
    }
}
