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

        var showLog = new State<bool>(false);
        var logControl = new LogControl
        {
            MaxCapacity = 500,
        }.WrapText(true);

        void AppendLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            showLog.Value = true;

            var prefix = $"[dim][{DateTime.Now:HH:mm:ss}] [/]";
            logControl.AppendMarkupLine(prefix + message);
        }

        var demoContext = new DemoContext
        {
            IsScreenshot = context.IsScreenshot,
            Log = AppendLog,
            NavigateToDemoId = context.NavigateToDemoId,
            Runtime = context.Runtime,
            Theme = context.Theme,
            ToastHost = context.ToastHost
        };

        var content = demo.Build(demoContext);

        Visual? logPanel = null;
        var logRegion = new ComputedVisual(() =>
        {
            if (!showLog.Value)
            {
                return null;
            }

            logPanel ??= BuildLogPanel(logControl);
            return logPanel;
        });

        var link = BuildSourceLink(meta);
        var toggleLog = new Button("Logs")
            .Click(() => showLog.Value = !showLog.Value);

        var header = new Header()
            .Left(meta.Name)
            .Right(new HStack(link, toggleLog).Spacing(1));

        // Page-level scrolling is opt-in per demo to avoid nested-scroll conflicts with controls that already
        // provide their own scrolling behavior (e.g. DocumentFlow, LogControl, MarkdownControl, ScrollViewer).
        var demoContent = demoContext.AllowPageScrollViewer
            ? new ScrollViewer(content).HorizontalScrollEnabled(false)
            : content;

        return new DockLayout()
            .Top(new VStack(header, new Rule()).Spacing(0))
            .Content(demoContent)
            .Bottom(logRegion);
    }

    private static Visual BuildLogPanel(LogControl logControl)
    {
        return new Group("Logs").Content(logControl).HorizontalAlignment(Align.Stretch).MaxHeight(12);
    }

    private static Visual BuildSourceLink(DemoMetadata meta)
    {
        if (GitHubLinks.TryGetSourceUri(meta.SourcePath, out var sourceUri))
        {
            return new Link(sourceUri.ToString(), "Source");
        }

        return new Markup("[dim]Source unavailable[/]");
    }
}
