// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class RuleLayoutTests
{
    [TestMethod]
    public void Horizontal_Arranges_Start_Center_End_Labels()
    {
        var rule = new Rule
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            StartLabel = "A",
            CenterLabel = "B",
            EndLabel = "C",
        };

        rule.Measure(new Size(10, 1));
        rule.Arrange(new Rectangle(0, 0, 10, 1));

        Assert.IsNotNull(rule.StartLabel);
        Assert.IsNotNull(rule.CenterLabel);
        Assert.IsNotNull(rule.EndLabel);

        Assert.AreEqual(1, rule.StartLabel.Bounds.X);
        Assert.AreEqual(1, rule.StartLabel.Bounds.Width);

        Assert.AreEqual(4, rule.CenterLabel.Bounds.X);
        Assert.AreEqual(1, rule.CenterLabel.Bounds.Width);

        Assert.AreEqual(8, rule.EndLabel.Bounds.X);
        Assert.AreEqual(1, rule.EndLabel.Bounds.Width);
    }

    [TestMethod]
    public void Vertical_Defaults_To_One_Column()
    {
        var rule = new Rule { Orientation = Orientation.Vertical };
        rule.Measure(new Size(10, 5));
        Assert.AreEqual(1, rule.DesiredSize.Width);
        Assert.AreEqual(1, rule.DesiredSize.Height);
    }

    [TestMethod]
    public void Vertical_Expands_When_Label_Requires_More_Columns()
    {
        var rule = new Rule
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Stretch,
            CenterLabel = "ABC",
        };

        rule.Measure(new Size(10, 5));
        Assert.AreEqual(5, rule.DesiredSize.Width);
        Assert.AreEqual(1, rule.DesiredSize.Height);

        rule.Arrange(new Rectangle(0, 0, 5, 5));
        Assert.IsNotNull(rule.CenterLabel);
        Assert.AreEqual(1, rule.CenterLabel.Bounds.X);
        Assert.AreEqual(3, rule.CenterLabel.Bounds.Width);
        Assert.AreEqual(2, rule.CenterLabel.Bounds.Y);
    }
}
