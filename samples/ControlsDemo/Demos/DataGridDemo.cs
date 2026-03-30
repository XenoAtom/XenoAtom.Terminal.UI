using DataRow = System.Data.DataRow;
using DataTable = System.Data.DataTable;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("DataGrid", "Data", Description = "Virtualized data grid with selection, filtering, search, and editing.")]
public sealed class DataGridDemo : ControlsDemoBase
{
    public DataGridDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var showHeader = new State<bool>(true);
        var showRowAnchor = new State<bool>(true);
        var rowAnchorWidth = new State<int>(1);
        var filterRowVisible = new State<bool>(false);
        var selectionMode = new State<DataGridSelectionMode>(DataGridSelectionMode.Cell);
        var editMode = new State<DataGridEditMode>(DataGridEditMode.OnEnter);
        var frozenColumns = new State<int>(1);
        var frozenRows = new State<int>(0);
        var readOnly = new State<bool>(false);

        var swim = BuildSwimMeetGrid(showHeader, showRowAnchor, rowAnchorWidth, filterRowVisible, selectionMode, editMode, frozenColumns, frozenRows, readOnly);
        var ledger = BuildLedgerGrid(showHeader, showRowAnchor, rowAnchorWidth, filterRowVisible, selectionMode, editMode, frozenColumns, frozenRows, readOnly);
        var dataTable = BuildDataTableGrid(showHeader, showRowAnchor, rowAnchorWidth, filterRowVisible, selectionMode, editMode, frozenColumns, frozenRows, readOnly);
        var mixed = BuildMixedTypesGrid(showHeader, showRowAnchor, rowAnchorWidth, filterRowVisible, selectionMode, editMode, frozenColumns, frozenRows, readOnly);
        var dialog = BuildDialogGridDemo(showHeader, showRowAnchor, rowAnchorWidth, filterRowVisible, selectionMode, editMode, frozenColumns, frozenRows, readOnly);

        var tabs = new TabControl(
                new TabPage(header: "Swim Meet", content: swim),
                new TabPage(header: "Ledger", content: ledger),
                new TabPage(header: "DataTable", content: dataTable),
                new TabPage(header: "Mixed", content: mixed),
                new TabPage(header: "Dialog", content: dialog))
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        var controls = new VStack(
                DemoUi.Title("Settings"),
                new HStack(
                        new CheckBox("Header").IsChecked(showHeader),
                        new CheckBox("Row anchor").IsChecked(showRowAnchor),
                        DemoUi.Title("Anchor width"),
                        new Slider<int> { Minimum = 0, Maximum = 4, Step = 1 }.Value(rowAnchorWidth))
                    .Spacing(2),
                new HStack(
                        new CheckBox("Filter row").IsChecked(filterRowVisible),
                        new CheckBox("Read-only").IsChecked(readOnly),
                        DemoUi.Title("Selection"),
                        new EnumSelect<DataGridSelectionMode>().Value(selectionMode),
                        DemoUi.Title("Edit mode"),
                        new EnumSelect<DataGridEditMode>().Value(editMode))
                    .Spacing(2),
                new HStack(
                        DemoUi.Title("Frozen columns"),
                        new Slider<int> { Minimum = 0, Maximum = 4, Step = 1 }.Value(frozenColumns),
                        DemoUi.Title("Frozen rows"),
                        new Slider<int> { Minimum = 0, Maximum = 4, Step = 1 }.Value(frozenRows))
                    .Spacing(2))
            .Spacing(1);

