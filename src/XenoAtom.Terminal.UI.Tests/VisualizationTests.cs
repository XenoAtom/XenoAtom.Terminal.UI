// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Newtonsoft.Json.Linq;
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
