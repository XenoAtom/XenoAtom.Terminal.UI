// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MaskedInputTests
{
    [TestMethod]
    public void MaskedInput_Renders_Template_With_Placeholders()
    {
        var input = new MaskedInput("99-99;_");

        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 4);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "__-__");
    }

    [TestMethod]
    public void MaskedInput_Uses_Digit_Placeholders_When_Template_Does_Not_Specify_One()
    {
        var input = new MaskedInput("99-99");

        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 4);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "00-00");
    }

    [TestMethod]
    public void MaskedInput_Filters_Invalid_Input()
    {
        var input = new MaskedInput("99-99;_");
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "a" });
        driver.Tick();

        Assert.AreEqual(string.Empty, input.Value);
    }

    [TestMethod]
    public void MaskedInput_Inserts_Text_Into_Slots_And_Skips_Separators()
    {
        var input = new MaskedInput("99-99;_");
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "1234" });
        driver.Tick();

        Assert.AreEqual("1234", input.Value);

        var screen = new AnsiTestScreen(20, 4);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "12-34");
    }

    [TestMethod]
    public void MaskedInput_Can_Select_And_Copy()
    {
        var input = new MaskedInput("99-99;_");
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Inline, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "1234" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Left, Modifiers = TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("4", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void MaskedInput_Applies_Case_Conversion_Directives()
    {
        var input = new MaskedInput(">AAA;_");
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "abc" });
        driver.Tick();

        Assert.AreEqual("ABC", input.Value);
    }

    [TestMethod]
    public void MaskedInput_Uses_Style_Default_Placeholder_Char_When_Not_Specified_By_Template()
    {
        var input = new MaskedInput("99-99")
            .Style(MaskedInputStyle.Default with { DefaultPlaceholderChar = '*', DigitPlaceholderChar = '*' });

        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 4));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 4);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "**-**");
    }

    [TestMethod]
    public void MaskedInput_When_Full_Can_Overwrite_Sequentially_From_Caret()
    {
        var input = new MaskedInput("9999-9999;_") { Value = "12345678" };
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 4));
        driver.Tick();

        input.CaretIndex = 0;
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "9" });
        driver.Tick();
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "8" });
        driver.Tick();

        Assert.AreEqual("98345678", input.Value);
    }

    [TestMethod]
    public void MaskedInput_When_Full_Does_Not_Jump_Caret_To_End_After_Overwrite()
    {
        var input = new MaskedInput("9999-9999;_") { Value = "12345678" };
        var root = new VStack { input };
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 4));
        driver.Tick();

        input.CaretIndex = 0;
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "9" });
        driver.Tick();

        Assert.IsTrue(input.CaretIndex < 8, $"Expected caret to stay in overwrite flow, but was {input.CaretIndex}.");
    }
}
