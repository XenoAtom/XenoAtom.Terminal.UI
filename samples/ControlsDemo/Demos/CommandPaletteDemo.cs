using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CommandPalette", "Overlays", Description = "Searchable command launcher powered by the unified command system.")]
public sealed class CommandPaletteDemo : ControlsDemoBase
{
    public CommandPaletteDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var counter = new State<int>(0);
        var enabled = new State<bool>(true);
        var widthPercent = new State<int>(50);
        var horizontalAlignment = new State<Align>(Align.Center);
        var verticalAlignment = new State<Align>(Align.Start);
        var offsetX = new State<int>(0);
        var offsetY = new State<int>(0);
        var clearQueryOnShow = new State<bool>(true);
        var queryText = new State<string?>(string.Empty);
        var showCommandName = new State<bool>(true);
        var commandNamePrefix = new State<string?>("/");
        var commandNameSeparator = new State<string?>(" - ");
        var palette = new CommandPalette().Style(() => CommandPaletteStyle.Default with
        {
            PopupWidthPercent = Math.Clamp(widthPercent.Value, 1, 100),
            MinWidth = 30,
            MaxWidth = 120,
            PopupHorizontalAlignment = horizontalAlignment.Value,
            PopupVerticalAlignment = verticalAlignment.Value,
            PopupOffsetX = offsetX.Value,
            PopupOffsetY = offsetY.Value,
            ShowCommandName = showCommandName.Value,
            CommandNamePrefix = commandNamePrefix.Value ?? string.Empty,
            CommandNameSeparator = commandNameSeparator.Value ?? string.Empty,
        })
            .QueryText(queryText)
            .ClearQueryOnShow(clearQueryOnShow);

        void ShowPalette()
        {
            palette.Show();
        }

        var focusProbe = new TextBox().Placeholder("Enter some text here to enable CTRL+P to be accessible");

        var host = new VStack(
            DemoUi.Title("Command palette"),
                new TextBlock("Press Ctrl+P to open the command palette. Type to search, press Enter to run the top match, use arrows to navigate, or resize the window with the mouse. Some commands intentionally define a typed name so you can preview the optional name display.")
                .Wrap(true),
                new Group("Query state").Content(new VStack(
                        new CheckBox("Clear query on show").IsChecked(clearQueryOnShow),
                        new HStack(
                                new Button("Preset query").Click(() => queryText.Value = "reset"),
                                new Button("Clear query").Click(() => queryText.Value = string.Empty))
                            .Spacing(1),
                        new TextBlock(() => $"Current query: {queryText.Value}"))
                    .Spacing(1)),
                new Group("Popup host style").Content(new VStack(
                        new HStack(
                                "Width (%):",
                                new NumberBox<int>().Value(widthPercent).MinWidth(6),
                                "Horizontal:",
                                new EnumSelect<Align>().Value(horizontalAlignment),
                                "Vertical:",
                                new EnumSelect<Align>().Value(verticalAlignment))
                            .Spacing(1),
                        new HStack(
                                "Offset X:",
                                new NumberBox<int>().Value(offsetX).MinWidth(6),
                                "Offset Y:",
                                new NumberBox<int>().Value(offsetY).MinWidth(6),
                                new Button("Open palette").Click(ShowPalette))
                            .Spacing(1),
                        DemoUi.Hint("Stretch ignores percentage sizing on that axis. Offsets are applied after alignment."))
                    .Spacing(1)),
                new Group("Command names").Content(new VStack(
                        new CheckBox("Show command names").IsChecked(showCommandName),
                        new HStack(
                                "Prefix:",
                                new TextBox(commandNamePrefix).MinWidth(8).MaxWidth(12),
                                "Separator:",
                                new TextBox(commandNameSeparator).MinWidth(8).MaxWidth(16))
                            .Spacing(1),
                        DemoUi.Hint("Only commands with Command.Name set use this prefix and separator; unnamed commands still show only their label."))
                    .Spacing(1)),
                new TextBlock(() => $"Popup style: {widthPercent.Value}% width, {horizontalAlignment.Value}/{verticalAlignment.Value}, offset ({offsetX.Value}, {offsetY.Value})"),
                focusProbe,
                new HStack(
                        new Button("Increment").Click(() => counter.Value++),
                        new CheckBox("Enabled").IsChecked(enabled))
                    .Spacing(2),
                new TextBlock(() => $"Counter: {counter.Value}"))
            .Spacing(1);

        host.AddCommand(new Command
        {
            Id = "App.CommandPalette",
            LabelMarkup = "Command palette",
            Gesture = new KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = _ => ShowPalette(),
        });

        for (int i = 0; i < 10; i++)
        {
            int localI = i; // Capture loop variable
            host.AddCommand(new Command
            {
                Id = $"Demo.Increment{i}",
                Name = localI % 2 == 0 ? $"inc.{localI}" : null,
                LabelMarkup = $"[primary]Increment {i}[/]",
                DescriptionMarkup = $"[dim]Increase the counter by {i}[/]",
                Gesture = new KeyGesture(TerminalChar.CtrlN, TerminalModifiers.Ctrl),
                Presentation = CommandPresentation.CommandPalette,
                CanExecute = _ => enabled.Value,
                Execute = _ => counter.Value += localI,
            });
        }

        host.AddCommand(new Command
        {
            Id = "Demo.Reset",
            Name = "reset",
            LabelMarkup = "[warning]Reset[/]",
            DescriptionMarkup = "[dim]Reset the counter to zero[/]",
            Presentation = CommandPresentation.CommandPalette,
            CanExecute = _ => enabled.Value && counter.Value != 0,
            Execute = _ => counter.Value = 0,
        });

        return host.InScreenshot(context, () =>
        {
            focusProbe.App?.Focus(focusProbe);
            ShowPalette();
        });
    }
}

