// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BackdropStyleTests
{
    [TestMethod]
    public void Backdrop_Sets_Foreground_To_Avoid_Color_Leak_From_Underlay()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsLight with { Name = "Test" });
        var red = Color.Basic16(1);

        // Underlay: a colored foreground.
        var buffer = new CellBuffer(1, 1);
        buffer.Clear();
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithForeground(red));

        // Overlay: backdrop fill. Historically, Backdrop only set background, so the foreground would be preserved.
        // That caused text in popups/dialogs (which commonly inherit TextBlockStyle.Default) to pick up the underlay
        // foreground. Ensure the backdrop writes an explicit foreground (theme foreground or default).
        var backdrop = new Backdrop()
            .Style(theme)
            .Style(BackdropStyle.Default);

        backdrop.Measure(new Size(1, 1));
        backdrop.Arrange(new Rectangle(0, 0, 1, 1));

        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(backdrop, new object[] { buffer });

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.IsTrue(cells[0].TryGetForeground(out var fg), "Backdrop should write an explicit foreground.");
        Assert.AreNotEqual(red, fg, "Backdrop should not preserve the underlay foreground.");
    }

    [TestMethod]
    public void Backdrop_Provides_Baseline_Foreground_For_Nested_Text()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsLight with { Name = "Test" });
        var red = Color.Basic16(1);

        var buffer = new CellBuffer(1, 1);
        buffer.Clear();

        // Underlay: colored foreground.
        buffer.SetCell(0, 0, new Rune('X'), Style.None.WithForeground(red));

        var surfaceFill = new Backdrop()
            .Style(theme)
            .Style(BackdropStyle.ThemeBackground);

        surfaceFill.Measure(new Size(1, 1));
        surfaceFill.Arrange(new Rectangle(0, 0, 1, 1));
        typeof(Visual).GetMethod("RenderTree", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(surfaceFill, new object[] { buffer });

        // Nested text: inherits from the cell style in the buffer (TextBlockStyle.Default).
        // If the backdrop did not set a foreground, this would inherit the underlay's foreground.
        buffer.SetCell(0, 0, new Rune('A'), Style.None);

        var cells = (Style[])typeof(CellBuffer).GetField("_cells", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(buffer)!;
        Assert.IsTrue(cells[0].TryGetForeground(out var fg));
        Assert.AreNotEqual(red, fg);
    }
}

