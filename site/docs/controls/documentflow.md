---
title: DocumentFlow
---

# DocumentFlow

`DocumentFlow` is a virtualized, scrollable feed of document items.
Each item is composed from blocks (paragraphs, tables, code blocks, or any visual-backed block).

![DocumentFlow](../../img/controls/documentflow.svg){.terminal}

## Basic usage

```csharp
var flow = new DocumentFlow();

flow.Items.Add(new DocumentFlowItem
{
    Content = new FlowDocument()
        .AddParagraph("Hello from DocumentFlow"),
    Alignment = DocumentFlowAlignment.Left,
    MaxWidthPercent = 60,
});
```

`MaxWidthPercent` is evaluated from the available viewport width.
If both `MaxWidth` and `MaxWidthPercent` are provided, the most restrictive width is used.

## Conversation-style alignment

```csharp
flow.Items.Add(new DocumentFlowItem
{
    Content = new FlowDocument().AddParagraph("Left bubble"),
    Alignment = DocumentFlowAlignment.Left,
    MaxWidthPercent = 65,
});

flow.Items.Add(new DocumentFlowItem
{
    Content = new FlowDocument().AddParagraph("Right bubble"),
    Alignment = DocumentFlowAlignment.Right,
    MaxWidthPercent = 65,
});
```

## Mixed block content

```csharp
var table = new Table()
    .Headers("Key", "Value")
    .AddRow("Mode", "Fast");

var log = new LogControl().MaxHeight(4);
log.AppendLine("code: Console.WriteLine(\"Hello\")");

var document = new FlowDocument()
    .AddParagraph("Mixed content item")
    .Add(table)
    .Add(log);
```

## Follow-tail

`DocumentFlow` follows the tail by default for append-heavy feeds.

```csharp
flow.ScrollToTail();
```

Use `MaxCapacity` to keep memory bounded for long-running sessions.

## Related

- [MarkdownControl](markdowncontrol.md)
- [Paragraph](paragraph.md)
- [Table](table.md)
- [LogControl](logcontrol.md)
- [DocumentFlow Specs](../specs/controls/documentflow.md)
