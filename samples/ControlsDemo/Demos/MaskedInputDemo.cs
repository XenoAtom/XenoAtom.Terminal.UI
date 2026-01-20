using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("MaskedInput", "Input", Description = "Template-based masked input (credit cards, dates, identifiers…).")]
public sealed class MaskedInputDemo : ControlsDemoBase
{
    public MaskedInputDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var card = new MaskedInput("9999-9999-9999-9999;_");
        var upper = new MaskedInput(">AAAA;_");

        return new VStack(
                DemoUi.Hint("MaskedInput renders a template mask and restricts input per position."),
                DemoUi.Title("Credit card"),
                card,
                new TextBlock(() => $"Value: {card.Value}"),
                new TextBlock(() => $"Compact: {card.CompactValue}  (valid: {card.IsValid})"),
                new Rule(),
                DemoUi.Title("Case conversion (>)"),
                upper,
                new TextBlock(() => $"Value: {upper.Value}  (valid: {upper.IsValid})"))
            .Spacing(1);
    }
}
