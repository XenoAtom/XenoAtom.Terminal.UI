using System.Text;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("CodeEditor", "Input", Description = "Code-oriented editor with line numbers, pluggable margins, search/replace, and async-ready syntax highlighting.")]
public sealed class CodeEditorDemo : ControlsDemoBase
{
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
        var statusText = new State<string>("Ready");

        var editor = new CodeEditor(BuildDemoSource())
            .Placeholder("Type C# code here…")
            .MinHeight(14)
            .MaxHeight(14)
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
            editor.SyntaxHighlighter = useSyntaxHighlighter.Value ? DemoSyntaxHighlighter.Instance : null;
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
            statusText.Value = $"Version {editor.TextDocument.CurrentSnapshot.Version} • length {editor.TextDocument.CurrentSnapshot.Length} • scroll ({editor.Scroll.OffsetX}, {editor.Scroll.OffsetY})";
        });

        var controls = new HStack(
                new CheckBox("Wrap").IsChecked(wordWrap),
                new CheckBox("Line numbers").IsChecked(showLineNumbers),
                new CheckBox("Current line").IsChecked(highlightCurrentLine),
                new CheckBox("Diff margin").IsChecked(showDiffMargin),
                new CheckBox("Advanced syntax").IsChecked(useSyntaxHighlighter),
                new Button("Find").Click(() => editor.OpenFind("CodeEditor")),
                new Button("Replace").Click(() => editor.OpenReplace("return")),
                new Button("Jump deep").Click(() => editor.Scroll.SetOffset(0, 20)),
                new Button("Reset view").Click(() => editor.Scroll.SetOffset(0, 0)))
            .Spacing(1);

        var help = new Markup(
            "[bold green]CodeEditor[/] shares the text engine with TextArea, then adds [cyan]line numbers[/], [cyan]margins[/], [cyan]search overlays[/], and [cyan]syntax highlighting[/]. [dim]Try Ctrl+F / Ctrl+H, Ctrl+Z / Ctrl+R, or scroll through the long sample file.[/]")
            .Wrap(true);

        var root = new VStack(
                help,
                controls,
                new Border(editor.Scrollable()).Stretch(),
                new TextBlock(() => statusText.Value),
                DemoUi.Hint("The left diff margin is implemented through the public CodeEditorMargin contract. Toggle Advanced syntax to switch between the simple delegate and persistent syntax-state pipelines."))
            .Spacing(1)
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

    private sealed class DemoSyntaxHighlighter : CodeEditorSyntaxHighlighter
    {
        public static DemoSyntaxHighlighter Instance { get; } = new();

        private DemoSyntaxHighlighter()
        {
        }

        public override CodeEditorSyntaxState Build(in CodeEditorSyntaxBuildContext context)
            => new DemoSyntaxState(context.Snapshot.Version);

        public override CodeEditorSyntaxState Update(CodeEditorSyntaxState previousState, in CodeEditorSyntaxUpdateContext context)
        {
            _ = previousState;
            return new DemoSyntaxState(context.Snapshot.Version);
        }

        public override void GetLineRuns(CodeEditorSyntaxState state, in CodeEditorLineSyntaxRequest request, List<StyledRun> runs)
        {
            _ = state;
            HighlightLine(new CodeEditorLineHighlightRequest(request.Snapshot, request.Theme, request.LineIndex, request.LineStart, request.LineLength, request.CaretIndex, request.SelectionStart, request.SelectionLength), runs);
        }

        private static void HighlightLine(in CodeEditorLineHighlightRequest request, List<StyledRun> runs)
        {
            var lineText = SnapshotLineText(request.Snapshot, request.LineIndex);
            AddWordRuns(lineText, "public", Style.None.WithForeground(Colors.DeepSkyBlue) | TextStyle.Bold, runs);
            AddWordRuns(lineText, "class", Style.None.WithForeground(Colors.DeepSkyBlue) | TextStyle.Bold, runs);
            AddWordRuns(lineText, "return", Style.None.WithForeground(Colors.HotPink) | TextStyle.Bold, runs);
            AddQuotedRuns(lineText, Style.None.WithForeground(Colors.Gold), runs);
            AddCommentRuns(lineText, Style.None.WithForeground(Colors.LimeGreen) | TextStyle.Dim, runs);
        }
    }

    private sealed class DemoSyntaxState : CodeEditorSyntaxState
    {
        public DemoSyntaxState(int snapshotVersion)
        {
            SnapshotVersion = snapshotVersion;
        }

        public override int SnapshotVersion { get; }
    }
}
