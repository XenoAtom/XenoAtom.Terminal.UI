// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Prompts;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalPromptsTests
{
    [TestMethod]
    public void TextPrompt_Completes_On_Enter()
    {
        var prompt = new TextPrompt("Name:")
        {
            Placeholder = "Type a value...",
        };

        var session = prompt.CreateSession();

        using var driver = new TerminalAppTestDriver(session.Root, TerminalHostKind.Inline);
        driver.App.SetUpdateCallback(session.Update);

        driver.Tick();
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Alex" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.IsTrue(session.IsCompleted);
        Assert.AreEqual("Alex", session.Result);
    }

    [TestMethod]
    public void TextPrompt_Cancels_On_Escape()
    {
        var prompt = new TextPrompt("Name:");
        var session = prompt.CreateSession();

        using var driver = new TerminalAppTestDriver(session.Root, TerminalHostKind.Inline);
        driver.App.SetUpdateCallback(session.Update);

        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.IsTrue(session.IsCanceled);
        Assert.IsFalse(session.IsCompleted);
    }

    [TestMethod]
    public void NumberPrompt_Completes_On_Enter_When_Number_Is_Valid()
    {
        var prompt = new NumberPrompt<int>("Port:")
        {
            InvalidNumberMessage = "Invalid port",
            Validator = v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]",
        };

        var session = prompt.CreateSession();

        using var driver = new TerminalAppTestDriver(session.Root, TerminalHostKind.Inline);
        driver.App.SetUpdateCallback(session.Update);

        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "42" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.Tick();

        Assert.IsTrue(session.IsCompleted);
        Assert.AreEqual(42, session.Result);
    }
}

