using System.Text;
using DataRow = System.Data.DataRow;
using DataTable = System.Data.DataTable;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.DataGrid;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;
using Stopwatch = System.Diagnostics.Stopwatch;

using var session = Terminal.Open();

// Shared demo state (bindings show the reactive model in action).
var status = new State<string>("Ready");
var animate = new State<bool>(true);
var userProgress = new State<double>(0.35);
var progress = new State<double>(0.0);

var userName = new State<string?>("Alex");
var issueTitle = new State<string?>("Add search + filters input.");
var notes = new State<string?>(
    """
    Welcome to the Fullscreen demo.

    - Ctrl+P: CommandPalette
    - Ctrl+F/Ctrl+H (TextArea): Find/Replace
    - Ctrl+F (LogControl): Search
    - Right-click the ContextMenu sample
    - F12: debug overlay
    """);
var port = new State<int>(8080);
var cardNumber = new State<string?>("4242424242424242");
var pickedColor = new State<Color>(Color.FromOklch(0.72f, 0.18f, 213f));

var showBackdrop = new State<bool>(false);

var chartTick = new State<int>(0);
var chartValues = new double[96];
var scrollValue = new State<int>(0);

var tasks = new ProgressTaskGroup();
tasks.Tasks.Add(new ProgressTask(new TextBlock("Layout")) { Value = 0.35 });
tasks.Tasks.Add(new ProgressTask(new TextBlock("Rendering")) { Value = 0.62 });
tasks.Tasks.Add(new ProgressTask(new TextBlock("Input")) { Value = 0.28 });

// Theme selection (predefined RootLoops schemes).
var schemes = ColorScheme.GetPredefinedSchemes();
var selectedSchemeIndex = new State<int>(Math.Max(0, schemes.FindIndex(x => x == ColorScheme.ElderberryDarkSoft)));
var selectedAccent = new State<ThemeAccentColor>(ThemeAccentColor.Blue);

var commandPalette = new CommandPalette();
var toastHost = new ToastHost();
var log = new LogControl { MaxCapacity = 5000 }.WrapText(true);

void PushStatus(string message, ToastSeverity? toast = null)
{
    status.Value = message;
    log.AppendLine(message);
    if (toast is { } severity)
    {
        toastHost.Show(new TextBlock(message), severity);
    }
}

Theme BuildTheme()
{
    if (schemes.Count == 0)
    {
        return Theme.Default;
    }

    var index = Math.Clamp(selectedSchemeIndex.Value, 0, schemes.Count - 1);
    return Theme.FromScheme(schemes[index], ThemeSchemeBrightness.Auto, selectedAccent.Value);
}

void ApplyTheme() => toastHost.SetStyle(Theme.Key, BuildTheme());

void ShowModalDialog()
{
    Dialog? dialog = null;

    var content = new VStack(
            new Markup("[bold]Modal dialog[/]") { Wrap = false },
            new TextBlock("Dialogs are hosted as overlay windows.").Wrap(true),
            new TextBlock("Try Tab/Shift+Tab, Enter/Space, and Escape.").Wrap(true),
            new HStack(
                    new Button("Toast").Tone(ControlTone.Primary)
                        .Click(() => toastHost.Show(new Markup("[success]Toast from a Dialog[/]") { Wrap = false }, ToastSeverity.Success)),
                    new Button("Close")
                        .Click(() => dialog!.Close()))
                .Spacing(2))
        .Spacing(1)
        .HorizontalAlignment(Align.Stretch);

    dialog = new Dialog()
        .Title("XenoAtom.Terminal.UI")
        .IsModal(true)
        .Padding(1)
        .Width(64)
        .Content(content);

    dialog.Show();
}

Popup? popup = null;
Button? popupButton = null;

void TogglePopup(Visual? anchor = null)
{
    if (popup is not null)
    {
        popup.Close();
        return;
    }

    anchor ??= popupButton;
    if (anchor is null)
    {
        return;
    }

    popup = new Popup
    {
        Anchor = anchor,
        Placement = PopupPlacement.Below,
        Content = new Group()
            .TopLeftText("Popup")
            .Padding(1)
            .Content(new VStack(
                    new TextBlock("Popups are non-modal overlays."),
                    new TextBlock("Press Escape to close."),
                    new Button("Close").Click(() => popup?.Close()))
                .Spacing(1)),
    };

    popup.Closed((_, _) => popup = null);
    popup.Show();
}

void ExportSvg()
{
    var app = toastHost.App;
    if (app is null)
    {
        PushStatus("Export SVG: app not ready");
        return;
    }

    try
    {
        var svg = app.CaptureSvg();
        var dir = Path.Combine(Environment.CurrentDirectory, "artifacts", "FullscreenDemo");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"fullscreen-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.svg");
        File.WriteAllText(path, svg);
        PushStatus($"Saved: {path}", ToastSeverity.Success);
    }
    catch (Exception ex)
    {
        PushStatus($"Export failed: {ex.Message}", ToastSeverity.Error);
    }
}

