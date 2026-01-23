using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;

using var session = Terminal.Open();

Terminal.WriteMarkupLine("[bold]XenoAtom.Terminal.UI — Inline / Live demo[/]");
Terminal.WriteLine();

Terminal.Write(new VStack(
    new TextFiglet("Welcome")
        .Font(FigletPredefinedFont.Slant)
        .LetterSpacing(0)
        .TextAlignment(TextAlignment.Left),
    new Canvas()
        .MinHeight(6)
        .MaxHeight(6)
        .Painter(ctx =>
        {
            var bounds = ctx.Bounds;
            var width = Math.Max(1, bounds.Width);
            var height = Math.Max(1, bounds.Height);
            for (var y = 0; y < height; y++)
            {
                var tY = height <= 1 ? 0.0 : (double)y / (height - 1);
                var lightness = 0.25 + (0.60 * (1.0 - tY));

                for (var x = 0; x < width; x++)
                {
                    var tX = width <= 1 ? 0.0 : (double)x / (width - 1);
                    var hue = (float)(360.0 * tX);
                    var color = Color.FromHsl(hue, 1.0f, (float)lightness);
                    ctx.SetPixel(x, y, Style.None.WithForeground(color));
                }
            }
        }),
    new Markup("[dim]This demo shows prompts, state binding, and a live inline dashboard.[/]"))
    .Spacing(1));

Terminal.WriteLine();
var name = Terminal.Ask("What’s your name?", prompt =>
{
    prompt.DefaultValue = Environment.UserName ?? "Ada";
    prompt.Placeholder = prompt.DefaultValue;
    prompt.Help = "Press Enter to accept the default.";
});

Terminal.WriteLine();
Terminal.WriteMarkupLine($"Hello [cyan]{name}[/]! Let’s run a short live progress…");
Terminal.WriteLine();

var download = new ProgressTask("Download").Maximum(100);
var extract = new ProgressTask("Extract").Maximum(100);
var verify = new ProgressTask("Verify").Maximum(100);

var tasks = new ProgressTaskGroup()
    .Tasks([download, extract, verify]);

var stopwatch = Stopwatch.StartNew();
Terminal.Live(
    tasks,
    _ =>
    {
        var t = stopwatch.Elapsed.TotalSeconds;

        download.Value = Math.Min(100, t * 30);
        extract.Value = Math.Min(100, Math.Max(0, (t - 1.0) * 30));
        verify.Value = Math.Min(100, Math.Max(0, (t - 2.0) * 30));

        return verify.Value >= 100
            ? TerminalLoopResult.StopAndKeepVisual
            : TerminalLoopResult.Continue;
    });

Terminal.WriteLine();
Terminal.WriteMarkupLine("[bold]Static output (one-shot render)[/]");
Terminal.WriteLine();

Terminal.Write(new BreakdownChart()
    .Title("Disk usage")
    .Segments([
        new BreakdownSegment(42, "🗂️ Data"),
        new BreakdownSegment(18, "📦 Packages"),
        new BreakdownSegment(9, "🧹 Temp"),
        new BreakdownSegment(3, "🧯 Other"),
    ])
    .ShowValues(true));

Terminal.WriteLine();
var port = Terminal.AskNumber<int>("Pick a port number:", prompt =>
{
    prompt.DefaultValue = 8080;
    prompt.Validator = value => value is >= 1 and <= 65535 ? null : "Port must be in [1..65535].";
    prompt.Help = "Use Up/Down to adjust the value.";
});

Terminal.WriteLine();
Terminal.WriteMarkupLine($"Thanks, [cyan]{name}[/]. Port: [yellow]{port}[/].");
Terminal.WriteMarkupLine("[dim]End of demo.[/]");
