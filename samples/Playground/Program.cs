using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

var progress = new State<float>(0.0f);

var tabs = new TabControl(
    new TabPage(
        header: "Tab1",
        content: "ContentTab1"
        ),
    new TabPage(
        header: "Tab2",
        content: "ContentTab2"));

Terminal.Run(tabs
    , () => true);
