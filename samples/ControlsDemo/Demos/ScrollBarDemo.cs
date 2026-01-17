using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("ScrollBar", "Layout", Description = "Explicit scrollbars (horizontal and vertical).")]
public sealed class ScrollBarDemo : ControlsDemoBase
{
    public ScrollBarDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var v = new VScrollBar(focusable: true)
            .Minimum(0)
            .Maximum(100)
            .ViewportSize(20)
            .Value(40);

        var h = new HScrollBar(focusable: true)
            .Minimum(0)
            .Maximum(100)
            .ViewportSize(30)
            .Value(60);

        return new VStack(
                DemoUi.Hint("ScrollBar exposes Minimum/Maximum/Value and a ValueChanged event."),
                new HStack(
                        new VStack(v.MinHeight(20).MaxHeight(20)).HorizontalAlignment(HorizontalAlignment.Left).VerticalAlignment(VerticalAlignment.Top),
                        new VStack(h).MinWidth(50).MaxWidth(50).HorizontalAlignment(HorizontalAlignment.Left).VerticalAlignment(VerticalAlignment.Top))
                    .Spacing(2),
                new TextBlock(() => $"Vertical: {v.Value}, Horizontal: {h.Value}"))
            .Spacing(1);
    }
}
