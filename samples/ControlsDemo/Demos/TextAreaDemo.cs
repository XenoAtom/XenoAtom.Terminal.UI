using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TextArea", "Input", Description = "Multi-line editing with selection and scrolling (use Border for outlines).")]
public sealed class TextAreaDemo : ControlsDemoBase
{
    public TextAreaDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var text = new State<string?>("Line 1\nLine 2\nLine 3");

        return new VStack(
                DemoUi.Hint("TextArea supports multi-line editing and selection."),
                new TextArea()
                    .Text(text)
                    .Placeholder("Type multiple lines.")
                    .MinHeight(6)
                    .MaxHeight(6),
                new Rule(),
                DemoUi.Title("With Border"),
                new Border(new TextArea("TextArea inside a Border")
                    .MinHeight(4)
                    .MaxHeight(4)),
                new Button("Log lines").Click(() => context.Log($"Lines: {(text.Value ?? string.Empty).Split('\n').Length}")))
            .Spacing(1);
    }
}
