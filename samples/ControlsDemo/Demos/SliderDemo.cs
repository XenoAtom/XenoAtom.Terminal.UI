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
        var value = new State<int>(35);

        return new VStack(
                DemoUi.Hint("Slider can be controlled with keyboard or mouse drag."),
                new Slider<int>
                {
                    Minimum = 0,
                    Maximum = 100,
                    Step = 5,
                    ShowValueLabel = true,
                }
                    .ValueFormatter(v => $"{v}%")
                    .Value(value),
                new TextBlock(() => $"Value: {value.Value}%"),
                new Button("Log value").Click(() => context.Log($"Slider: {value.Value}%")))
            .Spacing(1);
    }
}

