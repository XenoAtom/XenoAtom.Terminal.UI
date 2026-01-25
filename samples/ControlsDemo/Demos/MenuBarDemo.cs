using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("MenuBar", "Layout", Description = "Keyboard and mouse menus. Menu items can be backed by Command.")]
public sealed class MenuBarDemo : ControlsDemoBase
{
    public MenuBarDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var openEnabled = new State<bool>(true);
        var status = new State<string>("ready");

        var openCommand = new Command
        {
            Id = "Demo.Open",
            LabelMarkup = "[primary]Open[/]",
            Gesture = new KeyGesture(TerminalChar.CtrlO, TerminalModifiers.Ctrl),
            Presentation = CommandPresentation.Menu,
            CanExecute = _ => openEnabled.Value,
            Execute = _ => status.Value = "open",
        };

        var aboutCommand = new Command
        {
            Id = "Demo.About",
            LabelMarkup = "About",
            Presentation = CommandPresentation.Menu,
            Execute = _ => context.Log("XenoAtom.Terminal.UI ControlsDemo"),
        };

        var menuBar = new MenuBar();
        var file = new MenuItem("File")
            .Items(
                new MenuItem("Open", openCommand),
                MenuItem.Separator(),
                new MenuItem("Reset status", () => status.Value = "ready"));

        var help = new MenuItem("Help")
            .Items(new MenuItem("About", aboutCommand));

        menuBar.Items.AddRange(file, help);

        return new VStack(
                DemoUi.Title("MenuBar"),
                new TextBlock("Use Tab to focus the menu bar, then Enter/Down to open. Use arrow keys to navigate and Enter to activate.")
                    .Wrap(true),
                new CheckBox("Open enabled").IsChecked(openEnabled),
                menuBar,
                new TextBlock(() => $"Status: {status.Value}"),
                new TextArea("This is a focusable control. Try focusing it, then opening the menu.")
                    .MinHeight(4)
                    .MaxHeight(4))
            .Spacing(1);
    }
}

