using System.Text;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("MarkdownControl", "Content", Description = "Render CommonMark documents with tables and alert blocks using virtualized markdown flow.")]
public sealed class MarkdownControlDemo : ControlsDemoBase
{
    public MarkdownControlDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var markdown = LoadMarkdownSample();

        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
            Options = MarkdownRenderOptions.Default with
            {
                MaxCodeBlockHeight = context.IsScreenshot ? 8 : 14,
                WrapCodeBlocks = false,
            },
            RenderStyle = MarkdownStyle.Default with
            {
                LinkStyle = Style.None | TextStyle.Bold | TextStyle.Underline,
                InlineCodeStyle = Style.None | TextStyle.Bold,
                NoteAlert = MarkdownAlertStyle.Default with
                {
                    BorderStyle = Style.None | TextStyle.Bold,
                },
                TipAlert = MarkdownAlertStyle.Default with
                {
                    BorderStyle = Style.None | TextStyle.Bold,
                },
                WarningAlert = MarkdownAlertStyle.Default with
                {
                    BorderStyle = Style.None | TextStyle.Bold,
                },
                CautionAlert = MarkdownAlertStyle.Default with
                {
                    BorderStyle = Style.None | TextStyle.Bold,
                },
            },
        };

        var path = ResolveSamplePath();
        var pathText = path is null ? "embedded fallback" : Path.GetFileName(path);

        return new VStack(
                DemoUi.Hint($"Loaded markdown from disk: {pathText}. The sample exercises CommonMark plus tables and alert extensions."),
                control)
            .Spacing(1);
    }

    private string LoadMarkdownSample()
    {
        var path = ResolveSamplePath();
        if (path is null)
        {
            return "# MarkdownControl\n\nUnable to locate markdown sample file from disk.";
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private string? ResolveSamplePath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Assets", "markdown", "markdown-control-sample.md");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var sourceDir = Path.GetDirectoryName(SourcePath);
        if (sourceDir is null)
        {
            return null;
        }

        var repoPath = Path.GetFullPath(Path.Combine(sourceDir, "..", "Assets", "markdown", "markdown-control-sample.md"));
        return File.Exists(repoPath) ? repoPath : null;
    }
}

