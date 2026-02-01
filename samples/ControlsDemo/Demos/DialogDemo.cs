using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Dialog", "Overlays", Description = "Movable window with optional modal behavior.")]
public sealed class DialogDemo : ControlsDemoBase
{
    public DialogDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        Dialog CreateDialog()
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

            return dlg;
        }

        var showModal = new Button("Show modal dialog").Click(() =>
        {
            var dlg = CreateDialog();
            dlg.Show();
        });

        var root = new VStack(
                DemoUi.Hint("Dialogs are supported in fullscreen apps (WindowLayer)."),
                showModal)
            .Spacing(1);

        return root.InScreenshot(context, () =>
        {
            var dlg = CreateDialog();
            dlg.Show();
        });
    }
}

