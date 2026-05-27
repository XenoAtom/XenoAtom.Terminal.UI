using System.Diagnostics;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Geometry;

var flow = new DocumentFlow
{
    HorizontalAlignment = Align.Stretch,
    VerticalAlignment = Align.Stretch,
    ItemPadding = new Thickness(1, 0, 0, 0),
    ItemSpacing = 0,
    FollowTail = true,
};

flow.Items.Add(CreateCard("User", "Let me gather some key details about the project."));
flow.Items.Add(CreateCard("Tool Calls", "- `read_file readme.md`\n- `list_dir src`\n- `list_dir CodeAlta`"));
flow.Items.Add(CreateCard("Reasoning", "The user wants details about the project. I have the readme, the AGENTS.md, and the project structure. Let me give a concise summary."));

var assistantMarkdown = new MarkdownControl(string.Empty)
{
    HorizontalAlignment = Align.Stretch,
    VerticalAlignment = Align.Start,
    Options = MarkdownRenderOptions.Default with
    {
        MaxCodeBlockHeight = 8,
        WrapText = true,
    },
};

var assistantTimestamp = new Markup(string.Empty);
var assistantCard = new Group(new Markup("[success]🤖[/] [bold]Assistant[/]"), assistantMarkdown)
    .BottomRightText(assistantTimestamp)
    .HorizontalAlignment(Align.Stretch)
    .VerticalAlignment(Align.Start);

flow.Items.Add(new DocumentFlowItem
{
    Content = new FlowDocument().Add(assistantCard),
    Alignment = DocumentFlowAlignment.Stretch,
});

var markdown = """
Here's a summary of **CodeAlta**:

## What It Is

CodeAlta is a **terminal workspace for agentic coding** — a .NET 10 CLI tool (`alta`) written by Alexandre Mutel (xoofx). It's pre-release, licensed under BSD-2-Clause.

## Key Capabilities

- **Keyboard-first TUI**: tabs, prompt editor, project sidebar, command discovery, model selectors, and session timeline.
- **Progressive assistant output**: content arrives in small deltas while the document flow remains pinned to the tail.
- **Markdown-rich timeline**: headings, lists, inline code, links, and code blocks are rendered inside retained-mode chat cards.
- **Tool activity cards**: tool calls, file changes, and reasoning/status blocks are interleaved with assistant messages.

This paragraph is intentionally long enough to cross several terminal widths, because the regression shows up as a paragraph sometimes being arranged with its wrapped height and sometimes with a stale one-line height. If it reproduces, the card will visibly squash and expand while this text streams.

```csharp
var control = new MarkdownControl(markdown)
{
    HorizontalAlignment = Align.Stretch,
    VerticalAlignment = Align.Start,
};
```
""";

var index = 0;
var completedTicks = 0;
var lastUpdate = Stopwatch.GetTimestamp();

Terminal.Run(flow, () =>
{
    var now = Stopwatch.GetTimestamp();
    if (Stopwatch.GetElapsedTime(lastUpdate, now) < TimeSpan.FromMilliseconds(35))
    {
        return TerminalLoopResult.Continue;
    }

    lastUpdate = now;
    assistantTimestamp.Text = $"[dim]{DateTimeOffset.Now:HH:mm:ss}[/]";

    if (index < markdown.Length)
    {
        var step = char.IsWhiteSpace(markdown[index]) ? 1 : 2;
        index = Math.Min(markdown.Length, index + step);
        assistantMarkdown.Markdown = markdown[..index];
        //flow.ScrollToTail();
    }
    else if (++completedTicks > 35)
    {
        // Loop so the before/after behavior can be watched continuously.
        completedTicks = 0;
        index = 0;
        assistantMarkdown.Markdown = string.Empty;
        //flow.ScrollToTail();
    }

    return TerminalLoopResult.Continue;
});

static DocumentFlowItem CreateCard(string title, string body)
{
    var group = new Group(new Markup($"[primary]{title}[/]"), new MarkdownControl(body)
    {
        HorizontalAlignment = Align.Stretch,
        VerticalAlignment = Align.Start,
        Options = MarkdownRenderOptions.Default with
        {
            MaxCodeBlockHeight = 6,
            WrapText = true,
        },
    })
    .HorizontalAlignment(Align.Stretch)
    .VerticalAlignment(Align.Start);

    return new DocumentFlowItem
    {
        Content = new FlowDocument().Add(group),
        Alignment = DocumentFlowAlignment.Stretch,
    };
}
