using System.Linq;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var text = string.Join(
    "\n",
    Enumerable.Range(1, 100).Select(i => $"Line {i:00} - ScrollViewer + TextArea"));

var editor = new TextArea(text);

var view = new ScrollViewer(editor);

Terminal.Run(view, () => TerminalLoopResult.Continue);
