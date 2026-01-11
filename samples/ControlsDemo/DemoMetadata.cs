namespace XenoAtom.Terminal.UI.ControlsDemo;

public sealed record DemoMetadata(
    string Id,
    string Name,
    string Category,
    string Description,
    string SourcePath,
    int Order = 0);
