using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("StatusBar", "Navigation", Description = "Legacy-style status bar with left/right content.")]
public sealed class StatusBarDemo : ControlsDemoBase
{
    public StatusBarDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var status = new StatusBar()
            .LeftText(new Markup("[dim]Ready[/]") { Wrap = false })
            .RightText(new Markup("[dim]Ln 12, Col 3[/]") { Wrap = false });

        return new VStack(
                DemoUi.Hint("StatusBar is similar to Footer, but only provides left/right slots."),
                new Border(
                        new VStack(
                                new TextArea("Content above the status bar…").MinHeight(4).MaxHeight(4),
                                new Rule(),
                                status))
                    .MinWidth(60)
                    .MaxWidth(60)
                    .Padding(1))
            .Spacing(1);
    }
}

