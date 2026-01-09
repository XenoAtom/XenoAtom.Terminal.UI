// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkupMeasureTests
{
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
        Assert.IsTrue(markup.DesiredSize.Height >= 2);
    }
}

