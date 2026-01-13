using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

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
    .Cell(new TextBox().Text("Alex").HorizontalAlignment(HorizontalAlignment.Stretch), 0, 1)
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
    .Cell(new TextBox().Text("Grid uses GridCell objects instead of attached properties.").HorizontalAlignment(HorizontalAlignment.Stretch), 2, 1);

var control = new VStack(
        "Grid uses explicit GridCell entries (row/column definitions + Cells list).",
        new Border().Padding(1).Content(grid))
    .Spacing(1);

var scroll = new ScrollViewer
{
    Content = control,
};

var dock = new DockLayout()
    .Top(new VStack("Hello", new Rule()).Spacing(0))
    .Content(scroll)
    .Bottom("Bottom");


var split = new HSplitter("Left", dock).Ratio(0.16);


Terminal.Run(split
    , () => true);
