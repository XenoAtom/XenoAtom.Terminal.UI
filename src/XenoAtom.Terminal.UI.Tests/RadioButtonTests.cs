// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class RadioButtonTests
{
    [TestMethod]
    public void RadioButton_Unchecks_Others_In_Group()
    {
        var group = new object();
        var a = new RadioButton("A", group);
        var b = new RadioButton("B", group);

        var root = new VStack();
        root.Add(a);
        root.Add(b);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        driver.TickUntil(() => a.IsChecked);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });
        driver.TickUntil(() => b.IsChecked && !a.IsChecked);
    }
}

