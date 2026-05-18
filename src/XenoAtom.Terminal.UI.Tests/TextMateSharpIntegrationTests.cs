// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Hosting;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

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
    public void CodeEditor_TextMateSyntaxHighlighter_Loads_Bundled_Toml_Grammar()
    {
        const string source = """
            title = "Controls Demo"
            launched_at = 2026-05-18 09:30Z

            [ui."theme.settings"]
            accent = "\e[38;2;76;194;255m"
            features = ["code-editor", "textmate", "toml"]
            metadata = {
              enabled = true,
              ratio = +1_024.5,
              fingerprint = 0xDEAD_BEEF,
              times = [07:32, 1979-05-27T07:32-07:00],
            }
            """;

        var editor = new CodeEditor(source);
        var snapshot = editor.TextDocument.CurrentSnapshot;
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "toml",
            });

        var state = highlighter.Build(new CodeEditorSyntaxBuildContext(snapshot, Theme.Default, 0, 0, 0));
        var runs = new List<StyledRun>();
        var line = snapshot.GetLine(4);
        highlighter.GetLineRuns(
            state,
            new CodeEditorLineSyntaxRequest(snapshot, Theme.Default, 4, line.Start, line.Length, 0, 0, 0),
            runs);

        Assert.IsTrue(runs.Count > 0, "Expected the bundled TOML grammar to produce syntax runs.");

        var lineText = "accent = \"\\e[38;2;76;194;255m\"";
        var escapeIndex = lineText.IndexOf("\\e", StringComparison.Ordinal);
        Assert.IsTrue(escapeIndex >= 0, "Expected the TOML test line to contain a TOML 1.1 escape sequence.");
        var escapeStyle = FindStyleCovering(runs, escapeIndex, "\\e".Length);
        Assert.IsTrue(escapeStyle.TryGetForeground(out _), "Expected the TOML 1.1 escape sequence to receive a token foreground.");
    }

    [TestMethod]
    public void CodeEditor_TextMateSyntaxHighlighter_Resolves_Toml_By_FileName()
    {
        const string source = "name = \"demo\"";

        var editor = new CodeEditor(source);
        var snapshot = editor.TextDocument.CurrentSnapshot;
        var line = snapshot.GetLine(0);
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                FileName = "config.toml",
            });

        var state = highlighter.Build(new CodeEditorSyntaxBuildContext(snapshot, Theme.Default, 0, 0, 0));
        var runs = new List<StyledRun>();
        highlighter.GetLineRuns(
            state,
            new CodeEditorLineSyntaxRequest(snapshot, Theme.Default, 0, line.Start, line.Length, 0, 0, 0),
            runs);

        Assert.IsTrue(runs.Count > 0, "Expected .toml files to resolve to the bundled TOML grammar.");
    }

    [TestMethod]
    public void CodeEditor_TextMateSyntaxHighlighter_Does_Not_Override_Editor_Default_Colors_For_Punctuation()
    {
        const string source = "using System.Collections.Generic;";

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

        var punctuationIndex = source.IndexOf(';');
        Assert.IsTrue(punctuationIndex >= 0, "Expected the test source to contain a semicolon.");
        Assert.IsFalse(
            runs.Any(run => run.Start <= punctuationIndex && run.Start + run.Length > punctuationIndex),
            "Expected punctuation that only carries the TextMate default colors to keep the host CodeEditor foreground/background instead of receiving an explicit token style.");
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

    [TestMethod]
    public void TextMateSyntaxHighlighter_Builds_And_Updates_Lazily_For_Large_Documents()
    {
        var source = string.Join('\n', Enumerable.Range(0, 20_000).Select(i => $"public sealed class C{i:000000} {{ }}"));
        var document = new TextDocument(source);
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });

        var initialSnapshot = document.CurrentSnapshot;
        var state = highlighter.Build(new CodeEditorSyntaxBuildContext(initialSnapshot, Theme.Default, 0, 0, 0));

        Assert.AreEqual(0, highlighter.GetTokenizeLineCallCountForTests(), "Expected the initial syntax state build to defer TextMate tokenization until lines are requested.");

        var initialRuns = new List<StyledRun>();
        var initialLine = initialSnapshot.GetLine(7);
        highlighter.GetLineRuns(
            state,
            new CodeEditorLineSyntaxRequest(initialSnapshot, Theme.Default, 7, initialLine.Start, initialLine.Length, 0, 0, 0),
            initialRuns);

        Assert.IsTrue(initialRuns.Count > 0, "Expected TextMate to highlight the requested visible prefix.");
        Assert.AreEqual(8, highlighter.GetTokenizeLineCallCountForTests(), "Expected requesting line 7 to tokenize only the prefix needed to reach that line.");

        TextDocumentChangedEventArgs? change = null;
        document.Changed += (_, args) => change = args;
        document.Insert(0, "x");

        Assert.IsNotNull(change, "Expected the document edit to raise a change event.");
        var edit = change!;

        var updatedSnapshot = document.CurrentSnapshot;
        var updatedState = highlighter.Update(
            state,
            new CodeEditorSyntaxUpdateContext(
                updatedSnapshot,
                Theme.Default,
                edit,
                updatedSnapshot.GetLineIndexFromPosition(edit.Position),
                updatedSnapshot.GetLineIndexFromPosition(Math.Min(updatedSnapshot.Length, edit.Position + edit.InsertedLength)),
                0,
                0,
                0));

        var afterUpdateTokenizeCount = highlighter.GetTokenizeLineCallCountForTests();
        Assert.IsLessThanOrEqualTo(
            8 + highlighter.GetCheckpointLineIntervalForTests(),
            afterUpdateTokenizeCount,
            "Expected the incremental update step to limit retokenization to a small prefix while rebuilding sparse checkpoint state.");

        var updatedRuns = new List<StyledRun>();
        var updatedLine = updatedSnapshot.GetLine(7);
        highlighter.GetLineRuns(
            updatedState,
            new CodeEditorLineSyntaxRequest(updatedSnapshot, Theme.Default, 7, updatedLine.Start, updatedLine.Length, 0, 0, 0),
            updatedRuns);

        Assert.IsTrue(updatedRuns.Count > 0, "Expected TextMate to re-highlight the visible prefix after editing the start of the document.");
        Assert.IsLessThanOrEqualTo(
            afterUpdateTokenizeCount + 8,
            highlighter.GetTokenizeLineCallCountForTests(),
            "Expected re-highlighting after a start-of-document edit to retokenize only the visible prefix instead of the entire file.");
    }

    [TestMethod]
    public async Task TextMateSyntaxHighlighter_Async_Update_Keeps_Far_Line_Requests_NonBlocking()
    {
        const int farLineIndex = 4096;
        var source = string.Join('\n', Enumerable.Range(0, 6_000).Select(i => $"public sealed class C{i:000000} {{ }}"));
        var document = new TextDocument(source);
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });

        var initialSnapshot = document.CurrentSnapshot;
        var initialState = await highlighter.BuildAsync(new CodeEditorSyntaxBuildContext(initialSnapshot, Theme.Default, 0, 0, 0));
        Assert.IsFalse(initialState.IsComplete, "Expected async TextMate builds to produce a partial state first so the editor can stay responsive.");
        Assert.AreEqual(0, highlighter.GetCompletedLineCountForTests(initialState), "Expected the initial async build to apply an immediate partial state before any background chunk runs.");

        TextDocumentChangedEventArgs? change = null;
        document.Changed += (_, args) => change = args;
        document.Insert(0, "x");

        Assert.IsNotNull(change, "Expected the document edit to raise a change event.");
        var edit = change!;
        var snapshotAfterEdit = document.CurrentSnapshot;
        var updatedState = await highlighter.UpdateAsync(
            initialState,
            new CodeEditorSyntaxUpdateContext(
                snapshotAfterEdit,
                Theme.Default,
                edit,
                snapshotAfterEdit.GetLineIndexFromPosition(edit.Position),
                snapshotAfterEdit.GetLineIndexFromPosition(Math.Min(snapshotAfterEdit.Length, edit.Position + edit.InsertedLength)),
                0,
                0,
                0));

        var afterUpdateTokenizeCount = highlighter.GetTokenizeLineCallCountForTests();
        Assert.IsFalse(updatedState.IsComplete, "Expected async updates after an edit to remain progressive.");
        Assert.IsLessThanOrEqualTo(
            1,
            afterUpdateTokenizeCount,
            "Expected the immediate async edit refresh to avoid eagerly retokenizing the document before the editor can render again.");

        var updatedRuns = new List<StyledRun>();
        var updatedFarLine = snapshotAfterEdit.GetLine(farLineIndex);
        highlighter.GetLineRuns(
            updatedState,
            new CodeEditorLineSyntaxRequest(snapshotAfterEdit, Theme.Default, farLineIndex, updatedFarLine.Start, updatedFarLine.Length, 0, 0, 0),
            updatedRuns);

        Assert.AreEqual(0, updatedRuns.Count, "Expected a far-away line request on an async partial state not to block the caller by tokenizing on the UI thread.");
        Assert.AreEqual(
            afterUpdateTokenizeCount,
            highlighter.GetTokenizeLineCallCountForTests(),
            "Expected requesting a far-away line from an async partial state not to trigger synchronous speculative tokenization.");
    }

    [TestMethod]
    public void CodeEditor_TextMateSyntaxHighlighter_Allows_Far_Scroll_During_Background_Rehighlight()
    {
        var source = string.Join('\n', Enumerable.Range(0, 6_000).Select(i => $"public sealed class C{i:000000} {{ }}"));
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });
        var editor = new CodeEditor(source)
        {
            MinHeight = 8,
            MaxHeight = 8,
            SyntaxHighlighter = highlighter,
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(48, 10));
        driver.Tick();
        driver.App.Focus(editor);

        driver.Backend.PushEvent(new TerminalTextEvent { Text = "x" });
        driver.TickUntil(() => editor.Text is not null && editor.Text.StartsWith("x", StringComparison.Ordinal));

        driver.App.Post(() => editor.Scroll.SetOffset(0, 5_000));
        driver.Tick();

        Assert.AreEqual(5_000, editor.Scroll.OffsetY, "Expected far scrolling to remain responsive while TextMate is still rebuilding syntax state in the background.");
        Assert.IsTrue(
            editor.GetCachedHighlightLineCountForTests() <= editor.Scroll.ViewportHeight + 1,
            "Expected deep scrolling to keep the visible highlight cache bounded to the viewport while asynchronous highlighting catches up.");
    }

    [TestMethod]
    public void CodeEditor_TextMateSyntaxHighlighter_Live_Mode_Colors_Visible_Text()
    {
        const string source = """
            public sealed class Sample
            {
                public string Render() => "ok";
            }
            """;

        var editor = new CodeEditor(source)
        {
            MinHeight = 6,
            MaxHeight = 6,
            SyntaxHighlighter = new TextMateCodeEditorSyntaxHighlighter(
                new TextMateCodeEditorOptions
                {
                    LanguageId = "csharp",
                }),
        };

        using var driver = new TerminalAppTestDriver(new VStack { editor }, TerminalHostKind.Fullscreen, new TerminalSize(60, 8));
        driver.Tick();
        driver.Tick();

        var runs = editor.GetHighlightRunsForTests(0);
        Assert.IsNotNull(
            runs,
            $"Expected the live TextMate highlighter to populate visible-line syntax runs. stateVersion={editor.GetSyntaxStateSnapshotVersionForTests()}, cachedLines={editor.GetCachedHighlightLineCountForTests()}, visible=[{string.Join(',', editor.GetVisibleLogicalLineIndicesForTests())}]");
        Assert.IsTrue(runs.Length > 0, "Expected the live TextMate highlighter to produce non-empty syntax runs for the first visible line.");

        var keywordRun = runs.FirstOrDefault(run => run.Start == 0 && run.Length >= "public".Length);
        Assert.AreNotEqual(default, keywordRun, "Expected the `public` keyword to be covered by a syntax-highlight run.");
        Assert.IsTrue(keywordRun.Style.TryGetForeground(out var foreground), "Expected the syntax-highlight run to carry a foreground color.");
        Assert.AreNotEqual(Theme.Default.Foreground?.ToRgb() ?? Color.Default, foreground, "Expected live TextMate highlighting to color the keyword differently from the default editor foreground.");
    }

    public async Task TextMateSyntaxHighlighter_PrepareVisibleRangeAsync_Populates_Far_Line_Without_Blocking_GetLineRuns()
    {
        const int farLineIndex = 4096;
        var source = string.Join('\n', Enumerable.Range(0, 6_000).Select(i => $"public sealed class C{i:000000} {{ }}"));
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });

        var document = new TextDocument(source);
        var snapshot = document.CurrentSnapshot;
        var partialState = await highlighter.BuildAsync(new CodeEditorSyntaxBuildContext(snapshot, Theme.Default, 0, 0, 0));

        var initialFarRuns = new List<StyledRun>();
        var farLine = snapshot.GetLine(farLineIndex);
        highlighter.GetLineRuns(
            partialState,
            new CodeEditorLineSyntaxRequest(snapshot, Theme.Default, farLineIndex, farLine.Start, farLine.Length, 0, 0, 0),
            initialFarRuns);
        Assert.AreEqual(0, initialFarRuns.Count, "Expected a far line request on a fresh async partial state not to block by tokenizing synchronously.");

        var afterInitialRequests = highlighter.GetTokenizeLineCallCountForTests();
        var preparedState = await ((IAsyncCodeEditorSyntaxHighlighter)highlighter).PrepareVisibleRangeAsync(
            partialState,
            new CodeEditorSyntaxVisibleRangeContext(snapshot, Theme.Default, farLineIndex, farLineIndex + 8, 0, 0, 0));

        var preparedRuns = new List<StyledRun>();
        highlighter.GetLineRuns(
            preparedState,
            new CodeEditorLineSyntaxRequest(snapshot, Theme.Default, farLineIndex, farLine.Start, farLine.Length, 0, 0, 0),
            preparedRuns);

        Assert.IsTrue(preparedRuns.Count > 0, "Expected asynchronous visible-range preparation to provide syntax highlighting for the far visible line.");
        Assert.IsLessThanOrEqualTo(
            afterInitialRequests + 160,
            highlighter.GetTokenizeLineCallCountForTests(),
            "Expected visible-range preparation to tokenize only a bounded local window.");
    }

    [TestMethod]
    public async Task TextMateSyntaxHighlighter_Async_Update_Reuses_Previous_Tokens_Immediately_After_IntraLine_Edit()
    {
        var source = string.Join('\n', Enumerable.Range(0, 128).Select(i => $"public sealed class C{i:000000} {{ }}"));
        var document = new TextDocument(source);
        var highlighter = new TextMateCodeEditorSyntaxHighlighter(
            new TextMateCodeEditorOptions
            {
                LanguageId = "csharp",
            });

        var initialSnapshot = document.CurrentSnapshot;
        var exactState = highlighter.Build(new CodeEditorSyntaxBuildContext(initialSnapshot, Theme.Default, 0, 0, 0));

        var exactRuns = new List<StyledRun>();
        var exactChangedLine = initialSnapshot.GetLine(0);
        highlighter.GetLineRuns(
            exactState,
            new CodeEditorLineSyntaxRequest(initialSnapshot, Theme.Default, 0, exactChangedLine.Start, exactChangedLine.Length, 0, 0, 0),
            exactRuns);

        exactRuns.Clear();
        var exactUnaffectedLine = initialSnapshot.GetLine(10);
        highlighter.GetLineRuns(
            exactState,
            new CodeEditorLineSyntaxRequest(initialSnapshot, Theme.Default, 10, exactUnaffectedLine.Start, exactUnaffectedLine.Length, 0, 0, 0),
            exactRuns);

        TextDocumentChangedEventArgs? change = null;
        document.Changed += (_, args) => change = args;
        document.Insert(0, "x");
        Assert.IsNotNull(change);

        var updatedSnapshot = document.CurrentSnapshot;
        var partialUpdatedState = await highlighter.UpdateAsync(
            exactState,
            new CodeEditorSyntaxUpdateContext(
                updatedSnapshot,
                Theme.Default,
                change!,
                updatedSnapshot.GetLineIndexFromPosition(change!.Position),
                updatedSnapshot.GetLineIndexFromPosition(Math.Min(updatedSnapshot.Length, change.Position + change.InsertedLength)),
                0,
                0,
                0));

        Assert.IsFalse(partialUpdatedState.IsComplete, "Expected the edit to apply an immediate partial syntax state while exact background retokenization continues.");

        var changedLineRuns = new List<StyledRun>();
        var updatedChangedLine = updatedSnapshot.GetLine(0);
        highlighter.GetLineRuns(
            partialUpdatedState,
            new CodeEditorLineSyntaxRequest(updatedSnapshot, Theme.Default, 0, updatedChangedLine.Start, updatedChangedLine.Length, 0, 0, 0),
            changedLineRuns);
        Assert.IsTrue(changedLineRuns.Count > 0, "Expected the edited line to keep an approximate shifted tokenization instead of flashing white.");

        var unaffectedLineRuns = new List<StyledRun>();
        var updatedUnaffectedLine = updatedSnapshot.GetLine(10);
        highlighter.GetLineRuns(
            partialUpdatedState,
            new CodeEditorLineSyntaxRequest(updatedSnapshot, Theme.Default, 10, updatedUnaffectedLine.Start, updatedUnaffectedLine.Length, 0, 0, 0),
            unaffectedLineRuns);
        Assert.IsTrue(unaffectedLineRuns.Count > 0, "Expected unaffected lines after a small edit to immediately reuse their previous tokenization.");
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
