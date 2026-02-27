using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ScrollViewer", "Layout", Description = "Scrollable viewport with automatic scrollbars.")]
public sealed class ScrollViewerDemo : ControlsDemoBase
{
    public ScrollViewerDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var content = new VStack().Spacing(0);
        for (var i = 0; i < 100; i++)
        {
            content.Add($"Log line {i}");
        }

        return new VStack(
                DemoUi.Hint("Use the mouse wheel to scroll. Scrollbars appear when content overflows."),
                new Group("ScrollViewer")
                    .Content(new ScrollViewer(content).MinHeight(10).MaxHeight(10))
                )
            .Spacing(1);
    }
}

