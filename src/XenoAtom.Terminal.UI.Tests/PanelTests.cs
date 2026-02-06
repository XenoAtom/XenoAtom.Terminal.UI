// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class PanelTests
{
    [TestMethod]
    public void Panel_Defaults_To_Stretch_Alignment()
    {
        var panel = new TestPanel();

        Assert.AreEqual(Align.Stretch, panel.HorizontalAlignment);
        Assert.AreEqual(Align.Stretch, panel.VerticalAlignment);
    }

    [TestMethod]
    public void Panel_Children_Collection_Tracks_Additions()
    {
        var panel = new TestPanel();
        var first = new TextBlock("A");
        var second = new TextBlock("B");

        panel.Children.Add(first);
        panel.Children.Add(second);

        Assert.AreEqual(2, panel.Children.Count);
        Assert.AreSame(first, panel.Children[0]);
        Assert.AreSame(second, panel.Children[1]);
    }

    private sealed class TestPanel : Panel
    {
    }
}
