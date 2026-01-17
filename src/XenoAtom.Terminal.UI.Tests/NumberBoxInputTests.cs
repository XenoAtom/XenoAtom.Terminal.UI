// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class NumberBoxInputTests
{
    [TestMethod]
    public void NumberBox_Updates_Bound_State_When_Typing_Valid_Number()
    {
        var state = new State<int>(8080);
        var numberBox = new NumberBox<int>().Value(state);
        var root = new VStack { numberBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.App.Focus(numberBox);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "42" });

        driver.TickUntil(() => state.Value == 42);
        Assert.AreEqual(42, state.Value);
    }

    [TestMethod]
    public void NumberBox_DoesNot_Update_Value_When_Text_Is_Not_A_Number()
    {
        var state = new State<int>(10);
        var numberBox = new NumberBox<int>()
            .Value(state)
            .InvalidNumberMessage("Not a number");
        var root = new VStack { numberBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.App.Focus(numberBox);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "abc" });

        driver.TickUntil(() => numberBox.Text == "abc");
        Assert.AreEqual(10, state.Value);

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Not a number");
    }

    [TestMethod]
    public void NumberBox_Uses_Custom_Value_Validator_Message()
    {
        var state = new State<int>(5);
        var numberBox = new NumberBox<int>
        {
            ValueValidator = v => v is >= 0 and <= 9 ? null : "Must be a single digit",
        }.Value(state);
        var root = new VStack { numberBox };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        driver.App.Focus(numberBox);
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "12" });

        driver.TickUntil(() => numberBox.Text == "12");
        Assert.AreEqual(5, state.Value, "Invalid input should not update the bound state.");

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Must be a single digit");
    }
}
