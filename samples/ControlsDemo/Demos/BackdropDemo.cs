using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Backdrop", "Overlays", Description = "Fullscreen dimming surface typically used behind modal content.")]
public sealed class BackdropDemo : ControlsDemoBase
{
    public BackdropDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        // Backdrop is usually placed behind a dialog/popup.
        return new VStack(
                DemoUi.Hint("Backdrop fills the viewport with a dimmed style to separate overlays from background content."),
                new Border(
                        new ZStack(
                                new Backdrop(),
                                new Center()
                                    .Content(
                                        new Dialog()
                                            .Title("Modal dialog")
                                            .Content(new VStack(
                                                    "This dialog is drawn above a Backdrop.",
                                                    "Backdrop helps readability by dimming what's behind.",
                                                    new Button("Close"))
                                                .Spacing(1)))))
                    .MinWidth(54)
                    .MaxWidth(54)
                    .MinHeight(12)
                    .MaxHeight(12)
                    .Padding(1))
            .Spacing(1);
    }
}
