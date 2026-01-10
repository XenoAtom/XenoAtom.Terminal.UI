using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Tables, markup, and links", "Content", Description = "Table styling, Markup rendering, and clickable links (OSC 8).", Tags = ["Table", "Markup", "Link"], Order = 0)]
public sealed class TablesMarkupLinksDemo : ControlsDemoBase
{
    public TablesMarkupLinksDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var table = new Table()
            .Headers(new Markup("[bold]Key[/]"), new Markup("[bold]Value[/]"))
            .AddRow("Name", new Markup("[violet]XenoAtom.Terminal.UI[/]"))
            .AddRow("Docs", new Link("https://github.com/XenoAtom/XenoAtom.Terminal.UI", "GitHub repo"))
            .AddRow("Supports", new Markup("[green]Mouse[/], [green]keyboard[/], [green]popups[/], [green]bindings[/]"))
            .Style(TableStyle.DoubleGrid)
            .HorizontalAlignment(HorizontalAlignment.Left);

        var markup = new Markup(
                """
                [bold]Markup[/] supports inline styling:
                - [violet]accent[/]
                - [green]success[/]
                - [yellow]warning[/]
                - [red]error[/]
                - [underline]decorations[/]
                """)
            .Wrap(true);

        var link = new Link("https://spectreconsole.net/", "Clickable link (mouse/Enter)");
        link.Opened((_, e) => context.Log($"Link opened: {e.Uri}"));

        return new VStack(
                new Group().TopLeftText("Markup").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(markup),
                new Group().TopLeftText("Link").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(link),
                new Group().TopLeftText("Table").Padding(1).HorizontalAlignment(HorizontalAlignment.Stretch).Content(table))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}

