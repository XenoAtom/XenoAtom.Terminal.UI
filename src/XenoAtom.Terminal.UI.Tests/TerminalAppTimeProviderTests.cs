// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
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

    private static TerminalInstance CreateTerminal(int width = 20, int height = 5)
        => new(new InMemoryTerminalBackend(new TerminalSize(width, height)));

    private static TerminalAppOptions CreateOptions(TimeProvider timeProvider)
        => new() { HostKind = TerminalHostKind.Fullscreen, TimeProvider = timeProvider };

    // A clock that never moves on its own (the driver moves the app's time) with a non-default frequency.
    private sealed class ManualTimeProvider(long frequency) : TimeProvider
    {
        public override long TimestampFrequency => frequency;

        public override long GetTimestamp() => 1_000_000;
    }
}
