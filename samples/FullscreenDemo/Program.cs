using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Styling;

using var session = Terminal.Open();

var showModal = new CheckBox().Text("Show modal");
var statusState = new State<string>("ready");
var progressState = new State<double>(0.0);


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

var overlay = new ComputedVisual(() =>
{
    if (!showModal.IsChecked)
    {
        return null;
    }

    var dialogContent = new VStack(
        "Modal dialog",
        new TextBlock().Text("This is a wrapped paragraph demonstrating document-style text rendering.").Wrap(true),
        new Button()
            .Text("Close")
            .With(b => b.Click += (_, _) => showModal.IsChecked = false)).Spacing(1);

    var dialog = new Dialog()
        .Title("Modal dialog")
        .IsModal(true)
        .Padding(new Thickness(1))
        .Width(60)
        .Content(dialogContent);

    return new ZStack(new Backdrop(), dialog);
});

var progressBars = new VStack(
    new TextBlock("Progress variants:"),
    new HStack(
            new ProgressBar()
                .Label("Thin")
                .Value(() => progressState.Value)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Thin)),
            new ProgressBar()
                .Label("Segmented")
                .Value(() => progressState.Value)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Segmented)))
        .Spacing(2)
        .HorizontalAlignment(HorizontalAlignment.Stretch),
    new HStack(
            new ProgressBar()
                .Label("Shaded")
                .Value(() => progressState.Value)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Shaded)),
            new ProgressBar()
                .Label("Bracketed")
                .Value(() => progressState.Value)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Bracketed)))
        .Spacing(2)
        .HorizontalAlignment(HorizontalAlignment.Stretch))
    .Spacing(0)
    .HorizontalAlignment(HorizontalAlignment.Stretch);

var leftColumn = new VStack(
        new TextBox()
            .Text("Type here (Ctrl+A, Shift+Arrows, Ctrl+Left/Right)")
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new CheckBox().Text("Accept terms"),
        showModal,
        new Table().With(t =>
        {
            t.HeaderCells = ["Task", "Status"];
            t.RowCells =
            [
                ["Download", "Running"],
                ["Render", "OK"],
            ];
        }),
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
                new TextBlock()
                    .Text("Bottom aligned (Center + Bottom)")
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Bottom)),
        new HStack(
                new TextBlock()
                    .Text("This is a very long piece of text that will be trimmed.")
                    .Trimming(TextTrimming.EndEllipsis)
                    .MaxWidth(28),
                new TextBlock()
                    .Text("This is a very long piece of text that will be trimmed.")
                    .Trimming(TextTrimming.StartEllipsis)
                    .MaxWidth(28))
            .Spacing(2),
        new Group()
            .TopLeftText("Pick one")
            .TopRightText("wheel")
            .Padding(Thickness.Zero)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new ListBox()
                .Items(new[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth" })
                .Height(5)),
        new Group()
            .TopLeftText("ScrollViewer")
            .TopRightText("focus + wheel")
            .Padding(Thickness.Zero)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new ScrollViewer()
                .Height(4)
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .Content(new VStack().With(v =>
                {
                    for (var i = 0; i < 12; i++)
                    {
                        v.Add($"Log line {i}");
                    }
                }))),
        new Button()
            .Text("Click me (mouse or Enter)")
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .With(b => b.Click += (_, _) => statusState.Value = "click received"),
        new TextBlock().Text(() => $"Status: {statusState.Value}"))
    .Spacing(0)
    .HorizontalAlignment(HorizontalAlignment.Stretch)
    .VerticalAlignment(VerticalAlignment.Stretch);

var app = new TerminalApp(
    new WindowLayer()
        .Content(
            new DockLayout()
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Content(
                    new VStack(
                        "Fullscreen demo: Tab focus, mouse click, wheel scroll, F12 debug, Esc quit",
                        new HStack(leftColumn, rightColumn)
                            .Spacing(3)
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .VerticalAlignment(VerticalAlignment.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Stretch))
                .Bottom(
                    new StatusBar()
                        .LeftText("Tab focus | Mouse click | Wheel scroll | F12 debug | Esc quit")
                        .RightText("XenoAtom.Terminal.UI")))
        .With(layer => layer.AddWindow(overlay)),
    session.Instance,
    new TerminalAppOptions { HostKind = TerminalHostKind.Fullscreen });

using var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    var t = 0.0;
    while (!cts.IsCancellationRequested)
    {
        t += 0.02;
        var v = (Math.Sin(t) + 1.0) / 2.0;
        app.Post(() => progressState.Value = v);
        await Task.Delay(50, cts.Token).ConfigureAwait(false);
    }
}, cts.Token);

await app.RunAsync();
cts.Cancel();
