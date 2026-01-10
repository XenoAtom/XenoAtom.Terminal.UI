using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

Terminal.Run(
    new Border().Content(
        new ScrollViewer().Content(
            new VStack().Add(Enumerable.Range(0, 200).Select(x => (Visual)new TextBlock($"Hello {x.ToString()}")).ToArray())
        )
    ).HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch)
    , () => true);
