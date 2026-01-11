using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

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

        var table = new Table()
            .Headers("Task", "Status")
            .AddRow("Download", "Running")
            .AddRow("Render", "OK")
            .AddRow("Tests", "OK");

        return new VStack(
                DemoUi.Hint("Tables accept Visual cells, so headers/cells can be composed."),
                table)
            .Spacing(1);
    }
}

