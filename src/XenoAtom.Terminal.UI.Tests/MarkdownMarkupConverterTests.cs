// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class MarkdownMarkupConverterTests
{
    [TestMethod]
    public void MarkdownMarkupConverter_Uses_Themed_Default_Styles()
    {
        var markdown = """
            # Heading One

            Paragraph with **strong**, [link](https://example.com), and `inline code`.
            """;

        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);
        var converter = new MarkdownMarkupConverter
        {
            Theme = theme,
        };

        var markup = converter.Convert(markdown);
        var plain = ParseMarkup(theme, markup, out var runs);
        StringAssert.Contains(plain, "Heading One");
        StringAssert.Contains(plain, "strong");
        StringAssert.Contains(plain, "inline code");

        var headingStyle = FindStyleForToken(plain, runs, "Heading One");
        AssertStyleForegroundEquals(headingStyle, ResolveExpectedHeadingColor(theme));
        Assert.IsTrue((headingStyle.TextStyle & (TextStyle.Bold | TextStyle.Underline)) == (TextStyle.Bold | TextStyle.Underline));

        var strongStyle = FindStyleForToken(plain, runs, "strong");
        AssertStyleForegroundEquals(strongStyle, ResolveExpectedStrongColor(theme));
        Assert.IsTrue((strongStyle.TextStyle & TextStyle.Bold) == TextStyle.Bold);

        var linkStyle = FindStyleForToken(plain, runs, "link");
        AssertStyleForegroundEquals(linkStyle, ResolveExpectedLinkColor(theme));
        Assert.IsTrue((linkStyle.TextStyle & TextStyle.Underline) == TextStyle.Underline);

        var inlineCodeStyle = FindStyleForToken(plain, runs, "inline code");
        AssertStyleForegroundEquals(inlineCodeStyle, ResolveExpectedInlineCodeForeground(theme));
        Assert.IsTrue(inlineCodeStyle.TryGetBackground(out _), "Expected inline code style to provide a background.");
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Supports_Custom_Pipeline()
    {
        var markdown = """
            > [!WARNING]
            > Attention required.
            """;

        var converter = new MarkdownMarkupConverter();
        var defaultMarkup = converter.Convert(markdown);
        var defaultPlain = ParseMarkup(Theme.Default, defaultMarkup, out _);
        StringAssert.Contains(defaultPlain, "!WARNING");

        converter.Pipeline = new MarkdownPipelineBuilder().Configure("common").Build();
        var commonOnlyMarkup = converter.Convert(markdown);
        var commonOnlyPlain = ParseMarkup(Theme.Default, commonOnlyMarkup, out _);
        StringAssert.Contains(commonOnlyPlain, "[!WARNING]");
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Reuses_Internal_Buffer_And_Clears_Output()
    {
        var converter = new MarkdownMarkupConverter();

        var first = converter.Convert("**first value**");
        var second = converter.Convert("`x`");
        var third = converter.Convert("done");

        Assert.IsFalse(second.Contains("first", StringComparison.Ordinal));
        Assert.IsFalse(third.Contains("first", StringComparison.Ordinal));

        var secondPlain = ParseMarkup(Theme.Default, second, out _);
        Assert.AreEqual("x", secondPlain.Trim(), "Expected the second conversion to contain only the latest markdown content.");
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Can_Append_To_External_StringBuilder()
    {
        var converter = new MarkdownMarkupConverter();
        var destination = new System.Text.StringBuilder("prefix:");
        converter.Convert("**hello**", destination);
        converter.Convert(" world", destination);

        var plain = ParseMarkup(Theme.Default, destination.ToString(), out _);
        StringAssert.Contains(plain, "prefix:helloworld");
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Highlight_Styles_Original_Markdown_Syntax()
    {
        var markdown = """
            # Heading

            **strong** and `code` with a [link](https://example.com).
            """;

        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);
        var converter = new MarkdownMarkupConverter
        {
            Theme = theme,
        };

        var runs = converter.Highlight(markdown);

        var headingMarkerIndex = markdown.IndexOf('#', StringComparison.Ordinal);
        var strongMarkerIndex = markdown.IndexOf('*', StringComparison.Ordinal);
        var inlineCodeMarkerIndex = markdown.IndexOf('`', StringComparison.Ordinal);
        var linkMarkerIndex = markdown.IndexOf('[', StringComparison.Ordinal);

        AssertStyleForegroundEquals(FindStyleAtIndex(runs, headingMarkerIndex), ResolveExpectedHeadingColor(theme));
        AssertStyleForegroundEquals(FindStyleAtIndex(runs, strongMarkerIndex), ResolveExpectedStrongColor(theme));
        AssertStyleForegroundEquals(FindStyleAtIndex(runs, inlineCodeMarkerIndex), ResolveExpectedInlineCodeForeground(theme));
        AssertStyleForegroundEquals(FindStyleAtIndex(runs, linkMarkerIndex), ResolveExpectedLinkColor(theme));
    }

    [TestMethod]
    public void MarkdownMarkupConverter_ConvertPreservingSource_Preserves_Exact_Text()
    {
        var markdown = """
            # Heading

            Keep **all** markdown chars, including `ticks`, [links](x), and [brackets].
            """;

        var theme = Theme.FromScheme(ColorScheme.ElderberryDarkSoft);
        var converter = new MarkdownMarkupConverter
        {
            Theme = theme,
        };

        var markup = converter.ConvertPreservingSource(markdown);
        var plain = ParseMarkup(theme, markup, out var runs);

        Assert.AreEqual(markdown, plain);
        var inlineCodeMarkerIndex = markdown.IndexOf('`', StringComparison.Ordinal);
        AssertStyleForegroundEquals(FindStyleAtIndex(runs, inlineCodeMarkerIndex), ResolveExpectedInlineCodeForeground(theme));
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Highlight_Uses_SourcePipeline_When_Provided()
    {
        var markdown = """
            > [!WARNING]
            > Attention required.
            """;

        var theme = Theme.FromScheme(ColorScheme.RootLoopsDark);
        var converter = new MarkdownMarkupConverter
        {
            Theme = theme,
        };

        var warningMarkerIndex = markdown.IndexOf('!', StringComparison.Ordinal);

        var defaultRuns = converter.Highlight(markdown);
        var defaultStyle = FindStyleAtIndex(defaultRuns, warningMarkerIndex);
        AssertStyleForegroundEquals(defaultStyle, (theme.Warning ?? Colors.Goldenrod).ToRgb());

        converter.SourcePipeline = new MarkdownPipelineBuilder()
            .Configure("common")
            .UsePreciseSourceLocation()
            .Build();
        var commonRuns = converter.Highlight(markdown);
        var commonStyle = FindStyleAtIndex(commonRuns, warningMarkerIndex);

        Assert.AreNotEqual(defaultStyle, commonStyle);
    }

    [TestMethod]
    public void MarkdownMarkupConverter_Highlight_Can_Write_Into_Provided_List()
    {
        var converter = new MarkdownMarkupConverter();
        var destination = new List<StyledRun> { new StyledRun(0, 1, Style.None | TextStyle.Bold) };

        converter.Highlight("`x`", destination);

        Assert.AreEqual(1, destination.Count);
        Assert.AreEqual(0, destination[0].Start);
        Assert.AreEqual(3, destination[0].Length);
        Assert.IsTrue(destination[0].Style.TryGetForeground(out _));
    }

    private static string ParseMarkup(Theme theme, string markup, out StyledRun[] runs)
    {
        var parser = new MarkupTextParser();
        return parser.Parse(markup, out runs, theme.GetMarkupStyles());
    }

    private static Style FindStyleForToken(string text, StyledRun[] runs, string token)
    {
        var start = text.IndexOf(token, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Could not find token `{token}`.");
        var end = start + token.Length;

        foreach (var run in runs)
        {
            if (run.Start <= start && run.Start + run.Length >= end)
            {
                return run.Style;
            }
        }

        Assert.Fail($"Could not find a style run covering token `{token}`.");
        return Style.None;
    }

    private static Style FindStyleAtIndex(StyledRun[] runs, int index)
    {
        Assert.IsTrue(index >= 0, "Expected a valid source index.");
        for (var runIndex = 0; runIndex < runs.Length; runIndex++)
        {
            var run = runs[runIndex];
            if (run.Start <= index && run.Start + run.Length > index)
            {
                return run.Style;
            }
        }

        Assert.Fail($"Could not find a style run covering index `{index}`.");
        return Style.None;
    }

    private static void AssertStyleForegroundEquals(Style style, Color expected)
    {
        Assert.IsTrue(style.TryGetForeground(out var actual), "Expected style to define a foreground.");
        Assert.AreEqual(expected.ToRgb(), actual.ToRgb());
    }

    private static Color ResolveExpectedHeadingColor(Theme theme)
        => (theme.Scheme?.BrightYellow ?? Colors.TerminalBrightYellow).ToRgb();

    private static Color ResolveExpectedStrongColor(Theme theme)
        => (theme.Accent ?? theme.Primary ?? theme.Warning ?? Colors.TerminalBrightCyan).ToRgb();

    private static Color ResolveExpectedLinkColor(Theme theme)
        => (theme.Accent ?? theme.Primary ?? Colors.TerminalBrightBlue).ToRgb();

    private static Color ResolveExpectedInlineCodeForeground(Theme theme)
        => (theme.Scheme?.BrightRed ?? Colors.TerminalBrightRed).ToRgb();
}
