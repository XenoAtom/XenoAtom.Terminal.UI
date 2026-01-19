// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ColorSchemeRootLoopsGeneratorTests
{
    [TestMethod]
    public void RootLoops_Generate_Matches_RootLoopsDark()
    {
        var generated = ColorScheme.Generate(
            sugar: 7,
            colors: 9,
            sogginess: 4,
            flavor: RootLoopsFlavor.Classic,
            fruit: RootLoopsFruit.Plum,
            milk: 0.94,
            name: "Root Loops (Dark)");

        AssertSchemesEqual(ColorScheme.RootLoopsDark, generated);
    }

    [TestMethod]
    public void RootLoops_Generate_Matches_RootLoopsLight()
    {
        var generated = ColorScheme.Generate(
            sugar: 7,
            colors: 9,
            sogginess: 4,
            flavor: RootLoopsFlavor.Classic,
            fruit: RootLoopsFruit.Plum,
            milk: 3.0,
            name: "Root Loops (Light)");

        AssertSchemesEqual(ColorScheme.RootLoopsLight, generated);
    }

    private static void AssertSchemesEqual(ColorScheme expected, ColorScheme actual)
    {
        Assert.AreEqual(expected.Background, actual.Background);
        Assert.AreEqual(expected.Foreground, actual.Foreground);

        Assert.AreEqual(expected.Black, actual.Black);
        Assert.AreEqual(expected.Red, actual.Red);
        Assert.AreEqual(expected.Green, actual.Green);
        Assert.AreEqual(expected.Yellow, actual.Yellow);
        Assert.AreEqual(expected.Blue, actual.Blue);
        Assert.AreEqual(expected.Purple, actual.Purple);
        Assert.AreEqual(expected.Cyan, actual.Cyan);
        Assert.AreEqual(expected.White, actual.White);

        Assert.AreEqual(expected.BrightBlack, actual.BrightBlack);
        Assert.AreEqual(expected.BrightRed, actual.BrightRed);
        Assert.AreEqual(expected.BrightGreen, actual.BrightGreen);
        Assert.AreEqual(expected.BrightYellow, actual.BrightYellow);
        Assert.AreEqual(expected.BrightBlue, actual.BrightBlue);
        Assert.AreEqual(expected.BrightPurple, actual.BrightPurple);
        Assert.AreEqual(expected.BrightCyan, actual.BrightCyan);
        Assert.AreEqual(expected.BrightWhite, actual.BrightWhite);
    }
}
