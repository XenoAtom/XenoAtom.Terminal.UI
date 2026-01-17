// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerTextAreaInteractionTests
{
    [TestMethod]
    public void ScrollViewerTextArea_MouseWheel_Scrolls_Without_Focus()
    {
        var button = new Button("Top");
        var textArea = new TextArea(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")));

        var scrollViewer = new ScrollViewer(textArea)
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(button, scrollViewer).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        driver.App.Focus(button);

        var bounds = textArea.Bounds;
        Assert.IsGreaterThan(textArea.Scroll.ViewportHeight, textArea.Scroll.ExtentHeight, $"Expected scrollable content. extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight}");

        var wheelX = bounds.X + 1;
        var wheelY = bounds.Y + 2;
        var hit = root.HitTest(wheelX, wheelY)?.GetType().Name ?? "<null>";
        Assert.AreEqual(nameof(TextArea), hit);

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            X = wheelX,
            Y = wheelY,
            WheelDelta = -1,
        });

        driver.TickUntil(() => textArea.Scroll.OffsetY > 0);
    }

    [TestMethod]
    public void ScrollViewerTextArea_MouseWheel_Scrolls_When_Focused()
    {
        var textArea = new TextArea(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")));

        var scrollViewer = new ScrollViewer(textArea)
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(scrollViewer).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        driver.App.Focus(textArea);

        var bounds = textArea.Bounds;
        Assert.IsGreaterThan(textArea.Scroll.ViewportHeight, textArea.Scroll.ExtentHeight, $"Expected scrollable content. extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight}");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            X = bounds.X + 1,
            Y = bounds.Y + 2,
            WheelDelta = -1,
        });

        driver.TickUntil(() => textArea.Scroll.OffsetY > 0);
    }

    [TestMethod]
    public void ScrollViewerTextArea_ScrollBar_Click_Scrolls_Without_Focus()
    {
        var button = new Button("Top");
        var textArea = new TextArea(string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i:00}")));

        var scrollViewer = new ScrollViewer(textArea)
        {
            MinHeight = 8,
            MaxHeight = 8,
        };

        var root = new VStack(button, scrollViewer).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        driver.App.Focus(button);

        Assert.IsGreaterThan(textArea.Scroll.ViewportHeight, textArea.Scroll.ExtentHeight, $"Expected scrollable content. extent={textArea.Scroll.ExtentHeight} viewport={textArea.Scroll.ViewportHeight}");

        var scrollBounds = scrollViewer.Bounds;
        var barX = scrollBounds.X + scrollBounds.Width - 1;
        var barY = scrollBounds.Y + 2;
        var hit = root.HitTest(barX, barY)?.GetType().Name ?? "<null>";
        Assert.AreEqual(nameof(ScrollBar), hit);

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

        driver.TickUntil(() => textArea.Scroll.OffsetY > 0);
    }

    [TestMethod]
    public void TextArea_CtrlShiftHomeEnd_Selects_Entire_Document()
    {
        var textArea = new TextArea("Hello\nWorld\nAgain");
        var root = new VStack(textArea).Spacing(0);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        driver.App.Focus(textArea);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl | TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "X" });
        driver.TickUntil(() => (textArea.Text ?? string.Empty) == "X");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Home, Modifiers = TerminalModifiers.Ctrl });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.End, Modifiers = TerminalModifiers.Ctrl | TerminalModifiers.Shift });
        driver.Backend.PushEvent(new TerminalTextEvent { Text = "Y" });
        driver.TickUntil(() => (textArea.Text ?? string.Empty) == "Y");
    }
}
