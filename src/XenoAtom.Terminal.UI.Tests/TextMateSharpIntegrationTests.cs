// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TextMateSharpIntegrationTests
{
    [TestMethod]
    public void CodeEditor_TextMateSyntaxHighlighter_Highlights_CSharp_Keywords()
    {
        const string source = """
            public sealed class Sample
            {
                public string Render() => "ok";
            }
            """;

        var editor = new CodeEditor(source);
        var snapshot = editor.TextDocument.CurrentSnapshot;
        var line = snapshot.GetLine(0);
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });

        var state = highlighter.Build(new CodeEditorSyntaxBuildContext(snapshot, Theme.Default, 0, 0, 0));
        var runs = new List<StyledRun>();
        highlighter.GetLineRuns(
            state,
            new CodeEditorLineSyntaxRequest(snapshot, Theme.Default, 0, line.Start, line.Length, 0, 0, 0),
            runs);

        Assert.IsTrue(runs.Count > 0, "Expected TextMateSharp to produce syntax runs for the C# source line.");

        var keywordStyle = FindStyleCovering(runs, startIndex: 0, length: "public".Length);
        Assert.IsTrue(keywordStyle.TryGetForeground(out var keywordForeground), "Expected TextMateSharp to assign a foreground color to C# keywords.");
        Assert.AreNotEqual(Theme.Default.Foreground?.ToRgb() ?? Color.Default, keywordForeground, "Expected the keyword foreground to differ from the default theme foreground.");
        Assert.AreEqual(
            TextStyle.None,
            keywordStyle.TextStyle & (TextStyle.Bold | TextStyle.Underline | TextStyle.Strikethrough),
            "Expected a plain colored token without unexpected bold/underline/strikethrough decorations.");

        var classNameStyle = FindStyleCovering(runs, startIndex: "public sealed class ".Length, length: "Sample".Length);
        Assert.AreNotEqual(keywordStyle, classNameStyle, "Expected keywords and type identifiers to receive different TextMate styles.");
    }

    [TestMethod]
    public void MarkdownControl_TextMateCodeBlockRenderer_Highlights_Fenced_Code_Blocks()
    {
        var markdown = """
            ```csharp
            public sealed class Sample
            {
                public string Render() => "ok";
            }
            ```
            """;

        var control = new MarkdownControl(markdown)
        {
            Options = MarkdownRenderOptions.Default with
            {
                CodeBlockRenderer = new TextMateMarkdownCodeBlockRenderer(),
                WrapCodeBlocks = false,
            },
        };

        var flow = control.EnumerateVisualsDepthFirst().OfType<DocumentFlow>().FirstOrDefault();
        Assert.IsNotNull(flow);

        var codeBlockVisual = flow.Items[0].Content.GetBlock(0).CreateVisual();
        var buffer = VisualSnapshotRenderer.Render(codeBlockVisual, width: 60, maxHeight: 6, Theme.Default);
        var rowIndex = FindRowContaining(buffer, "public");
        var rowText = SnapshotRow(buffer, rowIndex);
        var keywordIndex = rowText.IndexOf("public", StringComparison.Ordinal);
        Assert.IsTrue(keywordIndex >= 0, $"Expected the fenced code block to render a C# source line. Row: `{rowText}`");

        var keywordStyle = GetCellStyle(buffer, keywordIndex, rowIndex);
        Assert.IsTrue(keywordStyle.TryGetForeground(out var keywordForeground), "Expected the rendered code cell to carry a TextMate foreground color.");
        Assert.AreEqual(Color.Rgb(0x56, 0x9C, 0xD6), keywordForeground);
    }

    [TestMethod]
    public void MarkdownControl_TextMateCodeBlockRenderer_Preserves_Whitespace_In_Code_Blocks()
    {
        var markdown = """
            ```csharp
            public static class Sample
            {
                public static void Main()
                {
                    Console.WriteLine("ok");
                }
            }
            ```
            """;

        var control = new MarkdownControl(markdown)
        {
            Options = MarkdownRenderOptions.Default with
            {
                CodeBlockRenderer = new TextMateMarkdownCodeBlockRenderer(),
                WrapCodeBlocks = false,
            },
        };

        var flow = control.EnumerateVisualsDepthFirst().OfType<DocumentFlow>().FirstOrDefault();
        Assert.IsNotNull(flow);

        var codeBlockVisual = flow.Items[0].Content.GetBlock(0).CreateVisual();
        var buffer = VisualSnapshotRenderer.Render(codeBlockVisual, width: 80, maxHeight: 8, Theme.Default);
        var rowIndex = FindRowContaining(buffer, "public static class Sample");
        var rowText = SnapshotRow(buffer, rowIndex);

        StringAssert.Contains(rowText, "public static class Sample", "Expected spaces between C# keywords to be preserved in the rendered code block.");
    }

    [TestMethod]
    public void MarkdownControl_CodeBlockRenderer_ExtensionPoint_Can_Override_Default_Rendering()
    {
        var renderer = new TestCodeBlockRenderer();
        var control = new MarkdownControl(
            """
            ```txt
            hello
            ```
            """)
        {
            Options = MarkdownRenderOptions.Default with
            {
                CodeBlockRenderer = renderer,
            },
        };

        var flow = control.EnumerateVisualsDepthFirst().OfType<DocumentFlow>().FirstOrDefault();
        Assert.IsNotNull(flow);

        var visual = flow.Items[0].Content.GetBlock(0).CreateVisual();
        var textBlock = visual.EnumerateVisualsDepthFirst().OfType<TextBlock>().FirstOrDefault(static x => string.Equals(x.Text, "custom renderer", StringComparison.Ordinal));
        Assert.IsNotNull(textBlock, "Expected MarkdownControl to delegate fenced code blocks to the configured renderer.");
        Assert.AreEqual("txt", renderer.LastContext.Language);
        Assert.IsTrue(renderer.LastContext.IsFenced);
        Assert.AreEqual("hello", renderer.LastContext.Code);
    }

    private static Style FindStyleCovering(List<StyledRun> runs, int startIndex, int length)
    {
        var endIndex = startIndex + length;
        foreach (var run in runs)
        {
            if (run.Start <= startIndex && run.Start + run.Length >= endIndex)
            {
                return run.Style;
            }
        }

        Assert.Fail($"Could not find a styled run covering range [{startIndex}, {endIndex}).");
        return Style.None;
    }

    private static int FindRowContaining(CellBuffer buffer, string token)
    {
        for (var row = 0; row < buffer.Height; row++)
        {
            if (SnapshotRow(buffer, row).Contains(token, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Could not find a rendered row containing `{token}`.");
        return -1;
    }

    private static Style GetCellStyle(CellBuffer buffer, int x, int y)
        => buffer.UnsafeCells[(y * buffer.Width) + x];

    private static string SnapshotRow(CellBuffer buffer, int y)
    {
        var scalars = buffer.UnsafeScalars;
        var chars = new char[buffer.Width];
        for (var x = 0; x < buffer.Width; x++)
        {
            chars[x] = (char)scalars[(y * buffer.Width) + x];
        }

        return new string(chars);
    }

    private sealed class TestCodeBlockRenderer : IMarkdownCodeBlockRenderer
    {
        public MarkdownCodeBlockRenderContext LastContext { get; private set; }

        public Visual? CreateVisual(in MarkdownCodeBlockRenderContext context)
        {
            LastContext = context;
            return new TextBlock("custom renderer");
        }
    }
}
