using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TextArea", "Input", Description = "Multi-line editing with selection/scrolling and built-in Find/Replace (Ctrl+F/Ctrl+H).")]
public sealed class TextAreaDemo : ControlsDemoBase
{
    public TextAreaDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var text = new State<string?>("Line 1\nLine 2\nLine 3");
        var autoExpandText = new State<string?>("Short note");

        return new VStack(
                DemoUi.Hint("TextArea supports multi-line editing, selection, and Find/Replace (Ctrl+F / Ctrl+H)."),
                new TextArea()
                    .Text(text)
                    .Placeholder("Type multiple lines.")
                    .MinHeight(6)
                    .MaxHeight(6).Scrollable(),
                new Rule(),
                DemoUi.Title("Auto expand height"),
                DemoUi.Hint("This TextArea grows with its content until MaxHeight is reached."),
                new TextArea()
                    .Text(autoExpandText)
                    .Placeholder("Add more lines to grow this editor.")
                    .AutoSizeMode(TextEditorAutoSizeMode.Height)
                    .MinHeight(2)
                    .MaxHeight(6).Scrollable(),
                new Rule(),
                DemoUi.Title("With Border"),
                new Border(new TextArea("TextArea inside a Border")
                    .MinHeight(4)
                    .MaxHeight(4).Scrollable()),
                new Button("Log lines").Click(() => context.Log($"Lines: {(text.Value ?? string.Empty).Split('\n').Length}")))
            .Spacing(1);
    }
}
