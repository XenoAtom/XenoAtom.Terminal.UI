// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Extensions.Markdown.Styling;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkdownControlTests
{
    [TestMethod]
    public void MarkdownControl_Renders_CommonMark_Features()
    {
        var markdown = """
            # Heading One

            Paragraph with **strong**, *emphasis*, and `inline code`.

            > Quoted line

            - First bullet
            1. Ordered one

            ---

            ```csharp
            Console.WriteLine("Hello markdown");
            ```
            """;

        var control = new MarkdownControl(markdown);
        var flow = GetFlow(control);
        Assert.AreEqual(1, flow.Items.Count);
        Assert.IsTrue(flow.Items[0].Content.BlockCount > 3, "Expected markdown parsing to produce multiple blocks.");

        var content = flow.Items[0].Content;
        var paragraphVisuals = new List<Paragraph>();
        var hasRule = false;
        var hasCode = false;
        for (var index = 0; index < content.BlockCount; index++)
        {
            var visual = content.GetBlock(index).CreateVisual();
            foreach (var child in visual.EnumerateVisualsDepthFirst())
            {
                if (child is Paragraph paragraph)
                {
                    paragraphVisuals.Add(paragraph);
                }
                else if (child is Rule)
                {
                    hasRule = true;
                }
                else if (child is LogControl)
                {
                    hasCode = true;
                }
            }
        }

        Assert.IsTrue(paragraphVisuals.Any(static p => string.Equals(p.Text, "Heading One", StringComparison.Ordinal)));
        Assert.IsTrue(paragraphVisuals.Any(static p => p.Text?.Contains("strong", StringComparison.Ordinal) == true));
        Assert.IsTrue(paragraphVisuals.Any(static p => p.Text?.Contains("emphasis", StringComparison.Ordinal) == true));
        Assert.IsTrue(paragraphVisuals.Any(static p => p.Text?.Contains("Quoted line", StringComparison.Ordinal) == true && p.LinePrefix is not null));
        Assert.IsTrue(paragraphVisuals.Any(static p => p.Text?.Contains("First bullet", StringComparison.Ordinal) == true && p.LinePrefix is not null));
        Assert.IsTrue(paragraphVisuals.Any(static p => p.Text?.Contains("Ordered one", StringComparison.Ordinal) == true && p.LinePrefix is not null));
        Assert.IsTrue(hasRule);
        Assert.IsTrue(hasCode);
    }

    [TestMethod]
    public void MarkdownControl_Renders_Table_Extension()
    {
        var markdown = """
            | Name  | Value |
            |:------|------:|
            | Alpha |    42 |
            | Beta  |    99 |
            """;

        var control = new MarkdownControl(markdown)
        {
            Options = MarkdownRenderOptions.Default with { TableStyle = TableStyle.RoundedGrid },
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(60, 12));
        driver.Tick();

        var screen = new AnsiTestScreen(60, 12);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Name");
        StringAssert.Contains(rendered, "Value");
        StringAssert.Contains(rendered, "Alpha");
        StringAssert.Contains(rendered, "99");
    }

    [TestMethod]
    public void MarkdownControl_Table_UsesMarkdownColumns_AndDoesNotStretch()
    {
        var markdown = """
            | Feature | Status | Notes |
            |:--------|:------:|------:|
            | Headings | Done | 100 |
            | Alerts | Done | 85 |
            """;

        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(100, 20));
        driver.Tick();

        var table = control.EnumerateVisualsDepthFirst().OfType<Table>().FirstOrDefault();
        Assert.IsNotNull(table);

        Assert.AreEqual(3, table.HeaderCells.Count);
        Assert.AreEqual(2, table.RowCells.Count);
        Assert.AreEqual(3, table.RowCells[0].Count);
        Assert.AreEqual(3, table.RowCells[1].Count);
        Assert.IsTrue(table.Bounds.Width < 100, $"Expected markdown table to keep natural width. Actual width: {table.Bounds.Width}");
    }

    [TestMethod]
    public void MarkdownControl_Renders_Alert_Extension()
    {
        var markdown = """
            > [!NOTE]
            > This is an alert body.
            """;

        var control = new MarkdownControl(markdown)
        {
            RenderStyle = MarkdownStyle.Default with
            {
                NoteAlert = MarkdownAlertStyle.Default with
                {
                    BorderStyle = Style.None | TextStyle.Bold,
                },
            },
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(70, 12));
        driver.Tick();

        var screen = new AnsiTestScreen(70, 12);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "NOTE");
        StringAssert.Contains(rendered, "alert body");
    }

    [TestMethod]
    public void MarkdownControl_Registers_Hyperlinks()
    {
        var markdown = "See [project](https://github.com/XenoAtom/XenoAtom.Terminal.UI).";
        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
        };

        var buffer = Rendering.VisualSnapshotRenderer.Render(control, width: 80, maxHeight: 6, Theme.Default);
        var hyperlinks = buffer.UnsafeHyperlinks;

        var foundLinkCell = false;
        for (var index = 0; index < hyperlinks.Length; index++)
        {
            if (hyperlinks[index] != 0ul)
            {
                foundLinkCell = true;
                break;
            }
        }

        Assert.IsTrue(foundLinkCell, "Expected at least one rendered cell to carry hyperlink metadata.");
    }

    [TestMethod]
    public void MarkdownControl_Rebuilds_When_Markdown_Changes()
    {
        var control = new MarkdownControl("# Before");
        var flow = GetFlow(control);
        Assert.AreEqual(1, flow.Items.Count);
        Assert.IsTrue(flow.Items[0].Content.BlockCount > 0);
        Assert.AreEqual("Before", ((Paragraph)flow.Items[0].Content.GetBlock(0).CreateVisual()).Text);

        control.Markdown = "# After";
        Assert.AreEqual("After", ((Paragraph)flow.Items[0].Content.GetBlock(0).CreateVisual()).Text);
    }

    [TestMethod]
    public void MarkdownControl_Resolves_Relative_Links_With_BaseUri()
    {
        var control = new MarkdownControl("See [docs](guide/readme.md).")
        {
            BaseUri = new Uri("https://example.com/docs/"),
        };

        var paragraph = GetParagraph(control, 0);
        Assert.AreEqual(1, paragraph.Hyperlinks.Length);
        Assert.AreEqual("https://example.com/docs/guide/readme.md", paragraph.Hyperlinks[0].Uri);
    }

    [TestMethod]
    public void MarkdownControl_Renders_Images_As_Link_Placeholders()
    {
        var control = new MarkdownControl("![Diagram](https://example.com/image.png)");

        var paragraph = GetParagraph(control, 0);
        Assert.AreEqual("[image: Diagram]", paragraph.Text);
        Assert.AreEqual(1, paragraph.Hyperlinks.Length);
        Assert.AreEqual("https://example.com/image.png", paragraph.Hyperlinks[0].Uri);
    }

    [TestMethod]
    public void MarkdownControl_Renders_Paragraph_Text_On_First_Frame()
    {
        var markdown = """
            # Title

            First paragraph line.

            Second paragraph line.
            """;

        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));
        driver.Tick();

        var screen = new AnsiTestScreen(80, 12);
        screen.Apply(driver.Backend.GetOutText());
        var rendered = screen.GetText();

        StringAssert.Contains(rendered, "Title");
        StringAssert.Contains(rendered, "First paragraph line.");
        StringAssert.Contains(rendered, "Second paragraph line.");
    }

    private static DocumentFlow GetFlow(MarkdownControl control)
    {
        var flow = control.EnumerateVisualsDepthFirst().OfType<DocumentFlow>().FirstOrDefault();
        Assert.IsNotNull(flow);
        return flow;
    }

    private static Paragraph GetParagraph(MarkdownControl control, int blockIndex)
    {
        var flow = GetFlow(control);
        var visual = flow.Items[0].Content.GetBlock(blockIndex).CreateVisual();
        Assert.IsInstanceOfType<Paragraph>(visual);
        return (Paragraph)visual;
    }
}
