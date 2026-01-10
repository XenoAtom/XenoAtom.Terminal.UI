namespace XenoAtom.Terminal.UI.ControlsDemo;

public interface IControlsDemo
{
    DemoMetadata Metadata { get; }

    XenoAtom.Terminal.UI.Visual Build(DemoContext context);
}

