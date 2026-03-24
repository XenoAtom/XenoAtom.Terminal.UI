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

        var formGrid = new Grid()
            .Rows(
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto })
            .Columns(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star(1) });

        formGrid
            .Cell("Name:", 0, 0)
            .Cell(new TextBox("Alex").HorizontalAlignment(Align.Stretch), 0, 1)
            .Cell("Mode:", 1, 0)
            .Cell(new Select<string>
                {
                    Items = { "Normal", "Safe", "Fast" }
                }.HorizontalAlignment(Align.Stretch), 1, 1)
            .Cell("Notes:", 2, 0)
            .Cell(new TextBox("Grid uses GridCell objects instead of attached properties.").HorizontalAlignment(Align.Stretch), 2, 1);

        var leftPane = new Group()
            .TopLeftText("2*")
            .Padding(1)
            .Content(new TextBlock("Star columns divide remaining space by weight on bounded layouts.")
                .Wrap(true))
            .Stretch();

        var rightPane = new Group()
            .TopLeftText("1*")
            .Padding(1)
            .Content(new TextBlock("Child minimum widths still apply.")
                .Wrap(true))
            .Stretch();

        var starGrid = new Grid()
            .Columns(
                new ColumnDefinition { Width = GridLength.Star(2) },
                new ColumnDefinition { Width = GridLength.Star(1) })
            .Rows(new RowDefinition { Height = GridLength.Auto })
            .Cell(leftPane, 0, 0)
            .Cell(rightPane, 0, 1)
            .HorizontalAlignment(Align.Stretch);

        return new VStack(
                DemoUi.Hint("Grid uses explicit GridCell entries (row/column definitions + Cells list)."),
                DemoUi.Hint("GridLength.Star(...) divides remaining space by weight. Use GridLength.FlexStar(...) for content-aware weighted tracks."),
                new Border(formGrid).Padding(1),
                new Border(starGrid).Padding(1).MinWidth(72))
            .Spacing(1);
    }
}
