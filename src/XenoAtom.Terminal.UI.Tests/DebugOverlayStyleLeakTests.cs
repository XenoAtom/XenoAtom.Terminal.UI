// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class DebugOverlayStyleLeakTests
{
    [TestMethod]
    public void DebugOverlay_Composes_On_Host_Output_Without_Polluting_RenderBuffer()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var red = Color.Basic16(1);

        var root = new ColoredUnderlay(red)
            .Style(theme);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 20));
        driver.Tick();

        // Enable debug overlay (F12).
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        var screen = new AnsiTestScreen(60, 20);
        screen.Apply(driver.Backend.GetOutText());
        Assert.AreEqual('+', screen.GetText()[0], "The composed frame sent to the host should include the debug overlay border.");

        var app = driver.App;
        var buffer = (CellBuffer)typeof(TerminalApp).GetField("_renderBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(app)!;

        // The retained render buffer must stay scene-only after the overlay frame completes.
        var index = 0;
        var scalar = buffer.UnsafeScalars[index];
        var cell = buffer.UnsafeCells[index];

        Assert.AreEqual((int)'X', scalar, "The retained render buffer should restore the underlying scene after overlay composition.");
        Assert.IsTrue(cell.TryGetForeground(out var fg), "The underlay scene cell should keep its foreground.");
        Assert.AreEqual(red, fg, "Overlay composition should not leave overlay styling in the retained scene buffer.");
    }

    [TestMethod]
    public void DebugOverlay_Hide_Restores_Underlay_When_Other_Dirty_Rect_Is_Pending()
    {
        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark with { Name = "Test" });
        var button = new Button("Hover")
        {
            HorizontalAlignment = Align.End,
            VerticalAlignment = Align.End,
        };

        var root = new ZStack(
            new ColoredUnderlay(Color.Basic16(1)),
            button)
            .Style(theme);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(40, 10));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Move,
            Button = TerminalMouseButton.None,
            X = button.Bounds.X,
            Y = button.Bounds.Y,
        });
        driver.Tick();

        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(driver.Backend.GetOutText());

        Assert.AreEqual('X', screen.GetText()[0], "Hiding the debug overlay should repaint the cells it previously covered.");
    }

    private sealed class ColoredUnderlay : Visual
    {
        private readonly Color _foreground;

        public ColoredUnderlay(Color foreground)
        {
            _foreground = foreground;
            HorizontalAlignment = Align.Stretch;
            VerticalAlignment = Align.Stretch;
        }

        protected override void RenderOverride(CellBuffer buffer)
        {
            var rect = Bounds;
            var style = Style.None.WithForeground(_foreground);

            for (var y = rect.Y; y < rect.Bottom; y++)
            {
                for (var x = rect.X; x < rect.Right; x++)
                {
                    buffer.SetCell(x, y, new Rune('X'), style);
                }
            }
        }
    }
}
