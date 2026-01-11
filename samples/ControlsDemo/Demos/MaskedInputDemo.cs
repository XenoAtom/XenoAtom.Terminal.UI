using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("MaskedInput", "Input", Description = "Password-style input with optional reveal.")]
public sealed class MaskedInputDemo : ControlsDemoBase
{
    public MaskedInputDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var reveal = new State<bool>(false);

        var input = new MaskedInput()
            .Placeholder("Secret…")
            .RevealMode(() => reveal.Value ? MaskedInputRevealMode.WhileFocused : MaskedInputRevealMode.Never);

        return new VStack(
                DemoUi.Hint("MaskedInput hides characters; reveal can be enabled while focused."),
                input,
                new CheckBox("Reveal while focused").IsChecked(reveal),
                new Button("Log length").Click(() => context.Log($"Length: {(input.Text ?? string.Empty).Length}")))
            .Spacing(1);
    }
}

