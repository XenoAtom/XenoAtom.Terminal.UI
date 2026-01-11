using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CheckBox", "Input", Description = "Checked state and binding to State<bool>.")]
public sealed class CheckBoxDemo : ControlsDemoBase
{
    public CheckBoxDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var acceptTerms = new State<bool>(false);

        return new VStack(
                DemoUi.Hint("A checkbox can be bound to State<bool> for live updates."),
                new CheckBox("Accept terms").IsChecked(acceptTerms),
                new TextBlock(() => acceptTerms.Value ? "State: checked" : "State: unchecked"),
                new Button("Log state").Click(() => context.Log($"Accept terms: {acceptTerms.Value}")))
            .Spacing(1);
    }
}

