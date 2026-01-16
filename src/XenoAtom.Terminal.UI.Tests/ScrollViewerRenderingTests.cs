// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerRenderingTests
{
    [TestMethod]
    public void ScrollViewer_Renders_Content_When_Inside_TabControl()
    {
        var demoTab = new ScrollViewer(new VStack(new TextBlock("Hello from ScrollViewer")).Spacing(1).HorizontalAlignment(HorizontalAlignment.Stretch))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var root = new TabControl(
            new TabPage("Demo", demoTab),
            new TabPage("Other", new TextBlock("Other")))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Hello from ScrollViewer");
    }

    [TestMethod]
    public void ScrollViewer_Renders_Content()
    {
        var content = new VStack
        {
            "Log line 0",
            "Log line 1",
            "Log line 2",
            "Log line 3",
            "Log line 4",
        };

        var root = new ScrollViewer(content);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Log line 0");
    }

    [TestMethod]
    public void ScrollViewer_Scroll_Updates_Rendered_Content()
    {
        var content = new VStack();
        for (var i = 0; i < 10; i++)
        {
            content.Add($"Item {i}");
        }

        var root = new ScrollViewer(content) { HorizontalAlignment = HorizontalAlignment.Stretch };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 1 });
        driver.Tick();

        var screen = new AnsiTestScreen(20, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 1");
        Assert.IsFalse(rendered.Contains("Item 0", StringComparison.Ordinal), "After scrolling down, Item 0 should no longer be visible in the viewport.");
    }

    [TestMethod]
    public void ScrollViewer_Renders_Content_When_Set_After_Initial_Render()
    {
        var root = new ScrollViewer((Visual?)null) { HorizontalAlignment = HorizontalAlignment.Stretch };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.App.Post(() => root.Content = new TextBlock("Late content"));
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "Late content");
    }

    [TestMethod]
    public void ScrollViewer_Renders_ScrollBars_When_Content_Overflows()
    {
        var items = new VStack();
        for (var i = 0; i < 200; i++)
        {
            items.Add($"Hello {i}");
        }

        var root = new ScrollViewer(items)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(30, 10));
        driver.Tick();

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsTrue(rendered.Contains('\u2591', StringComparison.Ordinal) || rendered.Contains('\u2588', StringComparison.Ordinal), "Expected ScrollViewer to render scrollbars when content overflows.");
    }
}
