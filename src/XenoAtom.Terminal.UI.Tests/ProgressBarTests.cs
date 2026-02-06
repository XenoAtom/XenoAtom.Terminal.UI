// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ProgressBarTests
{
    [TestMethod]
    public void ProgressBar_Measure_Accounts_For_Frame_Width()
    {
        var progress = new ProgressBar();
        progress.Style(ProgressBarStyle.Bracketed);

        progress.Measure(new Size(100, 4));

        Assert.IsTrue(progress.DesiredSize.Width >= 12);
        Assert.AreEqual(1, progress.DesiredSize.Height);
    }

    [TestMethod]
    public void ProgressBar_Renders_Expected_Fill_And_Track()
    {
        var progress = new ProgressBar { Value = 0.5 };
        progress.Style(new ProgressBarStyle
        {
            Variant = ProgressBarVariant.Bracketed,
            ShowFrame = true,
            FrameLeftGlyph = new Rune('['),
            FrameRightGlyph = new Rune(']'),
            FillGlyph = new Rune('#'),
            TrackGlyph = new Rune('-'),
        });

        using var driver = new TerminalAppTestDriver(progress, TerminalHostKind.Fullscreen, new TerminalSize(12, 3));
        driver.Tick();

        var screen = new AnsiTestScreen(12, 3);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "[#####-----]");
    }
}
