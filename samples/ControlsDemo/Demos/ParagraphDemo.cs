using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Paragraph", "Content", Description = "Rich paragraph rendering with wrapping, prefixes, style runs, and hyperlinks.")]
public sealed class ParagraphDemo : ControlsDemoBase
{
    public ParagraphDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var alignment = new State<TextAlignment>(TextAlignment.Left);

        var wrapped = new Paragraph("Paragraph wraps plain text with rich paragraph layout. Resize the terminal to observe hanging indentation and alignment.")
            .Wrap(true)
            .TextAlignment(alignment)
            .HorizontalAlignment(Align.Stretch);

        var bullet = new Paragraph("A bullet paragraph keeps wrapped continuation lines aligned under the text content for readable list rendering.")
            .Wrap(true)
            .Indent(1)
            .HangingIndent(2)
            .LinePrefix("• ")
            .ContinuationPrefix("  ")
            .HorizontalAlignment(Align.Stretch);

        var quote = new Paragraph("Quoted text can use line prefixes and continuation prefixes to mimic blockquote rendering in a Markdown-like flow.")
            .Wrap(true)
            .Indent(1)
            .LinePrefix("│ ")
            .ContinuationPrefix("│ ")
            .HorizontalAlignment(Align.Stretch);

        var richText = "Visit xenoatom.github.io for docs and source.";
        var rich = new Paragraph(richText)
        {
            Wrap = true,
            HorizontalAlignment = Align.Stretch,
            Runs =
            [
                new StyledRun(0, 5, (Style.None.WithForeground(Colors.Goldenrod) | TextStyle.Bold)),
                new StyledRun(6, 18, (Style.None.WithForeground(Colors.DeepSkyBlue) | TextStyle.Underline)),
                new StyledRun(29, 4, (Style.None.WithForeground(Colors.Silver) | TextStyle.Italic)),
            ],
            Hyperlinks =
            [
                new HyperlinkRun(6, 18, "https://xenoatom.github.io/terminal/docs/"),
            ],
        };

        return new VStack(
                DemoUi.Hint("Paragraph is display-only text with style runs and hyperlink spans."),
                new HStack(
                        DemoUi.Title("Alignment:"),
                        new EnumSelect<TextAlignment>()
                            .Value(alignment))
                    .Spacing(1),
                new Border(wrapped).Padding(1),
                new Border(new VStack(bullet, quote).Spacing(1)).Padding(1),
                new Border(rich).Padding(1))
            .Spacing(1);
    }
}
