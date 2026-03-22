// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Threading;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalLoopWaitTelemetryTests
{
    [TestMethod]
    public void WaitTelemetry_StartsWithConservativeYieldWindow()
    {
        var telemetry = new TerminalLoopWaitTelemetry("timeout", frequency: 1_000_000);

        var snapshot = telemetry.GetSnapshot();

        Assert.AreEqual("timeout", snapshot.BackendName);
        Assert.AreEqual(2_000, snapshot.YieldWindowTicks);
    }

    [TestMethod]
    public void WaitTelemetry_ConvergesTowardMinimumYieldWindow_WhenOvershootStaysLow()
    {
        var telemetry = new TerminalLoopWaitTelemetry("timeout", frequency: 1_000_000);

        for (var i = 0; i < 64; i++)
        {
            telemetry.RecordOvershoot(0);
        }

        var snapshot = telemetry.GetSnapshot();

        Assert.AreEqual(250, snapshot.YieldWindowTicks);
        Assert.AreEqual(0, snapshot.AverageOvershootTicks);
        Assert.AreEqual(0, snapshot.P95OvershootTicks);
    }

    [TestMethod]
    public void WaitTelemetry_ComputesAverageAndP95FromRecordedOvershootSamples()
    {
        var telemetry = new TerminalLoopWaitTelemetry("timeout", frequency: 1_000_000);

        for (var i = 1; i <= 20; i++)
        {
            telemetry.RecordOvershoot(i * 100);
        }

        var snapshot = telemetry.GetSnapshot();

        Assert.AreEqual(1_050, snapshot.AverageOvershootTicks);
        Assert.AreEqual(1_900, snapshot.P95OvershootTicks);
    }

    [TestMethod]
    public void DebugOverlayMetrics_TracksWakeDistributionAcrossRollingWindow()
    {
        var metrics = new DebugOverlayMetrics();

        metrics.RecordWake(TerminalLoopWakeReason.Input);
        metrics.RecordWake(TerminalLoopWakeReason.Render | TerminalLoopWakeReason.Animation);
        metrics.RecordWake(TerminalLoopWakeReason.Post | TerminalLoopWakeReason.AsyncUpdate);
        metrics.RecordWake(TerminalLoopWakeReason.Deadline | TerminalLoopWakeReason.Shutdown);

        Assert.AreEqual(1, metrics.WakeInputCount);
        Assert.AreEqual(1, metrics.WakeRenderCount);
        Assert.AreEqual(1, metrics.WakeAnimationCount);
        Assert.AreEqual(1, metrics.WakePostCount);
        Assert.AreEqual(1, metrics.WakeAsyncUpdateCount);
        Assert.AreEqual(1, metrics.WakeDeadlineCount);
        Assert.AreEqual(1, metrics.WakeShutdownCount);
    }
}
