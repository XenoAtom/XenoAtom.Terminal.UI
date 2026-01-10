namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class DemoSearch
{
    public static bool Matches(DemoMetadata meta, string query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        if (meta.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (meta.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        for (var i = 0; i < meta.Tags.Count; i++)
        {
            if (meta.Tags[i].Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