static DataGridDataTableDocument BuildDataTable()
{
    var table = new DataTable("issues");
    table.Columns.Add("done", typeof(bool));
    table.Columns.Add("kind", typeof(IssueKind));
    table.Columns.Add("priority", typeof(IssuePriority));
    table.Columns.Add("estimate_h", typeof(int));
    table.Columns.Add("title", typeof(string));
    table.Columns.Add("mood", typeof(string));

    table.Rows.Add(false, IssueKind.Feature, IssuePriority.High, 6, "DataGrid: column drag resize", "🧰");
    table.Rows.Add(true, IssueKind.Bug, IssuePriority.Critical, 2, "Fix selection background", "🩹");
    table.Rows.Add(false, IssueKind.Feature, IssuePriority.Normal, 4, "CommandPalette: fuzzy search", "🧠");
    table.Rows.Add(false, IssueKind.Chore, IssuePriority.Low, 1, "Refactor templates", "🧹");

    for (var i = 0; i < 50; i++)
    {
        table.Rows.Add(false, IssueKind.Feature, IssuePriority.Normal, (i % 8) + 1, $"Issue #{1000 + i}", "✨");
    }

    return new DataGridDataTableDocument(table);
}

var gridDoc = BuildDataTable();
var gridView = new DataGridDocumentView(gridDoc);

var gridSelectionMode = new State<DataGridSelectionMode>(DataGridSelectionMode.Cell);
var gridEditMode = new State<DataGridEditMode>(DataGridEditMode.OnEnter);
var gridReadOnly = new State<bool>(false);
var gridFilterRow = new State<bool>(false);



