using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

using var session = Terminal.Open();

Terminal.WriteMarkupLine("[bold]XenoAtom.Terminal.UI Inline Live Demo[/]");
Terminal.WriteMarkupLine("This sample will be expanded in the next commit.");
Terminal.WriteLine();

var state = new State<int>(0);
var root = new VStack(
    new TextBlock(() => $"Counter: {state.Value}"),
    new Button("Increment").Click(() => state.Value++))
    .Spacing(1);

Terminal.Live(root, () => TerminalLoopResult.Continue);

