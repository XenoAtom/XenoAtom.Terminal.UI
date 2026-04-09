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
    public void LogControl_FollowTail_Property_Can_Disable_And_Reenable_At_Tail()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 20; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        log.FollowTail = false;
        driver.Tick();
        Assert.IsFalse(log.FollowTail);

        log.AppendLine("AfterDisable");
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();
        Assert.IsFalse(rendered.Contains("AfterDisable", StringComparison.Ordinal), "Disabling FollowTail at the tail should keep newly appended lines out of view.");

        log.FollowTail = true;
        driver.Tick();
        Assert.IsTrue(log.FollowTail);

        screen.Apply(driver.Backend.GetOutText());
        rendered = screen.GetText();
        StringAssert.Contains(rendered, "AfterDisable");

        log.AppendLine("AfterReenable");
        driver.Tick();

        screen.Apply(driver.Backend.GetOutText());
        rendered = screen.GetText();
        StringAssert.Contains(rendered, "AfterReenable");
    }

    [TestMethod]
    public void LogControl_PageDown_To_Tail_Reenables_FollowTail_After_User_Scroll()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 30; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageUp });
        driver.Tick();
        Assert.IsFalse(log.FollowTail);

        for (var i = 0; i < 20 && !log.FollowTail; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
            driver.Tick();
        }

        Assert.IsTrue(log.FollowTail, "Paging back to the last line should resume follow-tail.");

        log.AppendLine("AfterPageDownTail");
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "AfterPageDownTail");
    }

    [TestMethod]
    public void LogControl_MouseWheel_Down_To_Tail_Reenables_FollowTail_After_User_Scroll()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 30; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        var wheelX = log.Bounds.X + 1;
        var wheelY = log.Bounds.Y + 1;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            X = wheelX,
            Y = wheelY,
            WheelDelta = 1,
        });
        driver.Tick();
        Assert.IsFalse(log.FollowTail);

        for (var i = 0; i < 40 && !log.FollowTail; i++)
        {
            driver.Backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Wheel,
                Button = TerminalMouseButton.Wheel,
                X = wheelX,
                Y = wheelY,
                WheelDelta = -1,
            });
            driver.Tick();
        }

        Assert.IsTrue(log.FollowTail, "Scrolling back to the last line with the mouse wheel should resume follow-tail.");

        log.AppendLine("AfterWheelTail");
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "AfterWheelTail");
    }

    [TestMethod]
    public void LogControl_ScrollBar_To_Tail_Reenables_FollowTail_After_User_Scroll()
    {
        var log = new LogControl();

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 30; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageUp });
        driver.Tick();
        Assert.IsFalse(log.FollowTail);

        var bar = log.EnumerateVisualsDepthFirst().OfType<VScrollBar>().Single();
        var barX = bar.Bounds.X;
        var barY = bar.Bounds.Bottom - 1;
        Assert.AreEqual(nameof(VScrollBar), log.HitTest(barX, barY)?.GetType().Name);

        for (var i = 0; i < 20 && !log.FollowTail; i++)
        {
            driver.Backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Down,
                Button = TerminalMouseButton.Left,
                X = barX,
                Y = barY,
            });
            driver.Backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Up,
                Button = TerminalMouseButton.Left,
                X = barX,
                Y = barY,
            });
            driver.Tick();
        }

        Assert.IsTrue(log.FollowTail, "Moving the vertical scrollbar to the bottom should resume follow-tail.");

        log.AppendLine("AfterScrollBarTail");
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "AfterScrollBarTail");
    }

    [TestMethod]
    public void LogControl_PageDown_To_Tail_Does_Not_Reenable_Programmatically_Disabled_FollowTail()
    {
        var log = new LogControl
        {
            FollowTail = false,
        };

        using var driver = new TerminalAppTestDriver(log, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        for (var i = 0; i < 30; i++)
        {
            log.AppendLine($"Line {i}");
        }

        driver.Tick();

        for (var i = 0; i < 20 && !log.FollowTail; i++)
        {
            driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.PageDown });
            driver.Tick();
        }

        Assert.IsFalse(log.FollowTail, "Paging to the bottom should not override an explicit FollowTail = false.");

        log.AppendLine("AfterProgrammaticDisable");
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        Assert.IsFalse(screen.GetText().Contains("AfterProgrammaticDisable", StringComparison.Ordinal));
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
