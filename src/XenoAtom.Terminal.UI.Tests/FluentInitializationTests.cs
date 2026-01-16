// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class FluentInitializationTests
{
    [TestMethod]
    public void Fluent_Bindable_Extensions_Are_Applied_During_Initialization()
    {
        var textBox = new TextBox("Hello");
        var root = new VStack(textBox).Spacing(2);

        Assert.AreEqual("Hello", textBox.Text);
        Assert.AreEqual(2, root.Spacing);

        root.Measure(new Size(40, 8));
        root.Arrange(new Rectangle(0, 0, 40, 8));

        Assert.IsTrue(textBox.Bounds.Width > 0);
    }
}

