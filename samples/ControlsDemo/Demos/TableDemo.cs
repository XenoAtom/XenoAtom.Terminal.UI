using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Table", "Content", Description = "Visual table cells and basic styling.")]
public sealed class TableDemo : ControlsDemoBase
{
    public TableDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var basicTable = new Table()
            .Headers("Task", "Status")
            .AddRow("Download", "Running")
            .AddRow("Render", "OK")
            .AddRow("Tests", "OK");

        var footerTable = new Table()
            .Headers("Item", "Qty", "Price")
            .AddRow("Keyboard", "1", "$79")
            .AddRow("Mouse", "2", "$50")
            .AddRow(new Markup("[bold]Total[/]"), new Markup("[bold]3[/]"), new Markup("[bold]$129[/]"))
            .LastRowIsFooter(true)
            .ShowFooterSeparator(true)
            .Style(TableStyle.RoundedGrid with { ShowRowSeparators = false });

        return new VStack(
                DemoUi.Hint("Tables accept Visual cells, so headers/cells can be composed."),
                basicTable,
                DemoUi.Hint("Footer mode can separate the last row (rounded style + footer separator)."),
                footerTable)
            .Spacing(1);
    }
}

