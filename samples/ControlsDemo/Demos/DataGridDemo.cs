using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("DataGrid", "Data", Description = "Virtualized data grid with sorting/filtering/search and editing.")]
public sealed class DataGridDemo : ControlsDemoBase
{
    public DataGridDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var laneAccessor = new BindingAccessor<int>("lane", o => ((SwimRow)o).Lane, (o, v) => ((SwimRow)o).Lane = v);
        var swimmerAccessor = new BindingAccessor<string>("swimmer", o => ((SwimRow)o).Swimmer, (o, v) => ((SwimRow)o).Swimmer = v);
        var countryAccessor = new BindingAccessor<string>("country", o => ((SwimRow)o).Country, (o, v) => ((SwimRow)o).Country = v);
        var timeAccessor = new BindingAccessor<double>("time", o => ((SwimRow)o).Time, (o, v) => ((SwimRow)o).Time = v);

        var doc = new DataGridListDocument();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("lane", "lane", typeof(int), ReadOnly: false, laneAccessor),
            new DataGridColumnInfo("swimmer", "swimmer", typeof(string), ReadOnly: false, swimmerAccessor),
            new DataGridColumnInfo("country", "country", typeof(string), ReadOnly: false, countryAccessor),
            new DataGridColumnInfo("time", "time", typeof(double), ReadOnly: false, timeAccessor),
        });

        doc.AddRow(new SwimRow { Lane = 4, Swimmer = "Joseph Schooling", Country = "Singapore", Time = 50.39 });
        doc.AddRow(new SwimRow { Lane = 2, Swimmer = "Michael Phelps", Country = "United States", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 5, Swimmer = "Chad le Clos", Country = "South Africa", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 6, Swimmer = "László Cseh", Country = "Hungary", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 3, Swimmer = "Li Zhuhua", Country = "China", Time = 51.26 });
        doc.AddRow(new SwimRow { Lane = 8, Swimmer = "Mehdy Metella", Country = "France", Time = 51.58 });
        doc.AddRow(new SwimRow { Lane = 7, Swimmer = "Tom Shields", Country = "United States", Time = 51.73 });
        doc.AddRow(new SwimRow { Lane = 1, Swimmer = "Aleksandr Sadovnikov", Country = "Russia", Time = 51.84 });
        doc.AddRow(new SwimRow { Lane = 10, Swimmer = "Darren Burns", Country = "Scotland", Time = 51.84 });

        // Add more rows to demonstrate virtualization/scrolling.
        for (var i = 0; i < 50; i++)
        {
            doc.AddRow(new SwimRow { Lane = 11 + i, Swimmer = $"Swimmer {i:00}", Country = "N/A", Time = 50 + (i / 10.0) });
        }

        var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view, FrozenColumns = 1 };
        grid.Columns.Add(new DataGridColumn<int> { Key = "lane", TypedAccessor = laneAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right });
        grid.Columns.Add(new DataGridColumn<string> { Key = "swimmer", TypedAccessor = swimmerAccessor, Width = GridLength.Star(2) });
        grid.Columns.Add(new DataGridColumn<string> { Key = "country", TypedAccessor = countryAccessor, Width = GridLength.Star(2) });
        grid.Columns.Add(new DataGridColumn<double> { Key = "time", TypedAccessor = timeAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right });

        var panel = new VStack(
                DemoUi.Hint("DataGrid is scrollable in both directions. Wrap it in a ScrollViewer to show scrollbars."),
                DemoUi.Hint("Ctrl+F: search (find), F3/Shift+F3: next/previous match"),
                DemoUi.Hint("Ctrl+Shift+F: toggle filter row, F2: edit current cell"),
                new Border(new ScrollViewer(grid).MinHeight(12).MaxHeight(12)).MinWidth(40).MaxWidth(120).HorizontalAlignment(Align.Stretch)
                )
            .Spacing(1);

        return panel;
    }

    private sealed class SwimRow
    {
        public int Lane { get; set; }
        public string Swimmer { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Time { get; set; }
    }
}
