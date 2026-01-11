using System.Reflection;
using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.ControlsDemo;

public abstract class ControlsDemoBase : IControlsDemo
{
    protected ControlsDemoBase(string sourcePath)
    {
        SourcePath = sourcePath ?? string.Empty;

        var type = GetType();
        var attr = type.GetCustomAttribute<DemoAttribute>();
        if (attr is null)
        {
            throw new InvalidOperationException($"{type.FullName} must be annotated with [Demo].");
        }

        var id = type.FullName ?? type.Name;
        Metadata = new DemoMetadata(
            Id: id,
            Name: attr.Name,
            Category: attr.Category,
            Description: attr.Description,
            SourcePath: SourcePath,
            Order: attr.Order);
    }

    protected string SourcePath { get; }

    public DemoMetadata Metadata { get; }

    public abstract Visual Build(DemoContext context);
}
