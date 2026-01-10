using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.ControlsDemo;

using var session = Terminal.Open();
var root = ControlsDemoApp.Build(out var onUpdate);
Terminal.Run(root, onUpdate);
