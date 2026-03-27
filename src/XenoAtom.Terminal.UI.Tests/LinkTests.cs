// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Layout;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class LinkTests
{
    [TestMethod]
    public void Link_Reports_Horizontal_Shrink_Budget()
    {
        var link = new Link("https://example.com", "HelloWorld")
        {
            Trimming = TextTrimming.EndEllipsis,
        };

        link.Measure(LayoutConstraints.Unbounded);

        Assert.AreEqual(1, link.MeasureHints.Min.Width);
        Assert.AreEqual(10, link.MeasureHints.Natural.Width);
        Assert.AreEqual(10, link.MeasureHints.Max.Width);
        Assert.AreEqual(1, link.MeasureHints.FlexShrinkX);
    }

    [TestMethod]
    public void Write_Emits_Osc8_When_Supported()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 5));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new Link("https://example.com", "Example"));

        var output = backend.GetOutText();
        StringAssert.Contains(output, "\x1b]8;;https://example.com\x1b\\");
        StringAssert.Contains(output, "\x1b]8;;\x1b\\");
        StringAssert.Contains(output, "Example");
    }
}

