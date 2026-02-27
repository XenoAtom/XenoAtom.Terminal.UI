using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("MarkdownMarkupConverter", "Content", Description = "Convert markdown to markup and preserve-source syntax highlighting for PromptEditor.")]
public sealed class MarkdownMarkupConverterDemo : ControlsDemoBase
{
    public MarkdownMarkupConverterDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var markdown = """
            # Markdown to Markup

            Render **strong**, *emphasis*, and `inline code` in a [Markup](https://xenoatom.github.io/terminal/docs/controls/markup.html) control.

            > Quote blocks and list items are preserved.

            - First item
            - Second item with `code`

            > [!TIP]
            > Use `MarkdownMarkupConverter` to colorize prompt snippets.
            """;

        var converter = new MarkdownMarkupConverter
        {
            Theme = context.Theme,
        };
        var preservedMarkup = converter.ConvertPreservingSource(markdown);
        var renderedMarkup = converter.Convert(markdown);

        var promptEditor = new PromptEditor(markdown)
            .PromptMarkup("[primary]md[/] ")
            .ContinuationPromptMarkup("[muted]·[/] ")
            .EnableWordHints(false)
            .Highlighter(HighlightMarkdown)
            .MinHeight(context.IsScreenshot ? 9 : 11)
            .MaxHeight(context.IsScreenshot ? 9 : 11)
            .Scrollable();

        return new VStack(
                DemoUi.Hint("`ConvertPreservingSource` keeps exact markdown text for PromptEditor highlighters. `Convert` renders interpreted markdown."),
                new Group("PromptEditor markdown source (preserved syntax + styles)", promptEditor)
                    .HorizontalAlignment(Align.Stretch),
                new Group("Markup preview (preserve source)", new Markup(preservedMarkup).Wrap(true))
                    .HorizontalAlignment(Align.Stretch),
                new Group("Markup preview (render interpreted markdown)", new Markup(renderedMarkup).Wrap(true))
                    .HorizontalAlignment(Align.Stretch))
            .Spacing(1);

        void HighlightMarkdown(in PromptEditorHighlightRequest request, List<StyledRun> runs)
        {
            converter.Theme = request.Theme;
            converter.Highlight(SnapshotToString(request.Snapshot), runs);
        }

        static string SnapshotToString(ITextSnapshot snapshot)
        {
            if (snapshot.Length == 0)
            {
                return string.Empty;
            }

            return string.Create(snapshot.Length, snapshot, static (span, s) => s.CopyTo(0, span));
        }
    }
}
