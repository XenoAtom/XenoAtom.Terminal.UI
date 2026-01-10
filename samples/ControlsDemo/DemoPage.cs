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
        });

        var demoTab = new ScrollViewer
        {
            Content = new VStack(content).Spacing(1).HorizontalAlignment(HorizontalAlignment.Stretch),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var logTab = BuildLogTab(log);
        var sourceTab = BuildSourceTab(meta, log);

        var tabs = new TabControl(
            new TabPage("Demo", demoTab),
            new TabPage(new TextBlock().Text(() => { _ = log.Version.Value; return $"Log ({log.Count})"; }), logTab),
            new TabPage("Source", sourceTab))
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var header = new VStack(
                new Markup($"[bold]{EscapeMarkup(meta.Name)}[/]"),
                new Markup($"[dim]{EscapeMarkup(meta.Description)}[/]").Wrap(true))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        return new DockLayout()
            .Top(header)
            .Content(tabs)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Stretch);
    }

    private static Visual BuildLogTab(DemoLog log)
    {
        return new ComputedVisual(() =>
        {
            _ = log.Version.Value;

            var lines = log.Lines;
            var stack = new VStack().Spacing(0);
            for (var i = 0; i < lines.Count; i++)
            {
                stack.Add(new TextBlock(lines[i]));
            }

            return new ScrollViewer
            {
                Content = stack,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
        });
    }

    private static Visual BuildSourceTab(DemoMetadata meta, DemoLog log)
    {
        var uri = GitHubLinks.TryGetSourceUri(meta.SourcePath, out var sourceUri) ? sourceUri : null;

        Visual link = uri is null
            ? new Markup("[dim]Source link unavailable[/]")
            : new Link(uri.ToString(), "Open demo source on GitHub");

        var copyPath = new Button("Copy relative path").Click(() =>
        {
            if (GitHubLinks.TryGetSourceRelativePath(meta.SourcePath, out var relative))
            {
                try
                {
                    XenoAtom.Terminal.Terminal.Instance.Clipboard.TrySetText(relative);
                    log.Add($"Copied: {relative}");
                }
                catch (Exception ex)
                {
                    log.Add($"Copy failed: {ex.Message}");
                }
            }
            else
            {
                log.Add("Copy failed: could not compute relative path.");
            }
        });

        return new VStack(
                new Group()
                    .TopLeftText("Source")
                    .Padding(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(new VStack(
                            link,
                            new Markup($"[dim]{EscapeMarkup(meta.SourcePath)}[/]").Wrap(true),
                            copyPath)
                        .Spacing(1)
                        .HorizontalAlignment(HorizontalAlignment.Stretch)),
                new Group()
                    .TopLeftText("Tags")
                    .Padding(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .Content(new Markup($"[dim]{EscapeMarkup(meta.Tags.Count == 0 ? "<none>" : string.Join(", ", meta.Tags))}[/]").Wrap(true)))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);
    }

    private static string EscapeMarkup(string text)
        => text
            .Replace("[", "[[", StringComparison.Ordinal)
            .Replace("]", "]]", StringComparison.Ordinal);

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
