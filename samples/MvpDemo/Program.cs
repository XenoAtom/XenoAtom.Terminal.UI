using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using System.Diagnostics;

using var session = Terminal.Open();

Terminal.WriteMarkupLine("[bold]XenoAtom.Terminal.UI MVP Demo[/]");
Terminal.WriteMarkupLine("Tab: focus  Space/Enter: activate  Ctrl+V: paste  Esc: quit");
Terminal.WriteLine();

var name = new TextBox();
var accept = new CheckBox("Accept terms");
var list = new ListBox<string>()
    .Items(["First", "Second", "Third", "Fourth", "Fifth"])
    .MinHeight(4).MaxHeight(4);
var status = new State<string>("ready");
var work = new ProgressTask("Work");

var content = new VStack(
    "Name:",
    name,
    accept,
    "Pick one:",
    list,
    new ProgressTaskGroup().Tasks([work]),
    new Button("Set status")
        .Click(() => status.Value = "click received"),
    new TextBlock().Text(() => $"Status: {status.Value}")).Spacing(1);

var root = new VStack(
    new Border(content)
        .Padding(new Thickness(1))).Spacing(1);

var lastTick = Stopwatch.GetTimestamp();
Terminal.Live(root, () =>
{
    var now = Stopwatch.GetTimestamp();
    if (Stopwatch.GetElapsedTime(lastTick, now) < TimeSpan.FromMilliseconds(50))
    {
        return TerminalLoopResult.Continue;
    }

    lastTick = now;
    if (work.Value < 1.0)
    {
        work.Value = Math.Min(1.0, work.Value + 0.01);
        return TerminalLoopResult.Continue;
    }

    Terminal.WriteMarkupLine("[green]Done![/]");
    work.Value = 0.0;
    return TerminalLoopResult.Continue;
});
Terminal.WriteMarkupLine("[yellow]Finished![/]");