Visual BuildDashboard()
{
    // Buttons shared across multiple sections (popup uses the button as anchor).
    popupButton = new Button("Popup").Click(() => TogglePopup(popupButton));
    var dialogButton = new Button("Dialog").Click(ShowModalDialog);
    var toastButton = new Button("Toast")
        .Click(() => toastHost.Show(new Markup("[success]Hello from ToastHost[/]") { Wrap = false }, ToastSeverity.Success));

    var backdropButton = new Button("Backdrop")
        .Click(() =>
        {
            showBackdrop.Value = !showBackdrop.Value;
            PushStatus($"Backdrop: {(showBackdrop.Value ? "On" : "Off")}");
        });

    // TextArea + SearchReplacePopup (Find/Replace).
    var textArea = new TextArea()
        .Text(notes)
        .Placeholder("Type multi-line text here…")
        .HorizontalAlignment(Align.Stretch);

    var openReplace = new Button("Find/Replace…")
        .Click(() =>
        {
            textArea.OpenReplace("demo");
            PushStatus("TextArea: opened Find/Replace");
        });

    // Text input / validation / pickers.
    var textBox = new TextBox()
        .Placeholder("Type here…")
        .HorizontalAlignment(Align.Stretch)
        .Text(userName);

    var validatedTitle = new TextBox()
        .Placeholder("Issue title…")
        .HorizontalAlignment(Align.Stretch)
        .Text(issueTitle)
        .Validate(
            issueTitle.Bind.Value,
            value => string.IsNullOrWhiteSpace(value)
                ? new ValidationMessage(ValidationSeverity.Error, new Markup("[error]Title is required.[/]") { Wrap = false })
                : null);

    var numberBox = new NumberBox<int>()
        .Value(port)
        .ValueValidator(v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]")
        .HorizontalAlignment(Align.Stretch);

    var maskedInput = new MaskedInput("9999-9999-9999-9999;_")
        .Value(cardNumber)
        .HorizontalAlignment(Align.Stretch);

    var layoutGrid = new Grid()
        .Columns(
            new ColumnDefinition { Width = GridLength.Auto },
            new ColumnDefinition { Width = GridLength.Star(1) })
        .Rows(
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto })
        .ColumnGap(1)
        .Cell("Name:", 0, 0)
        .Cell(textBox, 0, 1)
        .Cell("Port:", 1, 0)
        .Cell(numberBox, 1, 1)
        .Cell("Card:", 2, 0)
        .Cell(maskedInput, 2, 1)
        .HorizontalAlignment(Align.Stretch);

    // Buttons & toggles.
    var acceptTerms = new State<bool>(true);
    var acceptTermsCheckBox = new CheckBox("Accept terms").IsChecked(acceptTerms);

    var quality = new State<string>("Balanced");
    var radioGroup = new object();

    RadioButton MakeRadio(string label, bool isChecked, string value)
    {
        var rb = new RadioButton(label, radioGroup, isChecked);
        rb.KeyDown((_, e) =>
        {
            if (e.Key is TerminalKey.Space or TerminalKey.Enter)
            {
                quality.Value = value;
            }
        });
        rb.PointerReleased((_, e) =>
        {
            if (e.Button == TerminalMouseButton.Left)
            {
                quality.Value = value;
            }
        });
        return rb;
    }

    var radioBalanced = MakeRadio("Balanced", true, "Balanced");
    var radioFast = MakeRadio("Fast", false, "Fast");
    var radioPretty = MakeRadio("Pretty", false, "Pretty");

    var enableAnimations = new Switch("Animate")
        .IsOn(animate)
        .Toggled((_, e) => PushStatus($"Animate: {e.NewValue}"));

    var progressSlider = new Slider<double>
        {
            Minimum = 0,
            Maximum = 1,
            Step = 0.01,
            LargeStep = 0.1,
        }
        .Value(userProgress)
        .IsEnabled(() => !animate.Value)
        .ShowValueLabel(true)
        .HorizontalAlignment(Align.Stretch);

    var primaryButton = new Button("Primary").Tone(ControlTone.Primary)
        .Click(() => PushStatus("Button: Primary clicked", ToastSeverity.Info));
    var successButton = new Button("Success").Tone(ControlTone.Success)
        .Click(() => PushStatus("Button: Success clicked", ToastSeverity.Success));
    var warningButton = new Button("Warning").Tone(ControlTone.Warning)
        .Click(() => PushStatus("Button: Warning clicked", ToastSeverity.Warning));
    var dangerButton = new Button("Error").Tone(ControlTone.Error)
        .Click(() => PushStatus("Button: Error clicked", ToastSeverity.Error));

    var overlaysRow = new HStack(primaryButton, successButton, warningButton, dangerButton, openReplace)
        .Spacing(2)
        .HorizontalAlignment(Align.Stretch);

    var link = new Link("https://github.com/XenoAtom/XenoAtom.Terminal.UI", "GitHub")
        .Opened((_, e) => PushStatus($"Link opened: {e.Uri}", ToastSeverity.Info))
        .Tooltip(new TextBlock("Opens in your default browser."));

    // Lists & selection.
    var select = new Select<string>()
        .Items(["First", "Second", "Third", "Fourth"])
        .HorizontalAlignment(Align.Stretch);

    var listBox = new ListBox<string>()
        .MinHeight(6)
        .MaxHeight(6)
        .HorizontalAlignment(Align.Stretch);
    listBox.Items.AddRange(
        "Arrakis", "Caladan", "Chusuk", "Ginaz", "Giedi Prime", "Kaitain", "Ix", "Salusa Secundus", "Tleilax", "Wallach IX",
        "Ecaz", "Grumman", "Richese", "Lankiveil", "Junction", "Rossak", "Synchrony", "Otheym", "Tupile", "Poritrin");

    var selectionList = new SelectionList<string>()
        .MinHeight(6)
        .MaxHeight(6)
        .HorizontalAlignment(Align.Stretch);
    selectionList
        .AddItem("Alpha", isChecked: true)
        .AddItem("Beta")
        .AddItem("Gamma")
        .AddItem("Delta")
        .AddItem("Epsilon")
        .AddItem("Zeta")
        .AddItem("Eta")
        .AddItem("Theta")
        .AddItem("Iota");

    var optionList = new OptionList<OptionListItem>()
        .MinHeight(6)
        .MaxHeight(6)
        .HorizontalAlignment(Align.Stretch);
    optionList.Items.AddRange(
        new OptionListItem("Build", "Ctrl+B") { Description = "Build the project" },
        new OptionListItem("Run", "F5") { Description = "Run the app" },
        new OptionListItem("Search", "Ctrl+F") { Description = "Search in the focused control" },
        new OptionListItem("Theme", "Alt+T") { Description = "Pick a predefined theme" },
        new OptionListItem("Export SVG", "F8") { Description = "Capture current frame buffer" });
    optionList.ItemActivated((_, e) => PushStatus($"OptionList: activated item #{e.Index}", ToastSeverity.Info));

    var tree = new TreeView().HorizontalAlignment(Align.Stretch);
    var treeRoot = new TreeNode("XenoAtom") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true };
    treeRoot.Children.Add(new TreeNode("src") { Icon = TreeNodeIcons.FolderGlyph, IsExpanded = true });
    treeRoot.Children[0].Children.Add(new TreeNode("Program.cs") { Icon = TreeNodeIcons.FileGlyph });
    treeRoot.Children[0].Children.Add(new TreeNode("readme.md") { Icon = TreeNodeIcons.DocumentGlyph });
    treeRoot.Children.Add(new TreeNode("site") { Icon = TreeNodeIcons.FolderGlyph });
    tree.Roots.Add(treeRoot);

    var wrap = new WrapHStack()
        .Spacing(1)
        .Add(
            new Button("Chip A"),
            new Button("Chip B"),
            new Button("Chip C"),
            new Button("Chip D"),
            new Button("Chip E"),
            new Button("Chip F"),
            new Button("Chip G"),
            new Button("Chip H")
            );

    var padded = new Border(
            new Center(new Markup("[dim]Padder + Center[/]") { Wrap = false }.Pad(new Thickness(2, 0, 2, 0))))
        .Style(BorderStyle.Single)
        .Padding(Thickness.Zero)
        .HorizontalAlignment(Align.Stretch);

    // Visuals: Canvas + charts + progress tasks.
    Canvas spectrum = null!;
    spectrum = new Canvas()
        .MinHeight(8)
        .MaxHeight(8)
        .Painter(ctx =>
        {
            var theme = spectrum.GetTheme();
            var baseStyle = theme.BaseTextStyle();

            var width = Math.Max(1, ctx.Bounds.Width);
            var height = Math.Max(1, ctx.Bounds.Height);

            const float chroma = 0.18f;
            const float topLightness = 0.92f;
            const float bottomLightness = 0.25f;

            for (var y = 0; y < height; y++)
            {
                var yt = height <= 1 ? 0f : y / (float)(height - 1);
                var l = topLightness + ((bottomLightness - topLightness) * yt);

                for (var x = 0; x < width; x++)
                {
                    var xt = width <= 1 ? 0f : x / (float)(width - 1);
                    var hueDegrees = xt * 360f;
                    var color = Color.FromOklch(l, chroma, hueDegrees);
                    ctx.SetPixel(x, y, new Rune(' '), baseStyle.WithBackground(color));
                }
            }

            const byte a = 160;
            var boxStyleA = baseStyle.WithBackground(Color.RgbA(0x50, 0x9A, 0xF6, a));
            var boxStyleB = baseStyle.WithBackground(Color.RgbA(0xCA, 0x64, 0xF3, a));
            var boxStyleC = baseStyle.WithBackground(Color.RgbA(0x67, 0xAF, 0x34, a));

            var w = Math.Max(6, width / 4);
            var h = Math.Max(3, height / 2);
            ctx.FillRect(1, 1, w, h, new Rune(' '), boxStyleA);
            ctx.FillRect(2 + (w / 2), 1 + (h / 4), w, h, new Rune(' '), boxStyleB);
            ctx.FillRect(3 + (w / 3), 2 + (h / 2), w, h, new Rune(' '), boxStyleC);
        });

    var sparkline = new Sparkline()
        .Style(SparklineStyle.Default with { Glyphs = SparklineGlyphs.Blocks8 })
        .HorizontalAlignment(Align.Stretch)
        .Update(x =>
        {
            x.Values(chartValues);
            _ = chartTick.Value;
        });

    var lineChart = new LineChart()
        .MinHeight(4)
        .MaxHeight(4)
        .HorizontalAlignment(Align.Stretch)
        .Update(x =>
        {
            x.Values(chartValues);
            _ = chartTick.Value;
        });

    var barChart = new BarChart()
        .Title("Segments")
        .Minimum(0.0)
        .Maximum(1.0)
        .ShowValues(false)
        .ShowPercentages(true)
        .Items(
            new BarChartItem("A", 0),
            new BarChartItem("B", 0),
            new BarChartItem("C", 0),
            new BarChartItem("D", 0),
            new BarChartItem("E", 0))
        .MinHeight(6)
        .MaxHeight(6)
        .HorizontalAlignment(Align.Stretch)
        .Update(chart =>
        {
            for (var i = 0; i < chart.Items.Count; i++)
            {
                chart.Items[i].Value = chartValues[(i * chartValues.Length / 4) % chartValues.Length];
            }
            _ = chartTick.Value;
        });

    var breakdown = new BreakdownChart()
        .Title(new TextBlock("Breakdown"))
        .ShowValues(true)
        .ShowPercentages(true)
        .HorizontalAlignment(Align.Stretch);

    var breakdownScheme = ColorScheme.RootLoopsDark;
    breakdown.Segments.Add(new BreakdownSegment(35, new TextBlock("UI")) { Color = breakdownScheme.Blue });
    breakdown.Segments.Add(new BreakdownSegment(18, new TextBlock("IO")) { Color = breakdownScheme.Cyan });
    breakdown.Segments.Add(new BreakdownSegment(27, new TextBlock("Data")) { Color = breakdownScheme.Purple });
    breakdown.Segments.Add(new BreakdownSegment(20, new TextBlock("Docs")) { Color = breakdownScheme.Yellow });

    breakdown.SegmentClicked((_, e) =>
    {
        var seg = breakdown.Segments[e.Index];
        PushStatus($"Breakdown: {(seg.Label as TextBlock)?.Text ?? "Segment"} clicked");
    });

    var tabSample = new TabControl(
            new TabPage("Overview", new VStack(
                    new Markup("[bold]TabControl[/]") { Wrap = false },
                    new TextBlock("Used for navigation and compact content grouping.").Wrap(true)).Spacing(1)),
            new TabPage("Markup", new Markup("[primary]Markup[/] works here too.") { Wrap = true }),
            new TabPage("Notes", new TextBlock(() => $"Hello, {userName.Value}!").Wrap(true)))
        .MinHeight(6)
        .MaxHeight(6)
        .HorizontalAlignment(Align.Stretch);

    // Context menu sample.
    var contextMenuTarget = new TextBlock("Right-click this text to open a ContextMenu.")
        .Wrap(true)
        .PointerPressed((sender, e) =>
        {
            if (e.Button != TerminalMouseButton.Right || sender is not Visual v)
            {
                return;
            }

            var items = new MenuItem[]
            {
                new("Copy status", () => v.App?.Terminal.Clipboard.TrySetText(status.Value)) { Shortcut = "Ctrl+C" },
                new("Toast: success", () => toastHost.Show(new Markup("[success]Success![/]") { Wrap = false }, ToastSeverity.Success)),
                MenuItem.CreateSeparator(),
                new("Toggle Backdrop", () => showBackdrop.Value = !showBackdrop.Value),
            };

            _ = ContextMenuService.Show(v, items, e.UiX, e.UiY);
            e.Handled = true;
        });

    // Left pane content.
    var banner = new TextFiglet("XenoAtom")
        .Font(FigletPredefinedFont.Small)
        .TextAlignment(TextAlignment.Left);

    var leftPane = new HSplitter(
        new VStack(
            new Group()
                .TopLeftText("Text input")
                .TopRightText("Tab to move focus")
                .Padding(1)
                .Content(new VStack(
                        layoutGrid,
                        new Rule().Style(RuleStyle.Default with { Glyphs = RuleGlyphs.Dotted }),
                        new VStack("Title:", validatedTitle).HorizontalAlignment(Align.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(Align.Stretch)).VerticalAlignment(Align.Stretch).HorizontalAlignment(Align.Stretch),
            new Group()
                .TopLeftText("Lists & selection")
                .TopRightText("Wheel scroll")
                .Padding(1)
                .Content(new VStack(
                        new HStack(
                            new VStack("Select:", select).Spacing(1),
                            new VStack("OptionList:", optionList).Spacing(1)
                        ).Spacing(2).HorizontalAlignment(Align.Stretch),
                        new HStack(
                                new VStack("ListBox:", listBox).Spacing(1).HorizontalAlignment(Align.Stretch),
                                new VStack("SelectionList:", selectionList).Spacing(1).HorizontalAlignment(Align.Stretch))
                            .Spacing(2)
                            .HorizontalAlignment(Align.Stretch),
                        new VStack("TreeView:", tree.MinHeight(6).MaxHeight(6)).Spacing(1).HorizontalAlignment(Align.Stretch))
                    .Spacing(1)
                    .VerticalAlignment(Align.Stretch)
                    .HorizontalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch)).VerticalAlignment(Align.Stretch),
        new VStack(
            new Group()
                .TopLeftText("Buttons & toggles")
                .TopRightText("Click / Space / Enter")
                .Padding(1)
                .Content(new VStack(
                        overlaysRow,
                        new HStack(
                                new VStack("CheckBox:", acceptTermsCheckBox, new TextBlock(() => acceptTerms.Value ? "Accepted" : "Not accepted")).Spacing(1),
                                new VStack("Switch:", enableAnimations, new VStack("Manual progress:", progressSlider).Spacing(0)).Spacing(1).HorizontalAlignment(Align.Stretch),
                                new VStack("Radio:", new HStack(radioBalanced, radioFast, radioPretty).Spacing(2), new TextBlock(() => $"Quality: {quality.Value}")).Spacing(1))
                            .Spacing(3)
                            .HorizontalAlignment(Align.Stretch),
                        new HStack("Link:", link).Spacing(1))
                    .Spacing(1)
                    .HorizontalAlignment(Align.Stretch)).VerticalAlignment(Align.Stretch).HorizontalAlignment(Align.Stretch),
            new Group()
                .TopLeftText("Layout & scrolling")
                .TopRightText("Wrap / Pad / ScrollBar")
                .Padding(1)
                .Content(new VStack(
                        new VStack("WrapHStack:", wrap).Spacing(1),
                        new VStack("Padder + Center:", padded).Spacing(1),
                        new VStack("TabControl:", tabSample).Spacing(1)
                    ).Spacing(1).VerticalAlignment(Align.Stretch)
                ).HorizontalAlignment(Align.Stretch),
            new Center(banner)
        ).VerticalAlignment(Align.Stretch)
    ).Ratio(0.5);
        


    var visualsPanel = new Group()
        .TopLeftText("Canvas & charts")
        .TopRightText("Live")
        .Padding(1)
        .Content(new VStack(
                spectrum.HorizontalAlignment(Align.Stretch),
                new HStack(
                        new ProgressBar().Value(progress).HorizontalAlignment(Align.Stretch).Style(ProgressBarStyle.Shaded),
                        new Spinner())
                    .Spacing(2)
                    .HorizontalAlignment(Align.Stretch),
                sparkline,
                lineChart,
                barChart,
                breakdown)
            .VerticalAlignment(Align.Stretch)
            .HorizontalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);

    var miniTable = new Table()
        .Headers("Task", "Status")
        .AddRow(new Markup("[primary]Download[/]") { Wrap = false }, new Markup("[warning]Running[/]") { Wrap = false })
        .AddRow("Render", new Markup("[success]OK[/]") { Wrap = false })
        .AddRow("Tests", new Markup("[success]OK[/]") { Wrap = false })
        .Style(TableStyle.Minimal)
        .HorizontalAlignment(Align.Stretch);

    //var overlaysPanel = new Group()
    //    .TopLeftText("Overlays")
    //    .TopRightText("Popup / Dialog / Toast")
    //    .Padding(1)
    //    .Content(new VStack(
    //            new Markup("[dim]Tip:[/] Right-click the ContextMenu sample below.") { Wrap = true },
    //            contextMenuTarget,
    //            new Rule().Style(RuleStyle.Default with { Glyphs = RuleGlyphs.Dotted }),
    //            new HStack(
    //                    CreatePopupLauncher("Popup"),
    //                    new Button("Dialog").Click(ShowModalDialog),
    //                    new Button("Toast").Click(() => toastHost.Show(new Markup("[success]Hello![/]") { Wrap = false }, ToastSeverity.Success)),
    //                    new Button("Backdrop").Click(() => showBackdrop.Value = !showBackdrop.Value))
    //                .Spacing(2),
    //            new VStack("Table:", miniTable).Spacing(1))
    //        .Spacing(1)
    //        .HorizontalAlignment(Align.Stretch));

    var colorPicker = new HStack(
        new Group().TopLeftText("ColorPicker").Content(new ColorPicker()
            .AllowAlpha(true)
            .ShowPalette(true)
            .Value(pickedColor)
            .HorizontalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch),
        new Group().TopLeftText("ProgressTaskGroup").Padding(1).Content(tasks).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch);

    var rightPane = new VStack(visualsPanel, colorPicker).Spacing(0)
        .HorizontalAlignment(Align.Stretch)
        .VerticalAlignment(Align.Stretch);
    

    // Bottom panes.
    log.AppendMarkupLine("[dim]Welcome! This panel is a LogControl (tail -f style).[/]");
    log.AppendMarkupLine("[dim]Try Ctrl+F to open the search popup.[/]");

    var bottomLeft = new Group()
        .TopLeftText("TextArea")
        .TopRightText("Ctrl+F find • Ctrl+H replace")
        .Padding(1)
        .Content(new VStack(
                textArea.VerticalAlignment(Align.Stretch),
                new HStack(
                        new Button("Append note").Click(() =>
                        {
                            notes.Value = (notes.Value ?? string.Empty) + "\n- Another line…";
                            PushStatus("TextArea: appended note");
                        }),
                        new Button("Clear").Click(() => notes.Value = string.Empty),
                        new Button("Focus").Click(() => toastHost.App?.Focus(textArea)))
                    .Spacing(2).VerticalAlignment(Align.End))
            .Spacing(1)).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);

    var bottomCenter = new Group()
        .TopLeftText("LogControl")
        .TopRightText("Ctrl+F search • Ctrl+C copy")
        .Padding(1)
        .Content(log.MinHeight(10).HorizontalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);

    var dataGrid = new DataGridControl
        {
            View = gridView,
        }
        .ShowHeader(true)
        .ShowRowAnchor(true)
        .RowAnchorWidth(1)
        .SelectionMode(gridSelectionMode)
        .EditMode(gridEditMode)
        .ReadOnly(gridReadOnly)
        .FilterRowVisible(gridFilterRow)
        .FrozenColumns(1);

    dataGrid.Columns.Add(new DataGridColumn<bool>
    {
        Key = "done",
        TypedValueAccessor = new BindingAccessor<bool>("done", o => (bool)((DataRow)o)["done"], (o, v) => ((DataRow)o)["done"] = v),
        Width = GridLength.Auto,
    });
    dataGrid.Columns.Add(new DataGridColumn<IssueKind>
    {
        Key = "kind",
        TypedValueAccessor = new BindingAccessor<IssueKind>("kind", o => (IssueKind)((DataRow)o)["kind"], (o, v) => ((DataRow)o)["kind"] = v),
        Width = GridLength.Auto,
    });
    dataGrid.Columns.Add(new DataGridColumn<IssuePriority>
    {
        Key = "priority",
        TypedValueAccessor = new BindingAccessor<IssuePriority>("priority", o => (IssuePriority)((DataRow)o)["priority"], (o, v) => ((DataRow)o)["priority"] = v),
        Width = GridLength.Auto,
    });
    dataGrid.Columns.Add(new DataGridColumn<int>
    {
        Key = "estimate_h",
        TypedValueAccessor = new BindingAccessor<int>("estimate_h", o => (int)((DataRow)o)["estimate_h"], (o, v) => ((DataRow)o)["estimate_h"] = v),
        Width = GridLength.Auto,
        CellAlignment = TextAlignment.Right,
    });
    dataGrid.Columns.Add(new DataGridColumn<string>
    {
        Key = "title",
        TypedValueAccessor = new BindingAccessor<string>("title", o => (string)((DataRow)o)["title"], (o, v) => ((DataRow)o)["title"] = v),
        Width = GridLength.Star(3),
    });
    dataGrid.Columns.Add(new DataGridColumn<string>
    {
        Key = "mood",
        TypedValueAccessor = new BindingAccessor<string>("mood", o => (string)((DataRow)o)["mood"], (o, v) => ((DataRow)o)["mood"] = v),
        Width = GridLength.Auto,
    });


    // Right pane content.
    var dataGridPanel = new Group()
        .TopLeftText("DataGridControl")
        .TopRightText("Ctrl+F search • F2 edit")
        .Padding(1)
        .Content(new VStack(
                new HStack(
                        new CheckBox("Filter row").IsChecked(gridFilterRow),
                        new CheckBox("Read-only").IsChecked(gridReadOnly),
                        "Selection:",
                        new EnumSelect<DataGridSelectionMode>().Value(gridSelectionMode).MaxWidth(10),
                        "Edit:",
                        new EnumSelect<DataGridEditMode>().Value(gridEditMode).MaxWidth(10))
                    .Spacing(1)
                    .HorizontalAlignment(Align.Stretch),
                new Border(new ScrollViewer(dataGrid))
                    .Style(BorderStyle.Rounded)
                    .Padding(new Thickness(1, 0, 1, 0))
                    .HorizontalAlignment(Align.Stretch))
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch)).HorizontalAlignment(Align.Stretch).VerticalAlignment(Align.Stretch);

    //var bottomRight

    var content = new VSplitter(
            new HSplitter(leftPane, rightPane).Ratio(140.0/240),
            new HSplitter(new HSplitter(bottomLeft, bottomCenter).Ratio(0.50), dataGridPanel).Ratio(2.0/3))
        .MinFirst(22)
        .MinSecond(12)
        .Ratio(0.72)
        .HorizontalAlignment(Align.Stretch)
        .VerticalAlignment(Align.Stretch);

    // Theme selector UI for footer.
    var schemeSelect = new Select<ColorScheme>()
        .Items(schemes)
        .SelectedIndex(selectedSchemeIndex)
        .ItemTemplate(new DataTemplate<ColorScheme>(Display: (DataTemplateValue<ColorScheme> v, in DataTemplateContext _) => new TextBlock(v.GetValue().Name), Editor: null))
        .MaxWidth(24);

    schemeSelect.SelectionChanged((_, _) =>
    {
        ApplyTheme();
        var idx = Math.Clamp(selectedSchemeIndex.Value, 0, Math.Max(0, schemes.Count - 1));
        PushStatus($"Theme: {schemes[idx].Name}");
    });

    var accentSelect = new EnumSelect<ThemeAccentColor>()
        .Value(selectedAccent)
        .MaxWidth(12);

    accentSelect.SelectionChanged((_, _) =>
    {
        ApplyTheme();
        PushStatus($"Accent: {selectedAccent.Value}");
    });

    // Header / menu / footer.
    var header = new Header
    {
        Left = new Markup("[bold]XenoAtom.Terminal.UI[/] Fullscreen Demo") { Wrap = false },
        Center = new Markup("[dim]Tab focus • mouse wheel scroll • Ctrl+P palette • Ctrl+Q quit[/]") { Wrap = false },
        Right = new TextBlock(() => $"{(int)(progress.Value * 100),3}%"),
    };

    var menu = new MenuBar();
    var menuFile = new MenuItem("File");
    menuFile.Items.Add(new MenuItem("Command palette", commandPalette.Show) { Shortcut = "Ctrl+P" });
    menuFile.Items.Add(new MenuItem("Dialog", ShowModalDialog) { Shortcut = "Ctrl+M" });
    menuFile.Items.Add(new MenuItem("Popup", () => TogglePopup(popupButton)));
    menuFile.Items.Add(new MenuItem("Export SVG", ExportSvg) { Shortcut = "F8" });
    menuFile.Items.Add(MenuItem.CreateSeparator());
    menuFile.Items.Add(new MenuItem("Toggle Backdrop", () => showBackdrop.Value = !showBackdrop.Value));
    menuFile.Items.Add(new MenuItem("Toast", () => toastHost.Show(new Markup("[success]Hello![/]") { Wrap = false }, ToastSeverity.Success)));
    menu.Items.Add(menuFile);

    var menuView = new MenuItem("View");
    menuView.Items.Add(new MenuItem("Theme…", () => toastHost.App?.Focus(schemeSelect)));
    menuView.Items.Add(new MenuItem("Accent…", () => toastHost.App?.Focus(accentSelect)));
    menuView.Items.Add(MenuItem.CreateSeparator());
    menuView.Items.Add(new MenuItem("Reset status", () => status.Value = "Ready"));
    menu.Items.Add(menuView);

    var menuHelp = new MenuItem("Help");
    menuHelp.Items.Add(new MenuItem("About", () => toastHost.Show(new Markup("[primary]XenoAtom.Terminal.UI[/]") { Wrap = false }, ToastSeverity.Info)));
    menu.Items.Add(menuHelp);

    var footer = new Footer
    {
        Left = new TextBlock(() => $"Status: {status.Value}"),
        Center = new HStack("Theme:", schemeSelect, "Accent:", accentSelect).Spacing(1),
        Right = new TextBlock(() => $"Progress: {(int)(progress.Value * 100)}%"),
    };

    // Backdrop overlay demo.
    var root = new DockLayout()
        .HorizontalAlignment(Align.Stretch)
        .VerticalAlignment(Align.Stretch)
        .Top(new VStack(header, menu).Spacing(0))
        .Content(new ZStack(
            content,
            new ComputedVisual(() =>
            {
                if (!showBackdrop.Value)
                {
                    return null;
                }

                return new ZStack(
                    new Backdrop().IsEnabled(false),
                    new Center().Content(
                        new Group()
                            .TopLeftText("Backdrop")
                            .Padding(1)
                            .Content(new VStack(
                                    new TextBlock("This is the Backdrop control (RGBA dim surface).").Wrap(true),
                                    new Button("Close").Click(() => showBackdrop.Value = false))
                                .Spacing(1))));
            })))
        .Bottom(new VStack(new CommandBar(), footer).Spacing(0));

    return root;
}

