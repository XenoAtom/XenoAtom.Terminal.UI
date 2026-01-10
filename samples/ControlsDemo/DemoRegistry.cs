using System.Reflection;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoRegistry
{
    public static IReadOnlyList<IControlsDemo> Load()
    {
        var demos = new List<IControlsDemo>();
        var asm = Assembly.GetExecutingAssembly();

        foreach (var type in asm.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (!typeof(IControlsDemo).IsAssignableFrom(type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is IControlsDemo demo)
            {
                demos.Add(demo);
            }
        }

        demos.Sort(static (a, b) =>
        {
            var c = string.Compare(a.Metadata.Category, b.Metadata.Category, StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;

            c = a.Metadata.Order.CompareTo(b.Metadata.Order);
            if (c != 0) return c;

            return string.Compare(a.Metadata.Name, b.Metadata.Name, StringComparison.OrdinalIgnoreCase);
        });

        return demos;
    }
}

