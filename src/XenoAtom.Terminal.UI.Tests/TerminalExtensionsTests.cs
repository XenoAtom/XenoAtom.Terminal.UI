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
}
