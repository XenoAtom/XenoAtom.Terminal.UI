// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ButtonMeasureTests
{
    [TestMethod]
    public void Button_Default_Measures_To_Single_Row_Height()
    {
        var button = new Button("OK");
        button.Measure(new Size(80, 25));
        Assert.AreEqual(1 + ButtonStyle.Default.Padding.Vertical, button.DesiredSize.Height);
    }

    [TestMethod]
    public void Button_With_Border_Measures_To_Minimum_Three_Rows()
    {
        var button = new Button("OK");
        button.SetEnvironmentValue(ButtonStyle.Key, new ButtonStyle { ShowBorder = true });
        button.Measure(new Size(80, 25));
        Assert.AreEqual(3, button.DesiredSize.Height);
    }
}

