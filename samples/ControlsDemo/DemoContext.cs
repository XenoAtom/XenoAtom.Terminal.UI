using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;

namespace XenoAtom.Terminal.UI.ControlsDemo;

public sealed class DemoContext
{
    /// <summary>
    /// Gets a value indicating whether the demo is being rendered for deterministic screenshot export.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, demos may choose to render a richer "pre-interacted" state
    /// (e.g. open popups/dialogs, seeded log content) so screenshots are representative without
    /// requiring input.
    /// </remarks>
    public bool IsScreenshot { get; init; }

    public required Action<string> Log { get; init; }

    public required Action<string> NavigateToDemoId { get; init; }

    public required DemoRuntime Runtime { get; init; }

    public required Theme Theme { get; init; }

    public ToastHost? ToastHost { get; init; }
}
