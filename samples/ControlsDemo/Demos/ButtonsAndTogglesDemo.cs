using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Buttons and toggles", "Input", Description = "Button states + CheckBox/RadioButton/Switch.", Tags = ["Button", "CheckBox", "RadioButton", "Switch"], Order = 10)]
public sealed class ButtonsAndTogglesDemo : ControlsDemoBase
{
    public ButtonsAndTogglesDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var isOn = new State<bool>(true);

        var checkbox = new CheckBox()
            .Text("Accept terms")
            .IsChecked(false);

        var checkboxState = new Markup(() => checkbox.IsChecked ? "[dim]Checked[/]" : "[dim]Unchecked[/]");

        var group = new object();
        var radioA = new RadioButton().Text("Choice A").Group(group).IsChecked(true);
        var radioB = new RadioButton().Text("Choice B").Group(group).IsChecked(false);
        var radioC = new RadioButton().Text("Choice C").Group(group).IsChecked(false);

        var radioState = new Markup(() =>
        {
            if (radioA.IsChecked) return "[dim]Selected:[/] A";
            if (radioB.IsChecked) return "[dim]Selected:[/] B";
            if (radioC.IsChecked) return "[dim]Selected:[/] C";
            return "[dim]Selected:[/] <none>";
        });

        var sw = new Switch().IsOn(isOn).Toggled((_, e) => { isOn.Value = e.NewValue; context.Log($"Switch: {isOn.Value}"); });

        var enabledToggle = new CheckBox()
            .Text("Enable button")
            .IsChecked(true);

        var buttonHost = new ComputedVisual(() =>
        {
            _ = enabledToggle.IsChecked;
            return new Button("Click me")
                .IsEnabled(enabledToggle.IsChecked)
                .Click(() => context.Log("Button.Click"));
        });

        return new VStack(
                new Group().TopLeftText("Button").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(new VStack(enabledToggle, buttonHost).Spacing(1)),
                new Group().TopLeftText("CheckBox").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(new VStack(checkbox, checkboxState).Spacing(0)),
                new Group().TopLeftText("RadioButton").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(new VStack(radioA, radioB, radioC, radioState).Spacing(0)),
                new Group().TopLeftText("Switch").Padding(1).HorizontalAlignment(HorizontalAlignment.Left).Content(new HStack(sw, new Markup(() => isOn.Value ? "[dim]On[/]" : "[dim]Off[/]")).Spacing(1)))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }
}
