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
                "[bold]Bold[/], [dim]dim[/], [underline]underline[/], [italic]italic[/]\n" +
                "[primary]Primary[/], [success]Success[/], [warning]Warning[/], [error]Error[/], [accent]Accent[/]\n" +
                "[#ff8800]Truecolor hex[/], [rgb(64,200,255)]rgb(r,g,b)[/], [208]indexed 256[/]\n" +
                "[bg:#2a2f4a white]Background + foreground[/], [bg:rgb(20,80,60) #e8f8f0]bg:rgb + fg hex[/]\n" +
                "Nested styles: [bold][primary]bold + primary[/][/], [underline][bg:#1a1a1a #88e]underline + bg[/][/]\n" +
                "This is a long line that can be wrapped to demonstrate text wrapping in Markup.")
            .Wrap(() => wrap.Value);

        return new VStack(
                DemoUi.Hint("Markup supports styling tags, theme colors, and hard line breaks."),
                new CheckBox("Wrap").IsChecked(wrap),
                sample)
            .Spacing(1);
    }
}

