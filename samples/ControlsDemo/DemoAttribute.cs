namespace XenoAtom.Terminal.UI.ControlsDemo;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DemoAttribute : Attribute
{
    public DemoAttribute(string name, string category)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(category);

        Name = name;
        Category = category;
    }

    public string Name { get; }

    public string Category { get; }

    public string Description { get; init; } = string.Empty;

    public int Order { get; init; }
}
