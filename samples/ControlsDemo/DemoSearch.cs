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

        if (meta.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
