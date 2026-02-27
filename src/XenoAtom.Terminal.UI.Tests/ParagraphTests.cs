// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Hosting;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ParagraphTests
{
    [TestMethod]
    public void Paragraph_Wrap_With_Indent_And_HangingIndent_Preserves_Prefixes()
    {
        var paragraph = new Paragraph("alpha beta gamma delta epsilon zeta")
            .Wrap(true)
            .Indent(1)
            .HangingIndent(2)
            .LinePrefix("• ")
            .ContinuationPrefix("  ")
            .HorizontalAlignment(Align.Stretch);

        var buffer = VisualSnapshotRenderer.Render(paragraph, width: 14, maxHeight: 8, Theme.Default);
        var row0 = GetRowText(buffer, 0);
        var row1 = GetRowText(buffer, 1);

        StringAssert.StartsWith(row0, " • ");
        StringAssert.StartsWith(row1, "     ");
    }

    [TestMethod]
    public void Paragraph_SingleLine_Trimming_Reserves_Prefix()
    {
        var paragraph = new Paragraph("123456789")
            .Wrap(false)
            .Indent(1)
            .LinePrefix("> ")
            .Trimming(TextTrimming.EndEllipsis)
            .HorizontalAlignment(Align.Stretch);

        var buffer = VisualSnapshotRenderer.Render(paragraph, width: 7, maxHeight: 1, Theme.Default);
        var row0 = GetRowText(buffer, 0);

        Assert.AreEqual(" > 123…", row0);
    }

    [TestMethod]
    public void Paragraph_Applies_Styled_And_Hyperlink_Runs()
    {
        var paragraph = new Paragraph("Visit now")
            .Wrap(false)
            .Runs([new StyledRun(0, 5, Style.None | TextStyle.Bold)])
            .Hyperlinks([new HyperlinkRun(0, 5, "https://example.com")]);

        paragraph.Measure(new LayoutConstraints(0, 16, 0, 1));
        paragraph.Arrange(new Rectangle(0, 0, 16, 1));

        var buffer = new CellBuffer(16, 1);
        buffer.Clear(Style.None);
        paragraph.RenderTree(buffer);

        var hyperlinks = buffer.UnsafeHyperlinks;
        for (var index = 0; index < 5; index++)
        {
            Assert.AreNotEqual(0ul, hyperlinks[index]);
        }

        for (var index = 5; index < 8; index++)
        {
            Assert.AreEqual(0ul, hyperlinks[index]);
        }

        Assert.AreEqual(TextStyle.Bold, buffer.UnsafeCells[0].TextStyle & TextStyle.Bold);
        Assert.AreEqual(TextStyle.Bold, buffer.UnsafeCells[4].TextStyle & TextStyle.Bold);
    }

    [TestMethod]
    public void Paragraph_Emoji_Cluster_Uses_Double_Width()
    {
        var paragraph = new Paragraph("A🗃️B")
            .Wrap(false)
            .HorizontalAlignment(Align.Stretch);

        paragraph.Measure(new LayoutConstraints(0, 8, 0, 1));
        paragraph.Arrange(new Rectangle(0, 0, 8, 1));

        var buffer = new CellBuffer(8, 1);
        buffer.Clear(Style.None);
        paragraph.RenderTree(buffer);

        Assert.AreEqual('A', buffer.UnsafeScalars[0]);
        Assert.AreEqual('B', buffer.UnsafeScalars[3]);
    }

    [TestMethod]
    public void Paragraph_Tab_Expands_To_Default_Tab_Stop()
    {
        var paragraph = new Paragraph("A\tB")
            .Wrap(false)
            .HorizontalAlignment(Align.Stretch);

        paragraph.Measure(new LayoutConstraints(0, 8, 0, 1));
        paragraph.Arrange(new Rectangle(0, 0, 8, 1));

        var buffer = new CellBuffer(8, 1);
        buffer.Clear(Style.None);
        paragraph.RenderTree(buffer);

        Assert.AreEqual('B', buffer.UnsafeScalars[4]);
    }

    [TestMethod]
    public void Paragraph_MouseDragSelection_CtrlC_CopiesSelectedText()
    {
        var paragraph = new Paragraph("hello world").HorizontalAlignment(Align.Stretch);

        using var driver = new TerminalAppTestDriver(paragraph, TerminalHostKind.Fullscreen, new TerminalSize(30, 4));
        driver.Tick();

        var y = paragraph.Bounds.Y;
        var startX = paragraph.Bounds.X + 6;
        var endX = paragraph.Bounds.X + 11;

        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Down,
            Button = TerminalMouseButton.Left,
            X = startX,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Drag,
            Button = TerminalMouseButton.Left,
            X = endX,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalMouseEvent
        {
            Kind = TerminalMouseKind.Up,
            Button = TerminalMouseButton.Left,
            X = endX,
            Y = y,
        });
        driver.Backend.PushEvent(new TerminalKeyEvent
        {
            Key = TerminalKey.Unknown,
            Char = TerminalChar.CtrlC,
            Modifiers = TerminalModifiers.Ctrl,
        });

        driver.Tick();

        Assert.AreEqual("world", driver.Terminal.Clipboard.Text);
    }

    private static string GetRowText(CellBuffer buffer, int row)
    {
        var width = buffer.Width;
        var start = row * width;
        var sb = new StringBuilder(width);
        for (var x = 0; x < width; x++)
        {
            var scalar = buffer.UnsafeScalars[start + x];
            if (scalar <= 0)
            {
                sb.Append(' ');
            }
            else if (scalar <= char.MaxValue)
            {
                sb.Append((char)scalar);
            }
            else
            {
                sb.Append('�');
            }
        }

        return sb.ToString();
    }
}
