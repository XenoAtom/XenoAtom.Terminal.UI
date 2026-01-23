using System.Diagnostics;
using System.Globalization;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Styling;

using var session = Terminal.Open();

Terminal.WriteMarkupLine("[bold]XenoAtom.Terminal.UI — Inline / Live demo[/]");
Terminal.WriteMarkupLine("Tip: press [cyan]Esc[/] anytime to exit a live view.");
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
Terminal.WriteMarkupLine("[bold]Let’s configure your session.[/]");
Terminal.WriteLine();

var name = Terminal.Ask(new Markup("[bold]What’s your name?[/]"), prompt =>
{
    prompt.Placeholder = Environment.UserName ?? "Ada";
    prompt.DefaultValue = Environment.UserName ?? "Ada";
    prompt.Help = "Press Enter to accept the default.";
});

var updateMs = Terminal.AskNumber<int>(new Markup("[bold]Update speed (ms)[/]:"), prompt =>
{
    prompt.DefaultValue = 33;
    prompt.Validator = value => value is >= 10 and <= 250 ? null : "Pick a value in [10..250].";
    prompt.Help = "Smaller values animate more smoothly.";
});

var demoTheme = Terminal.Ask(new Markup("[bold]Theme for the live dashboard[/]"), prompt =>
{
    prompt.DefaultValue = "Terminal";
    prompt.Placeholder = "Terminal | Default | DefaultLight";
    prompt.Help = "This affects only the live visual tree.";
    prompt.Validator = value =>
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Theme is required.";
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return "Theme is required.";
        }

        return trimmed.Equals("Terminal", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals("DefaultLight", StringComparison.OrdinalIgnoreCase)
            ? null
            : "Allowed: Terminal, Default, DefaultLight.";
    };
});

Terminal.WriteLine();
Terminal.WriteMarkupLine($"Hello [cyan]{name}[/]! Starting the live dashboard…");
Terminal.WriteMarkupLine("Mouse works (hover + click), Tab moves focus. Try Ctrl+F inside the log.");
Terminal.WriteLine();

var nameState = new State<string?>(name);
var pausedState = new State<bool>(false);
var pendingTerminalLines = new State<int>(0);
var exitRequested = new State<bool>(false);

var taskDownload = new ProgressTask("🗃️  Download").Maximum(100);
var taskExtract = new ProgressTask("📦  Extract").Maximum(100);
var taskVerify = new ProgressTask("✅  Verify").Maximum(100);

var progressTasks = new ProgressTaskGroup()
    .Tasks([taskDownload, taskExtract, taskVerify]);

var barChart = new BarChart()
    .Title("Distribution")
    .Items([
        new BarChartItem("Alpha", 8),
        new BarChartItem("Beta", 5),
        new BarChartItem("Gamma", 2),
        new BarChartItem("Delta", 1)
    ])
    .ShowPercentages(false);

var breakdownChart = new BreakdownChart()
    .Title("Disk usage")
    .Segments([
        new BreakdownSegment(42, "🗂️ Data"),
        new BreakdownSegment(18, "📦 Packages"),
        new BreakdownSegment(9,  "🧹 Temp"),
        new BreakdownSegment(3,  "🧯 Other")
    ])
    .ShowValues(true);

var log = new LogControl()
    .MaxHeight(10)
    .WrapText(true);

log.AppendMarkupLine("[dim]Log started. Press Ctrl+F to search, Ctrl+H to toggle replace.[/]");

