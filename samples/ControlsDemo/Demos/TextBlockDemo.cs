using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("TextBlock", "Content", Description = "Text rendering with wrapping, alignment, and trimming.")]
public sealed class TextBlockDemo : ControlsDemoBase
{
    public TextBlockDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var wrap = new State<bool>(true);

        var sampleText =
            "This is a long TextBlock line that will reflow when wrapping is enabled and the terminal is resized.\n" +
            "It also contains an emoji sequence: 🗃️  Download (watch spacing).\n" +
            "Last line: The quick brown fox jumps over the lazy dog.";

        var sample = new TextBlock(sampleText)
            .Wrap(wrap);

        var singleLine = new TextBlock("This is a single-line TextBlock that will be trimmed when the viewport is too small.")
            .Wrap(false)
            .Trimming(TextTrimming.EndEllipsis);

        var styled = new VStack(
                new TextBlock("TextBlockStyle examples:") { TextAlignment = TextAlignment.Left },
                new TextBlock("Styled foreground")
                    .Style(TextBlockStyle.Default with { Foreground = Colors.DeepSkyBlue, TextStyle = TextStyle.Bold }),
                new TextBlock("Filled background (alpha)")
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Style(TextBlockStyle.Default with { Background = Color.RgbA(255, 255, 255, 0x10), FillBackground = true }))
            .Spacing(1);

        return new VStack(
                DemoUi.Hint("Resize the terminal to see wrapping and trimming behavior."),
                new CheckBox("Wrap").IsChecked(wrap),
                sample,
                styled,
                new Rule(),
                singleLine)
            .Spacing(1);
    }
}
