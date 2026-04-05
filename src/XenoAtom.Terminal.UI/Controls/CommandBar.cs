// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;
using XenoAtom.Ansi;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Controls;

/// <summary>
/// Displays discoverable keyboard shortcuts (commands) for the current focus context.
/// </summary>
/// <remarks>
/// The command bar is a lightweight UI surface intended for app chrome. By default it renders as a single row and clips
/// when space is insufficient. When <see cref="MultiLine"/> is enabled, it can wrap command entries onto additional rows.
/// </remarks>
public sealed partial class CommandBar : Visual
{
    private readonly MarkupTextParser _markupParser;
    private readonly List<(Command Command, Visual Target, bool IsEnabled)> _localCommands;
    private readonly List<(Command Command, Visual Target, bool IsEnabled)> _globalCommands;
    private readonly HashSet<string> _dedup;
    private readonly Dictionary<KeyGesture, string> _gestureTextCache;
    private readonly Dictionary<KeySequence, string> _sequenceTextCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBar"/> class.
    /// </summary>
    public CommandBar()
    {
        Presentation = CommandPresentation.CommandBar;
        HorizontalAlignment = Align.Stretch;
        _markupParser = new MarkupTextParser();
        _localCommands = new List<(Command, Visual, bool)>(16);
        _globalCommands = new List<(Command, Visual, bool)>(8);
        _dedup = new HashSet<string>(StringComparer.Ordinal);
        _gestureTextCache = new Dictionary<KeyGesture, string>();
        _sequenceTextCache = new Dictionary<KeySequence, string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBar"/> class with presentation flags.
    /// </summary>
    /// <param name="presentation">The command presentation flags used when collecting commands.</param>
    public CommandBar(CommandPresentation presentation) : this()
    {
        this.Presentation(presentation);
    }

    /// <summary>
    /// Gets or sets the presentation flags used when collecting commands.
    /// </summary>
    [Bindable]
    public partial CommandPresentation Presentation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether command entries can wrap onto multiple rows.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false"/> to preserve the existing single-row clipped behavior.
    /// </remarks>
    [Bindable]
    public partial bool MultiLine { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        var theme = GetTheme();
        CollectCommands(theme, out var pendingCount, out var pendingPrefix);

        var commandBarStyle = GetStyle<CommandBarStyle>();
        var separator = commandBarStyle.Separator ?? string.Empty;
        var separatorWidth = TerminalTextUtility.GetWidth(separator.AsSpan());
        var markupStyles = theme.GetMarkupStyles();

        if (!MultiLine)
        {
            // The default command bar is a single-row footer. It measures to its current content width
            // (based on the focused context), while allowing clipping when there is not enough width.
            var singleLineWidth = MeasureContentWidth(markupStyles, pendingPrefix, pendingCount, separatorWidth);
            var naturalSingleLine = constraints.Clamp(new Size(singleLineWidth, 1));

            // By default the command bar does not request extra space; it simply renders what fits.
            return SizeHints.Flex(min: new Size(0, 1), natural: naturalSingleLine, max: naturalSingleLine, growX: 0, growY: 0, shrinkX: 1, shrinkY: 0);
        }

        var contentWidth = MeasureContentWidth(markupStyles, pendingPrefix, pendingCount, separatorWidth);
        var availableWidth = constraints.IsWidthBounded ? Math.Max(1, constraints.MaxWidth) : Math.Max(1, contentWidth);
        var width = Math.Min(contentWidth, availableWidth);
        var height = MeasureWrappedHeight(commandBarStyle, markupStyles, pendingPrefix, pendingCount, separatorWidth, availableWidth);
        var natural = constraints.Clamp(new Size(width, height));

        // By default the command bar does not request extra space; it simply renders what fits.
        return SizeHints.Flex(min: new Size(0, 1), natural: natural, max: natural, growX: 0, growY: 0, shrinkX: 1, shrinkY: 0);
    }

    /// <inheritdoc />
    protected override void RenderOverride(CellBuffer buffer)
    {
        var rect = Bounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var theme = GetTheme();
        var commandBarStyle = GetStyle<CommandBarStyle>();
        var styles = commandBarStyle.Resolve(theme);

        // The command bar is app chrome: always clear the full bar area to ensure a stable background.
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
            {
                buffer.SetCell(x, y, new Rune(' '), styles.BarStyle);
            }
        }

        CollectCommands(theme, out var pendingCount, out var pendingPrefix);

        var xCursor = rect.X;
        var yCursor = rect.Y;
        var hasRenderedEntry = false;
        var previousRenderedItemWasPendingPrefix = false;

        if (pendingCount > 0)
        {
            // When a key sequence prefix is active, show the prefix to indicate that the next keystroke is awaited.
            xCursor = WriteKeycap(buffer, rect, xCursor, yCursor, pendingPrefix.ToString().AsSpan(), styles, commandBarStyle);
            xCursor = WritePlain(buffer, rect, xCursor, yCursor, " …".AsSpan(), styles.LabelStyle);
            hasRenderedEntry = true;
            previousRenderedItemWasPendingPrefix = true;
        }

        var separator = commandBarStyle.Separator ?? string.Empty;

        WriteCommandList(buffer, rect, ref xCursor, ref yCursor, styles, commandBarStyle, theme.GetMarkupStyles(), _localCommands, separator.AsSpan(), ref hasRenderedEntry, ref previousRenderedItemWasPendingPrefix);
        WriteCommandList(buffer, rect, ref xCursor, ref yCursor, styles, commandBarStyle, theme.GetMarkupStyles(), _globalCommands, separator.AsSpan(), ref hasRenderedEntry, ref previousRenderedItemWasPendingPrefix);
    }

