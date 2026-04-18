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

    [TestMethod]
    public void VScrollBar_Clicking_Track_Jumps_To_Clicked_Position()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 90,
            Value = 0,
            ViewportSize = 10,
            MinHeight = 10,
            MaxHeight = 10,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 12));
        driver.Tick();

        var x = bar.Bounds.X;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Bottom - 1,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Bottom - 1,
        });
        driver.Tick();

        Assert.AreEqual(bar.Maximum, bar.Value, "Clicking near the bottom of the track should jump the thumb to the end.");

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Y,
        });
        driver.Tick();

        Assert.AreEqual(bar.Minimum, bar.Value, "Clicking near the top of the track should jump the thumb back to the start.");
    }

    [TestMethod]
    public void VScrollBar_Drag_Uses_Current_Range_When_Track_Changes()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 90,
            Value = 0,
            ViewportSize = 10,
            MinHeight = 10,
            MaxHeight = 10,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 12));
        driver.Tick();

        var x = bar.Bounds.X;
        var thumbY = bar.Bounds.Y;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = thumbY,
        });
        driver.Tick();

        bar.Maximum = 190;
        driver.Tick();

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Bottom - 1,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = bar.Bounds.Bottom - 1,
        });
        driver.Tick();

        Assert.AreEqual(bar.Maximum, bar.Value, "Dragging should honor the current range even if the scroll extent changes while the thumb is captured.");
    }

    [TestMethod]
    public void VScrollBar_Clicking_Bottom_Edge_Reaches_Maximum_When_Thumb_Is_Already_Snapped_To_End()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 231,
            Value = 224,
            ViewportSize = 7,
            MinHeight = 8,
            MaxHeight = 8,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 10));
        driver.Tick();

        var x = bar.Bounds.X;
        var y = bar.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = y,
        });
        driver.Tick();

        Assert.AreEqual(bar.Maximum, bar.Value, "Clicking the last track cell should still reach the true maximum even when the thumb is already visually snapped to the bottom.");
    }

    [TestMethod]
    public void VScrollBar_Changing_Maximum_While_Dragging_Does_Not_Read_Then_Write_Value_In_Tracking_Context()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 40,
            Value = 20,
            ViewportSize = 10,
            MinHeight = 10,
            MaxHeight = 10,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 12));
        driver.Tick();

        var x = bar.Bounds.X;
        var thumbY = bar.Bounds.Y + 4;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = thumbY,
        });
        driver.Tick();

        using (BindingManager.Current.StartTracking())
        {
            bar.Maximum = 90;
        }

        Assert.AreEqual(90, bar.Maximum);
        Assert.AreEqual(40, bar.Value, "Changing the range while the thumb is captured should recompute the dragged value without tripping dependency tracking.");
    }

    [TestMethod]
    public void VScrollBar_Changing_Maximum_And_Viewport_While_Dragging_Does_Not_Read_Then_Write_Viewport_In_Tracking_Context()
    {
        var bar = new VScrollBar
        {
            Minimum = 0,
            Maximum = 40,
            Value = 20,
            ViewportSize = 10,
            MinHeight = 10,
            MaxHeight = 10,
        };

        using var driver = new TerminalAppTestDriver(bar, TerminalHostKind.Fullscreen, new TerminalSize(5, 12));
        driver.Tick();

        var x = bar.Bounds.X;
        var thumbY = bar.Bounds.Y + 4;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = x,
            Y = thumbY,
        });
        driver.Tick();

        using (BindingManager.Current.StartTracking())
        {
            bar.Maximum = 90;
            bar.ViewportSize = 12;
        }

        Assert.AreEqual(90, bar.Maximum);
        Assert.AreEqual(12, bar.ViewportSize);
        Assert.IsTrue(bar.Value >= bar.Minimum && bar.Value <= bar.Maximum, "Changing the range and viewport while the thumb is captured should keep the dragged value valid without tripping dependency tracking.");
    }
}
