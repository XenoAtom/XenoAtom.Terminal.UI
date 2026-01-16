// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PointerHoverTests
{
    [TestMethod]
    public void Hover_Sets_IsHovered_On_HitTest_Target()
    {
        var button = new Button("OK");
        var root = new VStack { button };

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        var insideX = button.Bounds.X + 1;
        var insideY = button.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = insideX, Y = insideY });
        driver.TickUntil(() => button.IsHovered);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 19, Y = 9 });
        driver.TickUntil(() => !button.IsHovered);
    }
}

