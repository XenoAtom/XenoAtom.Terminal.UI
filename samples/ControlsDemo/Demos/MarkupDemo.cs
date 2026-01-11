using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Markup", "Content", Description = "ANSI markup rendering with wrapping.")]
public sealed class MarkupDemo : ControlsDemoBase
{
    public MarkupDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var wrap = new State<bool>(true);

        var sample = new Markup(
                "[bold]Bold[/], [dim]dim[/], [underline]underline[/]\n" +
                "This is a long line that can be wrapped to demonstrate text wrapping in Markup.")
            .Wrap(() => wrap.Value);

        return new VStack(
                DemoUi.Hint("Markup supports styling tags and hard line breaks."),
                new CheckBox("Wrap").IsChecked(wrap),
                sample)
            .Spacing(1);
    }
}

