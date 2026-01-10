using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;
using Stopwatch = System.Diagnostics.Stopwatch;

using var session = Terminal.Open();

var statusState = new State<string>("ready");
var progressState = new State<double>(0.0);
var sliderState = new State<double>(0.35);
var switcherIndexState = new State<int>(0);
var switchState = new State<bool>(false);

var select = new Select();
select.Items.AddRange(
    new SelectItem("First"),
    new SelectItem("Second"),
    new SelectItem("Third"),
    new SelectItem("Fourth"));

var selectionList = new SelectionList()
    .Height(5);
selectionList.Items.AddRange(
    new SelectionListItem("Arrakis", isChecked: true),
    new SelectionListItem("Caladan"),
    new SelectionListItem("Chusuk"),
    new SelectionListItem("Giedi Prime"),
    new SelectionListItem("Ginaz"),
    new SelectionListItem("Grumman"),
    new SelectionListItem("Kaitain"));

var textArea = new TextArea()
    .Text("Line 1\nLine 2\nLine 3")
    .Placeholder("Type multi-line text here...");

var tree = new TreeView().Height(8);
var treeRoot = new TreeNode("XenoAtom") { Icon = TreeNodeIcon.Folder, IsExpanded = true };
treeRoot.Children.Add(new TreeNode("src") { Icon = TreeNodeIcon.Folder, IsExpanded = true });
treeRoot.Children[0].Children.Add(new TreeNode("Program.cs") { Icon = TreeNodeIcon.File });
treeRoot.Children[0].Children.Add(new TreeNode("readme.md") { Icon = TreeNodeIcon.Document });
tree.Roots.Add(treeRoot);

var showDialog = new Button("Show modal")
    .HorizontalAlignment(HorizontalAlignment.Left)
    .Click(() =>
    {
        Dialog? dialog = null;

        var dialogContent = new VStack(
            "Modal dialog",
            new TextBlock("This is a wrapped paragraph demonstrating document-style text rendering.").Wrap(true),
            new Button("Close")
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Click(() => dialog!.Close()))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        dialog = new Dialog()
            .Title("Modal dialog")
            .IsModal(true)
            .Padding(1)
            .Width(60)
            .Content(dialogContent);

        dialog.Show();
    });


// Disabling this part for now, as the custom color on the selection is not nice with the default theme.
//main.SetEnvironmentValue(Theme.Key, new Theme
//{
//    Foreground = Theme.Default.Foreground,
//    Background = Theme.Default.Background,
//    Border = Theme.Default.Border,
//    FocusBorder = Theme.Default.FocusBorder,
//    Accent = Theme.Default.Accent,
//    Selection = AnsiColor.Rgb(0x00, 0xFF, 0x00),
//    Disabled = Theme.Default.Disabled,
//});

var progressBars = new VStack(
    "Progress variants:",
    new HStack(
            new ProgressBar()
                .Label("Thin")
                .Value(progressState)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Style(ProgressBarStyle.Thin),
            new ProgressBar()
                .Label("Segmented")
                .Value(progressState)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Style(ProgressBarStyle.Segmented))
        .Spacing(2)
        .HorizontalAlignment(HorizontalAlignment.Stretch),
    new HStack(
            new ProgressBar()
                .Label("Shaded")
                .Value(progressState)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Style(ProgressBarStyle.Shaded),
            new ProgressBar()
                .Label("Bracketed")
                .Value(progressState)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Style(ProgressBarStyle.Bracketed))
        .Spacing(2)
        .HorizontalAlignment(HorizontalAlignment.Stretch))
    .Spacing(0)
    .HorizontalAlignment(HorizontalAlignment.Stretch);

var leftColumn = new VStack(
        new TextBox()
            .Text("Type here (Ctrl+A, Shift+Arrows, Ctrl+Left/Right)")
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new CheckBox().Text("Accept terms"),
        showDialog,
        new HStack(
                new Table()
                    .Headers("Task", "Status")
                    .AddRow("Download", "Running")
                    .AddRow("Render", "OK")
                    .Style(TableStyle.Minimal)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new Table()
                    .Headers("Task", "Status")
                    .AddRow("Download", "Running")
                    .AddRow("Render", "OK")
                    .Style(TableStyle.DoubleGrid)
                    .HorizontalAlignment(HorizontalAlignment.Stretch))
            .Spacing(2)
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new HStack(
                "Slider:",
                new Slider()
                    .Minimum(0)
                    .Maximum(1)
                    .Step(0.05)
                    .LargeStep(0.2)
                    .Value(sliderState.Value)
                    .ShowValueLabel(true)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .ValueChanged((_, e) => sliderState.Value = e.NewValue))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        progressBars)
    .Spacing(0)
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch);

