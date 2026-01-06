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

var app = new TerminalApp(
    new WindowLayer()
        .Content(
            new DockLayout()
                .HorizontalAlignment(HorizontalAlignment.Stretch)
                .VerticalAlignment(VerticalAlignment.Stretch)
                .Content(
                    new VStack(
                        "Fullscreen demo: Tab focus, mouse click, wheel scroll, F12 debug, Esc quit",
                        new TextBox()
                            .Text("Type here (Ctrl+A, Shift+Arrows, Ctrl+Left/Right)")
                            .HorizontalAlignment(HorizontalAlignment.Stretch),
                        new CheckBox().Text("Accept terms"),
                        showModal,
                        new Group()
                            .TopLeftText("Layout + Text")
                            .TopRightText("alignment / margin / trimming")
                            .Padding(new Thickness(1))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Content(
                                new VStack(
                                    new Border()
                                        .Padding(new Thickness(1))
                                        .MinHeight(5)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .Content(
                                            new TextBlock()
                                                .Text("Centered in a taller container (VerticalAlignment.Center)")
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .VerticalAlignment(VerticalAlignment.Center)),
                                    new HStack(
                                            new TextBlock("Trim end:").Margin(new Thickness(0, 0, 1, 0)),
                                            new TextBlock()
                                                .Text("This is a very long piece of text that will be trimmed.")
                                                .Trimming(TextTrimming.EndEllipsis)
                                                .MaxWidth(24),
                                            new TextBlock("Trim start:").Margin(new Thickness(2, 0, 1, 0)),
                                            new TextBlock()
                                                .Text("This is a very long piece of text that will be trimmed.")
                                                .Trimming(TextTrimming.StartEllipsis)
                                                .MaxWidth(24))
                                        .Spacing(1),
                                    new HStack(
                                            new Button()
                                                .Text("Left")
                                                .HorizontalAlignment(HorizontalAlignment.Left)
                                                .With(b => b.Click += (_, _) => statusState.Value = "left clicked"),
                                            new Button()
                                                .Text("Center")
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .With(b => b.Click += (_, _) => statusState.Value = "center clicked"),
                                            new Button()
                                                .Text("Right")
                                                .HorizontalAlignment(HorizontalAlignment.Right)
                                                .With(b => b.Click += (_, _) => statusState.Value = "right clicked"))
                                        .Spacing(2),
                                    new HStack(
                                            new TextBlock("Amount:").Margin(new Thickness(0, 0, 1, 0)),
                                            new TextBox()
                                                .Text("12345")
                                                .Placeholder("0")
                                                .TextAlignment(TextAlignment.Right)
                                                .MaxWidth(12))
                                        .Spacing(1))
                                .Spacing(1)),
                        new Table().With(t =>
                        {
                            t.HeaderCells = ["Task", "Status"];
                            t.RowCells =
                            [
                                ["Download", "Running"],
                                ["Render", "OK"],
                                ["Tests", "OK"]
                            ];
                        }),
                        new Group()
                            .TopLeftText("Pick one")
                            .TopRightText("mouse wheel supported")
                            .Padding(new Thickness(1))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Content(new ListBox()
                                .Items(new[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth" })
                                .Height(6)),
                        new Group()
                            .TopLeftText("ScrollViewer")
                            .TopRightText("focus + wheel")
                            .Padding(new Thickness(1))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Content(new ScrollViewer()
                                .Height(5)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .Content(new VStack().With(v =>
                                {
                                    for (var i = 0; i < 20; i++)
                                    {
                                        v.Add($"Log line {i}");
                                    }
                                }))),
                        new Group()
                            .TopLeftText("Progress")
                            .TopRightText("variants")
                            .Padding(new Thickness(1))
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .Content(
                                new VStack(
                                        new ProgressBar()
                                            .Label("Thin")
                                            .Value(() => progressState.Value)
                                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                                            .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Thin)),
                                        new ProgressBar()
                                            .Label("Segmented")
                                            .Value(() => progressState.Value)
                                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                                            .With(p => p.SetEnvironmentValue(ProgressBarStyle.Key, ProgressBarStyle.Segmented)),
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
                                    .Spacing(1)),
                        new Button()
                            .Text("Click me (mouse or Enter)")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .With(b => b.Click += (_, _) => statusState.Value = "click received"),
                        new TextBlock().Text(() => $"Status: {statusState.Value}")).Spacing(1))
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
