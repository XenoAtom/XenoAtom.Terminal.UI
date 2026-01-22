// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BorderDefaultsTests
{
    [TestMethod]
    public void ButtonStyle_Defaults_To_No_Border()
    {
        Assert.IsFalse(ButtonStyle.Default.ShowBorder);
    }

    [TestMethod]
    public void Button_Border_Is_OptIn_Via_Style()
    {
        var button = new Button("OK");
        var root = new VStack(button);
        using (var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(24, 6)))
        {
            driver.Tick();
            Assert.AreEqual(1, button.Bounds.Height, "Default ButtonStyle should not draw a border and should stay 1 row tall.");
        }

        var borderedButton = new Button("OK");
        var borderedRoot = new VStack(borderedButton);
        borderedRoot.SetStyle(ButtonStyle.Key, new ButtonStyle { ShowBorder = true });

        using (var driver2 = new TerminalAppTestDriver(borderedRoot, TerminalHostKind.Fullscreen, new TerminalSize(24, 6)))
        {
            driver2.Tick();
            Assert.IsGreaterThanOrEqualTo(3, borderedButton.Bounds.Height, "ButtonStyle.ShowBorder should expand the button to include a border.");
        }
    }
}
