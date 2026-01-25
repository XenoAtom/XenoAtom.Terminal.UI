using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CommandPalette", "Overlays", Description = "Searchable command launcher powered by the unified command system.")]
public sealed class CommandPaletteDemo : ControlsDemoBase
{
    public CommandPaletteDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var palette = new CommandPalette();
        var counter = new State<int>(0);
        var enabled = new State<bool>(true);

        var host = new VStack(
            DemoUi.Title("Command palette"),
                new TextBlock("Press Ctrl+P to open the command palette. Type to search, use arrows to select, and Enter to run.")
                .Wrap(true),
                new TextBox().Placeholder("Enter some text here to enable CTRL+P to be accessible"),
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
            Execute = _ => palette.Show(),
        });

        host.AddCommand(new Command
        {
            Id = "Demo.Increment",
            LabelMarkup = "[primary]Increment[/]",
            DescriptionMarkup = "[dim]Increase the counter[/]",
            Gesture = new KeyGesture(TerminalChar.CtrlI, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.CommandPalette,
            CanExecute = _ => enabled.Value,
            Execute = _ => counter.Value++,
        });

        host.AddCommand(new Command
        {
            Id = "Demo.Reset",
            LabelMarkup = "[warning]Reset[/]",
            DescriptionMarkup = "[dim]Reset the counter to zero[/]",
            Presentation = CommandPresentation.CommandPalette,
            CanExecute = _ => enabled.Value && counter.Value != 0,
            Execute = _ => counter.Value = 0,
        });

        return host;
    }
}

