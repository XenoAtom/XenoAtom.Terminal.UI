// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SparklineTests
{
    [TestMethod]
    public void Sparkline_Measure_Uses_Value_Count_And_One_Line()
    {
        var sparkline = new Sparkline();
        sparkline.Values.AddRange([1.0, 2.0, 3.0, 4.0]);

        sparkline.Measure(new Size(20, 10));
        sparkline.Arrange(new Rectangle(0, 0, 20, 10));

        Assert.AreEqual(4, sparkline.DesiredSize.Width);
        Assert.AreEqual(1, sparkline.DesiredSize.Height);
    }

    [TestMethod]
    public void Sparkline_Renders_With_Configured_Glyph_Set()
    {
        var sparkline = new Sparkline();
        sparkline.Values.AddRange([0.0, 0.3, 0.6, 1.0, 0.2, 0.8]);
        sparkline.Style(new SparklineStyle { Glyphs = SparklineGlyphs.Ascii8 });

        using var driver = new TerminalAppTestDriver(sparkline, TerminalHostKind.Fullscreen, new TerminalSize(16, 4));
        driver.Tick();

        var screen = new AnsiTestScreen(16, 4);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        Assert.IsTrue(rendered.IndexOfAny(['.', ':', '-', '=', '+', '*', '#']) >= 0, "Expected ASCII sparkline glyphs.");
    }
}
