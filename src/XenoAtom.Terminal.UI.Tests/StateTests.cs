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
}
