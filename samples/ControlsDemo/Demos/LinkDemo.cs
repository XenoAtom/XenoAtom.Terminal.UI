using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Link", "Content", Description = "Clickable OSC 8 hyperlinks.")]
public sealed class LinkDemo : ControlsDemoBase
{
    public LinkDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        return new VStack(
                DemoUi.Hint("Links use OSC 8; support depends on your terminal."),
                new Link("https://github.com/XenoAtom", "Open XenoAtom on GitHub").Trimming(TextTrimming.EndEllipsis))
            .Spacing(1);
    }
}

