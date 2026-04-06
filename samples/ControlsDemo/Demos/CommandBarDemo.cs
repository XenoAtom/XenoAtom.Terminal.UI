using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CommandBar", "Navigation", Description = "Discoverable key hints for the current focus context.")]
public sealed class CommandBarDemo : ControlsDemoBase
{
    public CommandBarDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var counter = new State<int>(0);
        var enabled = new State<bool>(true);
        var multiLine = new State<bool>(context.IsScreenshot);

        var editor = new TextBox().Placeholder("Focus me to populate the command bar…");
        editor.AutoFocus(true);
        var commandBar = new CommandBar()
            .MultiLine(() => multiLine.Value)
            .MaxWidth(34);

        var root = new VStack(
                DemoUi.Hint("CommandBar surfaces commands registered on the focused visual (and its parents), plus app-level commands."),
                editor,
                new HStack(
                        new Button("Increment").Click(() => counter.Value++),
                        new CheckBox("Enabled").IsChecked(enabled),
                        new CheckBox("Multi-line bar").IsChecked(multiLine))
                    .Spacing(2),
                new TextBlock(() => $"Counter: {counter.Value}"),
                DemoUi.Hint("The command bar below is width-limited to make clipping vs wrapping easy to compare."),
                new Rule(),
                new Border(commandBar).Padding(new Thickness(1, 0, 1, 0)))
            .Spacing(1);

        root.AddCommand(new Command
        {
            Id = "Demo.ToggleEnabled",
            LabelMarkup = "Toggle enabled",
            Gesture = new KeyGesture(TerminalChar.CtrlE, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = _ => enabled.Value = !enabled.Value,
        });

        root.AddCommand(new Command
        {
            Id = "Demo.Reset",
            LabelMarkup = "Reset counter",
            Gesture = new KeyGesture(TerminalChar.CtrlR, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            CanExecute = _ => counter.Value != 0,
            Execute = _ => counter.Value = 0,
        });

        root.AddCommand(new Command
        {
            Id = "Demo.Randomize",
            LabelMarkup = "Randomize value",
            Gesture = new KeyGesture(TerminalChar.CtrlD, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = _ => counter.Value = (counter.Value + 7) % 10,
        });

        root.AddCommand(new Command
        {
            Id = "Demo.Export",
            LabelMarkup = "Export snapshot",
            Gesture = new KeyGesture(TerminalChar.CtrlS, TerminalModifiers.Ctrl),
            Importance = CommandImportance.Secondary,
            Presentation = CommandPresentation.CommandBar,
            Execute = _ => { },
        });

        return root;
    }
}
