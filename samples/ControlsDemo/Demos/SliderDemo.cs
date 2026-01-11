using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Slider", "Input", Description = "Pointer + keyboard adjustments, value formatting.")]
public sealed class SliderDemo : ControlsDemoBase
{
    public SliderDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var value = new State<double>(0.35);

        return new VStack(
                DemoUi.Hint("Slider can be controlled with keyboard or mouse drag."),
                new Slider
                {
                    Minimum = 0.0,
                    Maximum = 1.0,
                    Step = 0.05,
                    ShowValueLabel = true,
                    ValueFormatter = v => $"{(int)Math.Round(v * 100)}%",
                }.Value(value),
                new TextBlock(() => $"Value: {value.Value:0.00}"),
                new Button("Log value").Click(() => context.Log($"Slider: {value.Value:0.00}")))
            .Spacing(1);
    }
}
