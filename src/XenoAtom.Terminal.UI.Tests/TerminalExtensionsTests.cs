// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

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
            new ProgressBar().Label("Work").Value(() => counter.Value / 3.0));

        session.Instance.Live(root, () =>
        {
            counter.Value++;
            if (counter.Value >= 3)
            {
                session.Instance.WriteMarkupLine("[green]Done[/]");
                return false;
            }

            return true;
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

        var root = new TextBox().Text("abc");
        session.Instance.Live(root, () => false, new TerminalLiveOptions(RemoveOnEnd: false));

        session.Instance.WriteLine("AFTER");

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        var rendered = screen.GetText().Split(Environment.NewLine, StringSplitOptions.None);

        var topLine = Array.FindIndex(rendered, line => line.Contains("TOP", StringComparison.Ordinal));
        var afterLine = Array.FindIndex(rendered, line => line.Contains("AFTER", StringComparison.Ordinal));

        Assert.IsTrue(topLine >= 0);
        Assert.IsTrue(afterLine >= 0);
        // TextBox renders 3 rows by default (border + content). With the initial TOP line, the live region
        // starts at row 1 and occupies rows 1..3, so output after Live() should start at row >= 4.
        Assert.IsTrue(afterLine >= 4, $"Expected output after Live() to appear after the live region. Screen:\n{screen.GetText()}");
    }

    [TestMethod]
    public void Live_Restores_Cursor_When_Removed()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(30, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        session.Instance.WriteLine("TOP");

        var root = new TextBox().Text("abc");
        session.Instance.Live(root, () => false, new TerminalLiveOptions(RemoveOnEnd: true));

        session.Instance.WriteLine("AFTER");

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(30, 10);
        screen.Apply(outText);
        var rendered = screen.GetText().Split(Environment.NewLine, StringSplitOptions.None);

        var topLine = Array.FindIndex(rendered, line => line.Contains("TOP", StringComparison.Ordinal));
        var afterLine = Array.FindIndex(rendered, line => line.Contains("AFTER", StringComparison.Ordinal));

        Assert.IsTrue(topLine >= 0);
        Assert.IsTrue(afterLine >= 0);
        Assert.IsTrue(afterLine == topLine + 1, "Expected output after Live(RemoveOnEnd=true) to continue where Live started.");
        Assert.IsFalse(screen.GetText().Contains("abc", StringComparison.Ordinal), "Expected the live region to be removed.");
    }
}
