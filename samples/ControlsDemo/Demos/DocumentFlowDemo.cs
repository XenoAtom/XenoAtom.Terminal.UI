using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("DocumentFlow", "Content", Description = "Virtualized conversation-like document feed with mixed block content.")]
public sealed class DocumentFlowDemo : ControlsDemoBase
{
    public DocumentFlowDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var itemSpacing = new State<int>(1);
        var itemPadding = new State<int>(1);
        var maxCapacity = new State<int>(200);
        var autoTail = new State<bool>(true);
        var alternateSides = new State<bool>(true);
        var appendBatchCount = new State<int>(3);
        var flow = new DocumentFlow()
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch)
            .ItemPadding(() => new Thickness(Math.Clamp(itemPadding.Value, 0, 6)))
            .ItemSpacing(itemSpacing)
            .MaxCapacity(maxCapacity)
            .AutoFocus(true);

        if (context.IsScreenshot)
        {
            PopulateScreenshotItems(flow);
            flow.ScrollToTail();
            return new VStack(
                    DemoUi.Hint("DocumentFlow composes existing controls and keeps scrolling smooth with block virtualization."),
                    flow)
                .Spacing(1);
        }

        PopulateInteractiveItems(flow, context);
        flow.ScrollToTail();

        var appendCounter = new State<int>(1);
        var nextRight = true;

        void AppendMessage(bool right)
        {
            var index = appendCounter.Value;
            appendCounter.Value = index + 1;
            var message = right
                ? CreateRightMessage(
                    new FlowDocument().AddParagraph($"Appended conversation item #{index} from the assistant."),
                    maxWidth: 48,
                    background: Style.None.WithBackground(Colors.Purple))
                : CreateLeftMessage(
                    new FlowDocument().AddParagraph($"Appended conversation item #{index} from the user."),
                    maxWidth: 52,
                    background: Style.None.WithBackground(Colors.DarkSlateBlue));
            flow.Items.Add(message);

            if (autoTail.Value)
            {
                flow.ScrollToTail();
            }
        }

        var appendButton = new Button("Append message")
            .Click(() =>
            {
                var useRight = alternateSides.Value ? nextRight : true;
                AppendMessage(useRight);
                if (alternateSides.Value)
                {
                    nextRight = !nextRight;
                }
            });

        var appendBatchButton = new Button("Append batch")
            .Click(() =>
            {
                var count = Math.Clamp(appendBatchCount.Value, 1, 20);
                for (var i = 0; i < count; i++)
                {
                    var useRight = alternateSides.Value ? nextRight : true;
                    AppendMessage(useRight);
                    if (alternateSides.Value)
                    {
                        nextRight = !nextRight;
                    }
                }
            });

        var resetButton = new Button("Reset")
            .Click(() =>
            {
                flow.Items.Clear();
                appendCounter.Value = 1;
                nextRight = true;
                PopulateInteractiveItems(flow, context);
                if (autoTail.Value)
                {
                    flow.ScrollToTail();
                }
            });

        var status = new TextBlock(() =>
        {
            _ = flow.Scroll.Version;
            return $"FollowTail: {(flow.FollowTail ? "On" : "Off")}  OffsetY: {flow.Scroll.OffsetY}  Extent: {flow.Scroll.ExtentHeight}  Viewport: {flow.Scroll.ViewportHeight}";
        });

        var settings = new HStack(
                new Switch("Auto tail").IsOn(autoTail),
                new Switch("Alternate sides").IsOn(alternateSides),
                "Item spacing:",
                new NumberBox<int>().Value(itemSpacing).ValueValidator(value => value is >= 0 and <= 6 ? null : "Use [0..6]"),
                "Item padding:",
                new NumberBox<int>().Value(itemPadding).ValueValidator(value => value is >= 0 and <= 6 ? null : "Use [0..6]"),
                "Max capacity:",
                new NumberBox<int>().Value(maxCapacity).ValueValidator(value => value is >= 0 and <= 1000 ? null : "Use [0..1000]"),
                "Batch size:",
                new NumberBox<int>().Value(appendBatchCount).ValueValidator(value => value is >= 1 and <= 20 ? null : "Use [1..20]"))
            .Spacing(1);

        return new VStack(
                DemoUi.Hint("DocumentFlow composes existing controls and keeps scrolling smooth with block virtualization."),
                new HStack(
                        appendButton,
                        appendBatchButton,
                        new Button("Scroll to tail").Click(flow.ScrollToTail),
                        resetButton)
                    .Spacing(1),
                settings,
                new Markup("[dim]Try: mouse wheel over content, Up/Down/Home/End keys, and toggle Auto tail.[/]"),
                status,
                flow)
            .Spacing(1);
    }

    private static void PopulateScreenshotItems(DocumentFlow flow)
    {
        flow.Items.Add(CreateLeftMessage(
            new FlowDocument()
                .AddParagraph("DocumentFlow virtualizes by block, so only visible document blocks are attached and arranged.")
                .AddParagraph("This is useful for chat, docs viewers, and streaming assistant transcripts."),
            maxWidth: 52,
            background: Style.None.WithBackground(Colors.DarkSlateBlue)));

        flow.Items.Add(CreateRightMessage(
            new FlowDocument()
                .AddParagraph("Right aligned bubble\nwith a second line to showcase alignment."),
            maxWidth: 46,
            background: Style.None.WithBackground(Colors.DarkSlateGray)));

        var table = new Table()
            .Headers("Phase", "Status")
            .AddRow("Parse", "Done")
            .AddRow("Layout", "Done")
            .AddRow("Render", "Running")
            .Style(TableStyle.RoundedGrid with { ShowRowSeparators = true });

        flow.Items.Add(CreateLeftMessage(
            new FlowDocument()
                .AddParagraph("Existing controls can be hosted directly as blocks.")
                .Add(table),
            maxWidth: 54,
            background: Style.None.WithBackground(Colors.Teal)));

        var codeLog = new LogControl()
            .WrapText(false)
            .MaxHeight(4)
            .HorizontalAlignment(Align.Stretch);
        codeLog.AppendMarkupLine("[green]// markdown code fence[/]");
        codeLog.AppendLine("public static void Render(DocumentFlow flow)");
        codeLog.AppendLine("{");
        codeLog.AppendLine("    // ...");
        codeLog.AppendLine("}");

        flow.Items.Add(CreateRightMessage(
            new FlowDocument().Add(codeLog),
            maxWidth: 58,
            background: Style.None.WithBackground(Colors.SlateGray)));
    }

    private static void PopulateInteractiveItems(DocumentFlow flow, DemoContext context)
    {
        PopulateScreenshotItems(flow);

        var collapsible = new Collapsible(
            new TextBlock("## Collapsible details"),
            new Paragraph("A collapsible block can host any visual content and keeps DocumentFlow generic.")
                .Wrap(true)
                .HorizontalAlignment(Align.Stretch))
            .IsExpanded(true);

        flow.Items.Add(CreateLeftMessage(
            new FlowDocument().Add(collapsible),
            maxWidth: 56,
            background: Style.None.WithBackground(Colors.DarkMagenta)));

        var streamingVisual = new HStack(
                new Spinner().Style(SpinnerStyles.Dots8),
                new TextBlock(() => $"Streaming tail update #{context.Runtime.Frame.Value}")
                    .Wrap(false))
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch);

        flow.Items.Add(CreateLeftMessage(
            new FlowDocument().Add(streamingVisual),
            maxWidth: 60,
            background: Style.None.WithBackground(Colors.DarkOliveGreen)));
    }

    private static DocumentFlowItem CreateLeftMessage(FlowDocument content, int maxWidth, Style background)
        => new()
        {
            Content = content,
            Alignment = DocumentFlowAlignment.Left,
            MaxWidth = maxWidth,
            BackgroundStyle = background,
            BorderStyle = Style.None.WithForeground(Colors.SlateBlue),
        };

    private static DocumentFlowItem CreateRightMessage(FlowDocument content, int maxWidth, Style background)
        => new()
        {
            Content = content,
            Alignment = DocumentFlowAlignment.Right,
            MaxWidth = maxWidth,
            BackgroundStyle = background,
            BorderStyle = Style.None.WithForeground(Colors.MediumPurple),
        };
}
