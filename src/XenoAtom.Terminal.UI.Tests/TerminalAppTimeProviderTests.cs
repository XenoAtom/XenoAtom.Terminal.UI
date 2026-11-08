// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Testing;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalAppTimeProviderTests
{
    // A frequency that differs from Stopwatch.Frequency on every platform, so a conversion that still uses Stopwatch.Frequency fails.
    private const long TestFrequency = 1000;

    [TestMethod]
    public void TimeProvider_DefaultsToSystem_WithTheStopwatchFrequency()
    {
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new TextBlock("x"), terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });

        Assert.AreSame(TimeProvider.System, app.TimeProvider);
        Assert.AreEqual(Stopwatch.Frequency, app.TimestampFrequency);
    }

    [TestMethod]
    public void TimeProvider_Null_IsRejected()
    {
        using var terminal = CreateTerminal();
        var options = new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen, TimeProvider = null! };

        Assert.ThrowsExactly<ArgumentException>(() => new TerminalApp(new TextBlock("x"), terminal, options));
    }

    [TestMethod]
    public void TimeProvider_FrequencyIsUsedByTheApp()
    {
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new TextBlock("x"), terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));

        Assert.AreEqual(TestFrequency, app.TimestampFrequency);
    }

    [TestMethod]
    public void Tooltip_OpensAfterItsDelay_InAppTime()
    {
        var button = new Button("OK");
        var tooltip = button.Tooltip("Tooltip text").ShowDelayMilliseconds(200);
        using var terminal = CreateTerminal(30, 10);
        var app = new TerminalApp(new VStack(tooltip), terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        var bounds = button.GetAbsoluteBounds();
        driver.MoveMouse(bounds.X + 1, bounds.Y);
        driver.Tick(TimeSpan.FromMilliseconds(100));
        Assert.DoesNotContain("Tooltip text", driver.Screen.GetText());

        driver.Tick(TimeSpan.FromMilliseconds(150));
        StringAssert.Contains(driver.Screen.GetText(), "Tooltip text");
    }

    [TestMethod]
    public void Toast_DismissesAfterItsDuration_InAppTime()
    {
        var host = new ToastHost(new VStack()) { DefaultDuration = TimeSpan.FromMilliseconds(300) };
        using var terminal = CreateTerminal(40, 8);
        var app = new TerminalApp(host, terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        app.Post(() => host.Show(new TextBlock("Message")));
        driver.Tick();
        Assert.HasCount(1, host.VisibleToasts);

        driver.Tick(TimeSpan.FromMilliseconds(200));
        Assert.HasCount(1, host.VisibleToasts);

        driver.Tick(TimeSpan.FromMilliseconds(200));
        Assert.IsEmpty(host.VisibleToasts);
    }

    [TestMethod]
    public void Spinner_AdvancesAtItsInterval_InAppTime()
    {
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = new TerminalApp(spinner, terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.AreEqual("a", driver.Screen[0, 0].Text);

        driver.Tick(TimeSpan.FromMilliseconds(50));
        Assert.AreEqual("a", driver.Screen[0, 0].Text);

        driver.Tick(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual("b", driver.Screen[0, 0].Text);
    }

    [TestMethod]
    public void FakeTimeProvider_AdvancedDirectly_DrivesTheApp()
    {
        var time = new FakeTimeProvider();
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = new TerminalApp(spinner, terminal, CreateOptions(time));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.AreEqual("a", driver.Screen[0, 0].Text);

        time.Advance(TimeSpan.FromMilliseconds(150));
        driver.Tick();
        Assert.AreEqual("b", driver.Screen[0, 0].Text);

        // Moving time through the driver and through the provider add up: 250 ms after the start is frame 2.
        driver.Tick(TimeSpan.FromMilliseconds(100));
        Assert.AreEqual("a", driver.Screen[0, 0].Text);
    }

    [TestMethod]
    public void Toast_ShownBeforeTheAppRuns_UsesTheAppClockFrequency()
    {
        var host = new ToastHost(new VStack()) { DefaultDuration = TimeSpan.FromMilliseconds(300) };
        host.Show(new TextBlock("Message"));
        using var terminal = CreateTerminal(40, 8);
        var app = new TerminalApp(host, terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();
        Assert.HasCount(1, host.VisibleToasts);

        driver.Tick(TimeSpan.FromMilliseconds(200));
        Assert.HasCount(1, host.VisibleToasts);

        driver.Tick(TimeSpan.FromMilliseconds(200));
        Assert.IsEmpty(host.VisibleToasts);
    }

    [TestMethod]
    public void Run_WithAFrozenFakeTimeProvider_KeepsProducingFrames()
    {
        var time = new FakeTimeProvider();
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new TextBlock("Hello"), terminal, CreateOptions(time));
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var frames = 0;
        var stopwatch = Stopwatch.StartNew();

        driver.Run(_ => ++frames < 10, cap.Token);

        stopwatch.Stop();
        Assert.AreEqual(10, frames, "The frozen clock must not stall the loop.");
        Assert.IsLessThan(TimeSpan.FromSeconds(10), stopwatch.Elapsed);
    }

    [TestMethod]
    public void Run_AdvancingTheFakeTimeProviderFromTheCallback_DrivesASpinner()
    {
        var time = new FakeTimeProvider();
        var spinner = new Spinner();
        spinner.Style(new SpinnerStyle("Test", TimeSpan.FromMilliseconds(100), "a", "b") { TextStyle = TextStyle.None });
        using var terminal = CreateTerminal(10, 3);
        var app = new TerminalApp(spinner, terminal, CreateOptions(time));
        using var driver = new TerminalAppDriver(app);
        using var cap = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var frames = 0;
        string? seen = null;

        driver.Run(d =>
        {
            frames++;
            seen = d.Screen[0, 0].Text;
            if (seen == "b" || frames > 500)
            {
                return false;
            }

            time.Advance(TimeSpan.FromMilliseconds(50));
            return true;
        }, cap.Token);

        Assert.AreEqual("b", seen);
    }

    [TestMethod]
    public void Run_AfterTimeWasMovedInSteppedMode_StillPacesFramesInRealTime()
    {
        // With the default provider the loop and wait clocks are the same Stopwatch: the moved time must not delay the waits.
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new TextBlock("Hello"), terminal, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick(TimeSpan.FromHours(1));
        driver.Stop();

        using var cap = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var frames = 0;
        var stopwatch = Stopwatch.StartNew();
        driver.Run(_ => ++frames < 10, cap.Token);

        Assert.AreEqual(10, frames);
        Assert.IsLessThan(TimeSpan.FromSeconds(10), stopwatch.Elapsed);
    }

    [TestMethod]
    public void MovedTime_IsKeptAcrossDrivers_AndInRunningMode()
    {
        var time = new FakeTimeProvider();
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new TextBlock("Hello"), terminal, CreateOptions(time));
        var first = new TerminalAppDriver(app);
        var start = first.Timestamp;
        first.Start();
        first.Tick(TimeSpan.FromSeconds(1));
        var moved = first.Timestamp;
        Assert.AreEqual(start + time.TimestampFrequency, moved);
        first.Dispose();

        using var second = new TerminalAppDriver(app);
        Assert.AreEqual(moved, second.Timestamp, "A second driver continues from the moved time.");

        using var cap = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        long seen = 0;
        second.Run(d =>
        {
            seen = d.Timestamp;
            return false;
        }, cap.Token);
        Assert.AreEqual(moved, seen, "Running mode continues from the moved time.");
    }

    [TestMethod]
    public void CommandSequence_TimesOut_InAppTime()
    {
        var probe = new SequenceProbe();
        using var terminal = CreateTerminal();
        var app = new TerminalApp(new VStack(probe), terminal, CreateOptions(new ManualTimeProvider(TestFrequency)));
        using var driver = new TerminalAppDriver(app);
        driver.Start();
        driver.Tick();

        driver.SendKey('k', TerminalModifiers.Ctrl);
        Assert.AreEqual(1, app.PendingCommandSequenceCount);

        driver.Tick(TimeSpan.FromSeconds(1));
        Assert.AreEqual(1, app.PendingCommandSequenceCount, "1 s is within the 1.5 s sequence timeout.");

        driver.Tick(TimeSpan.FromSeconds(1));
        Assert.AreEqual(0, app.PendingCommandSequenceCount, "2 s is past the 1.5 s sequence timeout.");
    }

    private static TerminalInstance CreateTerminal(int width = 20, int height = 5)
        => new(new InMemoryTerminalBackend(new TerminalSize(width, height)));

    private static TerminalAppOptions CreateOptions(TimeProvider timeProvider)
        => new() { HostKind = TerminalHostKind.Fullscreen, TimeProvider = timeProvider };

    private sealed class SequenceProbe : Visual
    {
        public SequenceProbe()
        {
            Focusable = true;
            AddCommand(new Command
            {
                Id = "probe",
                LabelMarkup = "Probe",
                Sequence = new XenoAtom.Terminal.UI.Input.KeySequence(
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlK, TerminalModifiers.Ctrl),
                    new XenoAtom.Terminal.UI.Input.KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl)),
                Execute = _ => { },
            });
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints) => SizeHints.Fixed(constraints.Clamp(new Size(10, 1)));
    }
}