var rightColumn = new VStack(
        new VSplitter(
                new Center().Content("Top pane (VSplitter)"),
                new Center().Content("Bottom pane (drag splitter bar)"))
            .MinHeight(4)
            .MaxHeight(4)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Ratio(0.5),
        new TabControl(new[]
            {
                new TabPage(
                    new HStack(
                            new Spinner().Style(SpinnerStyles.Dots3),
                            "Status",
                            new TextBlock()
                                .Text(() => statusState.Value)
                                .Trimming(TextTrimming.EndEllipsis)
                                .MaxWidth(12))
                        .Spacing(1),
                    new VStack(
                            "This is the Status tab.",
                            new Markup("[bold]Markup:[/] [green]success[/], [yellow]warning[/], [red]error[/].")
                                .Wrap(true),
                            new TextBlock()
                                .Text(() => $"Current status: {statusState.Value}"),
                            new HStack(
                                    new TextBlock("Trimming:"),
                                    new TextBlock("This is a very long piece of text that will be trimmed.")
                                        .Trimming(TextTrimming.EndEllipsis)
                                        .MaxWidth(24))
                                .Spacing(1),
                            new Group()
                                .TopLeftText("Grid")
                                .Padding(Thickness.Zero)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(
                                    new Grid()
                                        .Columns(
                                            new ColumnDefinition { Width = GridLength.Auto },
                                            new ColumnDefinition { Width = GridLength.Star(1) })
                                        .Rows(
                                            new RowDefinition { Height = GridLength.Auto },
                                            new RowDefinition { Height = GridLength.Auto },
                                            new RowDefinition { Height = GridLength.Auto })
                                        .ColumnGap(1)
                                        .Add(
                                            new TextBlock("User:").Row(0).Column(0),
                                            new TextBox().Text("alex").Row(0).Column(1).HorizontalAlignment(HorizontalAlignment.Stretch),
                                            new TextBlock("Password:").Row(1).Column(0),
                                            new MaskedInput().Text("hunter2").RevealMode(MaskedInputRevealMode.WhileFocused).Row(1).Column(1).HorizontalAlignment(HorizontalAlignment.Stretch),
                                            new TextBlock("Status:").Row(2).Column(0),
                                            new TextBlock().Text(() => statusState.Value).Row(2).Column(1))),
                            new Rule
                            {
                                StartLabel = "Start",
                                CenterLabel = "Rule",
                                EndLabel = "End",
                            }.Style(RuleStyle.Default with { Glyphs = RuleGlyphs.Dotted })
                                .HorizontalAlignment(HorizontalAlignment.Stretch),
                            "Spinners:",
                            new VStack(
                                    new HStack(
                                            new Spinner("Syncing").Style(SpinnerStyles.Dots2),
                                            new Spinner().Style(SpinnerStyles.BouncingBar))
                                        .Spacing(2),
                                    new HStack(
                                            new Spinner("Rendering").Style(SpinnerStyles.Line),
                                            new Spinner().Style(SpinnerStyles.Wave))
                                        .Spacing(2),
                                    new HStack(
                                            new Spinner("Launch").Style(SpinnerStyles.Rocket),
                                            new Spinner().Style(SpinnerStyles.DotsEllipsis2))
                                        .Spacing(2))
                                .Spacing(0),
                            new Button("Click me (mouse or Enter)")
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Click(() => statusState.Value = "click received"))
                        .Spacing(1)),
                new TabPage(
                    new HStack(
                            new Spinner().Style(SpinnerStyles.Dots2),
                            "Logs",
                            new TextBlock().Text(() => $"({(int)(progressState.Value * 100)}%)"))
                        .Spacing(1),
                    new ScrollViewer()
                        .Height(4)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                        .Content(new VStack().Add(Enumerable.Range(0, 12).Select(i => (Visual)new TextBlock($"Log line {i}")).ToArray())))
                ,
                new TabPage(
                    "Switcher",
                    new VStack(
                            new HStack(
                                    new Button("View 1").Click(() => switcherIndexState.Value = 0),
                                    new Button("View 2").Click(() => switcherIndexState.Value = 1),
                                    new TextBlock().Text(() => $"Selected={switcherIndexState.Value}"))
                                .Spacing(1),
                            new ContentSwitcher()
                                .SelectedIndex(switcherIndexState)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Add(
                                    new Group()
                                        .TopLeftText("First view")
                                        .Padding(Thickness.Zero)
                                        .Content(new VStack("Hello from view 1", "Use the buttons above to switch").Spacing(0)),
                                    new Group()
                                        .TopLeftText("Second view")
                                        .Padding(Thickness.Zero)
                                        .Content(new VStack("View 2 content", "This is a different visual tree").Spacing(0))),
                            new Rule().Style(RuleStyle.Default with { Glyphs = RuleGlyphs.Dotted }),
                            new Switch("Enable feature")
                                .IsOn(switchState)
                                .Toggled((_, e) => statusState.Value = $"switch={e.NewValue}")
                                .HorizontalAlignment(HorizontalAlignment.Left),
                            new MaskedInput()
                                .Text("hunter2")
                                .Placeholder("Password")
                                .RevealMode(MaskedInputRevealMode.WhileFocused)
                                .HorizontalAlignment(HorizontalAlignment.Stretch),
                            new Group()
                                .TopLeftText("Accordion")
                                .Padding(Thickness.Zero)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(
                                    new Accordion(
                                            new Collapsible("Section A", new TextBlock("First section content")).IsExpanded(true),
                                            new Collapsible("Section B", new TextBlock("Second section content")),
                                            new Collapsible("Section C", new TextBlock("Third section content")))
                                        .Spacing(0)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)))
                        .Spacing(1)
                        .HorizontalAlignment(HorizontalAlignment.Stretch))
                ,
                new TabPage(
                    new HStack("Lists", new TextBlock().Text(() => $"(Select={select.SelectedIndex}, Checked={selectionList.Items.Count(i => i.IsChecked)})"))
                        .Spacing(1),
                    new VStack(
                            new Group()
                                .TopLeftText("Select / Dropdown")
                                .Padding(Thickness.Zero)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(select.HorizontalAlignment(HorizontalAlignment.Stretch)),
                            new Group()
                                .TopLeftText("SelectionList (multi-select)")
                                .TopRightText("Space/Click toggles")
                                .Padding(Thickness.Zero)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(selectionList.HorizontalAlignment(HorizontalAlignment.Stretch)),
                            new Group()
                                .TopLeftText("Explicit ScrollBar")
                                .Padding(Thickness.Zero)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(new HStack(
                                        new ScrollBar().Orientation(Orientation.Vertical).Minimum(0).Maximum(100).ViewportSize(25).Value(() => (int)(progressState.Value * 100)),
                                        new ScrollBar().Orientation(Orientation.Horizontal).Minimum(0).Maximum(100).ViewportSize(25).Value(() => (int)(progressState.Value * 100)).HorizontalAlignment(HorizontalAlignment.Stretch))
                                    .Spacing(2)
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)))
                        .Spacing(1)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)),
                new TabPage(
                    "TextArea",
                    textArea.HorizontalAlignment(HorizontalAlignment.Stretch)),
                new TabPage(
                    "Tree",
                    tree.HorizontalAlignment(HorizontalAlignment.Stretch))
            })
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new TextBlock().Text(() => $"Status: {statusState.Value} | Slider: {sliderState.Value:0.00}"))
    .Spacing(1)
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch);

var header = new Header
{
    Left = "Fullscreen demo",
    Center = "Tab focus, mouse click, wheel scroll, F12 debug, Esc quit",
    Right = new TextBlock().Text(() => $"{(int)(progressState.Value * 100)}%"),
};

var footer = new Footer
{
    Left = "Tab focus | Mouse click | Wheel scroll | F12 debug | Esc quit",
    Right = "XenoAtom.Terminal.UI",
};

var root = new DockLayout()
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch)
    .Top(header)
    .Content(
        new HSplitter(leftColumn, rightColumn)
            .MinFirst(20)
            .MinSecond(25)
            .Ratio(0.45)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch))
    .Bottom(footer);

var lastTick = Stopwatch.GetTimestamp();
var t = 0.0;

Terminal.Run(root, () =>
{
    var now = Stopwatch.GetTimestamp();
    if (Stopwatch.GetElapsedTime(lastTick, now) < TimeSpan.FromMilliseconds(50))
    {
        return true;
    }

    lastTick = now;
    t += 0.02;
    progressState.Value = (Math.Sin(t) + 1.0) / 2.0;
    return true;
});