    private void CollectCommands(Theme theme, out int pendingCount, out KeySequence pendingPrefix)
    {
        pendingCount = 0;
        pendingPrefix = default;

        _localCommands.Clear();
        _globalCommands.Clear();
        _dedup.Clear();

        var app = App;
        if (app is null)
        {
            return;
        }

        pendingCount = app.PendingCommandSequenceCount;
        if (pendingCount > 0)
        {
            Span<Input.KeyGesture> buf = stackalloc Input.KeyGesture[pendingCount];
            for (var i = 0; i < pendingCount; i++)
            {
                buf[i] = app.GetPendingCommandSequenceGesture(i);
            }

            pendingPrefix = new KeySequence(buf.ToArray());
        }

        var focus = app.FocusedElement;

        for (var v = focus; v is not null; v = v.Parent)
        {
            AppendCommandsForTarget(_localCommands, v, v.Commands, pendingPrefix);
        }

        var globalTarget = focus ?? app.Root;
        if (app.GlobalCommands.Count > 0)
        {
            AppendCommandsForTarget(_globalCommands, globalTarget, app.GlobalCommands, pendingPrefix);
        }

        // Sort by importance, but preserve registration order within the same importance.
        _localCommands.Sort(static (a, b) => a.Command.Importance.CompareTo(b.Command.Importance));
        _globalCommands.Sort(static (a, b) => a.Command.Importance.CompareTo(b.Command.Importance));
    }

    private int MeasureContentWidth(
        Dictionary<string, AnsiStyle> markupStyles,
        in KeySequence pendingPrefix,
        int pendingCount,
        int separatorWidth)
    {
        var width = 0;
        var hasEntry = false;

        if (pendingCount > 0)
        {
            // Keycap prefix + " …" indicator.
            // Render uses " …" and then adds an extra space before commands (only when commands follow).
            var prefixText = pendingPrefix.ToString().AsSpan();
            width += 1 + TerminalTextUtility.GetWidth(prefixText) + 1; // keycap open + text + close
            width += TerminalTextUtility.GetWidth(" …".AsSpan());     // space + ellipsis

            if (_localCommands.Count > 0 || _globalCommands.Count > 0)
            {
                width += 1;
            }
        }

        width = MeasureCommandListWidth(width, markupStyles, _localCommands, separatorWidth, ref hasEntry);
        width = MeasureCommandListWidth(width, markupStyles, _globalCommands, separatorWidth, ref hasEntry);

        return width;
    }

    private int MeasureWrappedHeight(
        CommandBarStyle commandBarStyle,
        Dictionary<string, AnsiStyle> markupStyles,
        in KeySequence pendingPrefix,
        int pendingCount,
        int separatorWidth,
        int availableWidth)
    {
        availableWidth = Math.Max(1, availableWidth);
        var lineCount = 1;
        var x = 0;
        var hasEntry = false;
        var previousItemWasPendingPrefix = false;

        if (pendingCount > 0)
        {
            x = MeasurePendingPrefixWidth(pendingPrefix);
            hasEntry = true;
            previousItemWasPendingPrefix = true;
        }

        MeasureWrappedCommandList(markupStyles, _localCommands, separatorWidth, availableWidth, ref x, ref lineCount, ref hasEntry, ref previousItemWasPendingPrefix);
        MeasureWrappedCommandList(markupStyles, _globalCommands, separatorWidth, availableWidth, ref x, ref lineCount, ref hasEntry, ref previousItemWasPendingPrefix);

        return Math.Max(1, lineCount);
    }

