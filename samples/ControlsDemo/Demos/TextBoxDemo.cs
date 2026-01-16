using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TextBox", "Input", Description = "Single-line editing, placeholder, and borders via Border.")]
public sealed class TextBoxDemo : ControlsDemoBase
{
    public TextBoxDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var text = new State<string?>("Hello");

        return new VStack(
                DemoUi.Hint("Bind TextBox.Text to State<string> for live updates."),
                new TextBox()
                    .Text(text)
                    .Placeholder("Type here."),
                new TextBlock(() => $"Value: {text.Value}"),
                new Rule(),
                DemoUi.Title("With Border"),
                new Border(new TextBox("Bordered")
                    .Placeholder(".")),
                new Button("Log value").Click(() => context.Log($"Text: {text.Value}")))
            .Spacing(1);
    }
}
