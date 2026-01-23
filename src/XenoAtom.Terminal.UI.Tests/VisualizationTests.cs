// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class VisualizationTests
{
    [TestMethod]
    public void Sparkline_Renders_Block_Glyphs()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 2));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new Sparkline().Minimum(0.0).Maximum(1.0).Values([0.0, 0.5, 1.0]));

        var screen = new AnsiTestScreen(10, 2);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        var expected = string.Concat((char)0x2581, (char)0x2585, (char)0x2588);
        StringAssert.Contains(rendered, expected);
    }

    [TestMethod]
    public void BarChart_Renders_Filled_Bars()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new BarChart()
            .Minimum(0.0)
            .Maximum(1.0)
            .ShowValues(false)
            .Items(
                new BarChartItem("A", 0.0),
                new BarChartItem("B", 1.0),
                new BarChartItem("C", 0.5))
            .MinHeight(3)
            .MaxHeight(3));

        var screen = new AnsiTestScreen(30, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsTrue(rendered.Contains(((char)0x2588).ToString(), StringComparison.Ordinal));
    }

    [TestMethod]
    public void BarChart_Renders_Value_Near_Bar_End()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(60, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new BarChart()
            .Minimum(0.0)
            .Maximum(200.0)
            .ShowValues(true)
            .ShowPercentages(false)
            .Items(
                new BarChartItem("Small", 123.0))
            .MinHeight(1)
            .MaxHeight(1));

        var screen = new AnsiTestScreen(60, 6);
        screen.Apply(backend.GetOutText());

        var lines = screen.GetText().Split('\n');
        var line = lines.FirstOrDefault(l => l.Contains("Small", StringComparison.Ordinal));
        Assert.IsNotNull(line);

        var valueIndex = line.IndexOf("123", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, valueIndex, "Expected value text to be rendered.");

        // The value should be near the bar end, not flush-right at the end of the chart.
        Assert.IsLessThan(45, valueIndex, $"Expected value to be near the bar, but it was at column {valueIndex}.");
    }

    [TestMethod]
    public void BarChart_Renders_Small_Value_Near_Filled_End()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(80, 8));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new BarChart()
            .Minimum(0.0)
            .Maximum(10.0)
            .ShowValues(true)
            .ShowPercentages(false)
            .Items(
                new BarChartItem("Alpha", 8.0),
                new BarChartItem("Beta", 5.0),
                new BarChartItem("Gamma", 2.0),
                new BarChartItem("Delta", 1.0))
            .MinHeight(4)
            .MaxHeight(4));

        var screen = new AnsiTestScreen(80, 8);
        screen.Apply(backend.GetOutText());

        var line = screen.GetText().Split('\n').FirstOrDefault(l => l.Contains("Delta", StringComparison.Ordinal));
        Assert.IsNotNull(line);

        var valueIndex = line.IndexOf("1", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, valueIndex, "Expected value text to be rendered.");

        // With a wide chart, the smallest bar should still render its value close to the bar,
        // not flush-right at the end of the chart.
        Assert.IsLessThan(30, valueIndex, $"Expected value to be near the filled end, but it was at column {valueIndex}.");
    }

    [TestMethod]
    public void LineChart_Renders_Points()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(10, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new LineChart
        {
            Minimum = 0.0,
            Maximum = 1.0,
            MinHeight = 4,
            MaxHeight = 4,
        }.Values([0.0, 1.0, 0.0]));

        var screen = new AnsiTestScreen(10, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, ((char)0x2022).ToString());
    }
}
