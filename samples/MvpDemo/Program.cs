using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using System.Diagnostics;

using var session = Terminal.Open();

session.Instance.WriteMarkupLine("[bold]XenoAtom.Terminal.UI MVP Demo[/]");
session.Instance.WriteMarkupLine("Tab: focus  Space/Enter: activate  Ctrl+V: paste  Esc: quit");
session.Instance.WriteLine();

var name = new TextBox().Text("");
var accept = new CheckBox().Text("Accept terms");
var list = new ListBox()
    .Items(new[] { "First", "Second", "Third", "Fourth", "Fifth" })
    .Height(4);
var status = new State<string>("ready");
var progressState = new State<double>(0.0);

var content = new VStack(
    "Name:",
    name,
    accept,
    "Pick one:",
    list,
    new ProgressBar()
        .Label("Work")
        .Value(() => progressState.Value),
    new Button()
        .Text("Set status")
        .With(b => b.Click += (_, _) => status.Value = "click received"),
    new TextBlock().Text(() => $"Status: {status.Value}")).Spacing(1);

var root = new VStack(
    new Border()
        .Padding(new Thickness(1))
        .Content(content)).Spacing(1);

var lastTick = Stopwatch.GetTimestamp();
session.Instance.Live(root, ctx =>
{
    var now = Stopwatch.GetTimestamp();
    if (Stopwatch.GetElapsedTime(lastTick, now) < TimeSpan.FromMilliseconds(50))
    {
        return true;
    }

    lastTick = now;
    if (progressState.Value < 1.0)
    {
        progressState.Value = Math.Min(1.0, progressState.Value + 0.02);
        return true;
    }

    ctx.WriteMarkupLine("[green]Done![/]");
    return false;
});
