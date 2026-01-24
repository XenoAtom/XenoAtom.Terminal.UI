using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Bindings + composition", "Patterns", Description = "State<T> bindings and dynamic composition with ContentSwitcher.")]
public sealed class BindingsAndCompositionDemo : ControlsDemoBase
{
    public BindingsAndCompositionDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        // This page focuses on the "bind everything" model: any property can be bound to State<T>.
        var count = new State<int>(0);
        var view = new State<int>(0);

        var switcher = new ContentSwitcher()
            .SelectedIndex(view)
            .Add(
                new VStack(
                        DemoUi.Title("View 1"),
                        new TextBlock(() => $"Count = {count}"),
                        new Button("+1").Click(() => count.Value++))
                    .Spacing(1),
                new VStack(
                        DemoUi.Title("View 2"),
                        new HStack(
                                "Count/10",
                                new ProgressBar().Value(() => Math.Clamp(count.Value / 10.0, 0.0, 1.0)).HorizontalAlignment(Align.Stretch),
                                new TextBlock(() => $"{Math.Clamp((int)Math.Round(count.Value / 10.0 * 100.0), 0, 100),3}%"))
                            .Spacing(1)
                            .HorizontalAlignment(Align.Stretch),
                        new Button("Reset").Click(() => count.Value = 0))
                    .Spacing(1));

        return new VStack(
                DemoUi.Hint("Bindings are just lambdas; State<T> notifies dependents automatically."),
                new HStack(
                        new Button("Show view 1").Click(() => view.Value = 0),
                        new Button("Show view 2").Click(() => view.Value = 1))
                    .Spacing(1),
                new Rule(),
                switcher.MinHeight(6).MaxHeight(6))
            .Spacing(1);
    }
}
