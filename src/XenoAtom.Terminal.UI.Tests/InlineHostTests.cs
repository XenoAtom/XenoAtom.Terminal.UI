// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class InlineHostTests
{
    [TestMethod]
    public void Renders_TextBlock_In_InlineHost()
    {
        var root = new VStack { "Hello" };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(20, 10));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Hello");
    }

    [TestMethod]
    public void InlineHost_Delivers_Mouse_To_LiveRegion()
    {
        var button = new Button("OK");
        var clicked = false;
        button.Click((_, _) => clicked = true);

        var root = new VStack { button };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(20, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 1, Y = 0 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 1, Y = 0 });

        driver.TickUntil(() => clicked);
    }

    [TestMethod]
    public void InlineApp_Can_Append_Flow_Visual()
    {
        var progress = new ProgressBar { Value = 0.0 };
        var root = new VStack { progress };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Append(new TextBlock("Flow: Hello"));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Flow: Hello");
    }
}
