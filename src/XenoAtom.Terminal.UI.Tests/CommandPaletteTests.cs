// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CommandPaletteTests
{
    [TestMethod]
    public void CommandPalette_Filters_Items_Based_On_Query()
    {
        var palette = new CommandPalette();

        var root = new VStack { palette };

        root.AddCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        root.AddCommand(new Command
        {
            Id = "cmd.build",
            LabelMarkup = "Build",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "op" });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Open");
        Assert.IsFalse(rendered.Contains("Build", StringComparison.Ordinal), "Filtered results should no longer contain non-matching entries.");
    }

    [TestMethod]
    public void CommandPalette_Invokes_Action_On_Activated_Item()
    {
        var invoked = false;

        var palette = new CommandPalette();

        var root = new VStack { palette };

        root.AddCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => invoked = true,
        });

        root.AddCommand(new Command
        {
            Id = "cmd.build",
            LabelMarkup = "Build",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "op" });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Tab });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => invoked);
    }

    [TestMethod]
    public void CommandPalette_Show_Can_Be_Called_When_Hosted_In_Popup_Template()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        palette.Show(); // should not throw even though the palette is wrapped by the popup template.
        palette.Close();
        driver.Tick();
    }
}
