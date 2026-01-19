// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FocusTraversalTests
{
    [TestMethod]
    public void Tab_Skips_Invisible_And_Disabled()
    {
        var a = new ProbeFocusable("A");
        var b = new ProbeFocusable("B") { IsVisible = false };
        var c = new ProbeFocusable("C") { IsEnabled = false };
        var d = new ProbeFocusable("D");

        var root = new VStack();
        root.Add(a);
        root.Add(b);
        root.Add(c);
        root.Add(d);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 10));
        driver.Tick();

        Assert.AreSame(a, driver.App.FocusedElement);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.TickUntil(() => ReferenceEquals(driver.App.FocusedElement, d));
    }

    private sealed class ProbeFocusable : Visual
    {
        public ProbeFocusable(string text)
        {
            Focusable = true;
            Text = text;
        }

        public string Text { get; }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;

        protected override void RenderOverride(CellBuffer buffer)
        {
            buffer.WriteText(Bounds.X, Bounds.Y, Text.AsSpan(), ReferenceEquals(App?.FocusedElement, this) ? (Style.None | TextStyle.Invert) : Style.None);
        }
    }
}

