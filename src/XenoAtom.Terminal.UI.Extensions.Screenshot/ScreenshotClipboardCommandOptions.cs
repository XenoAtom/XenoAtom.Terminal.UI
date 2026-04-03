using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Input;

namespace XenoAtom.Terminal.UI.Extensions.Screenshot;

/// <summary>
/// Configures the screenshot-to-clipboard command registered by the screenshot extension.
/// </summary>
public sealed record ScreenshotClipboardCommandOptions
{
    /// <summary>
    /// Gets the default screenshot command options.
    /// </summary>
    public static ScreenshotClipboardCommandOptions Default { get; } = new();

    /// <summary>
    /// Gets the command identifier.
    /// </summary>
    public string CommandId { get; init; } = "Screenshot.CopyToClipboard";

    /// <summary>
    /// Gets the command label markup.
    /// </summary>
    public string LabelMarkup { get; init; } = "Screenshot";

    /// <summary>
    /// Gets the optional stable textual command name.
    /// </summary>
    public string? Name { get; init; } = "screenshot.clipboard";

    /// <summary>
    /// Gets the optional description/help markup.
    /// </summary>
    public string? DescriptionMarkup { get; init; } = "Copy the current application frame as a PNG image to the clipboard.";

    /// <summary>
    /// Gets optional additional search text for discovery surfaces.
    /// </summary>
    public string? SearchText { get; init; } = "screenshot capture copy clipboard image png";

    /// <summary>
    /// Gets the shortcut gesture used to trigger the command.
    /// </summary>
    public KeyGesture Gesture { get; init; } = new(TerminalKey.F12, TerminalModifiers.Ctrl);

    /// <summary>
    /// Gets the command importance for display ordering.
    /// </summary>
    public CommandImportance Importance { get; init; } = CommandImportance.Primary;

    /// <summary>
    /// Gets the command presentation surfaces.
    /// </summary>
    public CommandPresentation Presentation { get; init; } = CommandPresentation.CommandBar | CommandPresentation.CommandPalette;

    /// <summary>
    /// Gets a value indicating whether the gesture should be treated as handled when the command is unavailable.
    /// </summary>
    public bool ConsumesGestureWhenUnavailable { get; init; }

    /// <summary>
    /// Gets the PNG export options used to capture the screenshot.
    /// </summary>
    public CellBufferImageExportOptions ImageOptions { get; init; } = CellBufferImageExportOptions.Default;
}
