using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Graphics;
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

    /// <summary>
    /// Gets or sets a value indicating whether the demo page host should wrap this demo in a page-level <see cref="ScrollViewer"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/> to avoid nested-scroll conflicts with controls that already handle scrolling.
    /// </remarks>
    public bool AllowPageScrollViewer { get; set; }

    public required Action<string> Log { get; init; }

    public required Action<string> NavigateToDemoId { get; init; }

    public required DemoRuntime Runtime { get; init; }

    public required Theme Theme { get; init; }

    public ToastHost? ToastHost { get; init; }

    public DemoGraphicsOptions? Graphics { get; init; }
}

public sealed class DemoGraphicsOptions
{
    public required TerminalImageGraphicsPresenter Presenter { get; init; }

    public required TerminalSixelEncoderOptions SixelOptions { get; init; }
}
