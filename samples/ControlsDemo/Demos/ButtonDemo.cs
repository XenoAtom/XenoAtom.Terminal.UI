using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Button", "Input", Description = "Tones, disabled state, and optional border styling.")]
public sealed class ButtonDemo : ControlsDemoBase
{
    public ButtonDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        // Buttons are best demonstrated by showing tone variants and how styles affect them.
        var enabled = new State<bool>(true);

        return new VStack(
                DemoUi.Title("Tones"),
                new HStack(
                        new Button("Default").Click(() => context.Log("Default clicked")),
                        new Button("Primary").Tone(ControlTone.Primary).Click(() => context.Log("Primary clicked")),
                        new Button("Success").Tone(ControlTone.Success).Click(() => context.Log("Success clicked")),
                        new Button("Warning").Tone(ControlTone.Warning).Click(() => context.Log("Warning clicked")),
                        new Button("Error").Tone(ControlTone.Error).Click(() => context.Log("Error clicked")))
                    .Spacing(1),
                new Rule(),
                DemoUi.Title("Enabled / Disabled"),
                new CheckBox("Enabled", isChecked: true).IsChecked(enabled),
                new Button("Click me")
                    .IsEnabled(enabled)
                    .Click(() => context.Log("Button.Click")),
                new Rule(),
                DemoUi.Title("Borders (from style)"),
                DemoUi.Hint("Borders are off by default, but can be enabled via ButtonStyle."),
                new HStack(
                        new Button("Bordered").Style(ButtonStyle.Default with { ShowBorder = true }),
                        new Button("Bordered + Primary").Tone(ControlTone.Primary).Style(ButtonStyle.Default with { ShowBorder = true }))
                    .Spacing(1))
            .Spacing(1);
    }
}

