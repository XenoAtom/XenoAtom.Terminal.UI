// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ProgressTaskGroupRenderingTests
{
    [TestMethod]
    public void ProgressTaskGroup_Renders_Label_And_Percentage()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var task = new ProgressTask("Work") { Value = 0.5 };

        var group = new ProgressTaskGroup();
        group.Tasks.Add(task);

        session.Instance.Write(group);

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(backend.GetOutText());

        var rendered = screen.GetText();
        StringAssert.Contains(rendered, "Work");
        StringAssert.Contains(rendered, " 50%");
    }
}

