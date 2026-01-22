using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("LogControl", "Visualization", Description = "Scrollable log viewer with markup, selection/copy, and built-in search (Ctrl+F).")]
public sealed class LogControlDemo : ControlsDemoBase
{
    public LogControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var nextId = new State<int>(1);
        var wrap = new State<bool>(true);

        var log = new LogControl
        {
            MaxCapacity = 2000,
        }.WrapText(wrap);

        log.AppendMarkupLine("[dim]Try: mouse wheel, PageUp/Down, Ctrl+F, Ctrl+A, Ctrl+C[/]");

        return new VStack(
                DemoUi.Hint("LogControl is optimized for appending and renders only the visible rows."),
                new HStack(
                        new Button("Append line").Click(() =>
                        {
                            log.AppendLine($"Message {nextId.Value++}");
                        }),
                        new Button("Append markup").Click(() =>
                        {
                            var i = nextId.Value++;
                            log.AppendMarkupLine($"[green]✔[/] [dim]#{i}[/] [bold]Downloaded[/] [cyan]🗃️[/] item");
                        }),
                        new Button("Clear").Click(log.Clear),
                        new Switch("Wrap").IsOn(wrap))
                    .Spacing(1),
                new Border(log).HorizontalAlignment(HorizontalAlignment.Stretch).VerticalAlignment(VerticalAlignment.Stretch).MinWidth(20).MaxWidth(120).MinHeight(8).MaxHeight(25),
                new TextBlock("[dim]Search supports case/word/regex and navigation via buttons or keyboard.[/]"))
            .Spacing(1);
    }
}

