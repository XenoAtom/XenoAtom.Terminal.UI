// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CursorProviderTests
{
    [TestMethod]
    public void CursorProvider_Drives_Terminal_Cursor_Position()
    {
        var probe = new CursorProbe(x: 6, y: 3);
        var root = new VStack(probe);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        var screen = new AnsiTestScreen(20, 5);
        screen.Apply(driver.Backend.GetOutText());

        Assert.AreEqual(3, screen.CursorRow);
        Assert.AreEqual(6, screen.CursorCol);
    }

    private sealed class CursorProbe : Visual, XenoAtom.Terminal.UI.Input.ICursorProvider
    {
        private readonly int _x;
        private readonly int _y;

        public CursorProbe(int x, int y)
        {
            Focusable = true;
            _x = x;
            _y = y;
        }

        public bool TryGetCursorCell(out int x, out int y)
        {
            x = _x;
            y = _y;
            return true;
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => SizeHints.Fixed(constraints.Clamp(new Size(1, 1)));

        protected override void ArrangeCore(in Rectangle finalRect) => Bounds = finalRect;
    }
}

