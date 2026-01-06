using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;

using var session = Terminal.Open();

session.Instance.WriteMarkupLine("[bold]XenoAtom.Terminal.UI MVP Demo[/]");
session.Instance.WriteMarkupLine("Tab: focus  Space/Enter: activate  Ctrl+V: paste  Esc: quit");
session.Instance.WriteLine();

var name = new TextBox().Text("");
var accept = new CheckBox().Text("Accept terms");
var list = new ListBox()
    .Items(new[] { "First", "Second", "Third", "Fourth", "Fifth" })
    .Height(4);
var progress = new ProgressBar()
    .Label("Work")
    .Value(0.0);

var button = new Button().Text("Log line");
TerminalApp? app = null;
button.Click += (_, _) => app?.WriteMarkupLine("[dim]Click received[/]");

var content = new VStack(
    "Name:",
    name,
    accept,
    "Pick one:",
    list,
    progress,
    button).Spacing(1);

var root = new VStack(
    new Border()
        .Padding(new Thickness(1))
        .Content(content)).Spacing(1);

app = new TerminalApp(root, session.Instance);

using var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    var t = 0.0;
    while (!cts.IsCancellationRequested)
    {
        t += 0.02;
        var v = (Math.Sin(t) + 1.0) / 2.0;
        app.Post(() => progress.Value = v);
        await Task.Delay(50, cts.Token).ConfigureAwait(false);
    }
}, cts.Token);

await app.RunAsync();
cts.Cancel();
