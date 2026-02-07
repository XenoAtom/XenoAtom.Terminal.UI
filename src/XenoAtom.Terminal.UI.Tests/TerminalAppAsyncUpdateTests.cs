// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppAsyncUpdateTests
{
    [TestMethod]
    public void Tick_Supports_Async_Update_Callback()
    {
        var counter = new State<int>(0);
        var root = new TextBlock().Text(() => counter.Value.ToString());

        using var driver = new TerminalAppTestDriver(root);
        driver.App.SetUpdateCallback(async _ =>
        {
            await Task.Yield();
            counter.Value++;
            return counter.Value >= 3 ? TerminalLoopResult.StopAndKeepVisual : TerminalLoopResult.Continue;
        });

        driver.TickUntil(() => counter.Value >= 3, maxTicks: 30);
        Assert.AreEqual(3, counter.Value);
    }
}

