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
        var flow = new DocumentFlow()
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch)
            .ItemPadding(new Thickness(1))
            .ItemSpacing(1)
            .MaxCapacity(200);

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

        var appendCounter = new State<int>(1);
        var appendButton = new Button("Append message")
            .Click(() =>
            {
                var index = appendCounter.Value;
                appendCounter.Value = index + 1;
                flow.Items.Add(CreateRightMessage(
                    new FlowDocument().AddParagraph($"Appended conversation item #{index}."),
                    maxWidth: 44,
                    background: Style.None.WithBackground(Colors.Purple)));
                flow.ScrollToTail();
            });

        return new VStack(
                DemoUi.Hint("DocumentFlow composes existing controls and keeps scrolling smooth with block virtualization."),
                appendButton,
                flow)
            .Spacing(1);
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
