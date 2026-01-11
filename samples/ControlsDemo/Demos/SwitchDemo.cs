using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Switch", "Input", Description = "On/off toggle with an event.")]
public sealed class SwitchDemo : ControlsDemoBase
{
    public SwitchDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var isOn = new State<bool>(true);

        return new VStack(
                DemoUi.Hint("Switch exposes IsOn and a Toggled event."),
                new HStack(
                        new Switch().IsOn(isOn).Toggled((_, e) =>
                        {
                            isOn.Value = e.NewValue;
                            context.Log($"Switch: {isOn.Value}");
                        }),
                        new TextBlock(() => isOn.Value ? "On" : "Off"))
                    .Spacing(1))
            .Spacing(1);
    }
}

