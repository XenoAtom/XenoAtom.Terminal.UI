using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Header", "Navigation", Description = "App chrome header with left/center/right slots.")]
public sealed class HeaderDemo : ControlsDemoBase
{
    public HeaderDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var header = new Header()
            .Left(new Markup("[bold]XenoAtom.Terminal.UI[/]") { Wrap = false })
            .Center(new Markup("[dim]Header.Center slot[/]") { Wrap = false })
            .Right(new Markup("[dim]Ctrl+Q quit[/]") { Wrap = false });

        return new VStack(
                DemoUi.Hint("Header is a single-row bar intended for app-wide navigation and key hints."),
                new Border(
                        new VStack(
                                header,
                                new Rule(),
                                new TextArea("Main content goes here…").MinHeight(4).MaxHeight(4)))
                    .MinWidth(60)
                    .MaxWidth(60)
                    .Padding(1))
            .Spacing(1);
    }
}

