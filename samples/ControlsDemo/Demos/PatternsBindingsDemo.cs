using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Bindings, State<T>, and composition", "Patterns", Description = "Demonstrates live UI via State<T>, functional composition (ContentSwitcher), and the fluent binding syntax.", Tags = ["state", "bindings", "composition", "ContentSwitcher"], Order = -100)]
public sealed class PatternsBindingsDemo : ControlsDemoBase
{
    public PatternsBindingsDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var viewIndex = new State<int>(0);
        var enabled = new State<bool>(true);

        var valueLabel = new TextBlock().Text(() => $"Progress: {context.Runtime.Progress01.Value:P0}");

        var progress = new ProgressBar()
            .Value(context.Runtime.Progress01)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Style(ProgressBarStyle.Thin);

        var switcher = new ContentSwitcher()
            .SelectedIndex(viewIndex)
            .Add(
                new Group()
                    .TopLeftText("View A")
                    .Padding(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(new VStack(
                            new TextBlock("This subtree is selected via State<int> -> ContentSwitcher.SelectedIndex."),
                            new Spinner().Style(SpinnerStyles.Dots))
                        .Spacing(1)),
                new Group()
                    .TopLeftText("View B")
                    .Padding(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(new VStack(
                            new TextBlock("Switch to another view to re-compose the UI."),
                            new Sparkline()
                                .Values(() => new[] { 0.1, 0.2, 0.4, 0.3, context.Runtime.Progress01.Value, 0.7, 0.9 })
                                .HorizontalAlignment(HorizontalAlignment.Stretch))
                        .Spacing(1)),
                new Group()
                    .TopLeftText("View C")
                    .Padding(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(new Markup("[dim]Markup[/] + [violet]color[/] + [underline]decorations[/].")));

        var options = new HStack(
                new Button("View A").Click(() => viewIndex.Value = 0),
                new Button("View B").Click(() => viewIndex.Value = 1),
                new Button("View C").Click(() => viewIndex.Value = 2),
                new Switch()
                    .IsOn(enabled)
                    .Toggled((_, e) =>
                    {
                        enabled.Value = e.NewValue;
                        context.Log($"Enabled = {enabled.Value}");
                    }),
                new Markup(() => enabled.Value ? "[dim]Enabled[/]" : "[dim]Disabled[/]"))
            .Spacing(2)
            .HorizontalAlignment(HorizontalAlignment.Left);

        var interactive = new Group()
            .TopLeftText("Live UI")
            .Padding(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new VStack(
                    valueLabel,
                    progress,
                    options,
                    switcher)
                .Spacing(1)
                .HorizontalAlignment(HorizontalAlignment.Stretch));

        var recipe = new Group()
            .TopLeftText("Recipe")
            .Padding(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Content(new Markup(
                """
                [dim]Live updating widget:[/]

                [dim]var[/] progress [dim]=[/] [dim]new[/] State<double>(0);
                Terminal.Live([dim]new[/] ProgressBar().Value(progress), () => { progress.Value += 0.01; return true; });
                """));

        return new VStack(
                recipe,
                interactive)
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