var root = new VStack(
        new Markup($"[dim]Live region — Esc: exit  |  Tab: focus  |  Ctrl+F: search (log)[/]\n[/]"),
        new HStack(
            new Group()
                .TopLeftText("Controls")
                .Padding(1)
                .Content(new VStack(
                        new HStack(
                            "Name:",
                            new TextBox().Text(nameState).Placeholder("Type your name…")),
                        new CheckBox("Pause animations").IsChecked(pausedState),
                        new HStack(
                            new Button("Write line above").Click(() => pendingTerminalLines.Value++),
                            new Button("Add log line").Click(() => log.AppendMarkupLine($"[dim]{DateTimeOffset.Now:T}[/] Added from a button click."))),
                        new Button("Finish (keep region)").Click(() => exitRequested.Value = true),
                        new Markup("[dim]Tip: click the log and use Ctrl+F to search.[/]"))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch))
                .MinWidth(42),
            new VStack(
                    progressTasks,
                    new Rule().CenterLabel("Charts"),
                    barChart,
                    breakdownChart,
                    new Rule().CenterLabel("Log"),
                    log)
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch))
            .Spacing(3)
            .HorizontalAlignment(HorizontalAlignment.Stretch),
        new Markup("[dim]Press Esc to exit the live view and continue printing after it.[/]"))
    .Spacing(1)
    .HorizontalAlignment(HorizontalAlignment.Stretch);

if (demoTheme.Trim().Equals("DefaultLight", StringComparison.OrdinalIgnoreCase))
{
    root.Style(Theme.DefaultLight);
}
else if (demoTheme.Trim().Equals("Default", StringComparison.OrdinalIgnoreCase))
{
    root.Style(Theme.Default);
}

var stopwatch = Stopwatch.StartNew();
var lastUpdate = Stopwatch.GetTimestamp();
var lastLog = Stopwatch.GetTimestamp();
var throttleTicks = (long)(updateMs * (Stopwatch.Frequency / 1000.0));

Terminal.Live(
    root,
    context =>
    {
        var now = context.Timestamp;

        if (pendingTerminalLines.Value > 0)
        {
            var count = pendingTerminalLines.Value;
            pendingTerminalLines.Value = 0;
            context.Terminal.WriteMarkupLine($"[dim]Wrote {count} line(s) above the live region at {DateTimeOffset.Now:T}.[/]");
        }

        if (exitRequested.Value)
        {
            context.Terminal.WriteMarkupLine("[green]Demo finished by user. Keeping the live visual on screen.[/]");
            return TerminalLoopResult.StopAndKeepVisual;
        }

        if (now - lastUpdate < throttleTicks)
        {
            return TerminalLoopResult.Continue;
        }

        lastUpdate = now;

        if (!pausedState.Value)
        {
            var t = stopwatch.Elapsed.TotalSeconds;

            taskDownload.Value = Math.Min(100, t * 18);
            taskExtract.Value = Math.Min(100, Math.Max(0, (t - 1.5) * 20));
            taskVerify.Value = Math.Min(100, Math.Max(0, (t - 3.0) * 28));

            var items = barChart.Items;
            items[0].Value = 6 + (2 * Math.Sin(t * 0.9));
            items[1].Value = 4 + (2 * Math.Sin(t * 1.1 + 0.7));
            items[2].Value = 2 + (1 * Math.Sin(t * 1.3 + 1.3));
            items[3].Value = 1 + (0.5 * Math.Sin(t * 1.7 + 2.1));

            var segments = breakdownChart.Segments;
            segments[0].Value = 40 + (6 * Math.Sin(t * 0.4));
            segments[1].Value = 18 + (3 * Math.Sin(t * 0.6 + 1.2));
            segments[2].Value = 10 + (2 * Math.Sin(t * 0.8 + 2.4));
            segments[3].Value = 4 + (1 * Math.Sin(t * 1.0 + 3.8));
        }

        if (now - lastLog > Stopwatch.Frequency)
        {
            lastLog = now;
            log.AppendMarkupLine($"[dim]{DateTimeOffset.Now:T}[/] Tick — name: [cyan]{nameState.Value}[/].");
        }

        if (taskVerify.Value >= 100)
        {
            context.Terminal.WriteMarkupLine("[green]All tasks completed. Press Esc to exit or click “Finish”.[/]");
        }

        return TerminalLoopResult.Continue;
    },
    new TerminalLiveOptions
    {
        Culture = CultureInfo.InvariantCulture,
    });

Terminal.WriteLine();
Terminal.WriteMarkupLine("[bold]Back to normal terminal output.[/]");
Terminal.WriteMarkupLine($"The cursor is now after the live region. Goodbye, [cyan]{nameState.Value}[/]!");
