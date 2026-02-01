using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Footer", "Navigation", Description = "App chrome footer with left/center/right slots.")]
public sealed class FooterDemo : ControlsDemoBase
{
    public FooterDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var footer = new Footer()
            .Left(new Markup("[dim]Tab focus[/]") { Wrap = false })
            .Center(new Markup("[dim]Ready[/]") { Wrap = false })
            .Right(new Markup("[dim]Ctrl+Q quit[/]") { Wrap = false });

        return new VStack(
                DemoUi.Hint("Footer is a single-row bar intended for app-wide status and hints."),
                new Border(
                        new VStack(
                                new TextArea("Content above the footer…").MinHeight(4).MaxHeight(4),
                                new Rule(),
                                footer))
                    .MinWidth(60)
                    .MaxWidth(60)
                    .Padding(1))
            .Spacing(1);
    }
}

