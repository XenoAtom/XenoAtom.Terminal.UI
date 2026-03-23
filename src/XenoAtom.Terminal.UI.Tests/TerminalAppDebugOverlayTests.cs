// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppDebugOverlayTests
{
    [TestMethod]
    public void DebugOverlay_ContinuesRendering_OnActiveTicksWithoutOtherInvalidation()
    {
        using var driver = new TerminalAppTestDriver(new TextBlock("Overlay"));
        driver.App.SetUpdateCallback(_ => TerminalLoopResult.Continue);

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        var metrics = driver.App.DebugOverlayMetrics;
        Assert.IsNotNull(metrics, "Expected the debug overlay to be enabled after F12.");

        var firstFrameIndex = metrics.FrameIndex;
        driver.Tick();

        Assert.IsTrue(metrics.FrameIndex > firstFrameIndex, "Expected the debug overlay to render on the next tick even without other UI invalidation.");
    }
}
