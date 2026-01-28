// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

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
}
