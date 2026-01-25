// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class CommandQueryTests
{
    [TestMethod]
    public void Collect_Local_Overrides_Global_And_Sorts_By_Importance()
    {
        var leaf = new TextBox("leaf");
        var parent = new VStack(leaf);

        parent.AddCommand(new Command
        {
            Id = "cmd.tertiary",
            LabelMarkup = "Tertiary",
            Presentation = CommandPresentation.CommandPalette,
            Importance = CommandImportance.Tertiary,
            Execute = _ => { },
        });

        parent.AddCommand(new Command
        {
            Id = "cmd.primary",
            LabelMarkup = "Primary",
            Presentation = CommandPresentation.CommandPalette,
            Importance = CommandImportance.Primary,
            Execute = _ => { },
        });

        parent.AddCommand(new Command
        {
            Id = "cmd.shared",
            LabelMarkup = "Local",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        using var driver = new TerminalAppTestDriver(parent);
        driver.App.Focus(leaf);

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.shared",
            LabelMarkup = "Global",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.secondary",
            LabelMarkup = "Secondary",
            Presentation = CommandPresentation.CommandPalette,
            Importance = CommandImportance.Secondary,
            Execute = _ => { },
        });

        var list = new List<CommandQuery.Entry>();
        CommandQuery.Collect(driver.App, leaf, CommandPresentation.CommandPalette, list);

        // Local commands come first; within local commands, Primary should be ordered before Tertiary.
        Assert.HasCount(4, list);
        Assert.AreEqual("cmd.primary", list[0].Command.Id);
        Assert.AreEqual("cmd.shared", list[1].Command.Id);
        Assert.AreEqual("cmd.tertiary", list[2].Command.Id);

        // Then global commands.
        Assert.AreEqual("cmd.secondary", list[3].Command.Id);

        // Local overrides global.
        Assert.AreEqual("Local", list[1].Command.LabelMarkup);
    }

    [TestMethod]
    public void Collect_Filters_By_Presentation_And_Visibility()
    {
        var leaf = new TextBox("leaf");
        var root = new VStack(leaf);

        leaf.AddCommand(new Command
        {
            Id = "cmd.palette.only",
            LabelMarkup = "Palette",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        leaf.AddCommand(new Command
        {
            Id = "cmd.menu.only",
            LabelMarkup = "Menu",
            Presentation = CommandPresentation.Menu,
            Execute = _ => { },
        });

        leaf.AddCommand(new Command
        {
            Id = "cmd.hidden",
            LabelMarkup = "Hidden",
            Presentation = CommandPresentation.CommandPalette,
            IsVisible = _ => false,
            Execute = _ => { },
        });

        using var driver = new TerminalAppTestDriver(root);
        driver.App.Focus(leaf);

        var list = new List<CommandQuery.Entry>();
        CommandQuery.Collect(driver.App, leaf, CommandPresentation.CommandPalette, list);

        Assert.HasCount(1, list);
        Assert.AreEqual("cmd.palette.only", list[0].Command.Id);
    }
}

