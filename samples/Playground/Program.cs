using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Layout;

Terminal.Live(
    new VStack("Hello").VerticalAlignment(Align.Stretch),
    onUpdate: () => TerminalLoopResult.Continue);