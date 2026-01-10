namespace XenoAtom.Terminal.UI.ControlsDemo;

public sealed class DemoContext
{
    public required Action<string> Log { get; init; }

    public required Action<string> NavigateToDemoId { get; init; }

    public required DemoRuntime Runtime { get; init; }
}
