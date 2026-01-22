// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SpinnerTests
{
    [TestMethod]
    public void Spinner_Animates_Without_User_Input()
    {
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(10), "a", "b")
        {
            TextStyle = TextStyle.None,
        });

        using var driver = new TerminalAppTestDriver(spinner, TerminalHostKind.Fullscreen, new TerminalSize(10, 3));
        driver.Tick(30);

        var outText = driver.Backend.GetOutText();
        StringAssert.Contains(outText, "a");
        StringAssert.Contains(outText, "b");
    }

    [TestMethod]
    public void SpinnerStyle_Rejects_Different_FrameWidths()
    {
        try
        {
            _ = new SpinnerStyle("Bad", TimeSpan.FromMilliseconds(10), "a", "ab");
            Assert.Fail("Expected an ArgumentException.");
        }
        catch (ArgumentException)
        {
            // Expected.
        }
    }
}
