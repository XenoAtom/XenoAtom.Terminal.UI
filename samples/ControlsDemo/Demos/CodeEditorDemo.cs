using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CodeEditor", "Input", Description = "Code-oriented editor with line numbers, pluggable margins, search/replace, and TextMateSharp-backed syntax highlighting.")]
public sealed class CodeEditorDemo : ControlsDemoBase
{
    private static readonly TextMateCodeEditorSyntaxHighlighter TextMateSyntaxHighlighter =
        new(new TextMateCodeEditorOptions { LanguageId = "csharp" });
    private static readonly CodeEditorSyntaxHighlighter ScreenshotTextMateSyntaxHighlighter =
        new SynchronousTextMateSyntaxHighlighter(TextMateSyntaxHighlighter);

    public CodeEditorDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        context.AllowPageScrollViewer = false;

        var wordWrap = new State<bool>(true);
        var showLineNumbers = new State<bool>(true);
        var highlightCurrentLine = new State<bool>(true);
        var showDiffMargin = new State<bool>(true);
        var useSyntaxHighlighter = new State<bool>(true);
        var goToLine = new State<int>(12);
        var goToColumn = new State<int>(5);
        var goToPosition = new State<int>(0);
        var caretLocationText = new State<string?>("Ln 1, Col 1");

        var editor = new CodeEditor(BuildDemoSource())
            .Placeholder("Type C# code here…")
            .MinHeight(14)
            .ShowLineNumbers(showLineNumbers)
            .HighlightCurrentLine(highlightCurrentLine)
            .WordWrap(wordWrap)
            .Style(() => CodeEditorStyle.Default with
            {
                SearchMatchBackground = (context.Theme.Accent ?? context.Theme.Selection)?.WithAlpha(0x40),
                ActiveSearchMatchBackground = context.Theme.Warning ?? context.Theme.Selection,
            });

        var diffMargin = CodeEditor.CreateDiffIndicatorMargin(
            glyphProvider: static lineIndex => lineIndex switch
            {
                3 => new Rune('+'),
                4 => new Rune('+'),
                11 => new Rune('~'),
                _ => null,
            },
            styleProvider: lineIndex => lineIndex switch
            {
                3 or 4 => Style.None.WithForeground(Colors.LimeGreen) | TextStyle.Bold,
                11 => Style.None.WithForeground(Colors.Gold) | TextStyle.Bold,
                _ => Style.None,
            });

        editor.Update(_ =>
        {
            var hasMargin = editor.LeftMargins.Contains(diffMargin);
            if (showDiffMargin.Value && !hasMargin)
            {
                editor.LeftMargins.Insert(0, diffMargin);
            }
            else if (!showDiffMargin.Value && hasMargin)
            {
                editor.LeftMargins.Remove(diffMargin);
            }
        });

        editor.Update(_ =>
        {
            editor.SyntaxHighlighter = useSyntaxHighlighter.Value
                ? (context.IsScreenshot ? ScreenshotTextMateSyntaxHighlighter : TextMateSyntaxHighlighter)
                : null;
            if (!useSyntaxHighlighter.Value)
            {
                editor.Highlighter(HighlightLine);
            }
            else
            {
                editor.Highlighter(default(CodeEditorLineHighlighter));
            }
        });

        editor.Update(_ =>
        {
            caretLocationText.Value = $"Ln {editor.Line}, Col {editor.Column}";
        });

        var controls = new HStack(
                new CheckBox("Wrap").IsChecked(wordWrap),
                new CheckBox("Line numbers").IsChecked(showLineNumbers),
                new CheckBox("Current line").IsChecked(highlightCurrentLine),
                new CheckBox("Diff margin").IsChecked(showDiffMargin),
                new CheckBox("TextMate syntax").IsChecked(useSyntaxHighlighter),
                new Button("Find").Click(() => editor.OpenFind("CodeEditor")),
                new Button("Replace").Click(() => editor.OpenReplace("return")),
                new Button("Jump deep").Click(() => editor.Scroll.SetOffset(0, 20)),
                new Button("Reset view").Click(() => editor.Scroll.SetOffset(0, 0)))
            .Spacing(context.IsScreenshot ? 0 : 1);

        var navigationRow = new HStack(
                "Line",
                new NumberBox<int>().Value(goToLine).MinWidth(4).MaxWidth(6),
                "Column",
                new NumberBox<int>().Value(goToColumn).MinWidth(4).MaxWidth(6),
                new Button("Go line").Click(() => editor.GoToLine(goToLine.Value)),
                new Button("Go column").Click(() => editor.GoToColumn(goToColumn.Value)),
                new Button("Go line + column").Click(() => editor.GoToLine(goToLine.Value, goToColumn.Value)))
            .Spacing(1);

        var positionRow = new HStack(
                "Position",
                new NumberBox<int>().Value(goToPosition).MinWidth(6).MaxWidth(8),
                new Button("Go position").Click(() => editor.GoToPosition(goToPosition.Value)),
                DemoUi.Hint("The footer below binds directly to editor.Line and editor.Column."))
            .Spacing(1);

        var help = new Markup(
            "[bold green]CodeEditor[/] shares the text engine with TextArea, then adds [cyan]line numbers[/], [cyan]margins[/], [cyan]search overlays[/], [cyan]syntax highlighting[/], and now programmatic [cyan]Go To Line / Column / Position[/]. [dim]Try Ctrl+F / Ctrl+H, Ctrl+Z / Ctrl+R, or use the jump controls below.[/]")
            .Wrap(true);

