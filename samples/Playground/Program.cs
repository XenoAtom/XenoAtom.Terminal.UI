using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

State<string?> text = new("Type here");
State<bool> exit = new(false);

Terminal.Run(
    new VStack(
        new TextBox(text),
        new TextBlock(() => $"The text typed is: {text.Value}"),
        new Button("Exit").Click(() => exit.Value = true)
    ),
    onUpdate: () => exit.Value ? TerminalLoopResult.StopAndKeepVisual : TerminalLoopResult.Continue);