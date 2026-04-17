using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Templating;
using XenoAtom.Terminal.UI.ControlsDemo;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("LogControl", "Visualization", Description = "Scrollable log viewer with markup, selection/copy, and built-in search (Ctrl+F).")]
public sealed class LogControlDemo : ControlsDemoBase
{
    public LogControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var nextId = new State<int>(1);
        var wrap = new State<bool>(true);
        var lineToAdd = new State<int>(5);

        var log = new LogControl
        {
            MaxCapacity = 2000,
        }.WrapText(wrap);

        log.AppendMarkupLine("[dim]Try: mouse wheel, PageUp/Down, Ctrl+F, Ctrl+A, Ctrl+C[/]");

        if (context.IsScreenshot)
        {
            for (int j = 0; j < 60; j++)
            {
                var id = nextId.Value++;
                if (j % 7 == 0)
                {
                    log.AppendMarkupLine($"[green]✔[/] [dim]#{id}[/] [bold]Downloaded[/] [cyan]🗃️[/] item [dim](cached)[/]");
                }
                else if (j % 11 == 0)
                {
                    log.AppendMarkupLine($"[warning]⚠[/] [dim]#{id}[/] Retrying after transient error (attempt [bold]{(j % 3) + 1}[/]/3)…");
                }
                else
                {
                    log.AppendLine($"Message {id} - The quick brown fox jumps over the lazy dog.");
                }
            }
        }

        return new VStack(
                DemoUi.Hint("LogControl is optimized for appending and renders only the visible rows."),
                new HStack(
                        new Button("Append Lines").Click(() =>
                        {
                            for (int j = 0; j < lineToAdd.Value; j++)
                            {
                                log.AppendLine($"Message {nextId.Value++}");
                            }
                        }),
                        new Button("Append markup").Click(() =>
                        {
                            for (int j = 0; j < lineToAdd.Value; j++)
                            {
                                var i = nextId.Value++;
                                log.AppendMarkupLine($"[green]✔[/] [dim]#{i}[/] [bold]Downloaded[/] [cyan]🗃️[/] item");
                            }
                        }),
                        new Button("Clear").Click(log.Clear),
                        new Switch("Wrap").IsOn(wrap),
                        "| Line Count To Add:", lineToAdd.PresentAs(DataTemplateRole.Editor)
                        )
                    .Spacing(1),
                new Border(log).Stretch().MinWidth(20).MaxWidth(120).MinHeight(8).MaxHeight(25),
                new Markup("[dim]Search supports case/word/regex and navigation via buttons or keyboard.[/]"))
            .Spacing(1);
    }
}

