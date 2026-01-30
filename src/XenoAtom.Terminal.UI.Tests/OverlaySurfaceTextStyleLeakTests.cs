// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class OverlaySurfaceTextStyleLeakTests
{
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
}
