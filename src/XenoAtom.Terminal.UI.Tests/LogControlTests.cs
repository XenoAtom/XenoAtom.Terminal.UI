// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class LogControlTests
{
    [TestMethod]
    public void LogControl_AutoScrolls_On_Append()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 20; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Line 19");
        Assert.IsFalse(rendered.Contains("Line 0", StringComparison.Ordinal), "The view should follow the tail by default.");
    }

    [TestMethod]
    public void LogControl_Can_Reset_FollowTail_Programmatically()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 20; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        // Scroll away from the tail (disables follow-tail).
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageUp });
        driver.Tick();
        Assert.IsFalse(log.FollowTail);

        log.AppendLine("AfterScroll");
        driver.Tick();

        // Still not at the tail.
        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        Assert.IsFalse(rendered.Contains("AfterScroll", StringComparison.Ordinal), "Appending while not following tail should not jump to the bottom.");

        // Programmatically re-enable follow-tail and pin to the bottom.
        log.ScrollToTail();
        driver.Tick();

        screen.Apply(driver.Backend.GetOutText());
        rendered = screen.GetText();
        StringAssert.Contains(rendered, "AfterScroll");
        Assert.IsTrue(log.FollowTail);
    }

    [TestMethod]
    public void LogControl_Trims_MaxCapacity()
    {
        var log = new LogControl
        {
            MaxCapacity = 5,
        };

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 10; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        Assert.AreEqual(5, log.Count);

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Line 9");
        Assert.IsFalse(rendered.Contains("Line 0", StringComparison.Ordinal), "Oldest entries should be trimmed when MaxCapacity is exceeded.");
    }

    [TestMethod]
    public void LogControl_Can_SelectAll_And_Copy()
    {
        var log = new LogControl();
        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 8));
        driver.Tick();

        log.AppendLine("First");
        log.AppendLine("Second");
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlA, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlC, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        Assert.AreEqual("First\nSecond", driver.Terminal.Clipboard.Text);
    }

    [TestMethod]
    public void LogControl_CtrlF_Opens_Search_Popup()
    {
        var log = new LogControl();
        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Search");
        StringAssert.Contains(rendered, "Case");
    }

    [TestMethod]
    public void LogControl_Closing_Search_Popup_Clears_SearchText()
    {
        var log = new LogControl();
        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(60, 10));
        driver.Tick();

        log.AppendLine("foo bar");
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Unknown, Char = TerminalChar.CtrlF, Modifiers = TerminalModifiers.Ctrl });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "foo" });
        driver.Tick();

        Assert.AreEqual("foo", log.SearchText);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.IsTrue(string.IsNullOrEmpty(log.SearchText), "Closing the search popup should clear match highlighting.");
    }

    [TestMethod]
    public void LogControl_Search_And_Navigate_Matches_Scrolls_To_Results()
    {
        var log = new LogControl();
        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(50, 6));
        driver.Tick();
        var screen = new AnsiTestScreen(50, 6);
        screen.Apply(driver.Backend.GetOutText());

        for (var i = 0; i < 30; i++)
        {
            var suffix = i is 3 or 20 ? " foo" : string.Empty;
            log.AppendLine($"Line {i}{suffix}");
        }

        driver.Tick();
        screen.Apply(driver.Backend.GetOutText());

        log.Search("foo");
        log.GoToNextMatch();
        driver.Tick();
        screen.Apply(driver.Backend.GetOutText());

        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Line 20 foo");

        log.GoToNextMatch();
        driver.Tick();

        screen.Apply(driver.Backend.GetOutText());
        rendered = screen.GetText();
        StringAssert.Contains(rendered, "Line 3 foo");
    }
}
