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
        var promptCounter = 1;

        var help = new Markup("""
                              [bold green]PromptEditor[/] — prompt-style editor demo

                              [gray]Try:[/]
                               • Type [bold red]error[/], [bold yellow]warn[/], [bold green]info[/] to see syntax highlighting.
                               • The [underline]current word[/] is underlined (caret-aware highlight).
                               • Press [cyan]Tab[/] to request completion (popup list) for commands like [muted]help[/], [muted]clear[/], [muted]exit[/].
                               • Press [cyan]Alt+↑[/]/[cyan]Alt+↓[/] to navigate history.
                               • Press [cyan]Enter[/] to accept; [cyan]Ctrl+J[/] inserts a newline; [cyan]Esc[/] cancels completion/prompt.
                              """);

        var promptEditor = new PromptEditor()
            .PromptMarkup(GetPromptMarkup())
            .ContinuationPromptMarkup("[muted]·[/] ")
            .Placeholder("Type a command. Tab completes. Ctrl+J inserts a newline.")
            .EnableWordHints(false)
            .CompletionPresentation(PromptEditorCompletionPresentation.PopupList)
            .CompletionHandler(Complete)
            .Highlighter(Highlight)
            .AutoFocus(true)
            .MinHeight(6)
            .MaxHeight(6);

        var editor = promptEditor.Scrollable();

        promptEditor.Accepted((_, e) =>
        {
            lastAccepted.Value = e.Text;
            context.Log($"Accepted: {e.Text}");

            promptCounter++;
            promptEditor.PromptMarkup(GetPromptMarkup());

            // Clear the prompt after accepting so it feels like a terminal prompt.
            promptEditor.Text = string.Empty;
        });

        promptEditor.Canceled((_, _) => context.Log("Canceled."));

        return new VStack(
                help,
                editor,
                new TextBlock(() => $"Last accepted: {lastAccepted.Value}"),
                new Rule(),
                new CommandBar())
            .Spacing(1);

        string GetPromptMarkup()
            => $"[gray]{promptCounter,3}[/] [primary]demo[/] [muted]>[/] ";

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

        static void Highlight(in PromptEditorHighlightRequest request, List<StyledRun> runs)
        {
            var snapshot = request.Snapshot;
            if (snapshot.Length == 0)
            {
                return;
            }

            var text = string.Create(snapshot.Length, snapshot, static (span, s) => s.CopyTo(0, span));
            var spanText = text.AsSpan();
            var theme = request.Theme;

            // Underline the current word (matches the ReadLine sample behavior).
            var caret = Math.Clamp(request.CaretIndex, 0, spanText.Length);
            var wordStart = TerminalTextUtility.GetWordStart(spanText, caret);
            var wordEnd = TerminalTextUtility.GetWordEnd(spanText, caret);
            if (wordEnd > wordStart)
            {
                runs.Add(new StyledRun(wordStart, wordEnd - wordStart, Style.None.WithTextStyle(TextStyle.Underline)));
            }

            // Keyword highlight (error/warn/info) similar to HelloReadLine.
            for (var i = 0; i < spanText.Length; i++)
            {
                if (!TryMatchKeyword(spanText, i, out var length, out var style))
                {
                    continue;
                }

                runs.Add(new StyledRun(i, length, style));
                i += length - 1;
            }

            Style BuildStyle(Color? fg)
            {
                var s = Style.None.AddTextStyle(TextStyle.Bold);
                return fg is { } c ? s.WithForeground(c) : s;
            }

            bool TryMatchKeyword(ReadOnlySpan<char> fullText, int index, out int length, out Style style)
            {
                length = 0;
                style = Style.None;

                if (!TerminalTextUtility.IsWordStart(fullText, index))
                {
                    return false;
                }

                var remaining = fullText.Slice(index);

                if (remaining.StartsWith("error", StringComparison.OrdinalIgnoreCase) && TerminalTextUtility.IsWordEnd(fullText, index + 5))
                {
                    length = 5;
                    style = BuildStyle(theme.Error);
                    return true;
                }

                if (remaining.StartsWith("warning", StringComparison.OrdinalIgnoreCase) && TerminalTextUtility.IsWordEnd(fullText, index + 7))
                {
                    length = 7;
                    style = BuildStyle(theme.Warning);
                    return true;
                }

                if (remaining.StartsWith("warn", StringComparison.OrdinalIgnoreCase) && TerminalTextUtility.IsWordEnd(fullText, index + 4))
                {
                    length = 4;
                    style = BuildStyle(theme.Warning);
                    return true;
                }

                if (remaining.StartsWith("info", StringComparison.OrdinalIgnoreCase) && TerminalTextUtility.IsWordEnd(fullText, index + 4))
                {
                    length = 4;
                    style = BuildStyle(theme.Success);
                    return true;
                }

                return false;
            }
        }
    }
}