    private void MeasureWrappedCommandList(
        Dictionary<string, AnsiStyle> markupStyles,
        List<(Command Command, Visual Target, bool IsEnabled)> commands,
        int separatorWidth,
        int availableWidth,
        ref int x,
        ref int lineCount,
        ref bool hasEntry,
        ref bool previousItemWasPendingPrefix)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var (command, _, _) = commands[i];
            var entryWidth = MeasureCommandEntryWidth(in command, markupStyles);
            if (entryWidth == 0)
            {
                continue;
            }

            var leadingWidth = 0;
            if (hasEntry)
            {
                leadingWidth = previousItemWasPendingPrefix ? 1 : separatorWidth;
            }

            if (x > 0 && x + leadingWidth + entryWidth > availableWidth)
            {
                lineCount++;
                x = 0;
                leadingWidth = 0;
            }

            x += leadingWidth + entryWidth;
            hasEntry = true;
            previousItemWasPendingPrefix = false;
        }
    }

    private int MeasureCommandListWidth(
        int width,
        Dictionary<string, AnsiStyle> markupStyles,
        List<(Command Command, Visual Target, bool IsEnabled)> commands,
        int separatorWidth,
        ref bool hasEntry)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var (cmd, _, _) = commands[i];
            var keyTextString = GetKeyText(in cmd);
            if (keyTextString.Length == 0)
            {
                continue;
            }
            var keyText = keyTextString.AsSpan();

            if (hasEntry)
            {
                width += separatorWidth;
            }
            hasEntry = true;

            // Keycap + space.
            width += 1 + TerminalTextUtility.GetWidth(keyText) + 1;
            width += 1;

            // Label markup stripped to plain text.
            var plain = _markupParser.Parse(cmd.LabelMarkup, out _, markupStyles);
            width += TerminalTextUtility.GetWidth(plain.AsSpan());
        }

        return width;
    }

    private void AppendCommandsForTarget(
        List<(Command Command, Visual Target, bool IsEnabled)> destination,
        Visual target,
        IReadOnlyList<Command> commands,
        KeySequence pendingPrefix)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if ((cmd.Presentation & Presentation) == 0)
            {
                continue;
            }

            if (cmd.Gesture is null && cmd.Sequence is null)
            {
                continue;
            }

            if (!cmd.IsVisibleFor(target))
            {
                continue;
            }

            if (!_dedup.Add(cmd.Id))
            {
                continue;
            }

            var enabled = cmd.CanExecuteFor(target);

            if (pendingPrefix.Gestures.IsEmpty)
            {
                destination.Add((cmd, target, enabled));
                continue;
            }

            if (cmd.Sequence is null)
            {
                continue;
            }

            if (SequenceMatchesPrefix(cmd.Sequence.Value, pendingPrefix.Gestures))
            {
                destination.Add((cmd, target, enabled));
            }
        }
    }

    private static bool SequenceMatchesPrefix(in KeySequence sequence, ReadOnlySpan<Input.KeyGesture> prefix)
    {
        var gestures = sequence.Gestures;
        if (prefix.Length > gestures.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (!prefix[i].Matches(in gestures[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void WriteCommandList(
        CellBuffer buffer,
        Rectangle rect,
        ref int x,
        ref int y,
        CommandBarResolvedStyle styles,
        CommandBarStyle commandBarStyle,
        Dictionary<string, AnsiStyle> markupStyles,
        List<(Command Command, Visual Target, bool IsEnabled)> commands,
        ReadOnlySpan<char> separator,
        ref bool hasEntry,
        ref bool previousItemWasPendingPrefix)
    {
        if (y >= rect.Bottom || rect.Width <= 0)
        {
            return;
        }

        for (var i = 0; i < commands.Count; i++)
        {
            var (cmd, _, enabled) = commands[i];
            var keyTextString = GetKeyText(in cmd);
            if (keyTextString.Length == 0)
            {
                continue;
            }
            var keyText = keyTextString.AsSpan();
            var entryWidth = MeasureCommandEntryWidth(in cmd, markupStyles);
            if (entryWidth == 0)
            {
                continue;
            }

            ReadOnlySpan<char> separatorToRender = ReadOnlySpan<char>.Empty;
            if (hasEntry)
            {
                separatorToRender = previousItemWasPendingPrefix ? " ".AsSpan() : separator;
            }

            var separatorWidth = TerminalTextUtility.GetWidth(separatorToRender);
            if (MultiLine && x > rect.X && x + separatorWidth + entryWidth > rect.Right)
            {
                y++;
                x = rect.X;
                separatorToRender = ReadOnlySpan<char>.Empty;
                if (y >= rect.Bottom)
                {
                    return;
                }
            }

            if (!separatorToRender.IsEmpty)
            {
                x = WritePlain(buffer, rect, x, y, separatorToRender, styles.LabelStyle);
                if (x >= rect.Right)
                {
                    return;
                }
            }

            x = WriteKeycap(buffer, rect, x, y, keyText, styles, commandBarStyle);
            x = WritePlain(buffer, rect, x, y, " ".AsSpan(), styles.LabelStyle);

            var labelStyle = enabled ? styles.LabelStyle : styles.DisabledLabelStyle;
            x = WriteMarkup(buffer, rect, x, y, cmd.LabelMarkup, labelStyle, markupStyles);

            hasEntry = true;
            previousItemWasPendingPrefix = false;
            if (x >= rect.Right && !MultiLine)
            {
                return;
            }
        }
    }

    private string GetKeyText(in Command command)
    {
        if (command.Sequence is { } sequence)
        {
            if (_sequenceTextCache.TryGetValue(sequence, out var sequenceText))
            {
                return sequenceText;
            }

            sequenceText = sequence.ToString();
            _sequenceTextCache[sequence] = sequenceText;
            return sequenceText;
        }

        if (command.Gesture is { } gesture)
        {
            if (_gestureTextCache.TryGetValue(gesture, out var gestureText))
            {
                return gestureText;
            }

            gestureText = gesture.ToString();
            _gestureTextCache[gesture] = gestureText;
            return gestureText;
        }

        return string.Empty;
    }

    private static int WriteKeycap(CellBuffer buffer, Rectangle rect, int x, int y, ReadOnlySpan<char> keyText, CommandBarResolvedStyle styles, CommandBarStyle commandBarStyle)
    {
        if (y >= rect.Bottom || x >= rect.Right)
        {
            return x;
        }

        buffer.SetCell(x++, y, commandBarStyle.KeycapOpen, styles.KeyStyle);
        x = WritePlain(buffer, rect, x, y, keyText, styles.KeyStyle);

        if (x < rect.Right)
        {
            buffer.SetCell(x++, y, commandBarStyle.KeycapClose, styles.KeyStyle);
        }

        return x;
    }

    private static int WritePlain(CellBuffer buffer, Rectangle rect, int x, int y, ReadOnlySpan<char> text, Style style)
    {
        if (y >= rect.Bottom || x >= rect.Right)
        {
            return x;
        }

        var max = rect.Right;
        for (var i = 0; i < text.Length && x < max; i++)
        {
            buffer.SetCell(x++, y, new Rune(text[i]), style);
        }

        return x;
    }

    private int WriteMarkup(CellBuffer buffer, Rectangle rect, int x, int y, string labelMarkup, Style baseStyle, Dictionary<string, AnsiStyle> markupStyles)
    {
        if (y >= rect.Bottom || x >= rect.Right)
        {
            return x;
        }

        var plain = _markupParser.Parse(labelMarkup, out var runs, markupStyles);
        if (plain.Length == 0)
        {
            return x;
        }

        var max = rect.Right;

        // Runs are in plain-text coordinates; render them in order and clip to the bar width.
        for (var i = 0; i < runs.Length && x < max; i++)
        {
            var run = runs[i];
            var style = baseStyle | run.Style;

            var start = run.Start;
            var end = Math.Min(run.Start + run.Length, plain.Length);
            for (var j = start; j < end && x < max; j++)
            {
                buffer.SetCell(x++, y, new Rune(plain[j]), style);
            }
        }

        // If there were no runs, render as plain text with the base style.
        if (runs.Length == 0)
        {
            for (var j = 0; j < plain.Length && x < max; j++)
            {
                buffer.SetCell(x++, y, new Rune(plain[j]), baseStyle);
            }
        }

        return x;
    }

    private int MeasurePendingPrefixWidth(in KeySequence pendingPrefix)
        => MeasureKeycapWidth(pendingPrefix.ToString().AsSpan()) + TerminalTextUtility.GetWidth(" …".AsSpan());

    private int MeasureCommandEntryWidth(in Command command, Dictionary<string, AnsiStyle> markupStyles)
    {
        var keyText = GetKeyText(in command);
        if (keyText.Length == 0)
        {
            return 0;
        }

        var plain = _markupParser.Parse(command.LabelMarkup, out _, markupStyles);
        return MeasureKeycapWidth(keyText.AsSpan()) + 1 + TerminalTextUtility.GetWidth(plain.AsSpan());
    }

    private static int MeasureKeycapWidth(ReadOnlySpan<char> keyText)
        => 1 + TerminalTextUtility.GetWidth(keyText) + 1;
}
