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
    public void Spinner_Bound_IsActive_Starts_Animating_When_Switched_To_True()
    {
        var isActive = new State<bool>(false);
        var spinner = new Spinner()
            .IsActive(isActive);
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(10), "a", "b")
        {
            TextStyle = TextStyle.None,
        });

        using var driver = new TerminalAppTestDriver(spinner, TerminalHostKind.Fullscreen, new TerminalSize(10, 3));
        driver.Tick(20);

        var before = driver.Backend.GetOutText();
        StringAssert.Contains(before, "a");
        Assert.IsFalse(before.Contains("b", StringComparison.Ordinal), "Inactive spinner should stay on the first frame.");

        isActive.Value = true;
        driver.Tick(40);

        var after = driver.Backend.GetOutText();
        StringAssert.Contains(after, "a");
        StringAssert.Contains(after, "b");
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
