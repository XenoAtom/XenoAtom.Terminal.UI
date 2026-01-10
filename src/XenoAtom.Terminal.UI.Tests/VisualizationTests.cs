// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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

        session.Instance.Write(new Sparkline
        {
            Values = new[] { 0.0, 0.5, 1.0 },
            Minimum = 0.0,
            Maximum = 1.0,
        });

        var screen = new AnsiTestScreen(10, 2);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        var expected = string.Concat((char)0x2581, (char)0x2585, (char)0x2588);
        StringAssert.Contains(rendered, expected);
    }

    [TestMethod]
    public void BarChart_Renders_Filled_Bars()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(6, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new BarChart
        {
            Values = new[] { 0.0, 1.0, 0.5 },
            Minimum = 0.0,
            Maximum = 1.0,
            Orientation = Orientation.Vertical,
            MinHeight = 4,
            MaxHeight = 4,
        });

        var screen = new AnsiTestScreen(6, 6);
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
            Values = new[] { 0.0, 1.0, 0.0 },
            Minimum = 0.0,
            Maximum = 1.0,
            MinHeight = 4,
            MaxHeight = 4,
        });

        var screen = new AnsiTestScreen(10, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, ((char)0x2022).ToString());
    }
}
