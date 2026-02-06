// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ScrollBarTests
{
    [TestMethod]
    public void VScrollBar_Responds_To_Keyboard_And_Raises_ValueChanged()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 10,
            Value = 5,
            SmallChange = 2,
            MinHeight = 6,
            MaxHeight = 6,
        };

        var oldValue = -1;
        var newValue = -1;
        bar.ValueChanged((_, e) =>
        {
            oldValue = e.OldValue;
            newValue = e.NewValue;
        });

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 8));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Up });
        driver.Tick();

        Assert.AreEqual(3, bar.Value);
        Assert.AreEqual(5, oldValue);
        Assert.AreEqual(3, newValue);
    }

    [TestMethod]
    public void VScrollBar_Responds_To_Wheel()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 10,
            Value = 5,
            SmallChange = 1,
            MinHeight = 6,
            MaxHeight = 6,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 8));
        driver.Tick();

        var x = bar.Bounds.X;
        var y = bar.Bounds.Y + 1;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            WheelDelta = -1,
            X = x,
            Y = y,
        });
        driver.Tick();

        Assert.AreEqual(6, bar.Value);
    }
}
