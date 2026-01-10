using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

Terminal.Run(
        new ScrollViewer().Content(
            new VStack().Add(Enumerable.Range(0, 200).Select(x => (Visual)$"Hello {x.ToString()}").ToArray())
        )
    , () => true);
