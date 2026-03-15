using XenoAtom.Ansi;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.ControlsDemo;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Dialog", "Overlays", Description = "Movable, resizable window with optional modal behavior.")]
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
                .TopRightText(new TextBlock("Resizable").Style(new TextBlockStyle { Foreground = Colors.DeepSkyBlue }))
                .BottomLeftText("Min 36x9")
                .BottomRightText("Drag edge")
                .IsModal(true)
                .Padding(1)
                .MinWidth(36)
                .MinHeight(9)
                .Width(52)
                .Height(12)
                .Style(DialogStyle.Rounded with
                {
                    ResizeHandleHoverStyle = Style.None.WithBackground(Colors.DeepSkyBlue.WithAlpha(0x40)),
                })
                .Content(new VStack(
                        "This is a dialog window.",
                        DemoUi.Hint("Drag the title bar to move."),
                        DemoUi.Hint("Hover the left/right/bottom edges or the bottom-right corner to resize."),
                        new Group("Notes")
                            .TopRightText("DialogStyle")
                            .Content(new VStack(
                                    "Resizable is enabled by default.",
                                    "Minimum size uses the inherited MinWidth / MinHeight properties.")
                                .Spacing(1))
                            .Padding(1),
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

