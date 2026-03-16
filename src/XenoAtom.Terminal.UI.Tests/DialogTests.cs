// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

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
    public void Dialog_Arranges_Border_Labels_On_All_Sides()
    {
        var dialog = new Dialog
        {
            Width = 18,
            Height = 6,
            Title = new TextBlock("TL"),
            TopRightText = new TextBlock("TR"),
            BottomLeftText = new TextBlock("BL"),
            BottomRightText = new TextBlock("BR"),
            Content = new TextBlock("Body"),
        };

        dialog.Measure(new Size(40, 20));
        dialog.Arrange(new Rectangle(0, 0, 40, 20));

        Assert.AreEqual(dialog.Bounds.X + 2, dialog.Title!.Bounds.X);
        Assert.AreEqual(dialog.Bounds.Y, dialog.Title.Bounds.Y);

        Assert.AreEqual(dialog.Bounds.Right - 4, dialog.TopRightText!.Bounds.X);
        Assert.AreEqual(dialog.Bounds.Y, dialog.TopRightText.Bounds.Y);

        Assert.AreEqual(dialog.Bounds.X + 2, dialog.BottomLeftText!.Bounds.X);
        Assert.AreEqual(dialog.Bounds.Bottom - 1, dialog.BottomLeftText.Bounds.Y);

        Assert.AreEqual(dialog.Bounds.Right - 4, dialog.BottomRightText!.Bounds.X);
        Assert.AreEqual(dialog.Bounds.Bottom - 1, dialog.BottomRightText.Bounds.Y);
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

    [TestMethod]
    public void Dialog_Border_Controls_Can_Handle_Clicks_Without_Starting_Drag()
    {
        var titleClicked = false;
        var topRightClicked = false;

        var titleButton = new Button("L")
            .Style(ButtonStyle.Default with { Padding = Thickness.Zero });
        titleButton.Click(() => titleClicked = true);

        var topRightButton = new Button("R")
            .Style(ButtonStyle.Default with { Padding = Thickness.Zero });
        topRightButton.Click(() => topRightClicked = true);

        var dialog = new Dialog
        {
            Width = 16,
            Height = 6,
            Title = titleButton,
            TopRightText = topRightButton,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var initialLeft = dialog.Left;
        var initialTop = dialog.Top;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = titleButton.Bounds.X, Y = titleButton.Bounds.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = titleButton.Bounds.X, Y = titleButton.Bounds.Y });
        driver.TickUntil(() => titleClicked);

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = topRightButton.Bounds.X, Y = topRightButton.Bounds.Y });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = topRightButton.Bounds.X, Y = topRightButton.Bounds.Y });
        driver.TickUntil(() => topRightClicked);

        Assert.IsTrue(titleClicked);
        Assert.IsTrue(topRightClicked);
        Assert.AreEqual(initialLeft, dialog.Left);
        Assert.AreEqual(initialTop, dialog.Top);
    }

    [TestMethod]
    public void Dialog_Resizing_Right_Updates_Width()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.Right - 1;
        var handleY = dialog.Bounds.Y + 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY });
        driver.Tick();

        Assert.AreEqual(16, dialog.Width);
        Assert.AreEqual(14, dialog.Left);
    }

    [TestMethod]
    public void Dialog_Resizing_Left_Updates_Left_And_Width()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.X;
        var handleY = dialog.Bounds.Y + 2;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY });
        driver.Tick();

        Assert.AreEqual(8, dialog.Width);
        Assert.AreEqual(18, dialog.Left);
    }

    [TestMethod]
    public void Dialog_Resizing_Bottom_Updates_Height()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.X + 1;
        var handleY = dialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX, Y = handleY + 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX, Y = handleY + 3 });
        driver.Tick();

        Assert.AreEqual(9, dialog.Height);
        Assert.AreEqual(3, dialog.Top);
    }

    [TestMethod]
    public void Dialog_Resizing_BottomRight_Updates_Width_And_Height()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.Right - 1;
        var handleY = dialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY + 2 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY + 2 });
        driver.Tick();

        Assert.AreEqual(16, dialog.Width);
        Assert.AreEqual(8, dialog.Height);
    }

    [TestMethod]
    public void Dialog_Resizing_Clamps_To_MinWidth_And_MinHeight()
    {
        var dialog = new Dialog
        {
            Width = 12,
            Height = 8,
            MinWidth = 10,
            MinHeight = 6,
            Content = new TextBlock("Body"),
        };

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.Right - 1;
        var handleY = dialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX - 20, Y = handleY - 20 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX - 20, Y = handleY - 20 });
        driver.Tick();

        Assert.AreEqual(10, dialog.Width);
        Assert.AreEqual(6, dialog.Height);
    }

    [TestMethod]
    public void Dialog_Renders_Custom_Resize_Hover_Style()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.Right - 1;
        var handleY = dialog.Bounds.Y + 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = handleX, Y = handleY });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var cell = buffer.UnsafeCells[(handleY * buffer.Width) + handleX];

        Assert.IsTrue(cell.TryGetBackground(out var background));
        Assert.AreEqual(hoverBackground, background);
    }

    [TestMethod]
    public void Dialog_Renders_Custom_Resize_Hover_Style_On_Full_Bottom_Handle()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.X + 1;
        var handleY = dialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = handleX, Y = handleY });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var cell = buffer.UnsafeCells[(handleY * buffer.Width) + handleX];

        Assert.IsTrue(cell.TryGetBackground(out var background));
        Assert.AreEqual(hoverBackground, background);
    }

    [TestMethod]
    public void Dialog_Renders_Custom_Resize_Hover_Style_On_Move_Bar()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.X + 1;
        var handleY = dialog.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = handleX, Y = handleY });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var cell = buffer.UnsafeCells[(handleY * buffer.Width) + handleX];

        Assert.IsTrue(cell.TryGetBackground(out var background));
        Assert.AreEqual(hoverBackground, background);
    }

    [TestMethod]
    public void Dialog_Move_Hover_Does_Not_Overwrite_Top_Border_Labels()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 18,
            Height = 6,
            Title = new TextBlock("TL"),
            TopRightText = new TextBlock("TR"),
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var hoverX = dialog.Bounds.X + 7;
        var hoverY = dialog.Bounds.Y;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = hoverX, Y = hoverY });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var middleCell = buffer.UnsafeCells[(hoverY * buffer.Width) + hoverX];
        var leftLabelCell = buffer.UnsafeCells[(hoverY * buffer.Width) + (dialog.Bounds.X + 1)];
        var rightLabelCell = buffer.UnsafeCells[(hoverY * buffer.Width) + (dialog.Bounds.Right - 2)];

        Assert.IsTrue(middleCell.TryGetBackground(out var middleBackground));
        Assert.AreEqual(hoverBackground, middleBackground);
        Assert.IsFalse(leftLabelCell.TryGetBackground(out var leftBackground) && leftBackground == hoverBackground);
        Assert.IsFalse(rightLabelCell.TryGetBackground(out var rightBackground) && rightBackground == hoverBackground);
    }

    [TestMethod]
    public void Dialog_Bottom_Resize_Hover_Does_Not_Overwrite_Bottom_Border_Labels()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 18,
            Height = 6,
            BottomLeftText = new TextBlock("BL"),
            BottomRightText = new TextBlock("BR"),
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var hoverX = dialog.Bounds.X + 7;
        var hoverY = dialog.Bounds.Bottom - 1;
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = hoverX, Y = hoverY });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var middleCell = buffer.UnsafeCells[(hoverY * buffer.Width) + hoverX];
        var leftLabelCell = buffer.UnsafeCells[(hoverY * buffer.Width) + (dialog.Bounds.X + 1)];
        var rightLabelCell = buffer.UnsafeCells[(hoverY * buffer.Width) + (dialog.Bounds.Right - 2)];

        Assert.IsTrue(middleCell.TryGetBackground(out var middleBackground));
        Assert.AreEqual(hoverBackground, middleBackground);
        Assert.IsFalse(leftLabelCell.TryGetBackground(out var leftBackground) && leftBackground == hoverBackground);
        Assert.IsFalse(rightLabelCell.TryGetBackground(out var rightBackground) && rightBackground == hoverBackground);
    }

    [TestMethod]
    public void Dialog_Hover_Highlight_Clears_When_Pointer_Leaves()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var hoverBackground = Color.Rgb(255, 0, 0);

        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Content = new TextBlock("Body"),
        }
        .Style(theme)
        .Style(DialogStyle.Default with { ResizeHandleHoverStyle = Style.None.WithBackground(hoverBackground) });

        using var driver = new TerminalAppTestDriver(dialog, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();

        var handleX = dialog.Bounds.Right - 1;
        var handleY = dialog.Bounds.Y + 2;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = handleX, Y = handleY });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Move, Button = TerminalMouseButton.None, X = 0, Y = 0 });
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(driver.App)!;
        var cell = buffer.UnsafeCells[(handleY * buffer.Width) + handleX];

        Assert.IsFalse(cell.TryGetBackground(out var background) && background == hoverBackground);
    }
}