toastHost.Content(BuildDashboard());
ApplyTheme();

toastHost.AddCommand(new Command
{
    Id = "App.CommandPalette",
    LabelMarkup = "Command palette",
    Gesture = new KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl),
    Importance = CommandImportance.Secondary,
    Presentation = CommandPresentation.CommandBar | CommandPresentation.CommandPalette,
    Execute = _ => commandPalette.Show(),
});

toastHost.AddCommand(new Command
{
    Id = "Demo.Dialog",
    LabelMarkup = "Dialog",
    DescriptionMarkup = "Show a modal dialog.",
    Gesture = new KeyGesture(TerminalChar.CtrlM, TerminalModifiers.Ctrl),
    Importance = CommandImportance.Primary,
    Presentation = CommandPresentation.CommandPalette,
    Execute = _ => ShowModalDialog(),
});

toastHost.AddCommand(new Command
{
    Id = "Demo.Popup",
    LabelMarkup = "Popup",
    DescriptionMarkup = "Toggle a popup anchored to the Popup button.",
    Importance = CommandImportance.Primary,
    Presentation = CommandPresentation.CommandPalette,
    Execute = _ => TogglePopup(),
});

toastHost.AddCommand(new Command
{
    Id = "Demo.Toast",
    LabelMarkup = "Toast",
    DescriptionMarkup = "Show a toast notification.",
    Importance = CommandImportance.Primary,
    Presentation = CommandPresentation.CommandPalette,
    Execute = _ => toastHost.Show(new Markup("[success]Toast notification[/]") { Wrap = false }, ToastSeverity.Success),
});

