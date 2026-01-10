namespace XenoAtom.Terminal.UI.ControlsDemo;

public sealed record DemoMetadata(
    string Id,
    string Name,
    string Category,
    string Description,
    IReadOnlyList<string> Tags,
    string SourcePath,
    int Order = 0);

