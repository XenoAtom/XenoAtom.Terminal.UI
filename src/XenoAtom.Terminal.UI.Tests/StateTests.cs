// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class StateTests
{
    [TestMethod]
    public void State_Registers_Reads_In_Tracking()
    {
        var state = new State<int>(123);

        using var session = BindingManager.Current.StartTracking();
        _ = state.Value;

        var dep = (Binding)state;
        Assert.Contains(dep, session.Reads, "Expected State.Value read to be tracked as a Binding dependency.");
    }

    [TestMethod]
    public void State_Notifies_On_Change()
    {
        var state = new State<string>("a");
        var expected = (Binding)state;

        var notified = 0;
        void Handler(Binding binding)
        {
            if (binding.Equals(expected))
            {
                notified++;
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            state.Value = "b";
            state.Value = "b";
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }

        Assert.AreEqual(1, notified);
    }

    [TestMethod]
    public void State_SuppressReadTracking_Skips_Tracked_Reads()
    {
        var state = new State<int>(123);
        var expected = (Binding)state;

        using var session = BindingManager.Current.StartTracking();
        using (BindingManager.Current.SuppressReadTracking())
        {
            _ = state.Value;
        }

        Assert.IsFalse(session.Reads.Contains(expected), "Expected suppressed read tracking to ignore State.Value reads.");
    }

    [TestMethod]
    public void State_SuppressWriteTracking_Nested_Suppresses_Notifications()
    {
        var state = new State<int>(0);
        var expected = (Binding)state;
        var notified = 0;

        void Handler(Binding binding)
        {
            if (binding.Equals(expected))
            {
                notified++;
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            using (BindingManager.Current.SuppressWriteTracking())
            {
                state.Value = 1;
                using (BindingManager.Current.SuppressWriteTracking())
                {
                    state.Value = 2;
                }

                state.Value = 3;
            }

            state.Value = 4;
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }

        Assert.AreEqual(1, notified, "Only the write outside suppression should raise notifications.");
    }
}
