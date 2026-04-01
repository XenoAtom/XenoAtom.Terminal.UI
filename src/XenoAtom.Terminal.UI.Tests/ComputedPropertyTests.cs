// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ComputedPropertyTests
{
    [TestMethod]
    public void Visual_Func_Fluent_Applies_Initial_Value_And_Reapplies_On_State_Change()
    {
        var state = new State<int>(1);
        var visual = new TestVisualPhaseProbe().MinWidth(() => state.Value);
        var targetBinding = new Binding(visual, Visual.Accessor.MinWidth);
        var targetChanges = 0;

        void Handler(Binding binding)
        {
            if (binding.Equals(targetBinding))
            {
                targetChanges++;
            }
        }

        Assert.AreEqual(1, visual.MinWidth);
        Assert.IsTrue(visual.HasComputedProperty(Visual.Accessor.MinWidth));

        BindingManager.Current.ValueChanged += Handler;
        try
        {
            using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
            driver.Tick();

            state.Value = 3;
            driver.Tick();

            Assert.AreEqual(3, visual.MinWidth);
            Assert.AreEqual(1, targetChanges);
            Assert.IsTrue(visual.PrepareReadCount > 0);
            Assert.IsTrue(visual.MeasureReadCount > 0);
            Assert.IsTrue(visual.ArrangeReadCount > 0);
            Assert.IsTrue(visual.RenderReadCount > 0);
        }
        finally
        {
            BindingManager.Current.ValueChanged -= Handler;
        }
    }

    [TestMethod]
    public void Visual_Direct_Value_Fluent_Clears_Computed_Property()
    {
        var state = new State<int>(1);
        var visual = new TestVisualPhaseProbe().MinWidth(() => state.Value);

        visual.MinWidth(42);

        Assert.AreEqual(42, visual.MinWidth);
        Assert.IsFalse(visual.HasComputedProperty(Visual.Accessor.MinWidth));

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        state.Value = 10;
        driver.Tick();

        Assert.AreEqual(42, visual.MinWidth);
    }

    [TestMethod]
    public void Visual_Binding_Fluent_Clears_Computed_Property()
    {
        var computedState = new State<int>(1);
        var boundState = new State<int>(2);
        var visual = new TestVisualPhaseProbe().MinWidth(() => computedState.Value);

        visual.MinWidth((Binding<int>)boundState);

        Assert.AreEqual(2, visual.MinWidth);
        Assert.IsFalse(visual.HasComputedProperty(Visual.Accessor.MinWidth));

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        computedState.Value = 30;
        driver.Tick();
        Assert.AreEqual(2, visual.MinWidth);

        boundState.Value = 7;
        driver.Tick();
        Assert.AreEqual(7, visual.MinWidth);
    }

    [TestMethod]
    public void Visual_Func_Fluent_Replaces_Explicit_Binding()
    {
        var boundState = new State<int>(1);
        var computedState = new State<int>(2);
        var visual = new TestVisualPhaseProbe().MinWidth((Binding<int>)boundState);

        visual.MinWidth(() => computedState.Value);

        Assert.AreEqual(2, visual.MinWidth);
        Assert.IsTrue(visual.HasComputedProperty(Visual.Accessor.MinWidth));

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        boundState.Value = 9;
        driver.Tick();
        Assert.AreEqual(2, visual.MinWidth);

        computedState.Value = 7;
        driver.Tick();
        Assert.AreEqual(7, visual.MinWidth);
    }

    [TestMethod]
    public void Direct_Setter_Does_Not_Clear_Computed_Property()
    {
        var state = new State<int>(1);
        var visual = new TestVisualPhaseProbe().MinWidth(() => state.Value);

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        visual.MinWidth = 99;
        Assert.AreEqual(99, visual.MinWidth);
        Assert.IsTrue(visual.HasComputedProperty(Visual.Accessor.MinWidth));

        state.Value = 5;
        driver.Tick();

        Assert.AreEqual(5, visual.MinWidth);
        Assert.IsTrue(visual.HasComputedProperty(Visual.Accessor.MinWidth));
    }

    [TestMethod]
    public void User_Dynamic_Update_Sees_Fresh_Computed_Value()
    {
        var state = new State<int>(1);
        var seen = -1;
        var visual = new TestVisualPhaseProbe()
            .Update(v => seen = ((TestVisualPhaseProbe)v).MinWidth)
            .MinWidth(() => state.Value);

        using var driver = new TerminalAppTestDriver(new VStack { visual }, TerminalHostKind.Fullscreen, new TerminalSize(20, 5));
        driver.Tick();

        Assert.AreEqual(1, seen);

        state.Value = 8;
        driver.Tick();

        Assert.AreEqual(8, seen);
    }
}
