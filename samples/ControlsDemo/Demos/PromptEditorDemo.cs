using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("PromptEditor", "Input", Description = "Prompt-style editor with markup prompt, completion (Tab), and history (Alt+Up/Alt+Down).")]
public sealed class PromptEditorDemo : ControlsDemoBase
{
    private readonly record struct PromptCommand(string Command, string Description);

    public PromptEditorDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var lastAccepted = new State<string>("(none)");
        var promptCounter = new State<int>(1);

        var commands = new PromptCommand[]
        {
            new("/help", "Show help and available commands."),
            new("/clear", "Clear the current input."),
            new("/exit", "Exit the demo."),
            new("/open", "Open something (demo command)."),
            new("/theme", "Switch theme (demo command)."),
            new("/build", "Run build (demo command)."),
            new("/run", "Run a task (demo command)."),
            new("/search", "Search in the current buffer."),
            new("/grep", "Grep for a pattern (demo command)."),
            new("/status", "Show status (demo command)."),
        };

        var help = new Markup("""
                              [bold green]PromptEditor[/] — prompt-style editor demo

                              [gray]Try:[/]
                               • Type [bold red]error[/], [bold yellow]warn[/], [bold green]info[/] to see syntax highlighting.
                               • The [underline]current word[/] is underlined (caret-aware highlight).
                               • Type [muted]/[/] then press [cyan]Tab[/] to complete commands like [muted]/help[/], [muted]/clear[/], [muted]/exit[/].
                               • Press [cyan]Alt+↑[/]/[cyan]Alt+↓[/] to navigate history.
                               • Press [cyan]Enter[/] to accept; [cyan]Ctrl+J[/] inserts a newline; [cyan]Esc[/] cancels completion/prompt.
                              """);

        var prompt = new Markup(() => $"[gray]{promptCounter.Value,3}[/] [primary]demo[/] [muted]>[/]");

        var promptEditor = new PromptEditor()
            .Prompt(prompt)
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
        var suggestionBar = new ComputedVisual(() => BuildCommandSuggestionBar(promptEditor, commands));

        promptEditor.Accepted((_, e) =>
        {
            lastAccepted.Value = e.Text;
            context.Log($"Accepted: {e.Text}");

            promptCounter.Value++;

            // Clear the prompt after accepting so it feels like a terminal prompt.
            promptEditor.Text = string.Empty;
        });

        promptEditor.Canceled((_, _) => context.Log("Canceled."));

        return new VStack(
                help,
                editor,
                suggestionBar,
                new TextBlock(() => $"Last accepted: {lastAccepted.Value}"),
                new Rule(),
                new CommandBar())
            .Spacing(1);

        static PromptEditorCompletion Complete(in PromptEditorCompletionRequest request)
        {
            var text = SnapshotToString(request.Snapshot);
            var caret = Math.Clamp(request.CaretIndex, 0, text.Length);

            var start = GetCommandWordStart(text.AsSpan(), caret);
            var prefix = text.AsSpan(start, caret - start).ToString();

            var commandList = new[] { "/help", "/clear", "/exit", "/open", "/theme", "/build", "/run", "/search", "/grep", "/status" };
            var candidates = new List<string>(commandList.Length);
            if (prefix.StartsWith("/", StringComparison.Ordinal))
            {
                foreach (var c in commandList)
                {
                    if (prefix.Length == 0 || c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(c);
                    }
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

        static int GetCommandWordStart(ReadOnlySpan<char> text, int caret)
        {
            var start = TerminalTextUtility.GetWordStart(text, caret);

            // Treat `/` as part of the current token so `/help`-style commands can be completed.
            // TerminalTextUtility's word logic intentionally treats `/` as a word boundary.
            if (start > 0 && text[start - 1] == '/')
            {
                return start - 1;
            }

            if (caret > 0 && text[caret - 1] == '/')
            {
                return caret - 1;
            }

            return start;
        }

        static Visual? BuildCommandSuggestionBar(PromptEditor editor, IReadOnlyList<PromptCommand> commands)
        {
            var text = editor.Text;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var caret = Math.Clamp(editor.CaretIndex, 0, text.Length);
            var span = text.AsSpan();

            var start = GetCommandWordStart(span, caret);
            var end = GetCommandWordEnd(span, caret, start);
            if (end < start)
            {
                return null;
            }

            var prefix = span.Slice(start, caret - start);
            if (prefix.Length == 0 || prefix[0] != '/')
            {
                return null;
            }

            // Limit suggestions to the current token only (avoid showing for "/help something" when caret is past the token).
            if (caret > end)
            {
                return null;
            }

            var prefixText = prefix.ToString();
            var matches = new List<PromptCommand>(commands.Count);
            for (var i = 0; i < commands.Count; i++)
            {
                var cmd = commands[i];
                if (cmd.Command.StartsWith(prefixText, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(cmd);
                }
            }

            if (matches.Count == 0)
            {
                return new TextBlock("[muted]No matching commands. Try [/][cyan]/help[/][muted].[/]");
            }

            var accent = editor.GetTheme().Accent ?? editor.GetTheme().Primary ?? editor.GetTheme().Foreground;
            var chipBg = (accent ?? XenoAtom.Terminal.UI.Color.Default).WithAlpha(0x22);
            var chipBgActive = (accent ?? XenoAtom.Terminal.UI.Color.Default).WithAlpha(0x38);

            var prefixLen = prefixText.Length;
            var chipVisuals = new List<Visual>(Math.Min(matches.Count, 10) + 1)
            {
                new TextBlock("[muted]Commands:[/]")
            };

            var shown = 0;
            for (var i = 0; i < matches.Count && shown < 10; i++, shown++)
            {
                var cmd = matches[i];
                var isBest = i == 0;
                var bg = isBest ? chipBgActive : chipBg;
                var title = cmd.Command;

                // Use spaces as padding so we can keep this very lightweight (no extra layout controls).
                var chipText = $" {title} ";
                chipVisuals.Add(
                    new TextBlock(chipText).Style(TextBlockStyle.Default with
                    {
                        Background = bg,
                        FillBackground = true,
                        TextStyle = isBest ? TextStyle.Bold : default,
                    }));
            }

            var hint = matches.Count > 10 ? new TextBlock($"[muted]+{matches.Count - 10} more…[/]") : null;
            var details = matches.Count == 1
                ? new Markup($"[muted]↳[/] [primary]{matches[0].Command}[/] [muted]— {EscapeMarkup(matches[0].Description)}[/]")
                : new Markup($"[muted]↳ Press[/] [cyan]Tab[/] [muted]to complete. Prefix:[/] [primary]{EscapeMarkup(prefixText)}[/]");

            var wrap = new WrapHStack(chipVisuals.ToArray()).Spacing(1).RunSpacing(0);

            return new VStack(
                    wrap,
                    hint is null ? details : new HStack(details, hint).Spacing(1))
                .Spacing(0);

            static string EscapeMarkup(string text) => text.Replace("[", "\\[").Replace("]", "\\]");
        }

        static int GetCommandWordEnd(ReadOnlySpan<char> text, int caret, int tokenStart)
        {
            caret = Math.Clamp(caret, 0, text.Length);
            tokenStart = Math.Clamp(tokenStart, 0, text.Length);

            if (tokenStart < text.Length && text[tokenStart] == '/')
            {
                var index = Math.Min(text.Length, tokenStart + 1);
                return TerminalTextUtility.GetWordEnd(text, index);
            }

            return TerminalTextUtility.GetWordEnd(text, caret);
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
