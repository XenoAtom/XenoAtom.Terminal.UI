// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class LineChartTests
{
    [TestMethod]
    public void LineChart_Measure_Uses_Value_Count_And_Default_Height()
    {
        var chart = new LineChart();
        chart.Values.AddRange([1.0, 2.0, 3.0, 4.0, 5.0]);

        chart.Measure(new Size(20, 10));
        chart.Arrange(new Rectangle(0, 0, 20, 10));

        Assert.AreEqual(5, chart.DesiredSize.Width);
        Assert.AreEqual(4, chart.DesiredSize.Height);
    }

    [TestMethod]
    public void LineChart_Renders_Configured_Point_Glyph()
    {
        var chart = new LineChart();
        chart.Values.AddRange([0.0, 1.0, 0.0, 1.0, 0.0, 1.0]);
        chart.Style(new LineChartStyle { PointGlyph = new Rune('*') });

        using var driver = new TerminalAppTestDriver(chart, TerminalHostKind.Fullscreen, new TerminalSize(12, 6));
        driver.Tick();

        var screen = new AnsiTestScreen(12, 6);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "*");
    }
}
