// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextBoxPasswordTests
{
    [TestMethod]
    public void TextBoxPassword_Renders_Caret_When_Focused()
    {
        var input = new TextBox()
            .Text("secret")
            .IsPassword(true)
            .Style(TextBoxStyle.Default with { PasswordMaskGlyph = new Rune('*') });

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
    public void TextBoxPassword_Renders_Masked_Text_When_RevealNever()
    {
        var input = new TextBox()
            .Text("secret")
            .IsPassword(true)
            .PasswordRevealMode(PasswordRevealMode.Never)
            .Style(TextBoxStyle.Default with { PasswordMaskGlyph = new Rune('*') });

        var root = new VStack { input };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "**");
        Assert.IsFalse(rendered.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public void TextBoxPassword_Renders_Revealed_Text_When_RevealAlways()
    {
        var input = new TextBox()
            .Text("secret")
            .IsPassword(true)
            .PasswordRevealMode(PasswordRevealMode.Always)
            .Style(TextBoxStyle.Default with { PasswordMaskGlyph = new Rune('*') });

        var root = new VStack { input };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "secret");
    }
}
