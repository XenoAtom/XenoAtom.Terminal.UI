// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
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
    public void CommandPalette_Invokes_Action_On_Enter_From_Search()
    {
        var invoked = false;

        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => invoked = true,
        });

        palette.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => invoked);
    }

    [TestMethod]
    public void CommandPalette_Down_From_Search_Advances_To_Second_Item()
    {
        var firstInvoked = false;
        var secondInvoked = false;

        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => firstInvoked = true,
        });
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.build",
            LabelMarkup = "Build",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => secondInvoked = true,
        });

        palette.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Enter });
        driver.TickUntil(() => secondInvoked);

        Assert.IsFalse(firstInvoked, "Expected Down from the search box to advance past the default first selection.");
    }

    [TestMethod]
    public void CommandPalette_Show_Can_Be_Called_When_Hosted_In_Template_Wrapper()
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

    [TestMethod]
    public void CommandPalette_Show_Uses_Resizable_Dialog_Host()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
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
        driver.Tick();

        var hostDialog = GetHostDialog(palette);
        Assert.IsNotNull(hostDialog, "Expected the command palette to create a dialog-backed host window.");
        var initialWidth = hostDialog.Width;
        var initialHeight = hostDialog.Height;

        var handleX = hostDialog.Bounds.Right - 1;
        var handleY = hostDialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY + 2 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX + 4, Y = handleY + 2 });
        driver.Tick();

        Assert.IsTrue(initialWidth.HasValue && hostDialog.Width.HasValue && hostDialog.Width.Value > initialWidth.Value);
        Assert.IsTrue(initialHeight.HasValue && hostDialog.Height.HasValue && hostDialog.Height.Value > initialHeight.Value);
    }

    [TestMethod]
    public void CommandPalette_Resize_Stretches_Content_To_Fill_Dialog()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            DescriptionMarkup = "Open a file",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var hostDialog = GetHostDialog(palette);
        var searchBox = GetPrivateField<TextBox>(palette, "_searchBox");
        var resultsHost = GetPrivateField<ScrollViewer>(palette, "_resultsHost");
        var initialSearchWidth = searchBox.Bounds.Width;
        var initialResultsWidth = resultsHost.Bounds.Width;
        var initialResultsHeight = resultsHost.Bounds.Height;

        var handleX = hostDialog.Bounds.Right - 1;
        var handleY = hostDialog.Bounds.Bottom - 1;

        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Down, Button = TerminalMouseButton.Left, X = handleX, Y = handleY });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Drag, Button = TerminalMouseButton.Left, X = handleX + 8, Y = handleY + 3 });
        driver.Backend.PushEvent(new TerminalMouseEvent { Kind = TerminalMouseKind.Up, Button = TerminalMouseButton.Left, X = handleX + 8, Y = handleY + 3 });
        driver.Tick();

        var expectedContentWidth = hostDialog.Bounds.Width - 2 - hostDialog.Padding.Horizontal;
        var expectedContentHeight = hostDialog.Bounds.Height - 2 - hostDialog.Padding.Vertical;

        Assert.AreEqual(expectedContentWidth, palette.Bounds.Width, "Expected the palette to stretch to the dialog content width after resize.");
        Assert.AreEqual(expectedContentHeight, palette.Bounds.Height, "Expected the palette to stretch to the dialog content height after resize.");
        Assert.AreEqual(expectedContentWidth, searchBox.Bounds.Width, "Expected the search box to stretch with the resized dialog.");
        Assert.AreEqual(expectedContentWidth, resultsHost.Bounds.Width, "Expected the results host to stretch with the resized dialog.");
        Assert.IsTrue(searchBox.Bounds.Width > initialSearchWidth, "Expected the search box width to grow after resizing.");
        Assert.IsTrue(resultsHost.Bounds.Width > initialResultsWidth, "Expected the results host width to grow after resizing.");
        Assert.IsTrue(resultsHost.Bounds.Height > initialResultsHeight, "Expected the results host height to grow after resizing.");
    }

    [TestMethod]
    public void CommandPalette_Can_Reopen_After_Close()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
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
        driver.Tick();
        palette.Close();
        driver.Tick();

        palette.Show();
        driver.Tick();

        var hostDialog = GetHostDialog(palette);
        Assert.IsNotNull(hostDialog.Parent, "Expected the palette host dialog to be reattached after reopening.");
        Assert.IsInstanceOfType(driver.App.FocusedElement, typeof(TextBox));
    }

    [TestMethod]
    public void CommandPalette_Show_Defaults_To_Top_Centered_Alignment()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 20));
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
        driver.Tick();

        var hostDialog = GetHostDialog(palette);
        Assert.AreEqual(0, hostDialog.Bounds.Y, "Expected the palette to open aligned to the top of the viewport.");
        Assert.AreEqual(Math.Max(0, (80 - hostDialog.Bounds.Width) / 2), hostDialog.Bounds.X, "Expected the palette to open centered horizontally.");
    }

    [TestMethod]
    public void CommandPalette_Show_Is_Centered_Across_Complex_Root_Layout()
    {
        var root = new DockLayout()
            .Content(new HSplitter(new TextBlock("Browse"), new TextBlock("Page")).Ratio(0.16))
            .Bottom(new Footer().Left("Footer"));
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(160, 40));
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
        driver.Tick();

        var hostDialog = GetHostDialog(palette);
        Assert.AreEqual(160, driver.App.Root.Bounds.Width, $"Unexpected app root bounds: {driver.App.Root.Bounds}.");
        Assert.IsNotNull(hostDialog.Parent, "Expected dialog to be attached to the window layer.");
        Assert.AreEqual(160, hostDialog.Parent.Bounds.Width, $"Unexpected dialog parent bounds: {hostDialog.Parent.Bounds}.");
        Assert.AreEqual(hostDialog.Width, hostDialog.Bounds.Width, $"Unexpected dialog width/bounds mismatch. Width={hostDialog.Width} Bounds={hostDialog.Bounds}.");
        Assert.AreEqual(hostDialog.Left, hostDialog.Bounds.X, $"Unexpected dialog left/bounds mismatch. Left={hostDialog.Left} Bounds={hostDialog.Bounds}.");
        Assert.AreEqual(Math.Max(0, (160 - hostDialog.Bounds.Width) / 2), hostDialog.Bounds.X, "Expected centering to use the full window layer width.");
    }

    [TestMethod]
    public void CommandPalette_Show_Filters_Items_Based_On_Query()
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
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.build",
            LabelMarkup = "Build",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        Assert.IsInstanceOfType(driver.App.FocusedElement, typeof(TextBox), "Expected the command palette search box to be focused when shown.");

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "op" });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "op");
        StringAssert.Contains(rendered, "Open");
        Assert.IsFalse(rendered.Contains("Build", StringComparison.Ordinal), "Filtered results should no longer contain non-matching entries.");
    }

    [TestMethod]
    public void CommandPalette_Restores_Focus_On_Close()
    {
        var focusedBefore = new TextArea("Hello");

        var palette = new CommandPalette();
        var root = new VStack(focusedBefore);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        driver.App.Focus(focusedBefore);
        driver.Tick();

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        palette.Close();
        driver.Tick();

        Assert.AreSame(focusedBefore, driver.App.FocusedElement);
    }

    [TestMethod]
    public void CommandPalette_Ranks_Word_Boundary_Matches_Ahead_Of_Later_Matches()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette();

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.reset",
            LabelMarkup = "[dim]↺ Reset[/]",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.hardreset",
            LabelMarkup = "[dim]Hard reset[/]",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "reset" });
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        var resetIndex = rendered.IndexOf("Reset", StringComparison.Ordinal);
        var hardResetIndex = rendered.IndexOf("Hard reset", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, resetIndex, "Expected results to contain Reset.");
        Assert.IsGreaterThanOrEqualTo(0, hardResetIndex, "Expected results to contain Hard reset.");
        Assert.IsLessThan(hardResetIndex, resetIndex, "Expected Reset to be ranked above Hard reset.");
    }

    [TestMethod]
    public void CommandPalette_Shows_ScrollBar_When_Many_Items()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        for (var i = 0; i < 20; i++)
        {
            driver.App.AddGlobalCommand(new Command
            {
                Id = $"cmd.{i}",
                LabelMarkup = $"Command {i:00}",
                Presentation = CommandPresentation.CommandPalette,
                Execute = _ => { },
            });
        }

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with { ResultsHeight = 3 });
        palette.Show();
        driver.Tick();

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        Assert.IsTrue(rendered.Contains('░') || rendered.Contains('█'), "Expected a scroll bar to render for long result lists.");
    }

    private static Dialog GetHostDialog(CommandPalette palette)
        => (Dialog)typeof(CommandPalette).GetField("_hostDialog", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(palette)!;

    private static T GetPrivateField<T>(object instance, string fieldName)
        where T : class
        => (T)instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
}