toastHost.AddCommand(new Command
{
    Id = "Demo.ToggleBackdrop",
    LabelMarkup = "Toggle Backdrop",
    DescriptionMarkup = "Show/hide the Backdrop overlay.",
    Importance = CommandImportance.Secondary,
    Presentation = CommandPresentation.CommandPalette,
    Execute = _ => showBackdrop.Value = !showBackdrop.Value,
});

toastHost.AddCommand(new Command
{
    Id = "Demo.ExportSvg",
    LabelMarkup = "Export SVG",
    DescriptionMarkup = "Capture the current frame buffer as SVG (writes to artifacts).",
    Gesture = new KeyGesture(TerminalKey.F8),
    Importance = CommandImportance.Secondary,
    Presentation = CommandPresentation.CommandPalette,
    Execute = _ => ExportSvg(),
});

var lastTick = Stopwatch.GetTimestamp();
var t = 0.0;
var rnd = new Random(1234);

Terminal.Run(toastHost, () =>
{
    var now = Stopwatch.GetTimestamp();
    if (Stopwatch.GetElapsedTime(lastTick, now) < TimeSpan.FromMilliseconds(50))
    {
        return TerminalLoopResult.Continue;
    }

    lastTick = now;
    if (animate.Value)
    {
        t += 0.018;
        progress.Value = (Math.Sin(t) + 1.0) / 2.0;
    }
    else
    {
        progress.Value = Math.Clamp(userProgress.Value, 0.0, 1.0);
    }

    Array.Copy(chartValues, 1, chartValues, 0, chartValues.Length - 1);
    chartValues[^1] = Math.Clamp(progress.Value, 0.0, 1.0);
    chartTick.Value++;

    if (tasks.Tasks.Count >= 3)
    {
        tasks.Tasks[0].Value = 0.15 + (progress.Value * 0.85);
        tasks.Tasks[1].Value = 0.30 + (progress.Value * 0.70);
        tasks.Tasks[2].Value = 0.05 + (Math.Abs(Math.Sin(t * 1.7)) * 0.95);
    }

    scrollValue.Value = (int)Math.Round(progress.Value * 100);

    return TerminalLoopResult.Continue;
});

internal enum IssueKind
{
    Bug,
    Feature,
    Chore,
}

internal enum IssuePriority
{
    Low,
    Normal,
    High,
    Critical,
}
