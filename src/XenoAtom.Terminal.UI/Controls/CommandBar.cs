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
/// The command bar is a lightweight, single-row UI surface intended for app chrome. It surfaces commands registered on
/// the focused element (and its parents) and app-level commands.
/// </remarks>
public sealed partial class CommandBar : Visual
{
    private readonly MarkupTextParser _markupParser;
    private readonly List<(Command Command, Visual Target, bool IsEnabled)> _localCommands;
    private readonly List<(Command Command, Visual Target, bool IsEnabled)> _globalCommands;
    private readonly HashSet<string> _dedup;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBar"/> class.
    /// </summary>
    public CommandBar()
    {
        Presentation = CommandPresentation.CommandBar;
        _markupParser = new MarkupTextParser();
        _localCommands = new List<(Command, Visual, bool)>(16);
        _globalCommands = new List<(Command, Visual, bool)>(8);
        _dedup = new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets or sets the presentation flags used when collecting commands.
    /// </summary>
    [Bindable]
    public partial CommandPresentation Presentation { get; set; }

    /// <inheritdoc />
    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
    {
        // The command bar is intended as a single-row footer. It measures to its current content width
        // (based on the focused context), while allowing clipping when there is not enough width.

        var theme = GetTheme();
        CollectCommands(theme, out var pendingCount, out var pendingPrefix);

        var commandBarStyle = GetStyle<CommandBarStyle>();
        var separator = commandBarStyle.Separator ?? string.Empty;
        var separatorWidth = TerminalTextUtility.GetWidth(separator.AsSpan());

        var width = MeasureContentWidth(commandBarStyle, theme.GetMarkupStyles(), pendingPrefix, pendingCount, separatorWidth);
        var natural = constraints.Clamp(new Size(width, 1));

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

        // The command bar is app chrome: always clear the row to ensure a stable background.
        for (var x = rect.X; x < rect.X + rect.Width; x++)
        {
            buffer.SetCell(x, rect.Y, new Rune(' '), styles.BarStyle);
        }

        CollectCommands(theme, out var pendingCount, out var pendingPrefix);

        var xCursor = rect.X;
        var hasEntry = false;

        if (pendingCount > 0)
        {
            // When a key sequence prefix is active, show the prefix to indicate that the next keystroke is awaited.
            xCursor = WriteKeycap(buffer, rect, xCursor, pendingPrefix.ToString().AsSpan(), styles, commandBarStyle);
            xCursor = WritePlain(buffer, rect, xCursor, " …".AsSpan(), styles.LabelStyle);
            xCursor += 1;
        }

        var separator = commandBarStyle.Separator ?? string.Empty;

        xCursor = WriteCommandList(buffer, rect, xCursor, styles, commandBarStyle, theme.GetMarkupStyles(), _localCommands, separator.AsSpan(), ref hasEntry);
        xCursor = WriteCommandList(buffer, rect, xCursor, styles, commandBarStyle, theme.GetMarkupStyles(), _globalCommands, separator.AsSpan(), ref hasEntry);
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
        CommandBarStyle commandBarStyle,
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

        width = MeasureCommandListWidth(width, commandBarStyle, markupStyles, _localCommands, separatorWidth, ref hasEntry);
        width = MeasureCommandListWidth(width, commandBarStyle, markupStyles, _globalCommands, separatorWidth, ref hasEntry);

        return width;
    }

    private int MeasureCommandListWidth(
        int width,
        CommandBarStyle commandBarStyle,
        Dictionary<string, AnsiStyle> markupStyles,
        List<(Command Command, Visual Target, bool IsEnabled)> commands,
        int separatorWidth,
        ref bool hasEntry)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var (cmd, _, _) = commands[i];

            ReadOnlySpan<char> keyText;
            if (cmd.Sequence is { } seq)
            {
                keyText = seq.ToString().AsSpan();
            }
            else if (cmd.Gesture is { } g)
            {
                keyText = g.ToString().AsSpan();
            }
            else
            {
                continue;
            }

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

    private int WriteCommandList(
        CellBuffer buffer,
        Rectangle rect,
        int x,
        CommandBarResolvedStyle styles,
        CommandBarStyle commandBarStyle,
        Dictionary<string, AnsiStyle> markupStyles,
        List<(Command Command, Visual Target, bool IsEnabled)> commands,
        ReadOnlySpan<char> separator,
        ref bool hasEntry)
    {
        if (x >= rect.X + rect.Width)
        {
            return x;
        }

        for (var i = 0; i < commands.Count; i++)
        {
            var (cmd, _, enabled) = commands[i];

            ReadOnlySpan<char> keyText;
            if (cmd.Sequence is { } seq)
            {
                keyText = seq.ToString().AsSpan();
            }
            else if (cmd.Gesture is { } g)
            {
                keyText = g.ToString().AsSpan();
            }
            else
            {
                keyText = ReadOnlySpan<char>.Empty;
            }
            if (keyText.IsEmpty)
            {
                continue;
            }

            if (hasEntry && !separator.IsEmpty)
            {
                x = WritePlain(buffer, rect, x, separator, styles.LabelStyle);
                if (x >= rect.X + rect.Width)
                {
                    break;
                }
            }

            x = WriteKeycap(buffer, rect, x, keyText, styles, commandBarStyle);
            x = WritePlain(buffer, rect, x, " ".AsSpan(), styles.LabelStyle);

            var labelStyle = enabled ? styles.LabelStyle : styles.DisabledLabelStyle;
            x = WriteMarkup(buffer, rect, x, cmd.LabelMarkup, labelStyle, markupStyles);

            hasEntry = true;
            if (x >= rect.X + rect.Width)
            {
                break;
            }
        }

        return x;
    }

    private static int WriteKeycap(CellBuffer buffer, Rectangle rect, int x, ReadOnlySpan<char> keyText, CommandBarResolvedStyle styles, CommandBarStyle commandBarStyle)
    {
        if (x >= rect.X + rect.Width)
        {
            return x;
        }

        buffer.SetCell(x++, rect.Y, commandBarStyle.KeycapOpen, styles.KeyStyle);
        x = WritePlain(buffer, rect, x, keyText, styles.KeyStyle);

        if (x < rect.X + rect.Width)
        {
            buffer.SetCell(x++, rect.Y, commandBarStyle.KeycapClose, styles.KeyStyle);
        }

        return x;
    }

    private static int WritePlain(CellBuffer buffer, Rectangle rect, int x, ReadOnlySpan<char> text, Style style)
    {
        if (x >= rect.X + rect.Width)
        {
            return x;
        }

        var max = rect.X + rect.Width;
        for (var i = 0; i < text.Length && x < max; i++)
        {
            buffer.SetCell(x++, rect.Y, new Rune(text[i]), style);
        }

        return x;
    }

    private int WriteMarkup(CellBuffer buffer, Rectangle rect, int x, string labelMarkup, Style baseStyle, Dictionary<string, AnsiStyle> markupStyles)
    {
        if (x >= rect.X + rect.Width)
        {
            return x;
        }

        var plain = _markupParser.Parse(labelMarkup, out var runs, markupStyles);
        if (plain.Length == 0)
        {
            return x;
        }

        var max = rect.X + rect.Width;

        // Runs are in plain-text coordinates; render them in order and clip to the bar width.
        for (var i = 0; i < runs.Length && x < max; i++)
        {
            var run = runs[i];
            var style = baseStyle | run.Style;

            var start = run.Start;
            var end = Math.Min(run.Start + run.Length, plain.Length);
            for (var j = start; j < end && x < max; j++)
            {
                buffer.SetCell(x++, rect.Y, new Rune(plain[j]), style);
            }
        }

        // If there were no runs, render as plain text with the base style.
        if (runs.Length == 0)
        {
            for (var j = 0; j < plain.Length && x < max; j++)
            {
                buffer.SetCell(x++, rect.Y, new Rune(plain[j]), baseStyle);
            }
        }

        return x;
    }
}
