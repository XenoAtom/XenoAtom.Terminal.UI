// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OverlaySurfaceTextStyleLeakTests
{
    [TestMethod]
    public void Modal_Dialog_Does_Not_Inherit_Dim_From_Selected_ListBox_Row()
    {
        var list = new ListBox<string>().Style(ListBoxStyle.Default with { SelectedUnfocused = Style.None | TextStyle.Dim });
        for (var i = 0; i < 10; i++)
        {
            list.Items.Add($"Row {i}");
        }

        using var driver = new TerminalAppTestDriver(list, TerminalHostKind.Fullscreen, new TerminalSize(40, 12));
        driver.Tick();
        list.SelectedIndex = 3;
        var text = new TextBlock("Dialog body");
        var dialog = new Dialog { Left = 10, Top = 2, Width = 24, Height = 7, Content = new VStack(text, new Button("OK")) };
        dialog.Show();
        driver.Tick();

        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(driver.App)!;
        Assert.AreEqual(3, text.Bounds.Y, "The dialog text must overlap the selected list row.");
        Assert.AreNotEqual((TextStyle)0, buffer.UnsafeCells[3 * buffer.Width].TextStyle & TextStyle.Dim, "The unfocused selected list row must be dimmed.");
        for (var y = dialog.Bounds.Y; y < dialog.Bounds.Bottom; y++)
        {
            for (var x = dialog.Bounds.X; x < dialog.Bounds.Right; x++)
            {
                Assert.AreEqual((TextStyle)0, buffer.UnsafeCells[y * buffer.Width + x].TextStyle & TextStyle.Dim, $"Dialog cell ({x}, {y}) should not inherit dim.");
            }
        }

        using var parser = new AnsiStyledTextParser();
        var foundBody = false;
        foreach (var run in parser.Parse(driver.Backend.GetOutText()))
        {
            if (run.Text.Contains("Dialog body", StringComparison.Ordinal))
            {
                foundBody = true;
                Assert.AreEqual(AnsiDecorations.None, run.Style.Decorations & AnsiDecorations.Dim, "The emitted ANSI must also reset the list row's dim style.");
            }
        }

        Assert.IsTrue(foundBody);
    }

    [TestMethod]
    public void Dialog_Surface_DoesNotInherit_Underline_From_Underlay()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });

        var buffer = new CellBuffer(20, 10);
        buffer.Clear(theme.BaseTextStyle());

        var underlayStyle = theme.BaseTextStyle().WithTextStyle(TextStyle.Underline);
        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                buffer.SetCell(x, y, new Rune('X'), underlayStyle);
            }
        }

        var dialog = new Dialog
        {
            Width = 12,
            Height = 6,
            Padding = new Thickness(0),
        }.Style(theme);

        dialog.Measure(new Size(buffer.Width, buffer.Height));
        dialog.Arrange(new Rectangle(0, 0, buffer.Width, buffer.Height));

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(dialog, new object[] { buffer });

        var rect = dialog.Bounds;
        Assert.IsTrue(rect.Width >= 3 && rect.Height >= 3, "Dialog is expected to render a surface area.");

        var xInside = rect.X + 1;
        var yInside = rect.Y + 1;
        var index = (yInside * buffer.Width) + xInside;
        var cell = buffer.UnsafeCells[index];

        Assert.AreEqual((TextStyle)0, cell.TextStyle & TextStyle.Underline, "Dialog surface should not inherit underline from underlay.");
    }

    [TestMethod]
    public void Popup_Surface_DoesNotInherit_Underline_From_Underlay()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });

        var buffer = new CellBuffer(20, 10);
        buffer.Clear(theme.BaseTextStyle());

        var underlayStyle = theme.BaseTextStyle().WithTextStyle(TextStyle.Underline);
        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                buffer.SetCell(x, y, new Rune('X'), underlayStyle);
            }
        }

        var popup = new Popup
        {
            AnchorRect = new Rectangle(0, 0, 1, 1),
            Content = new TextBlock("Hello"),
        }
        .Style(theme)
        .MatchAnchorWidth(false)
        .HorizontalPopupAlignment(Align.Start)
        .VerticalPopupAlignment(Align.Start)
        .Placement(PopupPlacement.Below);

        popup.Measure(new Size(buffer.Width, buffer.Height));
        popup.Arrange(new Rectangle(0, 0, buffer.Width, buffer.Height));

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(popup, new object[] { buffer });

        var popupRect = (Rectangle)typeof(Popup).GetField("_popupRect", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(popup)!;
        Assert.IsTrue(popupRect.Width > 0 && popupRect.Height > 0, "Popup is expected to render a surface area.");

        var index = (popupRect.Y * buffer.Width) + popupRect.X;
        var cell = buffer.UnsafeCells[index];

        Assert.AreEqual((TextStyle)0, cell.TextStyle & TextStyle.Underline, "Popup surface should not inherit underline from underlay.");
    }

    [TestMethod]
    public void Toast_Surface_DoesNotInherit_Underline_Or_Foreground_From_Underlay()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var underlayForeground = Color.Basic16(1);

        var buffer = new CellBuffer(40, 10);
        buffer.Clear(theme.BaseTextStyle());

        var underlayStyle = theme.BaseTextStyle()
            .WithForeground(underlayForeground)
            .WithTextStyle(TextStyle.Underline);
        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                buffer.SetCell(x, y, new Rune('X'), underlayStyle);
            }
        }

        var toast = new Toast
        {
            Severity = ToastSeverity.Success,
            Title = null,
            Content = new TextBlock("Button: Success clicked"),
        }.Style(theme);

        toast.Measure(new Size(buffer.Width, buffer.Height));
        toast.Arrange(new Rectangle(0, 0, buffer.Width, buffer.Height));
        var rect = toast.Bounds;
        Assert.IsTrue(rect.Width >= 3 && rect.Height >= 3, "Toast is expected to render a surface area.");

        toast.RenderTree(buffer);

        var xInside = rect.X + 1;
        var yInside = rect.Y + 1;
        var index = (yInside * buffer.Width) + xInside;
        var cell = buffer.UnsafeCells[index];

        Assert.AreEqual((TextStyle)0, cell.TextStyle & TextStyle.Underline, "Toast surface should not inherit underline from underlay.");
        Assert.IsTrue(cell.TryGetForeground(out var fg), "Toast surface should write an explicit foreground.");
        Assert.AreNotEqual(underlayForeground, fg, "Toast surface should not inherit the underlay foreground.");
    }
}
