// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using ImageControl = XenoAtom.Terminal.UI.Graphics.Image;

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
        RequestRender(driver.App);
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

    [TestMethod]
    public void DebugOverlay_RetainsLastSceneUpdate_AfterReturningToIdle()
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
        RequestRender(driver.App);
        driver.Tick();

        Assert.IsTrue(metrics.HasLastSceneUpdate, "Expected the scene update to be remembered.");
        Assert.IsTrue(metrics.LastSceneHasRepaintRect);
        Assert.IsTrue(metrics.LastSceneHasDirtyRect);

        var lastRepaintRect = metrics.LastSceneRepaintRect;
        var lastDirtyRect = metrics.LastSceneDirtyRect;
        var lastSceneTimestamp = metrics.LastSceneUpdateTimestamp;

        driver.Tick();

        Assert.IsFalse(metrics.SceneHasRepaintRect, "The follow-up idle frame should return to no current scene repaint.");
        Assert.IsFalse(metrics.SceneHasDirtyRect, "The follow-up idle frame should return to no current scene dirty rect.");
        Assert.IsTrue(metrics.OverlayOnlyFrame, "The follow-up idle frame should be overlay-only.");
        Assert.IsTrue(metrics.HasLastSceneUpdate, "Expected the last scene update to remain visible after the app returns to idle.");
        Assert.AreEqual(lastRepaintRect, metrics.LastSceneRepaintRect);
        Assert.AreEqual(lastDirtyRect, metrics.LastSceneDirtyRect);
        Assert.AreEqual(lastSceneTimestamp, metrics.LastSceneUpdateTimestamp);
    }

    [TestMethod]
    public void DebugOverlay_ReportsGraphicsPresenterDiagnostics()
    {
        var presenter = new DiagnosticsBufferedGraphicsPresenter();
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(120, 30),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.F12 });
        driver.Tick();

        var metrics = driver.App.DebugOverlayMetrics;
        Assert.IsNotNull(metrics, "Expected the debug overlay to be enabled after F12.");
        Assert.IsTrue(metrics.GraphicsPresenterConfigured);
        Assert.IsTrue(metrics.GraphicsPresenterBuffered);
        Assert.AreEqual("diagnostic-image", metrics.GraphicsPresenterName);
        Assert.AreEqual(1, metrics.GraphicsCommandCount);
        Assert.IsTrue(metrics.GraphicsHasPendingOutput);
        Assert.IsTrue(metrics.HasGraphicsPresenterDiagnostics);
        Assert.AreEqual(TerminalGraphicsProtocol.Sixel, metrics.GraphicsPresenterDiagnostics.Protocol);
        Assert.AreEqual(1, metrics.GraphicsPresenterDiagnostics.LastEncodedFrameCount);
        Assert.AreEqual(2048, metrics.GraphicsPresenterDiagnostics.LastPayloadByteCount);
        Assert.IsTrue(presenter.PresentCalls > 0);

        var output = driver.Backend.GetOutText();
        StringAssert.Contains(output, "Gfx: diagnostic-image");
        StringAssert.Contains(output, "GfxImg: Sixel");
        StringAssert.Contains(output, "payload 2.0KiB");
    }

    private static void RequestRender(TerminalApp app)
    {
        typeof(TerminalApp)
            .GetMethod("RequestRender", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(app, []);
    }

    private static TerminalImageSource CreateRedPixelSource()
        => TerminalImageSource.FromRgba32(new byte[] { 255, 0, 0, 255 }, 1, 1, "red-pixel");

    private sealed class DiagnosticsBufferedGraphicsPresenter : IBufferedTerminalGraphicsPresenter, ITerminalGraphicsPresenterDiagnostics
    {
        private int _lastCommandCount;

        public int PresentCalls { get; private set; }

        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public bool HasPendingOutput(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context)
        {
            _ = context;
            _lastCommandCount = current.Count;
            return current.Count > 0;
        }

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
        {
            _ = context;
            cancellationToken.ThrowIfCancellationRequested();
            _lastCommandCount = current.Count;
            PresentCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, AnsiWriter writer, CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = writer;
            cancellationToken.ThrowIfCancellationRequested();
            _lastCommandCount = current.Count;
            PresentCalls++;
            return ValueTask.CompletedTask;
        }

        public TerminalGraphicsPresenterDiagnostics GetDiagnosticsSnapshot()
        {
            return new TerminalGraphicsPresenterDiagnostics
            {
                Name = "diagnostic-image",
                Protocol = TerminalGraphicsProtocol.Sixel,
                PresentationCount = PresentCalls,
                LastCommandCount = _lastCommandCount,
                LastPresentationDuration = TimeSpan.FromMilliseconds(1.5),
                EncodedFrameCount = 4,
                LastEncodedFrameCount = 1,
                TotalEncodeDuration = TimeSpan.FromMilliseconds(8),
                AverageEncodeDuration = TimeSpan.FromMilliseconds(2),
                LastEncodeDuration = TimeSpan.FromMilliseconds(1.25),
                PayloadByteCount = 8192,
                LastPayloadByteCount = 2048,
                DroppedFrameCount = 3,
                LastDroppedFrameCount = 1,
                EffectiveFramesPerSecond = 42.0,
            };
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
