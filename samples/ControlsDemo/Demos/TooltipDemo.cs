using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Tooltip", "Overlays", Description = "Hover hints shown as a non-interactive overlay in fullscreen apps.")]
public sealed class TooltipDemo : ControlsDemoBase
{
    public TooltipDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var primaryButton = new Button("Hover me")
            .Tooltip("Tooltip content is just a Visual.")
            // Use zero delay so screenshot generation can open it deterministically.
            .ShowDelayMilliseconds(0);

        var secondaryButton = new Button("Hover me too")
            .Tooltip(new Markup("[success]Separate[/] tooltip content on another button."))
            .ShowDelayMilliseconds(0);

        return new VStack(
                DemoUi.Hint("Tooltips appear when hovering in fullscreen apps. They are implemented via the window layer."),
                DemoUi.Hint("Click a button, then move to the other one to verify tooltips do not stick."),
                new Border(
                        new Center()
                            .Content(new HStack(primaryButton, secondaryButton).Spacing(2)))
                    .MinWidth(54)
                    .MaxWidth(54)
                    .MinHeight(10)
                    .MaxHeight(10)
                    .Padding(1))
            .Spacing(1);
    }
}
