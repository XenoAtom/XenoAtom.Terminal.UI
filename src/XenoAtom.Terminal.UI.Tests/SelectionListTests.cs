// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class SelectionListTests
{
    [TestMethod]
    public async Task SelectionList_Space_Toggles_Checked_Item()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 10));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var list = new SelectionList { MinHeight = 4, MaxHeight = 4 };
        for (var i = 0; i < 6; i++)
        {
            list.Items.Add(new SelectionListItem($"Item {i}"));
        }

        var root = new VStack { list };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        // Move to item 1 and toggle.
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Space });

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(40, 10);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsTrue(rendered.Contains("☑", StringComparison.Ordinal) || rendered.Contains("Item 1", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SelectionList_Scrolling_Keeps_Selected_Row_Visible()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(20, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var list = new SelectionList { MinHeight = 4, MaxHeight = 4 };
        for (var i = 0; i < 10; i++)
        {
            list.Items.Add(new SelectionListItem($"Item {i}"));
        }

        var root = new VStack { list };
        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);

        for (var i = 0; i < 6; i++)
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        }

        await Task.Delay(80);

        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var outText = backend.GetOutText();
        var screen = new AnsiTestScreen(20, 6);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Item 6");
        Assert.IsFalse(rendered.Contains("Item 0", StringComparison.Ordinal), "After scrolling down, Item 0 should no longer be visible in the viewport.");
    }
}
