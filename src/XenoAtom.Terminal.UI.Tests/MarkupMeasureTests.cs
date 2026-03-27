// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkupMeasureTests
{
    [TestMethod]
    public void Markup_SingleLine_Reports_Horizontal_Shrink_Budget()
    {
        var markup = new Markup("[red]Hello[/][green]World[/]")
        {
            Wrap = false,
            Trimming = TextTrimming.EndEllipsis,
        };

        markup.Measure(LayoutConstraints.Unbounded);

        Assert.AreEqual(1, markup.MeasureHints.Min.Width);
        Assert.AreEqual(10, markup.MeasureHints.Natural.Width);
        Assert.AreEqual(10, markup.MeasureHints.Max.Width);
        Assert.AreEqual(1, markup.MeasureHints.FlexShrinkX);
    }

    [TestMethod]
    public void Markup_Wrap_Reports_Horizontal_Shrink_Budget()
    {
        var markup = new Markup("[green]Hello[/] world")
        {
            Wrap = true,
        };

        markup.Measure(new LayoutConstraints(0, 8, 0, 5));

        Assert.AreEqual(1, markup.MeasureHints.Min.Width);
        Assert.AreEqual(8, markup.MeasureHints.Natural.Width);
        Assert.AreEqual(2, markup.MeasureHints.Natural.Height);
        Assert.AreEqual(1, markup.MeasureHints.FlexShrinkX);
    }

    [TestMethod]
    public void Measure_Uses_Visible_Text_Width()
    {
        var markup = new Markup("[red]Hi[/] [bold]there[/]!");
        markup.Measure(new Size(80, 1));

        Assert.AreEqual(9, markup.DesiredSize.Width); // "Hi there!" => 9 chars
        Assert.AreEqual(1, markup.DesiredSize.Height);
    }

    [TestMethod]
    public void Measure_Clips_Width_When_Not_Wrapping()
    {
        var markup = new Markup("[red]Hello[/]");
        markup.Measure(new Size(3, 1));

        Assert.AreEqual(3, markup.DesiredSize.Width);
        Assert.AreEqual(1, markup.DesiredSize.Height);
    }

    [TestMethod]
    public void Measure_Wrap_Uses_Available_Width()
    {
        var markup = new Markup("[green]Hello world[/]").Wrap(true);
        markup.Measure(new Size(5, 10));

        Assert.AreEqual(5, markup.DesiredSize.Width);
        Assert.IsGreaterThanOrEqualTo(2, markup.DesiredSize.Height);
    }
}
