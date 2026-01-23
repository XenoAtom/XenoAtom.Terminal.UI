using System;
using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Validation", "Input", Description = "Reusable validation/status message wrapper for inputs.")]
public sealed class ValidationDemo : ControlsDemoBase
{
    public ValidationDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var value = new State<string?>("8080");

        ValidationMessage? ValidatePort(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new(ValidationSeverity.Error, "Port is required.");
            }

            if (!int.TryParse(text, out var port) || port is < 1 or > 65535)
            {
                return new(ValidationSeverity.Error, "Port must be in [1..65535].");
            }

            return null;
        }

        var customStyle = ValidationStyle.Default with
        {
            Gap = 0,
            Padding = new Thickness(1, 0, 1, 0),
            InfoGlyph = new Rune(0x2139),
        };

        return new VStack(
                DemoUi.Title("Validate below"),
                new TextBox().Text(value)
                    .Validate(value.Bind.Value, ValidatePort),
                new Rule(),
                DemoUi.Title("Validate above (custom style)"),
                new TextBox().Text(value)
                    .Validate(value.Bind.Value, ValidatePort, placement: ValidationPlacement.Above)
                    .Style(customStyle),
                new Rule(),
                DemoUi.Hint("Tip: controls can also compose validation internally (for example, NumberBox can surface parsing errors)."))
            .Spacing(1);
    }
}
