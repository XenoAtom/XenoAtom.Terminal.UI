using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Dialog", "Overlays", Description = "Movable window with optional modal behavior.")]
public sealed class DialogDemo : ControlsDemoBase
{
    public DialogDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var showModal = new Button("Show modal dialog").Click(() =>
        {
            Dialog? dlg = null;
            dlg = new Dialog()
                .Title("Modal dialog")
                .IsModal(true)
                .Padding(1)
                .Width(50)
                .Content(new VStack(
                        "This is a dialog window.",
                        DemoUi.Hint("Drag the title bar to move."),
                        new Button("Close").Click(() => dlg!.Close()))
                    .Spacing(1));

            dlg.Show();
        });

        return new VStack(
                DemoUi.Hint("Dialogs are supported in fullscreen apps (WindowLayer)."),
                showModal)
            .Spacing(1);
    }
}

