// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class AccordionTests
{
    [TestMethod]
    public void SingleExpanded_Collapses_Other_Items()
    {
        var accordion = new Accordion
        {
            SingleExpanded = true,
        };

        var a = new Collapsible("A", "Content A");
        var b = new Collapsible("B", "Content B");

        accordion.Children.Add(a);
        accordion.Children.Add(b);

        a.IsExpanded = true;
        Assert.IsTrue(a.IsExpanded);
        Assert.IsFalse(b.IsExpanded);

        b.IsExpanded = true;
        Assert.IsFalse(a.IsExpanded);
        Assert.IsTrue(b.IsExpanded);
    }
}