        var topPanel = new VStack(help, controls, navigationRow, positionRow)
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch);

        var locationFooter = new Footer()
            .Left(new TextBlock(caretLocationText))
            .Center(new TextBlock(() => $"Targets: line {goToLine.Value}, column {goToColumn.Value}, position {goToPosition.Value}"))
            .Right(new Markup("[dim]Ctrl+F Find • Ctrl+H Replace[/]") { Wrap = false });

        var bottomPanel = new VStack(
                locationFooter,
                DemoUi.Hint("The left diff margin is implemented through the public CodeEditorMargin contract. Toggle TextMate syntax to switch between the simple delegate and the persistent TextMateSharp-backed syntax-state pipeline."))
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch);

        var root = new DockLayout()
            .Top(topPanel)
            .Content(new Border(editor.Stretch().Scrollable()).Stretch())
            .Bottom(bottomPanel)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);

        return root.InScreenshot(context, () =>
        {
            var editorText = editor.Text ?? string.Empty;
            var caretIndex = editorText.IndexOf("CodeEditorDemo", StringComparison.Ordinal);
            editor.CaretIndex = Math.Min(editor.TextDocument.CurrentSnapshot.Length, Math.Max(0, caretIndex));
            editor.OpenFind("CodeEditor");
            editor.Scroll.SetOffset(0, 7);
        });

        static void HighlightLine(in CodeEditorLineHighlightRequest request, List<StyledRun> runs)
        {
            var lineText = SnapshotLineText(request.Snapshot, request.LineIndex);
            AddWordRuns(lineText, "public", Style.None.WithForeground(Colors.DeepSkyBlue) | TextStyle.Bold, runs);
            AddWordRuns(lineText, "class", Style.None.WithForeground(Colors.DeepSkyBlue) | TextStyle.Bold, runs);
            AddWordRuns(lineText, "return", Style.None.WithForeground(Colors.HotPink) | TextStyle.Bold, runs);
            AddQuotedRuns(lineText, Style.None.WithForeground(Colors.Gold), runs);
            AddCommentRuns(lineText, Style.None.WithForeground(Colors.LimeGreen) | TextStyle.Dim, runs);
        }
    }

    private static string BuildDemoSource()
    {
        var builder = new StringBuilder();
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using XenoAtom.Terminal.UI.Controls;");
        builder.AppendLine();
        builder.AppendLine("namespace Demo;");
        builder.AppendLine();
        builder.AppendLine("public sealed class CodeEditorDemo");
        builder.AppendLine("{");
        builder.AppendLine("    public string Render(int count)");
        builder.AppendLine("    {");
        builder.AppendLine("        // Search for CodeEditor or toggle Wrap in the demo toolbar.");
        builder.AppendLine("        if (count <= 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return \"empty\";");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var lines = new List<string>();");
        builder.AppendLine("        for (var i = 0; i < count; i++)");
        builder.AppendLine("        {");
        builder.AppendLine("            lines.Add($\"CodeEditor sample line {i:000}\");");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return string.Join(\"\\n\", lines);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        for (var i = 0; i < 40; i++)
        {
            builder.AppendLine($"// region sample-{i:00} :: public static string Label{i:00} => \"CodeEditor line {i:00}\";");
        }

        return builder.ToString();
    }

    private static string SnapshotLineText(ITextSnapshot snapshot, int lineIndex)
    {
        var line = snapshot.GetLine(lineIndex);
        if (line.Length == 0)
        {
            return string.Empty;
        }

        var buffer = new char[line.Length];
        snapshot.CopyTo(line.Start, buffer);
        return new string(buffer);
    }

    private static void AddWordRuns(string text, string token, Style style, List<StyledRun> runs)
    {
        var start = 0;
        while (start < text.Length)
        {
            var index = text.IndexOf(token, start, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            var beforeOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var afterIndex = index + token.Length;
            var afterOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
            if (beforeOk && afterOk)
            {
                runs.Add(new StyledRun(index, token.Length, style));
            }

            start = index + token.Length;
        }
    }

    private static void AddQuotedRuns(string text, Style style, List<StyledRun> runs)
    {
        var start = 0;
        while (start < text.Length)
        {
            var open = text.IndexOf('"', start);
            if (open < 0)
            {
                break;
            }

            var close = text.IndexOf('"', open + 1);
            if (close < 0)
            {
                runs.Add(new StyledRun(open, text.Length - open, style));
                break;
            }

            runs.Add(new StyledRun(open, (close - open) + 1, style));
            start = close + 1;
        }
    }

    private static void AddCommentRuns(string text, Style style, List<StyledRun> runs)
    {
        var commentIndex = text.IndexOf("//", StringComparison.Ordinal);
        if (commentIndex >= 0)
        {
            runs.Add(new StyledRun(commentIndex, text.Length - commentIndex, style));
        }
    }

    private sealed class SynchronousTextMateSyntaxHighlighter : CodeEditorSyntaxHighlighter
    {
        private readonly TextMateCodeEditorSyntaxHighlighter _inner;

        public SynchronousTextMateSyntaxHighlighter(TextMateCodeEditorSyntaxHighlighter inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => _inner.Build(context);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
            => _inner.Update(previousState, context);

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
            => _inner.GetLineRuns(state, request, runs);
    }
}