        return new VStack(
                DemoUi.Hint("Wrap DataGridControl in a ScrollViewer to show scrollbars."),
                DemoUi.Hint("Click a header sort button to cycle off/descending/ascending. Ctrl+click adds a secondary sort."),
                DemoUi.Hint("Ctrl+F: search (find), F3/Shift+F3: next/previous match"),
                DemoUi.Hint("F4: toggle filter row, F2: edit current cell"),
                DemoUi.Hint("Bool cells toggle with Space or a single click. DirectActivate columns can replay button-like actions from the first click."),
                DemoUi.Hint("The Dialog tab shows Escape bubbling to the wrapping dialog when the grid is not editing a cell."),
                controls,
                new Rule(),
                tabs)
            .Spacing(1);
    }

    private static Visual BuildSwimMeetGrid(
        State<bool> showHeader,
        State<bool> showRowAnchor,
        State<int> rowAnchorWidth,
        State<bool> filterRowVisible,
        State<DataGridSelectionMode> selectionMode,
        State<DataGridEditMode> editMode,
        State<int> frozenColumns,
        State<int> frozenRows,
        State<bool> readOnly)
    {
        var laneAccessor = SwimRow.Accessor.Lane;
        var swimmerAccessor = SwimRow.Accessor.Swimmer;
        var countryAccessor = SwimRow.Accessor.Country;
        var timeAccessor = SwimRow.Accessor.Time;

        var doc = new DataGridListDocument<SwimRow>();
        using (doc.BeginUpdate())
        {
            doc
                .AddColumn(laneAccessor)
                .AddColumn(swimmerAccessor)
                .AddColumn(countryAccessor)
                .AddColumn(timeAccessor);
        }

        doc.AddRow(new SwimRow { Lane = 4, Swimmer = "Joseph Schooling", Country = "Singapore", Time = 50.39 });
        doc.AddRow(new SwimRow { Lane = 2, Swimmer = "Michael Phelps", Country = "United States", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 5, Swimmer = "Chad le Clos", Country = "South Africa", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 6, Swimmer = "László Cseh", Country = "Hungary", Time = 51.14 });
        doc.AddRow(new SwimRow { Lane = 3, Swimmer = "Li Zhuhua", Country = "China", Time = 51.26 });
        doc.AddRow(new SwimRow { Lane = 8, Swimmer = "Mehdy Metella", Country = "France", Time = 51.58 });
        doc.AddRow(new SwimRow { Lane = 7, Swimmer = "Tom Shields", Country = "United States", Time = 51.73 });
        doc.AddRow(new SwimRow { Lane = 1, Swimmer = "Aleksandr Sadovnikov", Country = "Russia", Time = 51.84 });
        doc.AddRow(new SwimRow { Lane = 10, Swimmer = "Darren Burns", Country = "Scotland", Time = 51.84 });

        for (var i = 0; i < 120; i++)
        {
            doc.AddRow(new SwimRow { Lane = 11 + i, Swimmer = $"Swimmer {i:000}", Country = "N/A", Time = 50 + (i / 10.0) });
        }

        var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view }
            .ShowHeader(showHeader)
            .ShowRowAnchor(showRowAnchor)
            .RowAnchorWidth(rowAnchorWidth)
            .FilterRowVisible(filterRowVisible)
            .SelectionMode(selectionMode)
            .EditMode(editMode)
            .FrozenColumns(frozenColumns)
            .FrozenRows(frozenRows)
            .ReadOnly(readOnly);

        grid.Columns.Add(new DataGridColumn<int> { Key = laneAccessor.Name, TypedValueAccessor = laneAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = swimmerAccessor.Name, TypedValueAccessor = swimmerAccessor, Width = GridLength.Star(2), Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = countryAccessor.Name, TypedValueAccessor = countryAccessor, Width = GridLength.Star(2), Sortable = true });
        grid.Columns.Add(new DataGridColumn<double> { Key = timeAccessor.Name, TypedValueAccessor = timeAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });

        var themed = new Border(new ScrollViewer(grid).MinHeight(12).MaxHeight(12))
            .Style(BorderStyle.Rounded)
            .Padding(new Thickness(1, 0, 1, 0));
        return themed;
    }

    private static Visual BuildLedgerGrid(
        State<bool> showHeader,
        State<bool> showRowAnchor,
        State<int> rowAnchorWidth,
        State<bool> filterRowVisible,
        State<DataGridSelectionMode> selectionMode,
        State<DataGridEditMode> editMode,
        State<int> frozenColumns,
        State<int> frozenRows,
        State<bool> readOnly)
    {
        var dateAccessor = new BindingAccessor<string>("date", o => ((LedgerRow)o).Date, (o, v) => ((LedgerRow)o).Date = v);
        var descAccessor = new BindingAccessor<string>("desc", o => ((LedgerRow)o).Description, (o, v) => ((LedgerRow)o).Description = v);
        var amountAccessor = new BindingAccessor<double>("amount", o => ((LedgerRow)o).Amount, (o, v) => ((LedgerRow)o).Amount = v);
        var categoryAccessor = new BindingAccessor<string>("cat", o => ((LedgerRow)o).Category, (o, v) => ((LedgerRow)o).Category = v);

        var doc = new DataGridListDocument<LedgerRow>();
        doc.SetColumns(new[]
        {
            new DataGridColumnInfo("date", "Date", typeof(string), ReadOnly: false, dateAccessor),
            new DataGridColumnInfo("desc", "Description", typeof(string), ReadOnly: false, descAccessor),
            new DataGridColumnInfo("amount", "Amount", typeof(double), ReadOnly: false, amountAccessor),
            new DataGridColumnInfo("cat", "Category", typeof(string), ReadOnly: false, categoryAccessor),
        });

        var rnd = new Random(123);
        var categories = new[] { "Fuel", "Food", "Tools", "Tickets", "Coffee", "Repairs", "Books" };
        var categoryOrder = categories
            .Select((name, index) => (name, index))
            .ToDictionary(static x => x.name, static x => x.index, StringComparer.Ordinal);

        for (var i = 0; i < 180; i++)
        {
            var cat = categories[i % categories.Length];
            var amount = Math.Round((rnd.NextDouble() * 120) - 20, 2);
            doc.AddRow(new LedgerRow
            {
                Date = $"2026-01-{(i % 28) + 1:00}",
                Description = $"{cat} • #{1000 + i}",
                Amount = amount,
                Category = cat,
            });
        }

        var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view }
            .ShowHeader(showHeader)
            .ShowRowAnchor(showRowAnchor)
            .RowAnchorWidth(rowAnchorWidth)
            .FilterRowVisible(filterRowVisible)
            .SelectionMode(selectionMode)
            .EditMode(editMode)
            .FrozenColumns(frozenColumns)
            .FrozenRows(frozenRows)
            .ReadOnly(readOnly);

        grid.Columns.Add(new DataGridColumn<string> { Key = "date", TypedValueAccessor = dateAccessor, Width = GridLength.Auto, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "desc", TypedValueAccessor = descAccessor, Width = GridLength.Star(3), Sortable = true });
        grid.Columns.Add(new DataGridColumn<double> { Key = "amount", TypedValueAccessor = amountAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string>
        {
            Key = "cat",
            TypedValueAccessor = categoryAccessor,
            Width = GridLength.Star(1),
            Sortable = true,
            SortComparer = Comparer<string>.Create((left, right) =>
            {
                var leftRank = categoryOrder.TryGetValue(left, out var rank) ? rank : int.MaxValue;
                var rightRank = categoryOrder.TryGetValue(right, out rank) ? rank : int.MaxValue;
                return leftRank != rightRank
                    ? leftRank.CompareTo(rightRank)
                    : string.Compare(left, right, StringComparison.Ordinal);
            }),
        });

        _ = grid.TrySetColumnSortDirection("cat", DataGridSortDirection.Ascending);
        _ = grid.TrySetColumnSortDirection("amount", DataGridSortDirection.Descending, additive: true);

        var styled = new Border(new ScrollViewer(grid).MinHeight(12).MaxHeight(12))
            .Style(BorderStyle.Single)
            .Padding(new Thickness(1, 0, 1, 0));
        return styled;
    }

    private static Visual BuildDataTableGrid(
        State<bool> showHeader,
        State<bool> showRowAnchor,
        State<int> rowAnchorWidth,
        State<bool> filterRowVisible,
        State<DataGridSelectionMode> selectionMode,
        State<DataGridEditMode> editMode,
        State<int> frozenColumns,
        State<int> frozenRows,
        State<bool> readOnly)
    {
        var table = new DataTable("planets");
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("planet", typeof(string));
        table.Columns.Add("distance_au", typeof(double));

        var planets = new[]
        {
            ("Mercury", 0.39),
            ("Venus", 0.72),
            ("Earth", 1.00),
            ("Mars", 1.52),
            ("Jupiter", 5.20),
            ("Saturn", 9.58),
            ("Uranus", 19.2),
            ("Neptune", 30.1),
        };

        for (var i = 0; i < 200; i++)
        {
            var p = planets[i % planets.Length];
            table.Rows.Add(i + 1, p.Item1, p.Item2 + (i / 1000.0));
        }

        var doc = new DataGridDataTableDocument(table);
        var view = new DataGridDocumentView(doc);

        // DataTable rows are not bindable; edits still propagate because DataTable emits change events.
        var idAccessor = new BindingAccessor<int>("id", o => (int)((DataRow)o)["id"], (o, v) => ((DataRow)o)["id"] = v);
        var planetAccessor = new BindingAccessor<string>("planet", o => (string)((DataRow)o)["planet"], (o, v) => ((DataRow)o)["planet"] = v);
        var distAccessor = new BindingAccessor<double>("distance_au", o => (double)((DataRow)o)["distance_au"], (o, v) => ((DataRow)o)["distance_au"] = v);

        var grid = new DataGridControl { View = view }
            .ShowHeader(showHeader)
            .ShowRowAnchor(showRowAnchor)
            .RowAnchorWidth(rowAnchorWidth)
            .FilterRowVisible(filterRowVisible)
            .SelectionMode(selectionMode)
            .EditMode(editMode)
            .FrozenColumns(frozenColumns)
            .FrozenRows(frozenRows)
            .ReadOnly(readOnly);

        grid.Columns.Add(new DataGridColumn<int> { Key = "id", TypedValueAccessor = idAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "planet", TypedValueAccessor = planetAccessor, Width = GridLength.Star(2), Sortable = true });
        grid.Columns.Add(new DataGridColumn<double> { Key = "distance_au", TypedValueAccessor = distAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });

        var framed = new Border(new ScrollViewer(grid).MinHeight(12).MaxHeight(12))
            .Style(BorderStyle.Double)
            .Padding(new Thickness(1, 0, 1, 0));
        return framed;
    }

    private static Visual BuildMixedTypesGrid(
        State<bool> showHeader,
        State<bool> showRowAnchor,
        State<int> rowAnchorWidth,
        State<bool> filterRowVisible,
        State<DataGridSelectionMode> selectionMode,
        State<DataGridEditMode> editMode,
        State<int> frozenColumns,
        State<int> frozenRows,
        State<bool> readOnly)
    {
        var idAccessor = MixedRow.Accessor.Id;
        var enabledAccessor = MixedRow.Accessor.Enabled;
        var severityAccessor = MixedRow.Accessor.Severity;
        var emojiAccessor = MixedRow.Accessor.Emoji;
        var messageAccessor = MixedRow.Accessor.Message;
        var progressAccessor = MixedRow.Accessor.Progress;
        var actionCountAccessor = MixedRow.Accessor.ActionCount;

        var doc = new DataGridListDocument<MixedRow>();
        using (doc.BeginUpdate())
        {
            doc
                .AddColumn(new DataGridColumnInfo<int>("id", "🆔", ReadOnly: true, idAccessor))
                .AddColumn(new DataGridColumnInfo<bool>("enabled", "✅ Enabled", ReadOnly: false, enabledAccessor))
                .AddColumn(new DataGridColumnInfo<Severity>("severity", "⚠️ Severity", ReadOnly: false, severityAccessor))
                .AddColumn(new DataGridColumnInfo<string>("emoji", "✨", ReadOnly: true, emojiAccessor))
                .AddColumn(new DataGridColumnInfo<string>("message", "📦 Message", ReadOnly: false, messageAccessor))
                .AddColumn(new DataGridColumnInfo<double>("progress", "📈 Progress", ReadOnly: false, progressAccessor))
                .AddColumn(new DataGridColumnInfo<int>("actioncount", "▶ Runs", ReadOnly: false, actionCountAccessor));
        }

        var emojis = new[] { "🛰️", "🧪", "🧰", "🧭", "🔧", "📡", "🚀", "🧲" };
        var messages = new[]
        {
            "Boot sequence",
            "Telemetry sync",
            "Cache warmup",
            "Running diagnostics",
            "Deploying update",
            "All systems nominal",
            "Packet loss detected",
            "Reconnecting…",
        };

        for (var i = 0; i < 60; i++)
        {
            doc.AddRow(new MixedRow
            {
                Id = i + 1,
                Enabled = (i % 3) != 0,
                Severity = (Severity)(i % 3),
                Emoji = emojis[i % emojis.Length],
                Message = $"{messages[i % messages.Length]} #{i + 1:00}",
                Progress = Math.Round((i % 100) / 10.0, 1),
                ActionCount = i % 4,
            });
        }

        var view = new DataGridDocumentView(doc);
        var grid = new DataGridControl { View = view }
            .ShowHeader(showHeader)
            .ShowRowAnchor(showRowAnchor)
            .RowAnchorWidth(rowAnchorWidth)
            .FilterRowVisible(filterRowVisible)
            .SelectionMode(selectionMode)
            .EditMode(editMode)
            .FrozenColumns(frozenColumns)
            .FrozenRows(frozenRows)
            .ReadOnly(readOnly);

        grid.Columns.Add(new DataGridColumn<int> { Key = "id", TypedValueAccessor = idAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, ReadOnly = true, Sortable = true });
        grid.Columns.Add(new DataGridColumn<bool> { Key = "enabled", TypedValueAccessor = enabledAccessor, Width = GridLength.Auto, Sortable = true });
        grid.Columns.Add(new DataGridColumn<Severity> { Key = "severity", TypedValueAccessor = severityAccessor, Width = GridLength.Auto, Sortable = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "emoji", TypedValueAccessor = emojiAccessor, Width = GridLength.Auto, ReadOnly = true });
        grid.Columns.Add(new DataGridColumn<string> { Key = "message", TypedValueAccessor = messageAccessor, Width = GridLength.Star(2), Sortable = true });
        grid.Columns.Add(new DataGridColumn<double> { Key = "progress", TypedValueAccessor = progressAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });
        grid.Columns.Add(new DataGridColumn<int> { Key = "actioncount", TypedValueAccessor = actionCountAccessor, Width = GridLength.Auto, CellAlignment = TextAlignment.Right, Sortable = true });
        grid.Columns.Add(new DataGridColumn<int>
        {
            Key = "actioncount",
            Header = new TextBlock("▶ Run"),
            TypedValueAccessor = actionCountAccessor,
            Width = GridLength.Fixed(8),
            CellActivationMode = DataGridCellActivationMode.DirectActivate,
            CellTemplate = new(DataGridButtonDisplay, null),
            CellEditorTemplate = new(null, DataGridRunButtonEditor),
        });

        var styled = new Border(new ScrollViewer(grid).MinHeight(12).MaxHeight(12))
            .Style(BorderStyle.Single)
            .Padding(new Thickness(1, 0, 1, 0));
        return styled;
    }

    private static Visual BuildDialogGridDemo(
        State<bool> showHeader,
        State<bool> showRowAnchor,
        State<int> rowAnchorWidth,
        State<bool> filterRowVisible,
        State<DataGridSelectionMode> selectionMode,
        State<DataGridEditMode> editMode,
        State<int> frozenColumns,
        State<int> frozenRows,
        State<bool> readOnly)
    {
        var nameAccessor = DialogGridRow.Accessor.Name;
        var valueAccessor = DialogGridRow.Accessor.Value;

        var doc = new DataGridListDocument<DialogGridRow>()
            .AddColumn(nameAccessor)
            .AddColumn(valueAccessor);

        doc.AddRow(new DialogGridRow { Name = "alpha", Value = "press Esc on the grid" });
        doc.AddRow(new DialogGridRow { Name = "beta", Value = "F2 starts editing first" });
        doc.AddRow(new DialogGridRow { Name = "gamma", Value = "Esc then cancels the active editor" });

        var view = new DataGridDocumentView(doc);

        var grid = new DataGridControl { View = view }
            .ShowHeader(showHeader)
            .ShowRowAnchor(showRowAnchor)
            .RowAnchorWidth(rowAnchorWidth)
            .FilterRowVisible(filterRowVisible)
            .SelectionMode(selectionMode)
            .EditMode(editMode)
            .FrozenColumns(frozenColumns)
            .FrozenRows(frozenRows)
            .ReadOnly(readOnly);

        grid.Columns.Add(new DataGridColumn<string> { Key = nameAccessor.Name, TypedValueAccessor = nameAccessor, Width = GridLength.Auto });
        grid.Columns.Add(new DataGridColumn<string> { Key = valueAccessor.Name, TypedValueAccessor = valueAccessor, Width = GridLength.Star(1) });

        var escapeCount = new State<int>(0);
        var dialog = new Dialog
        {
            Title = "DataGrid Dialog",
            Width = 52,
            Height = 11,
            Content = new ScrollViewer(grid).MinHeight(6).MaxHeight(6),
        };
        dialog.AddCommand(new Command
        {
            Id = "DataGridDemo.DialogEscape",
            LabelMarkup = "Count Esc",
            Gesture = new KeyGesture(TerminalKey.Escape),
            Execute = _ => escapeCount.Value++,
        });

        return new VStack(
                DemoUi.Hint("Click inside the grid, then press Esc while not editing to increment the dialog counter."),
                DemoUi.Hint("When a cell editor is active, Esc still cancels the cell edit first."),
                new TextBlock(() => $"Dialog Escape count: {escapeCount.Value}"),
                dialog)
            .Spacing(1);
    }

    private static Visual DataGridButtonDisplay(DataTemplateValue<int> value, in DataTemplateContext context)
    {
        _ = value;
        _ = context;
        return new Button("Run")
        {
            IsHitTestVisible = false,
        }.Tone(ControlTone.Primary);
    }

    private static Visual DataGridRunButtonEditor(Binding<int> binding, in DataTemplateContext context)
    {
        _ = context;
        return new Button("Run")
            .Tone(ControlTone.Primary)
            .Click(() => binding.SetValue(binding.GetValue() + 1));
    }
}

public sealed partial class SwimRow
{
    public SwimRow()
    {
        Swimmer = string.Empty;
        Country = string.Empty;
    }

    [Bindable] public partial int Lane { get; set; }
    [Bindable] public partial string Swimmer { get; set; }
    [Bindable] public partial string Country { get; set; }
    [Bindable] public partial double Time { get; set; }
}

internal sealed class LedgerRow
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Category { get; set; } = string.Empty;
}

public enum Severity
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

public sealed partial class MixedRow
{
    public MixedRow()
    {
        Emoji = string.Empty;
        Message = string.Empty;
    }

    [Bindable] public partial int Id { get; set; }
    [Bindable] public partial bool Enabled { get; set; }
    [Bindable] public partial Severity Severity { get; set; }
    [Bindable] public partial string Emoji { get; set; }
    [Bindable] public partial string Message { get; set; }
    [Bindable] public partial double Progress { get; set; }
    [Bindable] public partial int ActionCount { get; set; }
}

public sealed partial class DialogGridRow
{
    public DialogGridRow()
    {
        Name = string.Empty;
        Value = string.Empty;
    }

    [Bindable] public partial string Name { get; set; }
    [Bindable] public partial string Value { get; set; }
}
