// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalExtensionsTests
{
    [TestMethod]
    public void Write_Renders_Visual_To_Terminal()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.Write(new TextBlock("Hello"));

        StringAssert.Contains(backend.GetOutText(), "Hello");
    }

    [TestMethod]
    public void Live_Runs_Until_Callback_Returns_False()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var counter = new State<int>(0);
        var root = new VStack(
            new TextBlock().Text(() => $"Count: {counter.Value}"),
            new ProgressBar().Value(() => counter.Value / 3.0));

        session.Instance.Live(root, () =>
        {
            counter.Value++;
            if (counter.Value >= 3)
            {
                session.Instance.WriteMarkupLine("[green]Done[/]");
                return TerminalLoopResult.StopAndKeepVisual;
            }

            return TerminalLoopResult.Continue;
        });

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Done");

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        StringAssert.Contains(screen.GetText(), "Count: 3");
    }

    [TestMethod]
    public void Live_Places_Cursor_After_Region_When_Kept()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.WriteLine("TOP");

        var root = new TextBox("abc");
        session.Instance.Live(root, () => TerminalLoopResult.StopAndKeepVisual);

        session.Instance.WriteLine("AFTER");

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        var rendered = screen.GetText().Split(Environment.NewLine, StringSplitOptions.None);

        var topLine = Array.FindIndex(rendered, line => line.Contains("TOP", StringComparison.Ordinal));
        var afterLine = Array.FindIndex(rendered, line => line.Contains("AFTER", StringComparison.Ordinal));

        Assert.IsGreaterThanOrEqualTo(0, topLine);
        Assert.IsGreaterThanOrEqualTo(0, afterLine);
        // TextBox renders a single row by default (content only). With the initial TOP line, the live region
        // starts at row 1 and occupies row 1, so output after Live() should start at row >= 2.
        Assert.IsGreaterThanOrEqualTo(topLine + 2, afterLine, $"Expected output after Live() to appear after the live region. Screen:\n{screen.GetText()}");
    }

    [TestMethod]
    public void Live_Restores_Cursor_When_Removed()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.WriteLine("TOP");

        var root = new TextBox("abc");
        session.Instance.Live(root, () => TerminalLoopResult.Stop);

        session.Instance.WriteLine("AFTER");

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        var rendered = screen.GetText().Split(Environment.NewLine, StringSplitOptions.None);

        var topLine = Array.FindIndex(rendered, line => line.Contains("TOP", StringComparison.Ordinal));
        var afterLine = Array.FindIndex(rendered, line => line.Contains("AFTER", StringComparison.Ordinal));

        Assert.IsGreaterThanOrEqualTo(0, topLine);
        Assert.IsGreaterThanOrEqualTo(0, afterLine);
        Assert.AreEqual(topLine + 1, afterLine, "Expected output after Live(Stop) to continue where Live started.");
        Assert.IsFalse(screen.GetText().Contains("abc", StringComparison.Ordinal), "Expected the live region to be removed.");
    }

    [TestMethod]
    public async Task LiveAsync_Supports_Async_Update_Callback()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var counter = new State<int>(0);
        var root = new VStack(
            new TextBlock().Text(() => $"Count: {counter.Value}"),
            new ProgressBar().Value(() => counter.Value / 3.0));

        await session.Instance.LiveAsync(root, async _ =>
        {
            await Task.Yield();

            counter.Value++;
            if (counter.Value >= 3)
            {
                session.Instance.WriteMarkupLine("[green]Done[/]");
                return TerminalLoopResult.StopAndKeepVisual;
            }

            return TerminalLoopResult.Continue;
        });

        var outText = backend.GetOutText();
        StringAssert.Contains(outText, "Done");

        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        StringAssert.Contains(screen.GetText(), "Count: 3");
        StringAssert.Contains(screen.GetText(), "Done");
    }

    [TestMethod]
    public void Live_Options_UpdateWaitDuration_IsApplied()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tickCount = 0;
        var wait = TimeSpan.FromMilliseconds(35);
        var stopwatch = Stopwatch.StartNew();

        session.Instance.Live(
            new TextBlock("Wait test"),
            _ =>
            {
                tickCount++;
                return tickCount >= 2 ? TerminalLoopResult.Stop : TerminalLoopResult.Continue;
            },
            new TerminalLiveOptions { UpdateWaitDuration = wait });

        stopwatch.Stop();

        Assert.AreEqual(2, tickCount);
        var minimumExpected = wait - TimeSpan.FromMilliseconds(5);
        Assert.IsTrue(
            stopwatch.Elapsed >= minimumExpected,
            $"Expected configured wait duration to slow down loop ticks by roughly the configured wait. Elapsed: {stopwatch.Elapsed}.");
    }

    [TestMethod]
    public void Run_Options_UpdateWaitDuration_IsApplied()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var tickCount = 0;
        var wait = TimeSpan.FromMilliseconds(35);
        var stopwatch = Stopwatch.StartNew();

        session.Instance.Run(
            new TextBlock("Wait test"),
            _ =>
            {
                tickCount++;
                return tickCount >= 2 ? TerminalLoopResult.Stop : TerminalLoopResult.Continue;
            },
            new TerminalRunOptions { UpdateWaitDuration = wait });

        stopwatch.Stop();

        Assert.AreEqual(2, tickCount);
        var minimumExpected = wait - TimeSpan.FromMilliseconds(5);
        Assert.IsTrue(
            stopwatch.Elapsed >= minimumExpected,
            $"Expected configured wait duration to slow down loop ticks by roughly the configured wait. Elapsed: {stopwatch.Elapsed}.");
    }
}
