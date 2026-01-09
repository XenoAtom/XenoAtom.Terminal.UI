using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;
using System.Diagnostics;

using var session = Terminal.Open();

var statusState = new State<string>("ready");
var progressState = new State<double>(0.0);
var sliderState = new State<double>(0.35);

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
        new Border()
            .Padding(new Thickness(0))
            .MinHeight(4)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(
                new TextBlock("Bottom aligned (Center + Bottom)")
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Bottom)),
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
                                .Spacing(0))
                        .Spacing(0)),
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
            })
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new HStack(
                new TextBlock("This is a very long piece of text that will be trimmed.")
                    .Trimming(TextTrimming.EndEllipsis)
                    .MaxWidth(28),
                new TextBlock("This is a very long piece of text that will be trimmed.")
                    .Trimming(TextTrimming.StartEllipsis)
                    .MaxWidth(28))
            .Spacing(2),
        new Rule
        {
            StartLabel = "Start",
            CenterLabel = "Rule",
            EndLabel = "End",
        }.Style(RuleStyle.Default with { Glyphs = RuleGlyphs.Dotted })
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new Group()
            .TopLeftText("Pick one")
            .TopRightText("wheel")
            .Padding(Thickness.Zero)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new ListBox()
                .Items(["First", "Second", "Third", "Fourth", "Fifth", "Sixth"])
                .Height(5)),
        new Group()
            .TopLeftText("ScrollViewer")
            .TopRightText("focus + wheel")
            .Padding(Thickness.Zero)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new ScrollViewer()
                .Height(4)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Content(new VStack().Add(Enumerable.Range(0, 12).Select(i => (Visual)new TextBlock($"Log line {i}")).ToArray()))),
        new Button("Click me (mouse or Enter)")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Click(() => statusState.Value = "click received"),
        new TextBlock().Text(() => $"Status: {statusState.Value} | Slider: {sliderState.Value:0.00}"))
    .Spacing(0)
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch);

var root = new DockLayout()
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch)
    .Content(
        new VStack(
            "Fullscreen demo: Tab focus, mouse click, wheel scroll, F12 debug, Esc quit",
            new HStack(leftColumn, new Rule { Orientation = Orientation.Vertical }, rightColumn)
                .Spacing(2)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch))
        .Spacing(1)
        .HorizontalAlignment(HorizontalAlignment.Stretch)
        .VerticalAlignment(VerticalAlignment.Stretch))
    .Bottom(
        new StatusBar()
            .LeftText("Tab focus | Mouse click | Wheel scroll | F12 debug | Esc quit")
            .RightText("XenoAtom.Terminal.UI"));

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
