// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BarChartTests
{
    [TestMethod]
    public void BarChart_TitlePlacement_Arranges_Title_Above_Or_Below()
    {
        var title = new TextBlock("Title");
        var chart = new BarChart
        {
            Title = title,
            TitlePlacement = ChartTitlePlacement.Above,
            Minimum = 0,
            Maximum = 10,
        };
        chart.Items.Add(new BarChartItem("A", 5));

        chart.Measure(new Size(30, 6));
        chart.Arrange(new Rectangle(0, 0, 30, 6));
        Assert.AreEqual(0, title.Bounds.Y);
        var aboveY = title.Bounds.Y;

        chart.TitlePlacement = ChartTitlePlacement.Below;
        chart.Measure(new Size(30, 7));
        chart.Arrange(new Rectangle(0, 0, 30, 7));
        Assert.IsTrue(title.Bounds.Y > aboveY, "Expected title to move lower when placement is Below.");
    }

    [TestMethod]
    public void BarChart_Renders_Default_Percentage_Value_Text()
    {
        var chart = new BarChart
        {
            Minimum = 0,
            Maximum = 10,
            ShowValues = false,
            ShowPercentages = true,
        };
        chart.Items.Add(new BarChartItem("Alpha", 5));

        using var driver = new TerminalAppTestDriver(chart, TerminalHostKind.Fullscreen, new TerminalSize(40, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Alpha");
        StringAssert.Contains(rendered, "50%");
    }
}
