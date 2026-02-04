// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SplitterLayoutTests
{
    [TestMethod]
    public void HSplitter_Allocates_Space_With_Bar()
    {
        var first = new TextBlock("A").Stretch();
        var second = new TextBlock("B").Stretch();
        var splitter = new HSplitter(first, second)
        {
            Ratio = 0.5,
            BarSize = 1,
        };

        splitter.Measure(new Size(11, 3));
        splitter.Arrange(new Rectangle(0, 0, 11, 3));

        Assert.AreEqual(Align.Stretch, first.HorizontalAlignment);
        Assert.AreEqual(Align.Stretch, first.VerticalAlignment);

        Assert.AreEqual(5, first.Bounds.Width);
        Assert.AreEqual(3, first.Bounds.Height);
        Assert.AreEqual(5 + 1, second.Bounds.X);
        Assert.AreEqual(5, second.Bounds.Width);
    }

    [TestMethod]
    public void VSplitter_Allocates_Space_With_Bar()
    {
        var first = new TextBlock("A").Stretch();
        var second = new TextBlock("B").Stretch();
        var splitter = new VSplitter(first, second)
        {
            Ratio = 0.5,
            BarSize = 1,
        };

        splitter.Measure(new Size(10, 5));
        splitter.Arrange(new Rectangle(0, 0, 10, 5));

        Assert.AreEqual(Align.Stretch, first.VerticalAlignment);

        Assert.AreEqual(2, first.Bounds.Height);
        Assert.AreEqual(2 + 1, second.Bounds.Y);
        Assert.AreEqual(2, second.Bounds.Height);
    }
}
