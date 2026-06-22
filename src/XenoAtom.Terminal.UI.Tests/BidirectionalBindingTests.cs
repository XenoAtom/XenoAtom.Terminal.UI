// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BidirectionalBindingTests
{
    [TestMethod]
    public void Binding_GetValue_Registers_Reads_In_Tracking()
    {
        var state = new State<int>(123);
        var binding = (Binding<int>)state;

        using var session = BindingManager.Current.StartTracking();
        _ = binding.GetValue();

        Assert.Contains((Binding)state, session.Reads);
    }

    [TestMethod]
    public void BindableProperty_Can_Bind_Bidirectionally_To_State()
    {
        var state = new State<string?>("hello");
        var textBox = new TextBox().Text(state);

        Assert.AreEqual("hello", textBox.Text);

        textBox.Text = "world";
        Assert.AreEqual("world", state.Value);

        state.Value = "again";
        Assert.AreEqual("again", textBox.Text);
    }

    [TestMethod]
    public void Bound_Getter_Is_Side_Effect_Free_During_Tracked_Reads()
    {
        var state = new State<int>(1);
        var visual = new TestVisualPhaseProbe().MinWidth((Binding<int>)state);

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        state.Value = 2;
        driver.Tick();

        var targetBinding = new Binding(visual, Visual.Accessor.MinWidth);
        var targetChanges = 0;

        void Handler(Binding binding)
        {
            if (binding.Equals(targetBinding))
            {
                targetChanges++;
            }
        }

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            using var session = BindingManager.Current.StartTracking();
            _ = visual.MinWidth;

            Assert.AreEqual(2, visual.MinWidth);
            Assert.AreEqual(0, targetChanges);
            Assert.AreEqual(0, session.Writes.Count);
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public void Chained_Bindings_Propagate_Before_Later_Reads()
    {
        var state = new State<int>(1);
        var middle = new TestVisualPhaseProbe().MinWidth((Binding<int>)state);
        var target = new TestVisualPhaseProbe().MinWidth(((Visual.IBindings)middle).MinWidth);

        using var driver = new TerminalAppTestDriver(new VStack { middle, target }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        state.Value = 5;
        driver.Tick();

        Assert.AreEqual(5, middle.MinWidth);
        Assert.AreEqual(5, target.MinWidth);
    }

    [TestMethod]
    public void Bound_Coerced_Target_Setter_Reapplies_Source_When_Source_NoOps()
    {
        var state = new State<int>(0);
        var scrollBar = new VScrollBar()
            .Minimum(0)
            .Maximum(0)
            .Value((Binding<int>)state);

        state.Value = 7;

        Assert.AreEqual(7, state.Value);
        Assert.AreEqual(0, scrollBar.Value, "The target clamps the source while its local range is still narrow.");

        scrollBar.Maximum = 10;
        scrollBar.Value = state.Value;

        Assert.AreEqual(7, state.Value);
        Assert.AreEqual(7, scrollBar.Value, "Setting a bound target to the source value should refresh a stale coerced local value even when the source no-ops.");
    }

    [TestMethod]
    public void Rebinding_Detaches_The_Previous_Source()
    {
        var stateA = new State<int>(1);
        var stateB = new State<int>(10);
        var visual = new TestVisualPhaseProbe().MinWidth((Binding<int>)stateA);

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        visual.MinWidth((Binding<int>)stateB);

        stateA.Value = 2;
        driver.Tick();
        Assert.AreEqual(10, visual.MinWidth);

        stateB.Value = 11;
        driver.Tick();
        Assert.AreEqual(11, visual.MinWidth);
    }

    [TestMethod]
    public void Unbinding_Leaves_Last_Local_Value_And_Stops_Propagation()
    {
        var state = new State<int>(1);
        var visual = new TestVisualPhaseProbe().MinWidth((Binding<int>)state);

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        state.Value = 4;
        driver.Tick();

        visual.BindMinWidth(default);

        state.Value = 6;
        driver.Tick();

        Assert.AreEqual(4, visual.MinWidth);
    }
}
