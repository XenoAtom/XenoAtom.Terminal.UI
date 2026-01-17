using System.Globalization;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("NumberBox", "Input", Description = "Numeric editor with validation and bindable Value.")]
public sealed class NumberBoxDemo : ControlsDemoBase
{
    public NumberBoxDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var port = new State<int>(8080);
        var factor = new State<double>(1.25);

        return new VStack(
                DemoUi.Hint("NumberBox<T> updates Value when input parses and validation succeeds."),
                new HStack(
                        new VStack(
                                "Port (1..65535):",
                                new NumberBox<int>
                                {
                                    ValueValidator = v => v is >= 1 and <= 65535 ? null : "Port must be in [1..65535]",
                                }.Value(port))
                            .Spacing(1),
                        new VStack(
                                "Factor (invariant culture):",
                                new NumberBox<double>()
                                    .Value(factor)
                                    .ParseStyles(NumberStyles.Float)
                                    .FormatProvider(CultureInfo.InvariantCulture))
                            .Spacing(1))
                    .Spacing(4),
                new TextBlock(() => $"Port: {port.Value} | Factor: {factor.Value:0.###}"),
                new Button("Log").Click(() => context.Log($"Port={port.Value}, Factor={factor.Value:0.###}")))
            .Spacing(1);
    }
}
