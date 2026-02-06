// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class WindowLayerInteractionTests
{
    [TestMethod]
    public void Dialog_Can_Be_Dragged_With_Mouse()
    {
        var dialog = new Dialog
        {
            Title = "T",
            Width = 10,
            Height = 5,
            Content = new TextBlock("Hello"),
        };

        var root = new ZStack();
        root.Add(dialog);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        // Centered: left=5, top=2 for a 20x10 slot and 10x5 dialog.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 6, Y = 2 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = 9, Y = 4 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 9, Y = 4 });

        driver.TickUntil(() => dialog.Left == 8 && dialog.Top == 4);
    }

    [TestMethod]
    public void WindowLayer_Brings_Clicked_Window_To_Front()
    {
        var layer = new WindowLayer { Content = new TextBlock("Base") };

        var a = new Dialog { Title = "A", Width = 10, Height = 4, Left = 1, Top = 1, Content = new TextBlock("A") };
        var b = new Dialog { Title = "B", Width = 10, Height = 4, Left = 3, Top = 2, Content = new TextBlock("B") };
        layer.AddWindow(a);
        layer.AddWindow(b);

        using var driver = new TerminalAppTestDriver(layer, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        static Visual GetRootChild(WindowLayer layer, Visual visual)
        {
            var rootChild = visual;
            while (rootChild.Parent is not null && !ReferenceEquals(rootChild.Parent, layer))
            {
                rootChild = rootChild.Parent;
            }

            return rootChild;
        }

        var initialHit = layer.HitTest(4, 3);
        Assert.IsNotNull(initialHit);
        Assert.AreSame(b, GetRootChild(layer, initialHit));

        // Click within A only (not overlapped by B) to bring it to front.
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = 2, Y = 2 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = 2, Y = 2 });
        driver.Tick();

        var postClickHit = layer.HitTest(4, 3);
        Assert.IsNotNull(postClickHit);
        Assert.AreSame(a, GetRootChild(layer, postClickHit));
    }

    [TestMethod]
    public void WindowLayer_Brings_Back_Window_To_Front_When_Child_Handles_Click()
    {
        var aButton = new Button("A Button");
        aButton.Click(() => { });
        var bButton = new Button("B Button");
        bButton.Click(() => { });

        var layer = new WindowLayer();
        var a = new Dialog
        {
            Title = "A",
            Width = 20,
            Height = 6,
            Left = 1,
            Top = 1,
            Content = aButton,
        };
        var b = new Dialog
        {
            Title = "B",
            Width = 20,
            Height = 6,
            Left = 4,
            Top = 2,
            Content = bButton,
        };

        layer.AddWindow(a);
        layer.AddWindow(b);

        using var driver = new TerminalAppTestDriver(layer, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        static Visual GetRootChild(WindowLayer layer, Visual visual)
        {
            var rootChild = visual;
            while (rootChild.Parent is not null && !ReferenceEquals(rootChild.Parent, layer))
            {
                rootChild = rootChild.Parent;
            }

            return rootChild;
        }

        // Click on A's button area (covered by A window, not by B window).
        var clickX = aButton.Bounds.X + 1;
        var clickY = aButton.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = clickX, Y = clickY });
        driver.Tick();

        var hit = layer.HitTest(5, 3);
        Assert.IsNotNull(hit);
        Assert.AreSame(a, GetRootChild(layer, hit), "Expected window A to move to front after clicking its handled child.");
    }

    [TestMethod]
    public void ModalDialog_Blocks_Clicks_Behind()
    {
        var clicked = false;
        var button = new Button("Click");
        button.Click((_, _) => clicked = true);

        var content = new VStack();
        content.Add(button);

        var modal = new Dialog
        {
            Title = "Modal",
            IsModal = true,
            Width = 12,
            Height = 5,
            Left = 10,
            Top = 3,
            Content = new TextBlock("Modal"),
        };

        using var driver = new TerminalAppTestDriver(content, TerminalHostKind.Fullscreen, new TerminalSize(30, 12));
        driver.Tick();

        modal.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = button.Bounds.X + 1, Y = button.Bounds.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = button.Bounds.X + 1, Y = button.Bounds.Y });
        driver.Tick();

        Assert.IsFalse(clicked);

        modal.Close();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = button.Bounds.X + 1, Y = button.Bounds.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = button.Bounds.X + 1, Y = button.Bounds.Y });
        driver.TickUntil(() => clicked);
    }
}

