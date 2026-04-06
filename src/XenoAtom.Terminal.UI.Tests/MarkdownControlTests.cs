// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;
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
    public void MarkdownControl_Default_Styles_Are_Themed_And_Pleasant()
    {
        var markdown = """
            # Heading One

            Paragraph with **strong** and `inline code`.

            > [!WARNING]
            > Warning content.
            """;

        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);
        var control = new MarkdownControl(markdown).Style(theme);

        // Force a rebuild after the theme has been assigned locally.
        control.Options = control.Options with { WrapCodeBlocks = !control.Options.WrapCodeBlocks };

        var heading = FindParagraphContaining(control, "Heading One");
        var headingStyle = FindStyleForToken(heading, "Heading One");
        AssertStyleForegroundEquals(
            headingStyle,
            ResolveExpectedHeadingColor(theme));
        Assert.IsTrue((headingStyle.TextStyle & (TextStyle.Bold | TextStyle.Underline)) == (TextStyle.Bold | TextStyle.Underline));

        var body = FindParagraphContaining(control, "Paragraph with");
        var strongStyle = FindStyleForToken(body, "strong");
        AssertStyleForegroundEquals(
            strongStyle,
            (theme.Accent ?? theme.Primary ?? theme.Warning ?? Colors.TerminalBrightCyan).ToRgb());
        Assert.IsTrue((strongStyle.TextStyle & TextStyle.Bold) == TextStyle.Bold);

        var inlineCodeStyle = FindStyleForToken(body, "inline code");
        AssertStyleForegroundEquals(inlineCodeStyle, ResolveExpectedInlineCodeForeground(theme));
        var expectedInlineCodeBackground = ResolveExpectedInlineCodeBackground(theme);
        Assert.IsTrue(expectedInlineCodeBackground.GetRelativeLuminance() < 0.15f, "Inline code background should remain subtle on dark themes.");
        AssertStyleBackgroundEquals(inlineCodeStyle, expectedInlineCodeBackground);
    }

    [TestMethod]
    public void MarkdownControl_Default_Styles_Fallback_To_Terminal_Brights_When_Scheme_Is_Missing()
    {
        var markdown = """
            # Heading One

            Paragraph with `inline code`.
            """;

        var theme = new Theme
        {
            Background = Colors.TerminalBlack,
            Foreground = Colors.TerminalWhite,
        };
        var control = new MarkdownControl(markdown).Style(theme);

        // Force a rebuild after the theme has been assigned locally.
        control.Options = control.Options with { WrapCodeBlocks = !control.Options.WrapCodeBlocks };

        var heading = FindParagraphContaining(control, "Heading One");
        var headingStyle = FindStyleForToken(heading, "Heading One");
        AssertStyleForegroundEquals(headingStyle, Colors.TerminalBrightYellow);

        var body = FindParagraphContaining(control, "Paragraph with");
        var inlineCodeStyle = FindStyleForToken(body, "inline code");
        AssertStyleForegroundEquals(inlineCodeStyle, Colors.TerminalBrightRed);
    }

    [TestMethod]
    public void MarkdownControl_Rebuilds_Themed_Default_Styles_When_Theme_Changes()
    {
        var markdown = """
            # Heading One

            > [!CAUTION]
            > Be careful.
            """;

        var firstTheme = Theme.FromScheme(ColorScheme.RootLoopsDark, accent: ThemeAccentColor.Blue);
        var secondTheme = Theme.FromScheme(ColorScheme.RootLoopsDark, accent: ThemeAccentColor.Yellow);
        var control = new MarkdownControl(markdown);
        var root = new VStack(control).Style(firstTheme);

        using var driver = new TerminalAppTestDriver(root, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));
        driver.Tick();

        var headingBefore = FindStyleForToken(FindParagraphContaining(control, "Heading One"), "Heading One");
        AssertStyleForegroundEquals(
            headingBefore,
            ResolveExpectedHeadingColor(firstTheme));

        var cautionBefore = FindAlertGroup(control);
        var cautionBeforeStyle = cautionBefore.GetStyle<GroupStyle>();
        AssertStyleForegroundEquals(
            cautionBeforeStyle.BorderCellStyle.GetValueOrDefault(),
            (firstTheme.Error ?? Colors.IndianRed).ToRgb());

        root.Style(secondTheme);
        driver.Tick();

        var headingAfter = FindStyleForToken(FindParagraphContaining(control, "Heading One"), "Heading One");
        AssertStyleForegroundEquals(
            headingAfter,
            ResolveExpectedHeadingColor(secondTheme));

        var cautionAfter = FindAlertGroup(control);
        var cautionAfterStyle = cautionAfter.GetStyle<GroupStyle>();
        AssertStyleForegroundEquals(
            cautionAfterStyle.BorderCellStyle.GetValueOrDefault(),
            (secondTheme.Error ?? Colors.IndianRed).ToRgb());
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
    public void MarkdownControl_Converts_Absolute_Windows_File_Links_To_FileUris()
    {
        const string path = @"C:\docs\guide.md";
        var control = new MarkdownControl($"See [docs]({path}).");

        var paragraph = GetParagraph(control, 0);
        Assert.AreEqual(1, paragraph.Hyperlinks.Length);
        Assert.AreEqual(CreateExpectedFileUri(path), paragraph.Hyperlinks[0].Uri);
    }

    [TestMethod]
    public void MarkdownControl_Resolves_Relative_File_Links_With_LocalFileRootPath()
    {
        var localRoot = Path.Combine(Path.GetTempPath(), "markdown-local-root");
        var control = new MarkdownControl("See [docs](guide/readme.md).")
        {
            BaseUri = new Uri("https://example.com/docs/"),
            Options = MarkdownRenderOptions.Default with
            {
                LocalFileRootPath = localRoot,
            },
        };

        var paragraph = GetParagraph(control, 0);
        Assert.AreEqual(1, paragraph.Hyperlinks.Length);
        Assert.AreEqual(
            CreateExpectedFileUri(Path.GetFullPath(Path.Combine(localRoot, "guide", "readme.md"))),
            paragraph.Hyperlinks[0].Uri);
    }

    [TestMethod]
    public void MarkdownControl_Keeps_Fragment_Links_Resolvable_With_BaseUri_When_LocalFileRootPath_Is_Set()
    {
        var control = new MarkdownControl("See [section](#intro).")
        {
            BaseUri = new Uri("https://example.com/docs/page.md"),
            Options = MarkdownRenderOptions.Default with
            {
                LocalFileRootPath = Path.Combine(Path.GetTempPath(), "markdown-local-root"),
            },
        };

        var paragraph = GetParagraph(control, 0);
        Assert.AreEqual(1, paragraph.Hyperlinks.Length);
        Assert.AreEqual("https://example.com/docs/page.md#intro", paragraph.Hyperlinks[0].Uri);
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

    [TestMethod]
    public void MarkdownControl_DoesNot_Add_Trailing_Blank_Line_For_Last_Block()
    {
        var control = new MarkdownControl("Hello");
        var content = GetFlow(control).Items[0].Content;

        Assert.AreEqual(1, content.BlockCount);
        Assert.AreEqual(0, content.GetBlock(0).MarginBottom);
    }

    [TestMethod]
    public void MarkdownControl_DoesNotFollowTail_ByDefault()
    {
        var markdown = string.Join("\n\n", Enumerable.Range(0, 80).Select(static i => $"Paragraph {i:00}"));
        var control = new MarkdownControl(markdown);

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 10));
        driver.Tick();

        var flow = GetFlow(control);
        Assert.IsFalse(flow.FollowTail);
        Assert.AreEqual(0, flow.Scroll.OffsetY, "Markdown should open at the top by default.");
    }

    [TestMethod]
    public void MarkdownControl_DefaultSpacing_Is_Compact_Around_Headings()
    {
        var control = new MarkdownControl(
            """
            Intro paragraph.

            ## Section

            Body paragraph.
            """);

        var flow = GetFlow(control);
        var content = flow.Items[0].Content;
        Assert.AreEqual(3, content.BlockCount);

        var firstBlock = content.GetBlock(0);
        var headingBlock = content.GetBlock(1);
        var thirdBlock = content.GetBlock(2);

        Assert.AreEqual(1, firstBlock.MarginBottom, "Paragraph spacing should default to a single blank row.");
        Assert.AreEqual(0, headingBlock.MarginTop, "Heading spacing before should default to compact (no extra blank rows).");
        Assert.AreEqual(1, headingBlock.MarginBottom, "Heading spacing after should default to a single blank row.");
        Assert.AreEqual(0, thirdBlock.MarginBottom, "The final block should not leave a trailing blank line.");
    }

    [TestMethod]
    public void MarkdownControl_Spacing_Can_Be_Configured()
    {
        var control = new MarkdownControl(
            """
            Intro paragraph.

            ## Section

            Body paragraph.
            """)
        {
            Options = MarkdownRenderOptions.Default with
            {
                ParagraphSpacing = 0,
                HeadingSpacingBefore = 2,
                HeadingSpacingAfter = 0,
                BlockSpacing = 2,
            },
        };

        var flow = GetFlow(control);
        var content = flow.Items[0].Content;
        Assert.AreEqual(3, content.BlockCount);

        var firstBlock = content.GetBlock(0);
        var headingBlock = content.GetBlock(1);
        var thirdBlock = content.GetBlock(2);

        Assert.AreEqual(0, firstBlock.MarginBottom);
        Assert.AreEqual(2, headingBlock.MarginTop);
        Assert.AreEqual(0, headingBlock.MarginBottom);
        Assert.AreEqual(0, thirdBlock.MarginBottom);
    }

    [TestMethod]
    public void MarkdownControl_DefaultSpacing_Keeps_List_Items_Compact_But_Separates_Lists()
    {
        var markdown = """
            - Unordered item one
            - Unordered item two
            - Unordered item three

            1. Ordered item one
            2. Ordered item two
            3. Ordered item three
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
        var lines = screen.GetText().Split('\n');

        var unorderedOne = FindLineContaining(lines, "Unordered item one", startIndex: 0);
        var unorderedTwo = FindLineContaining(lines, "Unordered item two", startIndex: unorderedOne + 1);
        var unorderedThree = FindLineContaining(lines, "Unordered item three", startIndex: unorderedTwo + 1);
        Assert.AreEqual(unorderedOne + 1, unorderedTwo, "Unordered list items should render on consecutive rows.");
        Assert.AreEqual(unorderedTwo + 1, unorderedThree, "Unordered list items should render on consecutive rows.");

        var orderedOne = FindLineContaining(lines, "Ordered item one", startIndex: unorderedThree + 1);
        var orderedTwo = FindLineContaining(lines, "Ordered item two", startIndex: orderedOne + 1);
        var orderedThree = FindLineContaining(lines, "Ordered item three", startIndex: orderedTwo + 1);
        Assert.AreEqual(unorderedThree + 2, orderedOne, "Separate top-level lists should be separated by one blank row by default.");
        Assert.AreEqual(orderedOne + 1, orderedTwo, "Ordered list items should render on consecutive rows.");
        Assert.AreEqual(orderedTwo + 1, orderedThree, "Ordered list items should render on consecutive rows.");
    }

    [TestMethod]
    public void MarkdownControl_NestedQuote_IsCompact_ByDefault()
    {
        var markdown = """
            > A simple quote with **strong text** and a [quoted link](https://example.com/quoted).
            >
            > > Nested quote with `inline code`.
            """;

        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(90, 10));
        driver.Tick();

        var screen = new AnsiTestScreen(90, 10);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        var firstQuoteLine = FindLineContaining(lines, "A simple quote with", 0);
        var nestedQuoteLine = FindLineContaining(lines, "Nested quote with", firstQuoteLine + 1);
        Assert.AreEqual(firstQuoteLine + 1, nestedQuoteLine, "Nested quote should render immediately after the preceding quote line by default.");
    }

    [TestMethod]
    public void MarkdownControl_DefaultSpacing_Adds_Blank_Lines_After_TopLevel_Quote_And_List()
    {
        var markdown = """
            > A simple quote line.
            > > Nested quote line.
            - Unordered item one
            - Unordered item two

            Tail paragraph.
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
        var lines = screen.GetText().Split('\n');

        var nestedQuoteLine = FindLineContaining(lines, "Nested quote line.", 0);
        var firstListLine = FindLineContaining(lines, "Unordered item one", nestedQuoteLine + 1);
        var secondListLine = FindLineContaining(lines, "Unordered item two", firstListLine + 1);
        var tailLine = FindLineContaining(lines, "Tail paragraph.", secondListLine + 1);

        Assert.AreEqual(nestedQuoteLine + 2, firstListLine, "A top-level quote should be followed by one blank row.");
        Assert.AreEqual(firstListLine + 1, secondListLine, "List items should remain compact.");
        Assert.AreEqual(secondListLine + 2, tailLine, "A top-level list should be followed by one blank row.");
    }

    [TestMethod]
    public void MarkdownControl_Quote_And_List_Spacing_Can_Be_Configured()
    {
        var markdown = """
            > A simple quote line.
            > > Nested quote line.
            - Unordered item one
            - Unordered item two

            Tail paragraph.
            """;

        var control = new MarkdownControl(markdown)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
            Options = MarkdownRenderOptions.Default with
            {
                QuoteSpacingAfter = 0,
                ListSpacingAfter = 0,
            },
        };

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));
        driver.Tick();

        var screen = new AnsiTestScreen(80, 12);
        screen.Apply(driver.Backend.GetOutText());
        var lines = screen.GetText().Split('\n');

        var nestedQuoteLine = FindLineContaining(lines, "Nested quote line.", 0);
        var firstListLine = FindLineContaining(lines, "Unordered item one", nestedQuoteLine + 1);
        var secondListLine = FindLineContaining(lines, "Unordered item two", firstListLine + 1);
        var tailLine = FindLineContaining(lines, "Tail paragraph.", secondListLine + 1);

        Assert.AreEqual(nestedQuoteLine + 1, firstListLine);
        Assert.AreEqual(firstListLine + 1, secondListLine);
        Assert.AreEqual(secondListLine + 1, tailLine);
    }

    [TestMethod]
    public void MarkdownControl_MouseWheel_Scrolls_DocumentFlow_Content()
    {
        var markdown = string.Join("\n\n", Enumerable.Range(0, 40).Select(static i => $"Paragraph {i:00}"));
        var control = new MarkdownControl(markdown);

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));
        driver.Tick();

        var flow = GetFlow(control);
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Wheel,
            Button = TerminalMouseButton.Wheel,
            WheelDelta = -1,
            X = 2,
            Y = 2,
        });
        driver.Tick();

        Assert.IsTrue(flow.Scroll.OffsetY > 0, "Mouse wheel over markdown content should scroll.");
    }

    [TestMethod]
    public void MarkdownControl_Scroll_Moves_Rendered_Text()
    {
        var markdown = string.Join("\n\n", Enumerable.Range(0, 80).Select(static i => $"Paragraph {i:00}"));
        var control = new MarkdownControl(markdown);

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 10));
        driver.Tick();

        var before = new AnsiTestScreen(80, 10);
        before.Apply(driver.Backend.GetOutText());
        var textBefore = before.GetText();
        StringAssert.Contains(textBefore, "Paragraph 00");

        for (var i = 0; i < 25; i++)
        {
            driver.Backend.PushEvent(new TerminalMouseEvent
            {
                Kind = TerminalMouseKind.Wheel,
                Button = TerminalMouseButton.Wheel,
                WheelDelta = -1,
                X = 2,
                Y = 2,
            });
            driver.Tick();
        }

        var flow = GetFlow(control);
        Assert.IsTrue(flow.Scroll.OffsetY > 0, "Expected markdown flow offset to increase after scrolling.");

        var after = new AnsiTestScreen(80, 10);
        after.Apply(driver.Backend.GetOutText());
        var textAfter = after.GetText();
        Assert.IsFalse(
            textAfter.Contains("Paragraph 00", StringComparison.Ordinal),
            "Expected the initial paragraph to scroll out of the viewport.");
    }

    [TestMethod]
    public void MarkdownControl_ArrowKeys_Scroll_After_Clicking_Content()
    {
        var markdown = string.Join("\n\n", Enumerable.Range(0, 40).Select(static i => $"Paragraph {i:00}"));
        var control = new MarkdownControl(markdown);

        using var driver = new TerminalAppTestDriver(control, TerminalHostKind.Fullscreen, new TerminalSize(80, 12));
        driver.Tick();

        var flow = GetFlow(control);
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = 2,
            Y = 2,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = 2,
            Y = 2,
        });
        driver.Backend.PushEvent(new TerminalKeyEvent { Key = TerminalKey.Down });
        driver.Tick();

        Assert.IsTrue(flow.Scroll.OffsetY > 0, "Down arrow should scroll markdown flow when content is focused.");
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

    private static Paragraph FindParagraphContaining(MarkdownControl control, string token)
    {
        var flow = GetFlow(control);
        var content = flow.Items[0].Content;
        for (var index = 0; index < content.BlockCount; index++)
        {
            var visual = content.GetBlock(index).CreateVisual();
            foreach (var paragraph in visual.EnumerateVisualsDepthFirst().OfType<Paragraph>())
            {
                if (paragraph.Text?.Contains(token, StringComparison.Ordinal) == true)
                {
                    return paragraph;
                }
            }
        }

        Assert.Fail($"Could not find paragraph containing token `{token}`.");
        return null!;
    }

    private static int FindLineContaining(string[] lines, string token, int startIndex)
    {
        for (var index = Math.Max(0, startIndex); index < lines.Length; index++)
        {
            if (lines[index].Contains(token, StringComparison.Ordinal))
            {
                return index;
            }
        }

        Assert.Fail($"Could not find line containing token `{token}` starting at index {startIndex}.");
        return -1;
    }

    private static Group FindAlertGroup(MarkdownControl control)
    {
        var flow = GetFlow(control);
        var content = flow.Items[0].Content;
        for (var index = 0; index < content.BlockCount; index++)
        {
            var visual = content.GetBlock(index).CreateVisual();
            var group = visual.EnumerateVisualsDepthFirst().OfType<Group>().FirstOrDefault();
            if (group is not null)
            {
                return group;
            }
        }

        Assert.Fail("Could not find an alert group visual.");
        return null!;
    }

    private static Style FindStyleForToken(Paragraph paragraph, string token)
    {
        var text = paragraph.Text ?? string.Empty;
        var start = text.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Could not find token `{token}` in paragraph text.");
        var end = start + token.Length;

        foreach (var run in paragraph.Runs)
        {
            if (run.Start <= start && run.Start + run.Length >= end)
            {
                return run.Style;
            }
        }

        Assert.Fail($"Could not find a style run covering token `{token}`.");
        return Style.None;
    }

    private static void AssertStyleForegroundEquals(Style style, Color expected)
    {
        Assert.IsTrue(style.TryGetForeground(out var actual), "Expected style to define a foreground.");
        Assert.AreEqual(expected, actual);
    }

    private static void AssertStyleBackgroundEquals(Style style, Color expected)
    {
        Assert.IsTrue(style.TryGetBackground(out var actual), "Expected style to define a background.");
        Assert.AreEqual(expected, actual);
    }

    private static Color ResolveExpectedInlineCodeBackground(Theme theme)
    {
        var candidate = (
            theme.InputFill ??
            theme.SurfaceAlt ??
            theme.InputFillFocused ??
            theme.Selection ??
            Colors.TerminalBrightBlack);
        var baseBackground = ResolveColorAgainstThemeBackground(candidate, theme.Background ?? Color.Default);
        var adjusted = IsLightTheme(theme)
            ? baseBackground.Darken(0.04f)
            : baseBackground.Lighten(0.04f);
        return adjusted.WithAlpha(0x66);
    }

    private static Color ResolveExpectedHeadingColor(Theme theme)
        => theme.Scheme?.BrightYellow ?? Colors.TerminalBrightYellow;

    private static Color ResolveExpectedInlineCodeForeground(Theme theme)
        => theme.Scheme?.BrightRed ?? Colors.TerminalBrightRed;

    private static Color ResolveColorAgainstThemeBackground(Color color, Color themeBackground)
    {
        color = color.Kind is ColorKind.Basic16 or ColorKind.Indexed256 ? color.ToRgb() : color;
        if (color.Kind == ColorKind.Rgb)
        {
            return color;
        }

        if (color.Kind != ColorKind.RgbA)
        {
            return Colors.TerminalBrightBlack.ToRgb();
        }

        if (themeBackground.Kind is ColorKind.Basic16 or ColorKind.Indexed256)
        {
            themeBackground = themeBackground.ToRgb();
        }
        else if (themeBackground.Kind == ColorKind.RgbA)
        {
            themeBackground = Color.Rgb(themeBackground.R, themeBackground.G, themeBackground.B);
        }

        if (themeBackground.Kind != ColorKind.Rgb)
        {
            return Color.Rgb(color.R, color.G, color.B);
        }

        if (color.A == 0)
        {
            return themeBackground;
        }

        if (color.A >= byte.MaxValue)
        {
            return Color.Rgb(color.R, color.G, color.B);
        }

        var alpha = color.A / 255f;
        return themeBackground.Mix(Color.Rgb(color.R, color.G, color.B), alpha, ColorMixSpace.LinearRgb);
    }

    private static bool IsLightTheme(Theme theme)
    {
        var background = theme.Background?.ToRgb() ?? Color.Default;
        var foreground = theme.Foreground?.ToRgb() ?? Color.Default;
        if (background.Kind == ColorKind.Default || foreground.Kind == ColorKind.Default)
        {
            return false;
        }

        var backgroundLuminance = background.GetRelativeLuminance();
        var foregroundLuminance = foreground.GetRelativeLuminance();
        return backgroundLuminance > foregroundLuminance && backgroundLuminance >= 0.55f;
    }

    private static string CreateExpectedFileUri(string path)
    {
        return new UriBuilder(Uri.UriSchemeFile, string.Empty, -1, path).Uri.AbsoluteUri;
    }
}
