// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MaskedInputTests
{
    [TestMethod]
    public void MaskedInput_Renders_Caret_When_Focused()
    {
        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Never);

        var root = new VStack { input };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var output = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(output);

        Assert.AreEqual(0, screen.CursorRow);
        Assert.AreEqual(1, screen.CursorCol);
    }

    [TestMethod]
    public void MaskedInput_Renders_Masked_Text_When_RevealNever()
    {
        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Never);

        var root = new VStack { input };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "••");
        Assert.IsFalse(rendered.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MaskedInput_Renders_Revealed_Text_When_RevealAlways()
    {
        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Always);

        var root = new VStack { input };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "secret");
    }
}
