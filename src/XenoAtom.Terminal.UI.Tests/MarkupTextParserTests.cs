// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkupTextParserTests
{
    [TestMethod]
    public void MarkupTextParser_Extracts_PlainText_And_Runs()
    {
        var parser = new MarkupTextParser();

        var text = parser.Parse("[red]a[/]b", out var runs);

        Assert.AreEqual("ab", text);
        Assert.HasCount(2, runs);

        Assert.AreEqual(0, runs[0].Start);
        Assert.AreEqual(1, runs[0].Length);
        Assert.IsTrue(runs[0].Style.TryGetForeground(out var fg));
        Assert.AreEqual(Colors.TerminalRed, fg);

        Assert.AreEqual(1, runs[1].Start);
        Assert.AreEqual(1, runs[1].Length);
        Assert.IsTrue(runs[1].Style.TryGetForeground(out var fgDefault));
        Assert.AreEqual(ColorKind.Default, fgDefault.Kind);
        Assert.IsTrue(runs[1].Style.TryGetBackground(out var bgDefault));
        Assert.AreEqual(ColorKind.Default, bgDefault.Kind);
    }
}
