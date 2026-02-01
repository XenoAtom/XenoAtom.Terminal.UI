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

        var host = new Button("Hover me")
            .Tooltip("Tooltip content is just a Visual.")
            // Use zero delay so screenshot generation can open it deterministically.
            .ShowDelayMilliseconds(0);

        return new VStack(
                DemoUi.Hint("Tooltips appear when hovering in fullscreen apps. They are implemented via the window layer."),
                new Border(
                        new Center()
                            .Content(host))
                    .MinWidth(46)
                    .MaxWidth(46)
                    .MinHeight(10)
                    .MaxHeight(10)
                    .Padding(1))
            .Spacing(1);
    }
}
