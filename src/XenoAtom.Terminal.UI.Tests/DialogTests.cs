// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DialogTests
{
    [TestMethod]
    public void Dialog_Arranges_Centered_When_No_Position_Is_Set()
    {
        var dialog = new Dialog
        {
            Width = 10,
            Height = 4,
            Content = new TextBlock("Body"),
        };

        dialog.Measure(new Size(40, 20));
        dialog.Arrange(new Rectangle(0, 0, 40, 20));

        Assert.AreEqual(15, dialog.Bounds.X);
        Assert.AreEqual(8, dialog.Bounds.Y);
        Assert.AreEqual(10, dialog.Bounds.Width);
        Assert.AreEqual(4, dialog.Bounds.Height);
    }

    [TestMethod]
    public void Dialog_Dragging_Title_Updates_Left_And_Top()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 5,
            Title = new TextBlock("D"),
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var startX = dialog.Bounds.X + 1;
        var startY = dialog.Bounds.Y;
        var dragToX = startX + 4;
        var dragToY = startY + 2;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = startX, Y = startY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = dragToX, Y = dragToY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = dragToX, Y = dragToY });
        driver.Tick();

        Assert.IsTrue(dialog.Left.HasValue && dialog.Left.Value > 0);
        Assert.IsTrue(dialog.Top.HasValue && dialog.Top.Value > 0);
    }
}
