using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
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

        var editor = new TextBox().Placeholder("Focus me to populate the command bar…");
        editor.AutoFocus(true);

        var root = new VStack(
                DemoUi.Hint("CommandBar surfaces commands registered on the focused visual (and its parents), plus app-level commands."),
                editor,
                new HStack(
                        new Button("Increment").Click(() => counter.Value++),
                        new CheckBox("Enabled").IsChecked(enabled))
                    .Spacing(2),
                new TextBlock(() => $"Counter: {counter.Value}"),
                new Rule(),
                new CommandBar())
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

        return root;
    }
}

