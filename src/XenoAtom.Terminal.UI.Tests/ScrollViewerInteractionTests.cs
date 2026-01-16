// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollViewerInteractionTests
{
    [TestMethod]
    public void ScrollViewer_Scrolls_On_Wheel()
    {
        var content = new VStack();
        for (var i = 0; i < 10; i++)
        {
            content.Add(new TextBlock($"Item {i}"));
        }

        var scroll = new ScrollViewer { Content = content };
        var root = new VStack { Spacing = 1 };
        root.Add(scroll);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 6));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Wheel, Button = TerminalMouseButton.Wheel, WheelDelta = -1, X = 1, Y = 0 });
        driver.TickUntil(() => scroll.VerticalOffset > 0);
    }
}

