using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Grid", "Layout", Description = "Rows/columns layout with attached Row/Column properties.")]
public sealed class GridDemo : ControlsDemoBase
{
    public GridDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var grid = new Grid()
            .Rows(
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto })
            .Columns(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star(1) });

        grid.Children.Add(new TextBlock("Name:").Row(0).Column(0));
        grid.Children.Add(new TextBox().Text("Alex").Row(0).Column(1));

        grid.Children.Add(new TextBlock("Mode:").Row(1).Column(0));
        grid.Children.Add(new Select
            {
                Items =
                {
                    new SelectItem("Normal"),
                    new SelectItem("Safe"),
                    new SelectItem("Fast"),
                }
            }.Row(1).Column(1));

        grid.Children.Add(new TextBlock("Notes:").Row(2).Column(0));
        grid.Children.Add(new TextBox().Text("Grid uses attached properties.").Row(2).Column(1));

        return new VStack(
                DemoUi.Hint("Grid uses Row/Column attached properties and row/column definitions."),
                new Border().Padding(1).Content(grid))
            .Spacing(1);
    }
}
