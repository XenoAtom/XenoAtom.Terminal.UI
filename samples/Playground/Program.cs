using System.Diagnostics;
using System.Linq;
using System.Threading;
using XenoAtom.Ansi;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

bool resetOnResize = true;

using var session = Terminal.Open(options: new TerminalOptions
{
    TreatControlCAsInput = true,
    PreferUtf8Output = true,
});

var terminal = session.Instance;
var caps = CreateAnsiCapabilities(terminal.Capabilities);

terminal.WriteLine("Raw inline resize repro (no UI framework).");
terminal.WriteLine("Resize horizontally quickly to reproduce reflow artifacts. Press Esc to exit.");
terminal.WriteLine();

var builder = new AnsiBuilder(initialCapacity: 4096);
var reservedHeight = 0;
var lastWidth = 0;
var lastHeight = 0;

try
{
    terminal.ShowCursor(false);

    var sw = Stopwatch.StartNew();
    var frame = 0;
    while (true)
    {
        while (terminal.TryReadEvent(out var ev))
        {
            if (ev is TerminalKeyEvent { Key: TerminalKey.Escape })
            {
                return;
            }

            if (ev is TerminalKeyEvent key && key.Char is TerminalChar.CtrlC or TerminalChar.CtrlQ)
            {
                return;
            }
        }

        var width = Math.Max(1, terminal.Size.Columns);
        var availableHeight = Math.Max(1, terminal.Size.Rows);
        var height = Math.Min(12, Math.Max(1, availableHeight - 1));

        if (reservedHeight != height)
        {
            reservedHeight = height;
        }

        builder.Clear();
        var writer = new AnsiWriter(builder, caps);

        writer.PrivateMode(2026, enabled: true);
        writer.ShowCursor(false);

        writer.CursorHorizontalAbsolute(1);

        writer.SaveCursorPosition();

        if (resetOnResize && lastWidth != 0 && (lastWidth != width || lastHeight > reservedHeight))
        {
            // When the viewport width changes, many terminals reflow existing content, which means the region we drew last
            // frame might have wrapped and pushed parts of it below. Clearing from the cursor down is the simplest way to
            // ensure the next frame starts clean.
            writer.EraseInDisplay(0);
            writer.RestoreCursorPosition();
        }
        
        DrawTable(writer, width, reservedHeight, frame, sw.Elapsed);

        writer.RestoreCursorPosition();

        writer.PrivateMode(2026, enabled: false);

        terminal.Write(builder.UnsafeAsSpan());

        lastWidth = width;
        lastHeight = reservedHeight;
        frame++;
        Thread.Sleep(1);
    }
}
finally
{
    try
    {
        terminal.ResetStyle();
        terminal.ShowCursor(true);
    }
    catch
    {
        // Best effort.
    }

    builder.Dispose();
}

static void DrawTable(AnsiWriter writer, int width, int height, int frame, TimeSpan elapsed)
{
    if (height <= 0)
    {
        return;
    }

    var innerWidth = Math.Max(0, width - 2);

    writer.EraseLine(2);
    writer.CursorHorizontalAbsolute(1);
    if (width == 1)
    {
        writer.Write("┌");
    }
    else
    {
        writer.Write("┌");
        writer.Write(new string('─', innerWidth));
        writer.Write("┐");
    }
    writer.NextLine();

    for (var row = 0; row < Math.Max(0, height - 2); row++)
    {
        writer.EraseLine(2);
        writer.CursorHorizontalAbsolute(1);

        if (width == 1)
        {
            writer.Write("│");
            writer.NextLine();
            continue;
        }

        writer.Write("│");

        var content = row switch
        {
            0 => $"Frame: {frame}",
            1 => $"Elapsed: {elapsed.TotalSeconds:0.000}s",
            2 => $"Cols: {width}  Rows: {height}",
            3 => "Resize horizontally quickly to reproduce.",
            _ => $"Row {row:00} {new string('#', (frame + row) % Math.Max(1, innerWidth))}",
        };

        if (content.Length > innerWidth)
        {
            content = content[..innerWidth];
        }

        writer.Write(content);
        var remaining = innerWidth - content.Length;
        if (remaining > 0)
        {
            writer.Write(new string(' ', remaining));
        }

        writer.Write("│");
        writer.NextLine();
    }

    if (height > 1)
    {
        writer.EraseLine(2);
        writer.CursorHorizontalAbsolute(1);
        if (width == 1)
        {
            writer.Write("└");
        }
        else
        {
            writer.Write("└");
            writer.Write(new string('─', innerWidth));
            writer.Write("┘");
        }
        writer.NextLine();
    }
}

static AnsiCapabilities CreateAnsiCapabilities(TerminalCapabilities caps)
{
    var colorLevel = caps.ColorLevel switch
    {
        TerminalColorLevel.None => AnsiColorLevel.None,
        TerminalColorLevel.Color16 => AnsiColorLevel.Colors16,
        TerminalColorLevel.Color256 => AnsiColorLevel.Colors256,
        _ => AnsiColorLevel.TrueColor,
    };

    return new AnsiCapabilities
    {
        AnsiEnabled = caps.AnsiEnabled,
        ColorLevel = colorLevel,
        SupportsOsc8 = caps.SupportsOsc8Links,
        Prefer7BitC1 = true,
        SafeMode = false,
        OscTermination = AnsiOscTermination.StringTerminator,
    };
}
