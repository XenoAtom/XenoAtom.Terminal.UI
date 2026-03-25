// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using System.Reflection;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppDebugOverlayTests
{
    [TestMethod]
    public void DebugOverlay_OverlayOnlyFrames_DoNotReportSceneRepaint()
    {
        using var driver = new TerminalAppTestDriver(new TextBlock("Overlay"));
        driver.App.SetUpdateCallback(_ => TerminalLoopResult.Continue);
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        var metrics = driver.App.DebugOverlayMetrics;
        Assert.IsNotNull(metrics, "Expected the debug overlay to be enabled after F12.");
        Assert.IsTrue(metrics.OverlayVisible);
        Assert.IsTrue(metrics.OverlayComposited);
        Assert.IsTrue(metrics.HasOverlayRect);
        Assert.IsTrue(metrics.OverlayOnlyFrame, "Expected the overlay-only frame marker when only the overlay is updating.");
        Assert.IsFalse(metrics.SceneFullRepaint, "The overlay should not force a scene full repaint.");
        Assert.IsFalse(metrics.SceneHasDirtyRect, "Overlay-only frames should not invent scene dirty rectangles.");
        Assert.IsFalse(metrics.SceneHasRepaintRect, "Overlay-only frames should not invent scene repaint rectangles.");
        Assert.AreEqual(0L, metrics.RenderTreeTicks, "Overlay-only frames should not rerender the scene tree.");

        var firstFrameIndex = metrics.FrameIndex;
        driver.Tick();

        Assert.IsTrue(metrics.FrameIndex > firstFrameIndex, "Expected the debug overlay to render on the next tick even without other UI invalidation.");
        Assert.IsTrue(metrics.OverlayOnlyFrame);
        Assert.IsFalse(metrics.SceneFullRepaint);
        Assert.AreEqual(0L, metrics.RenderTreeTicks);
    }

    [TestMethod]
    public void DebugOverlay_VisibleSceneInvalidation_RemainsDirtyRectBased()
    {
        var button = new Button("Hover")
        {
            HorizontalAlignment = Align.End,
            VerticalAlignment = Align.End,
        };

        using var driver = new TerminalAppTestDriver(
            new ZStack(
                new TextBlock("Root")
                {
                    HorizontalAlignment = Align.Stretch,
                    VerticalAlignment = Align.Stretch,
                },
                button),
            TerminalHostKind.Fullscreen,
            new TerminalSize(40, 10));

        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        var metrics = driver.App.DebugOverlayMetrics;
        Assert.IsNotNull(metrics, "Expected the debug overlay to be enabled after F12.");

        typeof(TerminalApp)
            .GetMethod("AddRenderDirtyRect", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(driver.App, [button]);
        driver.App.RequestRender();
        driver.Tick();

        Assert.IsFalse(metrics.SceneFullRepaint, "The overlay should not convert local scene invalidation into a full repaint.");
        Assert.IsTrue(metrics.SceneHasDirtyRect, "Expected the targeted control to contribute a scene dirty rectangle.");
        Assert.IsTrue(metrics.SceneHasRepaintRect, "Expected the frame to report the scene repaint rectangle.");
        Assert.IsFalse(metrics.OverlayOnlyFrame, "A scene invalidation frame should not be reported as overlay-only.");

        var repaintRect = metrics.SceneRepaintRect;
        Assert.IsTrue(
            repaintRect.Width < 40 || repaintRect.Height < 10,
            $"Expected a local scene repaint. Actual rect: {repaintRect}");
    }
}
