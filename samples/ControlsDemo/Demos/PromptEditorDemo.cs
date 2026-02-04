using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("PromptEditor", "Input", Description = "Prompt-style editor with markup prompt, completion (Tab), and history (Alt+Up/Alt+Down).")]
public sealed class PromptEditorDemo : ControlsDemoBase
{
    public PromptEditorDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var lastAccepted = new State<string>("(none)");

        var promptEditor = new PromptEditor()
            .PromptMarkup("[primary]demo[/] [muted]>[/] ")
            .ContinuationPromptMarkup("[muted]·[/] ")
            .Placeholder("Type a command. Tab completes. Ctrl+J inserts a newline.")
            .EnableWordHints(true)
            .CompletionPresentation(PromptEditorCompletionPresentation.PopupList)
            .CompletionHandler(Complete)
            .Highlighter(new DemoHighlighter())
            .AutoFocus(true)
            .MinHeight(6)
            .MaxHeight(6);

        var editor = promptEditor.Scrollable();

        promptEditor.Accepted((_, e) =>
        {
            lastAccepted.Value = e.Text;
            context.Log($"Accepted: {e.Text}");

            // Clear the prompt after accepting so it feels like a terminal prompt.
            promptEditor.Text = string.Empty;
        });

        promptEditor.Canceled((_, _) => context.Log("Canceled."));

        return new VStack(
                DemoUi.Hint("Enter accepts by default. Use Ctrl+J to insert a newline. Tab requests completion."),
                editor,
                new TextBlock(() => $"Last accepted: {lastAccepted.Value}"),
                new Rule(),
                new CommandBar())
            .Spacing(1);

        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
        {
            var text = SnapshotToString(request.Snapshot);
            var caret = Math.Clamp(request.CaretIndex, 0, text.Length);

            var start = TerminalTextUtility.GetWordStart(text.AsSpan(), caret);
            var prefix = text.AsSpan(start, caret - start).ToString();

            var commands = new[]
            {
                "help",
                "clear",
                "exit",
                "open",
                "theme",
                "build",
                "run",
                "search",
                "grep",
                "status",
            };

            var candidates = new List<string>(commands.Length);
            foreach (var c in commands)
            {
                if (prefix.Length == 0 || c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(c);
                }
            }

            string? ghost = null;
            if (candidates.Count > 0 && caret == text.Length)
            {
                var best = candidates[0];
                if (best.Length > prefix.Length && best.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    ghost = best[prefix.Length..];
                }
            }

            return new PromptEditorCompletion(
                Handled: true,
                Candidates: candidates,
                ReplaceStart: start,
                ReplaceLength: caret - start,
                SelectedIndex: 0,
                GhostText: ghost);
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

    private sealed class DemoHighlighter : IPromptEditorHighlighter
    {
        public void Highlight(in PromptEditorHighlightRequest request, List<StyledRun> runs)
        {
            var snapshot = request.Snapshot;
            if (snapshot.Length == 0)
            {
                return;
            }

            var text = string.Create(snapshot.Length, snapshot, static (span, s) => s.CopyTo(0, span));

            // Highlight flags like --help.
            for (var i = 0; i < text.Length - 1; i++)
            {
                if (text[i] == '-' && text[i + 1] == '-')
                {
                    var end = i + 2;
                    while (end < text.Length && TerminalTextUtility.IsWordChar(text[end]))
                    {
                        end++;
                    }

                    var style = Style.None | TextStyle.Bold;
                    if (request.Theme.Accent is { } accent)
                    {
                        style = style.WithForeground(accent);
                    }

                    runs.Add(new StyledRun(i, end - i, style));
                    i = end;
                }
            }

            // Dim numbers (e.g. --count 10).
            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                {
                    continue;
                }

                var end = i + 1;
                while (end < text.Length && char.IsDigit(text[end]))
                {
                    end++;
                }

                runs.Add(new StyledRun(i, end - i, Style.None | TextStyle.Dim));
                i = end;
            }
        }
    }
}
