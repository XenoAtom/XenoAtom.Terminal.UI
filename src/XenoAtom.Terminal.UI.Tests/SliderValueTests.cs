// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SliderValueTests
{
    [TestMethod]
    public void Value_Is_Clamped_To_Range()
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 10,
            SnapToStep = false,
        };

        slider.Value = 100;
        Assert.AreEqual(10, slider.Value);

        slider.Value = -5;
        Assert.AreEqual(0, slider.Value);
    }

    [TestMethod]
    public void Value_Snaps_To_Step()
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 10,
            Step = 2,
            SnapToStep = true,
        };

        slider.Value = 3;
        Assert.AreEqual(4, slider.Value);
    }

    [TestMethod]
    public void Range_Is_Kept_Consistent()
    {
        var slider = new Slider();
        slider.Maximum = 0;
        slider.Minimum = 5;

        Assert.AreEqual(5, slider.Maximum);
        Assert.AreEqual(5, slider.Minimum);
        Assert.AreEqual(5, slider.Value);
    }
}

