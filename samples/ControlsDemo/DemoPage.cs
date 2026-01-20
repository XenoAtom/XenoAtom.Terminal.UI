using System.Diagnostics;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoPage
{
    public static Visual Build(IControlsDemo demo, DemoContext context)
    {
        ArgumentNullException.ThrowIfNull(demo);
        ArgumentNullException.ThrowIfNull(context);

        var meta = demo.Metadata;

        var log = new DemoLog();
        var content = demo.Build(new DemoContext
        {
            Log = log.Add,
            NavigateToDemoId = context.NavigateToDemoId,
            Runtime = context.Runtime,
            Theme = context.Theme
        });

        var link = BuildSourceLink(meta, log);
        var header = new Header()
            .Left(meta.Name)
            .Right(link);

        // Keep horizontal scrolling disabled for the overall demo page so that text controls can reflow (wrap)
        // as the terminal is resized. Individual demos can still opt into horizontal scrolling by using their own
        // ScrollViewer instances.
        var demoContent = new ScrollViewer(content)
            .HorizontalScrollEnabled(false);

        var logPanel = BuildLogPanel(log);

        return new DockLayout()
            .Top(new VStack(header, new Rule()).Spacing(0))
            .Content(demoContent)
            .Bottom(logPanel);
    }

    private static Visual BuildLogPanel(DemoLog log)
    {
        var logLines = new ComputedVisual(() =>
        {
            _ = log.Version.Value;

            var lines = log.Lines;
            if (lines.Count == 0)
            {
                return (Visual)"[dim]Log is empty.[/]";
            }

            var stack = new VStack().Spacing(0);
            var start = Math.Max(0, lines.Count - 4);
            for (var i = start; i < lines.Count; i++)
            {
                stack.Add(lines[i]);
            }

            return stack;
        });

        // Always visible, but small: show up to the last 4 lines.
        return new VStack(
            new Rule(),
            logLines.MaxHeight(4)).Spacing(0);
    }

    private static Visual BuildSourceLink(DemoMetadata meta, DemoLog log)
    {
        if (GitHubLinks.TryGetSourceUri(meta.SourcePath, out var sourceUri))
        {
            return new Link(sourceUri.ToString(), "Source");
        }

        _ = log;
        return new Markup("[dim]Source unavailable[/]");
    }

    private sealed class DemoLog
    {
        private readonly List<string> _lines = new();

        public State<int> Version { get; } = new(0);

        public int Count => _lines.Count;

        public IReadOnlyList<string> Lines => _lines;

        public void Add(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _lines.Add(text);
            if (_lines.Count > 200)
            {
                _lines.RemoveAt(0);
            }
            Version.Value++;
        }
    }
}
