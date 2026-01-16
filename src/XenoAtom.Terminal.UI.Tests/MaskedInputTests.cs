// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MaskedInputTests
{
    [TestMethod]
    public async Task MaskedInput_Renders_Caret_When_Focused()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Never);

        var root = new VStack { input };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        var foundCursor = false;
        var deadline = DateTime.UtcNow.AddSeconds(1);
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                var output = backend.GetOutText();
                var screen = new AnsiTestScreen(40, 6);
                screen.Apply(output);
                if (screen.CursorRow == 0 && screen.CursorCol == 1)
                {
                    foundCursor = true;
                    break;
                }

                await Task.Delay(20);
            }

            Assert.IsTrue(foundCursor, "Expected the focused input to drive the terminal cursor position.");
        }
        finally
        {
            backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
            await runTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [TestMethod]
    public async Task MaskedInput_Renders_Masked_Text_When_RevealNever()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Never);

        var root = new VStack { input };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "••");
        Assert.IsFalse(rendered.Contains("secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task MaskedInput_Renders_Revealed_Text_When_RevealAlways()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(40, 6));
        using var session = Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);

        var input = new MaskedInput()
            .Text("secret")
            .RevealMode(MaskedInputRevealMode.Always);

        var root = new VStack { input };

        var app = new TerminalApp(root, session.Instance, new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });
        var runTask = app.RunInBackgroundAsync();

        await Task.Delay(60);
        backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        await runTask.WaitAsync(TimeSpan.FromSeconds(2));

        var screen = new AnsiTestScreen(40, 6);
        screen.Apply(backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "secret");
    }
}
