// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Reflection;
using XenoAtom.Terminal.Backends;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;

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
    public void CommandPalette_DefaultStyle_Shows_Command_Name_Before_Label()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            Name = "open",
            LabelMarkup = "Open File",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var rendered = GetRenderedText(driver, 80, 16);
        StringAssert.Contains(rendered, "/open - Open File");
    }

    [TestMethod]
    public void CommandPalette_DefaultStyle_Does_Not_Show_Name_Prefix_When_Command_Name_Is_Null()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        var palette = new CommandPalette();
        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            LabelMarkup = "Open File",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var rendered = GetRenderedText(driver, 80, 16);
        StringAssert.Contains(rendered, "Open File");
        Assert.IsFalse(rendered.Contains("/open - Open File", StringComparison.Ordinal), "Expected commands without a name to render only their label markup.");
    }

    [TestMethod]
    public void CommandPalette_Style_Can_Configure_Command_Name_Display()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with
        {
            CommandNamePrefix = ":",
            CommandNameSeparator = " => ",
        });

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            Name = "open",
            LabelMarkup = "Open File",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var rendered = GetRenderedText(driver, 80, 16);
        StringAssert.Contains(rendered, ":open => Open File");
    }

    [TestMethod]
    public void CommandPalette_Style_Can_Hide_Command_Name_Display()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with
        {
            ShowCommandName = false,
        });

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.open",
            Name = "open",
            LabelMarkup = "Open File",
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var rendered = GetRenderedText(driver, 80, 16);
        StringAssert.Contains(rendered, "Open File");
        Assert.IsFalse(rendered.Contains("/open - Open File", StringComparison.Ordinal), "Expected ShowCommandName = false to suppress the command name prefix.");
    }

    [TestMethod]
    public void CommandPalette_Keeps_MultiStroke_Shortcut_Visible_When_Description_Is_Long()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with
        {
            MinWidth = 50,
            MaxWidth = 50,
        });

        driver.App.AddGlobalCommand(new Command
        {
            Id = "cmd.long-description",
            LabelMarkup = "Long description command",
            DescriptionMarkup = "[dim]This description is intentionally long enough to exceed the command palette viewport and must not push the shortcut outside the visible row.[/]",
            Sequence = new KeySequence(
                new KeyGesture(TerminalChar.CtrlG, TerminalModifiers.Ctrl),
                new KeyGesture(TerminalChar.CtrlT, TerminalModifiers.Ctrl)),
            Presentation = CommandPresentation.CommandPalette,
            Execute = _ => { },
        });

        palette.Show();
        driver.Tick();

        var rendered = GetRenderedText(driver, 80, 16);
        StringAssert.Contains(rendered, "Ctrl+G Ctrl+T");
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
    public void CommandPalette_QueryText_Tracks_Search_Box_Input()
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
        driver.Tick();

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "op" });
        driver.Tick();

        Assert.AreEqual("op", palette.QueryText, "Expected typing in the search box to update the bindable QueryText property.");
    }

    [TestMethod]
    public void CommandPalette_Show_Clears_Query_By_Default()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette
        {
            QueryText = "op",
        };

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

        Assert.AreEqual(string.Empty, palette.QueryText, "Expected Show() to clear the previous query by default.");

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Open");
        StringAssert.Contains(rendered, "Build");
    }

    [TestMethod]
    public void CommandPalette_Show_Can_Preserve_Query_When_Configured()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette
        {
            ClearQueryOnShow = false,
            QueryText = "op",
        };

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

        Assert.AreEqual("op", palette.QueryText, "Expected Show() to preserve the query when ClearQueryOnShow is disabled.");

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Open");
        Assert.IsFalse(rendered.Contains("Build", StringComparison.Ordinal), "Expected the preserved query to remain active when the palette opens.");
    }

    [TestMethod]
    public void CommandPalette_QueryText_Can_Be_Set_Programmatically_While_Open()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var palette = new CommandPalette
        {
            ClearQueryOnShow = false,
        };

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

        palette.QueryText = "build";
        driver.Tick();

        var searchBox = GetPrivateField<TextBox>(palette, "_searchBox");
        Assert.AreEqual("build", palette.QueryText);
        Assert.AreEqual("build", searchBox.Text, "Expected programmatic QueryText updates to synchronize the search box content.");

        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(outText);
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Build");
        Assert.IsFalse(rendered.Contains("Open", StringComparison.Ordinal), "Expected programmatic QueryText updates to re-filter the palette immediately.");
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
    public void CommandPalette_Show_Can_Use_Viewport_Percentage_For_Width_And_Height()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(160, 40));
        driver.Tick();

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with
        {
            PopupWidthPercent = 50,
            PopupHeightPercent = 40,
            MinWidth = 20,
            MaxWidth = LayoutConstants.Infinite,
        });

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
        Assert.AreEqual(80, hostDialog.Bounds.Width, "Expected width percent to size the palette host from the viewport width.");
        Assert.AreEqual(16, hostDialog.Bounds.Height, "Expected height percent to size the palette host from the viewport height.");
        Assert.AreEqual(Math.Max(0, (160 - 80) / 2), hostDialog.Bounds.X, "Expected percentage sizing to preserve centered horizontal placement.");
        Assert.AreEqual(0, hostDialog.Bounds.Y, "Expected percentage sizing to preserve top alignment by default.");
    }

    [TestMethod]
    public void CommandPalette_Show_Percentage_Size_Still_Uses_Alignment_And_Offsets()
    {
        var root = new VStack();
        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(120, 30));
        driver.Tick();

        var palette = new CommandPalette().Style(CommandPaletteStyle.Default with
        {
            PopupWidthPercent = 50,
            PopupHeightPercent = 50,
            MinWidth = 20,
            MaxWidth = LayoutConstants.Infinite,
            PopupHorizontalAlignment = Align.End,
            PopupVerticalAlignment = Align.End,
            PopupOffsetX = -4,
            PopupOffsetY = -2,
        });

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
        Assert.AreEqual(60, hostDialog.Bounds.Width, "Expected width percent to use half of the viewport width.");
        Assert.AreEqual(15, hostDialog.Bounds.Height, "Expected height percent to use half of the viewport height.");
        Assert.AreEqual(56, hostDialog.Bounds.X, "Expected end alignment to position the palette from the right edge before applying offset.");
        Assert.AreEqual(13, hostDialog.Bounds.Y, "Expected end alignment to position the palette from the bottom edge before applying offset.");
    }

    [TestMethod]
    public void CommandPalette_Show_Applies_Distinct_Search_And_Selected_Row_Surfaces()
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
        var theme = hostDialog.GetTheme();
        var dialogSurface = hostDialog.GetStyle<DialogStyle>().ResolveSurfaceStyle(theme);
        Assert.IsTrue(dialogSurface.TryGetBackground(out var dialogBackground), "Expected the host dialog to define a popup background.");

        var searchBox = GetPrivateField<TextBox>(palette, "_searchBox");
        var searchBoxStyle = searchBox.GetStyle<TextBoxStyle>();
        Assert.IsNotNull(searchBoxStyle.Background, "Expected the palette search box to use an explicit fill.");
        Assert.AreNotEqual(dialogBackground, searchBoxStyle.Background!.Value, "Expected the search box fill to be visually distinct from the dialog surface.");

        var results = GetPrivateField<OptionList<ResolvedCommand>>(palette, "_results");
        var resultsStyle = results.GetStyle<OptionListStyle>();
        Assert.IsNotNull(resultsStyle.SelectedFocused, "Expected the palette to define a focused selected-row style.");
        var selectedFocused = resultsStyle.SelectedFocused!.Value;
        Assert.IsTrue(selectedFocused.TryGetBackground(out var selectedBackground), "Expected the selected result row to define a background.");
        Assert.AreNotEqual(dialogBackground, selectedBackground, "Expected the selected result row to be lifted from the dialog surface.");
    }

    [TestMethod]
    public void CommandPalette_Show_Uses_Current_Theme_For_Internal_Colors()
    {
        var theme = Theme.DefaultLight;
        var root = new VStack().Style(theme);
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

        var searchBox = GetPrivateField<TextBox>(palette, "_searchBox");
        var searchBoxStyle = searchBox.GetStyle<TextBoxStyle>();
        Assert.IsNotNull(searchBoxStyle.Background, "Expected the palette search box to use a theme-derived fill.");
        Assert.IsTrue(searchBoxStyle.Background.Value.GetRelativeLuminance() > 0.5f, "Expected a light-theme palette search box to use a light fill.");

        var results = GetPrivateField<OptionList<ResolvedCommand>>(palette, "_results");
        var resultsStyle = results.GetStyle<OptionListStyle>();
        Assert.IsNotNull(resultsStyle.SelectedFocused, "Expected the palette to define a focused selected-row style.");
        var selectedFocused = resultsStyle.SelectedFocused!.Value;
        Assert.IsTrue(selectedFocused.TryGetForeground(out var selectedForeground), "Expected the selected result row to define a foreground.");
        Assert.AreEqual(theme.Foreground, selectedForeground, "Expected the selected result foreground to come from the active light theme.");
        Assert.IsTrue(selectedFocused.TryGetBackground(out var selectedBackground), "Expected the selected result row to define a background.");
        Assert.IsTrue(selectedBackground.GetRelativeLuminance() > 0.4f, "Expected a light-theme selected row to use a light selection fill.");
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
    public void CommandPalette_Escape_Restores_Exact_Previous_Focus()
    {
        var first = new TextBox("First");
        var second = new TextBox("Second");
        var palette = new CommandPalette();
        var root = new VStack(first, second);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 16));
        driver.Tick();

        driver.App.Focus(second);
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

        Assert.IsInstanceOfType(driver.App.FocusedElement, typeof(TextBox), "Expected the palette search box to take focus.");

        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Escape });
        driver.Tick();

        Assert.AreSame(second, driver.App.FocusedElement);
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

    private static string GetRenderedText(TerminalAppTestDriver driver, int width, int height)
    {
        var outText = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(width, height);
        screen.Apply(outText);
        return screen.GetText();
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
        where T : class
        => (T)instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
}
