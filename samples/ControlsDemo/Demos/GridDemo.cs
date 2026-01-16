using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Grid", "Layout", Description = "Rows/columns layout with explicit GridCell entries.")]
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

        grid
            .Cell("Name:", 0, 0)
            .Cell(new TextBox("Alex").HorizontalAlignment(HorizontalAlignment.Stretch), 0, 1)
            .Cell("Mode:", 1, 0)
            .Cell(new Select
                {
                    Items =
                    {
                        new SelectItem("Normal"),
                        new SelectItem("Safe"),
                        new SelectItem("Fast"),
                    }
                }.HorizontalAlignment(HorizontalAlignment.Stretch), 1, 1)
            .Cell("Notes:", 2, 0)
            .Cell(new TextBox("Grid uses GridCell objects instead of attached properties.").HorizontalAlignment(HorizontalAlignment.Stretch), 2, 1);

        return new VStack(
                DemoUi.Hint("Grid uses explicit GridCell entries (row/column definitions + Cells list)."),
                new Border(grid).Padding(1))
            .Spacing(1);
    }
}
